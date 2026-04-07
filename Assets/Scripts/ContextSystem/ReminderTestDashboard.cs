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
        
        private Rect windowRect = new Rect(20, 20, 300, 200);

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

            windowRect = GUILayout.Window(0, windowRect, DrawWindow, "Inky Context Tester");
        }

        private void DrawWindow(int windowID)
        {
            GUILayout.Label("Reminder Text:");
            testReminderText = GUILayout.TextField(testReminderText);

            GUILayout.Label("Time Remaining (mins): " + testTimeRemainingMins.ToString("F1"));
            testTimeRemainingMins = GUILayout.HorizontalSlider(testTimeRemainingMins, 0f, 60f);

            if (GUILayout.Button("Send Reminder"))
            {
                SendTestReminder();
            }

            if (GUILayout.Button("Update Time Linearly"))
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
