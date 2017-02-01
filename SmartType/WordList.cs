using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SmartType
{
    public class WordList
    {
        List<Word> words = new List<Word>();

        public WordList()
        {

        }

        public WordList(string filename)
        {
            LoadFromFile(filename);
        }

        public void UpdateWord(string strWord)
        {
            if (strWord.Length < 3) return;
            Word testWord = new Word(strWord);
            int idx = words.BinarySearch(testWord);

            if (idx >= 0) words[idx].count++;
            else words.Insert(~idx, testWord);
        }

        public void UpdateWord(string strWord, int cnt)
        {
            if (strWord.Length < 3) return;
            Word testWord = new Word(strWord, cnt);
            int idx = words.BinarySearch(testWord);

            if (idx >= 0) words[idx].count+= cnt;
            else words.Insert(~idx, testWord);
        }

        public List<Word> GetSuggestions(string prefix)
        {
            List<Word> result = new List<Word>();

            int idx = words.BinarySearch(new Word(prefix));
            if (idx < 0) idx = ~idx;
            while(idx < words.Count)
            {
                if (!words[idx].word.StartsWith(prefix)) break;
                result.Add(words[idx]);
                idx++;
            }

            result.Sort(new CountComparer());

            return result;
        }

        public void LoadFromFile(string filename)
        {
            String[] lines;
            try
            {
                lines = File.ReadAllLines(filename);
            }
            catch(Exception e)
            {
                Console.WriteLine("Couldn't read input file {0}", filename);
                return;
            }

            foreach(string line in lines)
            {
                String[] parts = line.Split(' ');
                if (parts.Length != 2) continue;

                string strWord = parts[0];
                int count;
                if (int.TryParse(parts[1], out count)) words.Add(new Word(strWord, count));
            }

            words.Sort();
        }

        public void SaveToFile(string filename)
        {
            StringBuilder sb = new StringBuilder();
            foreach(Word word in words)
            {
                sb.Append(word.word);
                sb.Append(' ');
                sb.AppendLine(word.count.ToString());
            }

            File.WriteAllText(filename, sb.ToString());
        }

        class CountComparer : IComparer<Word>
        {
            public int Compare(Word x, Word y)
            {
                return y.count.CompareTo(x.count);
            }
        }
    }
}
