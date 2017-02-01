using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartType
{
    public class Word : IComparable
    {
        public readonly string word;
        public int count;

        public Word(string str)
        {
            word = str;
            count = 1;
        }

        public Word(string str, int count)
        {
            word = str;
            this.count = count;
        }

        public int CompareTo(object obj)
        {
            Word otherWord = obj as Word;
            return word.CompareTo(otherWord.word);
        }
    }
}
