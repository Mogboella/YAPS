using UnityEngine;

namespace YAPS.ContextSystem
{
    public class ReminderTestDashboard : MonoBehaviour
    {
        [Header("System Reference")]
        public InkyContextController contextController;

        [Header("Test Input")]
        public string testReminderText = "football match at 6 pm";
        [Range(0, 60)]
        public float testTimeRemainingMins = 45f;

        [Header("UI Settings")]
        public bool showDebugUI = true;
        
        // Smaller, less intrusive window
        private Rect windowRect = new Rect(20, 20, 220, 180);
        private GUIStyle textAreaStyle;
        private GUIStyle labelStyle;

        void Start()
        {
            if (contextController == null)
            {
                contextController = FindFirstObjectByType<InkyContextController>();
            }
        }

        [ContextMenu("Simulate Reminder (Inspector)")]
        public void SendTestReminder()
        {
            if (contextController != null)
            {
                contextController.ReceiveReminder(testReminderText, testTimeRemainingMins);
            }
            else
            {
                Debug.LogError("[TestDashboard] Missing Context Controller!");
            }
        }

        private void OnGUI()
        {
            if (!showDebugUI) return;

            if (textAreaStyle == null)
            {
                textAreaStyle = new GUIStyle(GUI.skin.textArea) { wordWrap = true, fontSize = 12 };
                labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            }

            windowRect = GUILayout.Window(0, windowRect, DrawWindow, "Inky Context Tester");
        }

        private void DrawWindow(int windowID)
        {
            GUILayout.Space(5);
            
            GUILayout.Label("Reminder Text:", labelStyle);
            // Smaller text area box
            testReminderText = GUILayout.TextArea(testReminderText, textAreaStyle, GUILayout.Height(35));

            GUILayout.Space(10);

            GUILayout.Label("Time Remaining (mins): " + testTimeRemainingMins.ToString("F1"), labelStyle);
            testTimeRemainingMins = GUILayout.HorizontalSlider(testTimeRemainingMins, 0f, 60f);

            GUILayout.Space(15);

            if (GUILayout.Button("Send Reminder", GUILayout.Height(25)))
            {
                SendTestReminder();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Update Time Linearly", GUILayout.Height(25)))
            {
                if (contextController != null)
                {
                    contextController.SetTimeRemaining(testTimeRemainingMins);
                }
            }

            GUI.DragWindow();
        }
    }
}
