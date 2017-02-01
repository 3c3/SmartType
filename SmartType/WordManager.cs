using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartType
{
    public class WordManager
    {
        public delegate void SuggestionsChangedHandler(List<Word> suggestions, int idx);

        public event SuggestionsChangedHandler SuggestionsChanged;

        static string enWordsFilename = "enWords.txt";
        static string bgWordsFilename = "bgWords.txt";
        WordList enWords, bgWords;
        StringBuilder current = new StringBuilder();

        List<Word> suggestions;
        int selectedIdx;

        public WordManager()
        {
            KeyboardHook.WordActionHappened += KeyboardHook_WordActionHappened;
            KeyboardHook.CharacterAdded += KeyboardHook_CharacterAdded;
            KeyboardHook.CharacterRemoved += KeyboardHook_CharacterRemoved;
            KeyboardHook.AutoCompleteRequested += KeyboardHook_AutoCompleteRequested;
            KeyboardHook.ArrowKeysPressed += KeyboardHook_ArrowKeysPressed;

            enWords = new WordList(enWordsFilename);
            bgWords = new WordList(bgWordsFilename);
        }

        

        private bool KeyboardHook_ArrowKeysPressed(bool up)
        {
            if (suggestions == null) return false;
            if (suggestions.Count == 0) return false;

            if (up)
            {
                selectedIdx--;
                if (selectedIdx < 0) selectedIdx = 0;
            }
            else
            {                
                selectedIdx++;
                if (selectedIdx >= suggestions.Count) selectedIdx = suggestions.Count - 1;
            }

            SuggestionsChanged?.Invoke(suggestions, selectedIdx);

            return true;
        }

        public void Save()
        {
            enWords.SaveToFile(enWordsFilename);
            bgWords.SaveToFile(bgWordsFilename);
        }

        private bool KeyboardHook_AutoCompleteRequested()
        {
            Console.WriteLine("autocomplete request");

            if(suggestions != null && suggestions.Count > 0)
            {
                string reqWord = suggestions[selectedIdx].word;
                KeyInjector.Send(reqWord, current.Length);
                for (int i = current.Length - 1; i < reqWord.Length; i++) current.Append(reqWord[i]);
                return true;
            }

            return false;
        }

        private void KeyboardHook_CharacterAdded(char c)
        {
            current.Append(c);
            UpdateSuggestions();
        }

        private void KeyboardHook_CharacterRemoved()
        {
            if (current.Length == 0) return;
            current.Remove(current.Length - 1, 1);
            UpdateSuggestions();
        }

        private void KeyboardHook_WordActionHappened(KeyboardHook.WordAction action)
        {
            if (action == KeyboardHook.WordAction.WordCompleted)
            {
                if (current.Length == 0) return;

                string word = current.ToString();
                current.Clear();

                if (KeyboardHook.Language == 1026) bgWords.UpdateWord(word);
                else if (KeyboardHook.Language == 1033) enWords.UpdateWord(word);
            }
            else if(action == KeyboardHook.WordAction.WordTerminated) current.Clear();

            suggestions = null;
            selectedIdx = 0;
            SuggestionsChanged?.Invoke(suggestions, selectedIdx);
        }

        private void UpdateSuggestions()
        {
            if (current.Length == 0)
            {
                SuggestionsChanged?.Invoke(null, 0);
                return;
            }
            string prefix = current.ToString();
            
            if (KeyboardHook.Language == 1026) suggestions = bgWords.GetSuggestions(prefix);
            else if (KeyboardHook.Language == 1033) suggestions = enWords.GetSuggestions(prefix);
            selectedIdx = 0;

            SuggestionsChanged?.Invoke(suggestions, selectedIdx);
        }
    }
}
