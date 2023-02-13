using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NaturalLanguageProcessing.Dictionaries;
using NaturalLanguageProcessing.NGrams;
using NaturalLanguageProcessing.TextData;
using System.Collections;
using System.Xml.Linq;

namespace NGramsApplication
{
    public partial class MainForm : Form
    {
        private const string TEXT_FILTER = "Text files (*.txt)|*.txt";

        private TextDataSet writtenDataSet;
        private TextDataSet spokenDataSet;
        private NGramSet writtenUniGramSet; 
        private NGramSet writtenBiGramSet;
        private NGramSet writtenTriGramSet;
        private NGramSet spokenUniGramSet;
        private NGramSet spokenBiGramSet;
        private NGramSet spokenTriGramSet;

        private Thread tokenizationThread;
        private Thread indexingThread;
        private Thread processingThread;

        private List<string> analysisList;

        public MainForm()
        {
            InitializeComponent();
        }

        private void ImportTextData(string fileName)
        {
            writtenDataSet = new TextDataSet();
            spokenDataSet = new TextDataSet();
            using (StreamReader dataReader = new StreamReader(fileName))
            {
                while (!dataReader.EndOfStream)
                {
                    string line = dataReader.ReadLine();
                    List<string> lineSplit = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    Sentence sentence = new Sentence();
                    sentence.Text = lineSplit[1];
                    sentence.Text = sentence.Text.Replace(" , ", " ");

                    if (lineSplit[0] == "0") // Spoken sentence (Class 0)
                    {
                        spokenDataSet.SentenceList.Add(sentence);
                    }
                    else // Written sentence (Class 1)
                    {
                        writtenDataSet.SentenceList.Add(sentence);
                    }
                }
                dataReader.Close();
            }
        }

