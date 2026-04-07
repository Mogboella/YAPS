using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace YAPS.ContextSystem
{
    public class ContextExtractor : MonoBehaviour
    {
        [Header("Debug")]
        public bool showDebugLogs = true;

        public void ExtractContext(string reminderText, Action<ContextCategory, string> onComplete)
        {
            if (showDebugLogs) Debug.Log($"[ContextExtractor] Evaluating context for: '{reminderText}'");
            
            var result = ExtractCategoryRuleBased(reminderText);
            
            if (showDebugLogs) Debug.Log($"[ContextExtractor] Extracted Keyword: '{result.keyword}', Category: {result.cat}");
            
            onComplete?.Invoke(result.cat, result.keyword);
        }

        private (ContextCategory cat, string keyword) ExtractCategoryRuleBased(string reminderText)
        {
            string cleanText = reminderText.ToLowerInvariant();

            // Simple dictionary fallback mapping words to ContextCategories
            Dictionary<ContextCategory, List<string>> keywordMap = new Dictionary<ContextCategory, List<string>>
            {
                { ContextCategory.Sports, new List<string> { "football", "soccer", "basketball", "tennis", "rugby", "match", "game" } },
                { ContextCategory.Fitness, new List<string> { "gym", "workout", "run", "lift", "exercise", "training" } },
                { ContextCategory.Study, new List<string> { "study", "exam", "assignment", "homework", "read", "book", "lecture" } },
                { ContextCategory.Work, new List<string> { "work", "meeting", "deadline", "project", "presentation", "boss" } },
                { ContextCategory.Social, new List<string> { "party", "friends", "hangout", "dinner", "date", "meet", "cook" } },
                { ContextCategory.Leisure, new List<string> { "cinema", "movie", "gaming", "relax", "chill", "sleep", "guitar", "piano", "music" } }
            };

            foreach (var kvp in keywordMap)
            {
                foreach (string keyword in kvp.Value)
                {
                    // Use regex for whole-word matching
                    if (Regex.IsMatch(cleanText, $@"\b{keyword}\b"))
                    {
                        return (kvp.Key, keyword); // Give back the exact mapped keyword
                    }
                }
            }

            // Fallback strategy to find any long noun
            string[] words = cleanText.Split(new[] { ' ', '.', ',', '!' }, StringSplitOptions.RemoveEmptyEntries);
            string bestKeyword = "";
            foreach(string w in words) {
                if (w.Length > bestKeyword.Length && w.Length > 3)
                {
                    // Exclude common structural words
                    if(w != "with" && w != "that" && w != "this" && w != "have")
                        bestKeyword = w;
                }
            }

            return (ContextCategory.General, string.IsNullOrEmpty(bestKeyword) ? "box" : bestKeyword);
        }
    }
}
