using System;
using UnityEngine;

namespace YAPS.ContextSystem
{
    public enum ContextCategory
    {
        General,
        Sports,
        Study,
        Fitness,
        Social,
        Work,
        Leisure
    }

    [Serializable]
    public struct ReminderData
    {
        public string text;
        public float timeRemainingMinutes;
        public ContextCategory extractedCategory;
        public string extractedKeyword;
        
        public ReminderData(string text, float timeRemainingMinutes)
        {
            this.text = text;
            this.timeRemainingMinutes = timeRemainingMinutes;
            this.extractedCategory = ContextCategory.General; // Default
            this.extractedKeyword = "";
        }
    }
}
