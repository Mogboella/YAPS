using UnityEngine;
using TMPro;

public class InkySpeechBubble : MonoBehaviour
{
    public Animator animator;
    public TMP_Text bubbleText;

    [TextArea] public string calmMessage = "Everything feels okay.";
    [TextArea] public string restlessMessage = "Something feels off.";
    [TextArea] public string urgentMessage = "Please pay attention.";
    [TextArea] public string distressedMessage = "I need help.";

    private static readonly int MoodHash = Animator.StringToHash("Mood");
    private int lastMood = -1;

    void Update()
    {
        if (animator == null || bubbleText == null) return;

        int mood = animator.GetInteger(MoodHash);
        if (mood == lastMood) return;

        UpdateBubbleText();
    }

    public void UpdateBubbleText()
    {
        int mood = animator.GetInteger(MoodHash);
        lastMood = mood;

        switch (mood)
        {
            case 0:
                bubbleText.text = calmMessage;
                break;
            case 1:
                bubbleText.text = restlessMessage;
                break;
            case 2:
                bubbleText.text = urgentMessage;
                break;
            case 3:
                bubbleText.text = distressedMessage;
                break;
        }
    }
}