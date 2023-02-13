using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;

namespace NaiveBayesApplication
{
    public class BayesianDocumentClassifier
    {
        private List<double> priorProbabilitiesList;
        private List<ConditionalWordProbability> conditionalWordProbabilityList;

        public BayesianDocumentClassifier()
        {
            priorProbabilitiesList = new List<double>();
            conditionalWordProbabilityList = new List<ConditionalWordProbability>();
        }

        // To do: Write this method
        // The method takes a document as input, then computes
        // the log probability as in Eq. (4.26) (in the compendium).
        //
        // Among other things, this involves summing (for each class label)
        // over the tokens in the document, ignoring any
        // token for which a conditional word probability is not available
        // (i.e. for those (rare) words that did not appear anywhere in the training set)
        //
        // Then infer (and return) the class label; again see Eq. (4.26).
        //
        // (Note (final edit!), the returned int label is not really used here. You must
        // also assign the inferred label to the document (done below, row 42). The
        // returned label WOULD be used if you extend your code to classify a new (single)
        // sentence ...
        public void ComputePriorProbabilities(List<Document> documentList) {
            double nPositiveDocuments = 0, nNegativeDocuments = 0;
            foreach (Document document in documentList)
            {
                if (document.Label == 0)
                {
                    nNegativeDocuments += 1;
                } else
                {
                    nPositiveDocuments += 1;
                }
            }
            double nTotal = nPositiveDocuments + nNegativeDocuments;
            priorProbabilitiesList.Add(nNegativeDocuments / nTotal);
            priorProbabilitiesList.Add(nPositiveDocuments / nTotal);
        }
   
        public int Classify(Document document)
        {
            int numberOfClasses = priorProbabilitiesList.Count;
            int inferredClass = -1; // The inferred class label, either 0 or 1, should be assigned below
            List<double> logSum = new List<double>();

            //Logsum will represent the contents of the large parenthesis in equation 4.26 in the compendium
            //First, add prior probabilities: 
            logSum.Add(Math.Log(priorProbabilitiesList[0]));
            logSum.Add(Math.Log(priorProbabilitiesList[1]));


            //This loop will sum the conditional log probabilities for each word in the given document
            //Then add that to the logSum variables for a given class.
            for (int i = 0; i < numberOfClasses; i++)
            {
                double conditionalLogSum = 0;
                foreach (string token in document.TokenList)
                {
                    foreach(ConditionalWordProbability cwp in conditionalWordProbabilityList)
                    {
                        if (cwp.Word == token)
                        {
                        conditionalLogSum += Math.Log(cwp.ConditionalProbabilityList[i]);
                        }
                    }
                }
                logSum[i] += conditionalLogSum;
            }
            //Out of the two log sums, the largest one will represent the inferred label.
            if (logSum[1] > logSum[0])
            {
                inferredClass = 1;
            }
            if (logSum[0] > logSum[1])
            {
                inferredClass = 0;
            }

            document.InferredLabel = inferredClass; // Assign the inferred label here - needed later on, in the MainForm.
            return inferredClass;
        }

        // To do: Write this method.
        public void ComputeConditionalProbabilities(List<Document> documentList)
        {
            // Step 1: Find the list of all distinct words in the documents
            //         i.e. loop over the words, add the words to the wordList (defined just below)
            //         then reduce the list (Hint: Apply the Distinct() method)
            //         to make it a list of distinct words.
            List<string> wordList = new List<string>();

            foreach (Document document in documentList)
            {
                foreach (string token in document.TokenList)
                {
                    wordList.Add(token);
                }
            }
            wordList = wordList.Distinct().ToList();

            // Step 2: Define conditional probabilities (just the words for now)
            // This step is complete, you get it for free. :)

            conditionalWordProbabilityList = new List<ConditionalWordProbability>();
            foreach (string word in wordList)
            {
                ConditionalWordProbability cwp = new ConditionalWordProbability();
                cwp.Word = word;
                conditionalWordProbabilityList.Add(cwp);
            }

            // Step 3: Generate merged document
            // 
            int numberOfClasses = priorProbabilitiesList.Count;  // NOTE: See the MainForm, where the prior probabilities are defined.
            List<Document> mergedClassDocumentList = new List<Document>();

            Document positiveTokens = new Document();
            Document negativeTokens = new Document();
            foreach (Document document in documentList)
            {
                if (document.Label == 1)
                {
                    foreach (string token in document.TokenList)
                    {
                        positiveTokens.TokenList.Add(token);
                    }
                }
                if (document.Label == 0)
                {
                    foreach (string token in document.TokenList)
                    {
                        negativeTokens.TokenList.Add(token);
                    }
                }
            }
            negativeTokens.Label = 0;
            positiveTokens.Label = 1;
            //Two documents which contains all tokens for their class:
            mergedClassDocumentList.Add(negativeTokens);
            mergedClassDocumentList.Add(positiveTokens);


            // ... add code here: The mergedClassDocumentList should contain two
            //                    merged documents, one for each class (label). [Done]

            // Now compute the conditional word probabilities:
            // NOTE: Use add-1 (Laplace) smoothing here! Very important!

            // ... add code here: For each distinct token (word), run through
            //                    the merged documents (one per class), and
            //                    compute P(w_i|c_j), and store the values
            //                    in the conditionalWordProbabilityList.
            //                    See also the description in the ConditionalWordProbability class.


            double countOccurencesInNegative, countOccurencesInPositive;

            foreach (ConditionalWordProbability cwp in conditionalWordProbabilityList)
            {
                List<double> probabilityVector = new List<double>(); //Will store the two conditional probabilities
                probabilityVector.Add(0);
                probabilityVector.Add(0);

                countOccurencesInNegative =
                        mergedClassDocumentList[0].TokenList.Count(t => t == cwp.Word);

                 countOccurencesInPositive =
                        mergedClassDocumentList[1].TokenList.Count(t => t == cwp.Word);

                  probabilityVector[0] = ((countOccurencesInNegative + 1)/(mergedClassDocumentList[0].TokenList.Count + 1));
                  probabilityVector[1] = ((countOccurencesInPositive + 1)/(mergedClassDocumentList[1].TokenList.Count + 1));
                  cwp.ConditionalProbabilityList = probabilityVector;
            }
        }



        public List<double> PriorProbabilitiesList
        {
            get { return priorProbabilitiesList; }
            set { priorProbabilitiesList = value; }
        }

        public List<ConditionalWordProbability> ConditionalWordProbabilityList
        {
            get { return conditionalWordProbabilityList; }
            set { conditionalWordProbabilityList = value; }
        }
    }
}
