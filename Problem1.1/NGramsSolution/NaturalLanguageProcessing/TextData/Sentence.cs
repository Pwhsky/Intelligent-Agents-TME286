using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaturalLanguageProcessing.TextData
{
    public class Sentence
    {
        private string text;
        private List<string> tokenList;
        private List<int> tokenIndexList;
        public Sentence()
        {
            tokenList = new List<string>();
            tokenIndexList = new List<int>();   
        }

        // Write this method:
        //
        // First, make the text lower-case (ToLower()...)
        // Remember to handle (remove) end-of-sentence markers, as well as "," and
        // quotation marks (if any). Also, make sure *not* to split abbreviations and contractions.
        //
        // Spend some effort on this method: It should be more than just a few lines - there are
        // many special cases to deal with!
        public void Tokenize()
        {
            string wordToken;

            text = text.ToLower();

            //remove various symbols and characters, including periods.
            text = text.Replace(",", "").Replace("!", "").Replace("''", "").Replace("(","").
            Replace(")", "").Replace(":", "").Replace("  ", " ").Replace("?", "").Replace(" ?", "").Replace(".", "");

            //Append words to tokenList & add periods to titles if they appear
            string[] splitSentence = text.Split(' ');
            foreach (string word in splitSentence)
            {
                if (word == "mr" || word == "dr" || word == "mrs" || word == "prof" || word == "st")
                {
                    wordToken = word + ".";
                }
                else
                {
                    wordToken = word;
                }
                //Ugly solution for the rare empty string occurence.
                if (wordToken == "")
                {
                    continue;
                }
                tokenList.Add(wordToken);
            }


          
        }
        

        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        public List<string> TokenList
        {
            get { return tokenList; }
            set { tokenList = value; }
        }

        public List<int> TokenIndexList
        {
            get { return tokenIndexList; }
            set { tokenIndexList = value; }
        }
    }
}
