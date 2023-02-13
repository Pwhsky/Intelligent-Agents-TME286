using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaturalLanguageProcessing.Dictionaries
{
    public class DictionaryItem
    {
        private string token;
        private int count;
        private double ratio;
        public DictionaryItem(string token)
        {
            this.token = token;
        }

        public string Token
        {
            get { return token; }
            set { token = value; }
        }

        public int Count
        {
            get { return count; }
            set { count = value; }
        }
        public double Ratio
        {
            get { return ratio; }
            set { ratio = value; }
        }
    }
}