        private void importTextDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = TEXT_FILTER;
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ImportTextData(openFileDialog.FileName);
                    tokenizeButton.Enabled = true;  
                }
            }
        }

        // this methdo should display items in the analysis text box (it should be
        // called by ThreadSafeShowAnalysis().
        private void ShowAnalysis()
        {
            
            for (int i = 0; i < analysisList.Count; i++)
            {
                analysisTextBox.Text += analysisList[i] + "\r\n";
            }
             


        }
        // This method should, in turn, call ShowAnalysis() in a thread-safe manner
        private void ThreadSafeShowAnalysis()
        {
            if (InvokeRequired) { this.Invoke(new MethodInvoker(() => ShowAnalysis())); }
            else {ShowAnalysis(); }
        }

        private void ThreadSafeToggleButtonEnabled(ToolStripButton button, Boolean enabled)
        {
            if (InvokeRequired) { this.Invoke(new MethodInvoker(() => button.Enabled = enabled)); }
            else { button.Enabled = enabled; }
        }

        private void ThreadSafeToggleMenuItemEnabled(ToolStripMenuItem menuItem, Boolean enabled)
        {
            if (InvokeRequired) { this.Invoke(new MethodInvoker(() => menuItem.Enabled = enabled)); }
            else { menuItem.Enabled = enabled; }
        }

        private void TokenizationLoop()
        {
            writtenDataSet.Tokenize();
            spokenDataSet.Tokenize();
            ThreadSafeToggleButtonEnabled(makeDictionaryAndIndexButton, true);
        }

        private void tokenizeButton_Click(object sender, EventArgs e)
        {
            tokenizeButton.Enabled = false;
            tokenizationThread = new Thread(new ThreadStart(() => TokenizationLoop()));
            tokenizationThread.Start();
        }

        private void IndexingLoop()
        {
            writtenDataSet.MakeDictionaryAndIndex();
            spokenDataSet.MakeDictionaryAndIndex();
            ThreadSafeToggleButtonEnabled(processButton, true);
        }

        private void makeDictionaryAndIndexButton_Click(object sender, EventArgs e)
        {
            makeDictionaryAndIndexButton.Enabled = false;
            indexingThread = new Thread(new ThreadStart(() => IndexingLoop()));
            indexingThread.Start();
        }


        // ========================================================
        // Here, write methods for
        // ========================================================
        //
        // * Finding the 300 most common 1-grams, 2-grams, 3-grams in each set
        //     For the unigrams (1-grams), use the dictionary that you generated above.
        //     You must generate the sets of bigrams (2-grams) and trigrams (3-grams), using
        //     the NGramSet and its methods (see NGramSet.Append(...) ...)
        // * Finding the number of distinct tokens (unigrams) in the spoken set (i.e. the
        //   number of items in its dictionary) and the written set, as well as the number of shared (distinct) tokens.
        // * Finding the 50 tokens with the largest values of r (see the problem formulation)
        //     and the 50 tokens with the smallest values of r.
        // 
        private void ProcessingLoop()
        {
            const int COUNT_CUTOFF = 5;
            const int NUMBER_OF_N_GRAMS_SHOWN = 300;

            // Add your analysis as a list of strings (that can then be shown on screen and saved to file).
            analysisList = new List<string>();

            // Step (1)
            // Find the 300 most common unigrams (from the dictionaries, after sorting
            // the dictionary items based on the number of instances:
            Dictionary writtenUniGramDictionary = writtenDataSet.Dictionary;
            writtenUniGramSet = new NGramSet();
            writtenBiGramSet = new NGramSet();
            writtenTriGramSet = new NGramSet();

            Dictionary spokenUniGramDictionary = spokenDataSet.Dictionary;
            spokenUniGramSet = new NGramSet();
            spokenBiGramSet = new NGramSet();
            spokenTriGramSet = new NGramSet();


            //Find shared tokens using nested for-loop and compute the ratios
            //I added Ratio as a field to the DictionaryItem class for this.
            Dictionary sharedTokens = new Dictionary();
            List<double> ratioList = new List<double>();

            double writtenSpokenRatio = 0;
            for (int j = 0; j < spokenUniGramDictionary.ItemList.Count; j++) 
            {  for (int i = 0; i < writtenUniGramDictionary.ItemList.Count; i++)
                 {
                     if (spokenUniGramDictionary.ItemList[j].Token == writtenUniGramDictionary.ItemList[i].Token)
                     {
                        writtenSpokenRatio = (double)writtenUniGramDictionary.ItemList[i].Count /
                            (double)spokenUniGramDictionary.ItemList[j].Count;

                        DictionaryItem dictionaryItem = new DictionaryItem(spokenUniGramDictionary.ItemList[j].Token);
                        dictionaryItem.Ratio = writtenSpokenRatio;
                        sharedTokens.ItemList.Add(dictionaryItem);
                        break;
                    }
                }
            }

          
            sharedTokens.bubbleSortRatio();
            Dictionary smallestRatioValues = new Dictionary();
            Dictionary largestRatioValues = new Dictionary();

            int sharedTokensLength = sharedTokens.ItemList.Count-1;
            //take the 50 elements at the start, and then at the end and append:
            for (int i = 0; i < 50; i++)
            {
                DictionaryItem smallest = new DictionaryItem(sharedTokens.ItemList[i].Token);
                DictionaryItem largest = new DictionaryItem(sharedTokens.ItemList[sharedTokensLength-i].Token);

                smallest.Ratio = sharedTokens.ItemList[i].Ratio;
                largest.Ratio = sharedTokens.ItemList[sharedTokensLength - i].Ratio;


                smallestRatioValues.ItemList.Add(smallest);
                largestRatioValues.ItemList.Add(largest);
            }





            // Add code here for generating and sorting the written bigram set.
            // Before sorting, run through the list and remove rare bigrams (speeds up
            // the sorting - we are only interested in the most frequent bigrams anyway;
            // see also the assignment text.




            //find unigrams
            writtenUniGramSet.ItemList = writtenUniGramDictionary.get300MostFrequent();
            spokenUniGramSet.ItemList = spokenUniGramDictionary.get300MostFrequent();

            analysisList.Add("=========================================");
            analysisList.Add("Written 1-grams: ");
            analysisList.Add("=========================================");
            for (int ii = 0; ii < NUMBER_OF_N_GRAMS_SHOWN; ii++)
            {
                analysisList.Add(writtenUniGramSet.ItemList[ii].AsString());
            }
            analysisList.Add("=========================================");
            analysisList.Add("Spoken 1-grams: ");
            analysisList.Add("=========================================");
            for (int ii = 0; ii < NUMBER_OF_N_GRAMS_SHOWN; ii++)
            {
                analysisList.Add(spokenUniGramSet.ItemList[ii].AsString());
            }



            Dictionary writtenBiGramDictionary = writtenDataSet.Dictionary;
            writtenBiGramSet.ItemList = writtenBiGramDictionary.generateBigramsList(writtenDataSet);


            analysisList.Add("=========================================");
            analysisList.Add("Written 2-grams: ");
            analysisList.Add("=========================================");
            for (int ii = 0; ii < NUMBER_OF_N_GRAMS_SHOWN; ii++)
            {
                analysisList.Add(writtenBiGramSet.ItemList[ii].AsString());
            }



            // Add code here for generating and sorting the spoken bigram set.
            // Before sorting, run through the list and remove rare bigrams (speeds up
            // the sorting - we are only interested in the most frequent bigrams anyway;
            // see also the assignment text.



            Dictionary spokenBiGramDictionary = spokenDataSet.Dictionary;
            spokenBiGramSet.ItemList = spokenBiGramDictionary.generateBigramsList(spokenDataSet);


            analysisList.Add("=========================================");
            analysisList.Add("Spoken 2-grams: ");
            analysisList.Add("=========================================");
            for (int ii = 0; ii < NUMBER_OF_N_GRAMS_SHOWN; ii++)
            {
                analysisList.Add(spokenBiGramSet.ItemList[ii].AsString());
            }


            // Step (3) 
            //     Find the 300 most common trigrams (after generating the trigram set.
            //     using the NGramSet class (where you have to write code for appending
            //     n-grams, making sure to keep them sorted in alphabetical order based
            //     on the full token string (see the NGramSet class)

            // Add code here for generating and sorting the written trigrams set.
            // Before sorting, run through the list and remove rare trigrams (speeds up
            // the sorting - we are only interested in the most frequent trigram anyway;
            // see also the assignment text.




            Dictionary writtenTriGramDictionary = writtenDataSet.Dictionary;
            writtenTriGramSet.ItemList = writtenTriGramDictionary.generateTrigramsList(writtenDataSet);

            analysisList.Add("=========================================");
            analysisList.Add("Written 3-grams: ");
            analysisList.Add("=========================================");
            for (int ii = 0; ii < NUMBER_OF_N_GRAMS_SHOWN; ii++)
            {
                analysisList.Add(writtenTriGramSet.ItemList[ii].AsString());
            }


            // Add code here for generating and sorting the spoken trigram set.
            // Before sorting, run through the list and remove rare trigrams (speeds up
            // the sorting - we are only interested in the most frequent trigrams anyway;
            // see also the assignment text.


            Dictionary spokenTriGramDictionary = spokenDataSet.Dictionary;
            spokenTriGramSet.ItemList = spokenTriGramDictionary.generateTrigramsList(spokenDataSet);

            analysisList.Add("=========================================");
            analysisList.Add("Spoken 3-grams: ");
            analysisList.Add("=========================================");
            for (int ii = 0; ii < NUMBER_OF_N_GRAMS_SHOWN; ii++)
            {
                analysisList.Add(spokenTriGramSet.ItemList[ii].AsString());
            }


            // Step (4) Using the dictionaries (one for the written set and one for the spoken),
            // Find the 50 tokens with the largest values of r and the 50 tokens with the
            // smallest values of r. Consider only tokens that are present (with at least
            // one instance) in both the written and the spoken data sets.
            //
            // Here, you can run through one of the dictionaries (e.g. the spoken one, which
            // is probably smaller) token by token, then check if the corresponding token
            // can be found in the other dictionary. If yes, then compute the ratio r,
            // and store the information. To that end, you can define new classes, 
            // similar to the NGramSet and NGram classes, but with items
            // consisting of (i) the token and (ii) the value of r. Then sort on r,
            // and get the top 50 elements as well as the 50 bottom elements ...
            // You'll need a custom comparer too which uses the 
            // value of r as the basis for comparison.
            //
            // There are other ways - this is just a suggestion... You can, for example,
            // use the "Tuple" concept in C#, defining a tuple (string, double) with
            // tokens and r-values, then make a list of such tuples and sort it (based on r, i.e. "Item2";
            // see the definition of the Tuple concept e.g. on MSDN.
            // In that case, use: List<Tuple<string, double>> tokenRTupleList = new List<Tuple<string, double>>();

            //
            // Add code here:
            //
            
            
            // Then store information about the number of tokens in each set, as well as the number of shared tokens:
            analysisList.Add("=========================================");
            analysisList.Add("Number of tokens:");
            analysisList.Add("=========================================");
            analysisList.Add("Written set: " + writtenDataSet.Dictionary.ItemList.Count.ToString());
            analysisList.Add("Spoken set:  " + spokenDataSet.Dictionary.ItemList.Count.ToString());
            analysisList.Add("No. of distinct unigrams in written set: " + writtenUniGramDictionary.ItemList.Count.ToString());
            analysisList.Add("No. of distinct unigrams in spoken set: " + spokenUniGramDictionary.ItemList.Count.ToString());
            analysisList.Add("=========================================");

            //    analysisList.Add("Shared:      " ... Add code here ...)
            analysisList.Add("Shared Tokens: " + sharedTokens.ItemList.Count.ToString());

            // Then store information about the 50 tokens with the highest r-values:
            analysisList.Add("=========================================");
            analysisList.Add("High-r tokens: ");
            analysisList.Add("=========================================");
            for (int i = 0; i < largestRatioValues.ItemList.Count; i++)
            {
                analysisList.Add("r = "+ largestRatioValues.ItemList[i].Ratio + "  | " + largestRatioValues.ItemList[i].Token.ToString());
            }
            // Add code here

            // Then store information about the 50 tokens with the lowest r-values:
            analysisList.Add("=========================================");
            analysisList.Add("Small-r tokens: ");
            analysisList.Add("=========================================");
            // Add code here
            for (int i = 0; i < smallestRatioValues.ItemList.Count; i++)
            {
                analysisList.Add("r = " + smallestRatioValues.ItemList[i].Ratio + " | " + smallestRatioValues.ItemList[i].Token.ToString());
            }

            // =================================================================
            // (5) Write a thread-safe method here for
            // display the analysis in the analysisTextBox
            // see also ThreadSafeToggleMenuItemEnabled(), to see how it's done. 
            // Hint: The ThreadSafeMethod (that uses "Invoke..." can itself
            // call a standard (non-thread safe) method, in case one needs to
            // carry out more operations than just a single assignment...)
            // See also Appendix A.4 in the compendium.
            // ==================================================================



            ThreadSafeShowAnalysis();

            // =============================
            // Don't change below this line:
            // =============================
            ThreadSafeToggleMenuItemEnabled(saveAnalysisToolStripMenuItem, true);
        }

        private void processButton_Click(object sender, EventArgs e)
        {
            processButton.Enabled = false;
            processingThread = new Thread(new ThreadStart(() => ProcessingLoop()));
            processingThread.Start();
        }

        // Here, the results shown in the analysisTextBox are shown. One can also
        // (equivalently) just save the contents of the analysisList (which is indeed
        // what is shown in the textBox...)
        private void saveAnalysisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = TEXT_FILTER;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    using (StreamWriter dataWriter = new StreamWriter(saveFileDialog.FileName))
                    {
                        for (int ii = 0; ii < analysisList.Count(); ii++)
                        {
                            dataWriter.WriteLine(analysisList[ii]);
                        }
                        dataWriter.Close();
                    }
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

    }
}
