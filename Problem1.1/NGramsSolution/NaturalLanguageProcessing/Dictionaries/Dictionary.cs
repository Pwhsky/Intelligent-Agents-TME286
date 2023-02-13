using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using NaturalLanguageProcessing.TextData;

namespace NaturalLanguageProcessing.Dictionaries
{
    public class Dictionary
    {
        private List<DictionaryItem> itemList;

        public Dictionary()
        {
            itemList = new List<DictionaryItem>();  
        }

        // Method 1: Make a list of all tokens (i.e. a list of strings) in all sentences,
        // then sort the list. Next, build the list of dictionary items, counting the
        // number of instances in the process.
        //
        // Method 2: Here, you will need a DictionaryItemComparer that compares (in alphabetical order)
        // the tokens of two DictionaryItems. You will use the dictionary item comparer to carry out
        // a binary search, to find the index (in the growing dictionary) of the item
        // corresponding to the token under consideration. If that index is negative,
        // the token is not yet represented in the dictionary: If so, insert it AT THE
        // RIGHT point in the dictionary (to maintain the sort order). Try to figure it
        // out, otherwise ask the examiner or the assistant.
        // If instead the index is non-negative, then increment the count of the item.
        //
        // The running time of the two methods is roughly the same: On a reasonably
        // fast computer, it takes around one minute (in debug mode, much faster in
        // release mode) for a data set of the size considered here.
        public void Build(TextDataSet dataSet)
        {
            List<string> sortedList = new List<string>();

            foreach (Sentence sentence in dataSet.SentenceList) 
            {
                foreach (string token in sentence.TokenList)
                {
                  sortedList.Add(token);
                }
            }


            for (int i = 0;i < sortedList.Count(); i++)
            {
                bool found = false;
                for (int j = 0; j < itemList.Count; j++)
                {
                    if (sortedList[i] == itemList[j].Token)
                    {
                        found = true;
                        itemList[j].Count++;
                        break;
                    }
                }
                if (found == false)
                {
                    DictionaryItem D = new DictionaryItem(sortedList[i]);
                    itemList.Add(D);
                    sortedList.Add(D.Token);
                    D.Count = 1;
                }
            }
            bubbleSort();
        }



        public List<NGrams.NGram> get300MostFrequent()
        {
            trimDictionary(5);
            List<DictionaryItem> returnList = new List<DictionaryItem>();
            int LowestCount = 0;
            int HighestCount = 0;

            foreach (DictionaryItem token in itemList)
            {
                int placement = AddToList(token.Count, returnList, LowestCount, HighestCount);
                //set thing for if over 300 items
                if (placement != -1)
                {
                    returnList.Insert(placement, token);
                    if (placement == 0)
                    {
                        HighestCount = token.Count;
                    }
                }
                else if (placement == -1 && returnList.Count < 300)
                {
                    returnList.Add(token);
                    LowestCount = token.Count;
                }
                if (returnList.Count > 300)
                {
                    returnList.RemoveAt(300);
                }
            }

            List<NGrams.NGram> NgramList = new List<NGrams.NGram>();

            for (int i = 0; i < 300; i++)
            {
                List<string> tokenlist = new List<string>();
                tokenlist.Add(returnList[i].Token);
                NgramList.Add(new NGrams.NGram(tokenlist));
                NgramList[i].NumberOfInstances = returnList[i].Count;
            }

            return NgramList;

        }

      
        public List<NGrams.NGram> generateBigramsList(TextDataSet dataSet)
        {
            Dictionary outputDictionary = new Dictionary();
            List<string> bigramList = new List<string>();
            
            //dataSet.SentenceList
            foreach (Sentence sentence in dataSet.SentenceList)
            { 
                for (int i = 0; i < sentence.TokenList.Count - 1; i++)
                {
                    bigramList.Add(sentence.TokenList[i] + " " +  sentence.TokenList[i + 1]);
                }
            }

            for (int i = 0; i < bigramList.Count(); i++)
            {
                bool found = false;
                for (int j = 0; j < outputDictionary.itemList.Count; j++)
                {
                    if (bigramList[i] == outputDictionary.itemList[j].Token)
                    {
                        found = true;
                        outputDictionary.itemList[j].Count++;
                        break;
                    }
                }
                if (found == false)
                {
                    DictionaryItem D = new DictionaryItem(bigramList[i]);
                    D.Count = 1;    
                    outputDictionary.ItemList.Add(D);
                }
            }
            
            outputDictionary.trimDictionary(5);

            outputDictionary.SortDictionary(0, outputDictionary.ItemList.Count - 1);

            List<NGrams.NGram> NgramList = new List<NGrams.NGram>();
            
            for (int i = 1; i < 301; i++)
            {
                string[] split = outputDictionary.itemList[outputDictionary.itemList.Count - i].Token.Split(' ');
                List<string> tokenlist = new List<string>();
                for (int j = 0; j < 2; j++)
                {
                    tokenlist.Add(split[j]);

                }
                NgramList.Add(new NGrams.NGram(tokenlist));
                NgramList[i - 1].NumberOfInstances = outputDictionary.ItemList[outputDictionary.itemList.Count - i].Count;
            }
            

            //To do: Count the occurences of the bigrams and append to the dictionary.
            return NgramList;
        }

        public List<NGrams.NGram> generateTrigramsList(TextDataSet dataSet)
        {
            Dictionary outputDictionary = new Dictionary();
            List<string> bigramList = new List<string>();

            //dataSet.SentenceList
            foreach (Sentence sentence in dataSet.SentenceList)
            {
                for (int i = 0; i < sentence.TokenList.Count - 2; i++)
                {
                    bigramList.Add(sentence.TokenList[i] + " " + sentence.TokenList[i + 1] + 
                        " "+ sentence.TokenList[i + 2]);
                }
            }

            for (int i = 0; i < bigramList.Count(); i++)
            {
                bool found = false;
                for (int j = 0; j < outputDictionary.itemList.Count; j++)
                {
                    if (bigramList[i] == outputDictionary.itemList[j].Token)
                    {
                        found = true;
                        outputDictionary.itemList[j].Count++;
                        break;
                    }
                }
                if (found == false)
                {
                    DictionaryItem D = new DictionaryItem(bigramList[i]);
                    D.Count = 1;
                    outputDictionary.itemList.Add(D);
                }
            }
            outputDictionary.trimDictionary(5);
            outputDictionary.SortDictionary(0, outputDictionary.ItemList.Count - 1);

            List<NGrams.NGram> NgramList = new List<NGrams.NGram>();

            for (int i = 1; i < 301; i++)
            {
                string[] split = outputDictionary.itemList[outputDictionary.itemList.Count - i].Token.Split(' ');
                List<string> tokenlist = new List<string>();
                for (int j = 0; j < 3; j++)
                {
                    tokenlist.Add(split[j]);

                }
                NgramList.Add(new NGrams.NGram(tokenlist));
                NgramList[i-1].NumberOfInstances = outputDictionary.ItemList[outputDictionary.itemList.Count - i].Count;
            }
            return NgramList;
        }

        public void SortDictionary(int leftIndex, int rightIndex)
        {
            var i = leftIndex;
            var j = rightIndex;
            DictionaryItem pivot = itemList[leftIndex];
            while (i <= j)
            {
                while (itemList[i].Count < pivot.Count)
                {
                    i++;
                }

                while (itemList[j].Count > pivot.Count)
                {
                    j--;
                }
                if (i <= j)
                {
                    DictionaryItem temp = itemList[i];
                    itemList[i] = itemList[j];
                    itemList[j] = temp;
                    i++;
                    j--;
                }
            }

            if (leftIndex < j)
                SortDictionary(leftIndex, j);
            if (i < rightIndex)
                SortDictionary(i, rightIndex);
        }


        public int binarySearch(string word)
        {
            int index = ItemList.Count();
            index = index / 2;
            int relativePosition = word.CompareTo(ItemList[index].Token);
            if (relativePosition < 0)
            {
                index = index / 2;
                relativePosition = word.CompareTo(ItemList[index].Token);
                if (relativePosition < 0)
                {
                    return 0;
                }
                else
                {
                    return index;
                }
            }
            else
            {
                int temp = index;
                index += index / 2;
                relativePosition = word.CompareTo(ItemList[index].Token);
                if (relativePosition < 0)
                {
                    return temp;
                }
                else
                {
                    return index;
                }
            }
        }

        private int AddToList(int tokenCount, List<DictionaryItem> list, int lowest, int highest)
        {
            if (tokenCount > highest)
            {
                return 0;
            }
            if (tokenCount < lowest)
            {
                return -1;
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (tokenCount < list[i].Count)
                {
                    return i + 1;
                }
            }

            return -1;
        }

        public void bubbleSort()
        {
            var n = itemList.Count;
            for (int i = 0; i < n - 1; i++)
                for (int j = 0; j < n - i - 1; j++)
                    if (string.Compare(itemList[j].Token, itemList[j + 1].Token) > 0)
                    {
                        DictionaryItem tempVar = itemList[j];
                        itemList[j] = itemList[j + 1];
                        itemList[j + 1] = tempVar;
                    }

            
        }

        public void bubbleSortRatio()
        {
            int n = itemList.Count;

            for (int i = 0; i < n - 1; i++)
                for (int j = 0; j < n - i - 1; j++)
                    if (itemList[j].Ratio > itemList[j + 1].Ratio)
                    {
                        DictionaryItem tempVar = itemList[j];
                        itemList[j] = itemList[j + 1];
                        itemList[j + 1] = tempVar;
                    }


        }


        public void trimDictionary(int lowerLimit)
        {
            int i = 0;
            while (i < itemList.Count)
            {
                if (itemList[i].Count < lowerLimit)
                {
                    itemList.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }
        public List<DictionaryItem> ItemList
        {
            get { return itemList; }
            set { itemList = value; }
        }
    }
}
