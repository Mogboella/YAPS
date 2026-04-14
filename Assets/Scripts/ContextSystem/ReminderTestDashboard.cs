using UnityEngine;
using System.Collections.Generic;
using System.Linq;
namespace YAPS.ContextSystem
{
    public class ReminderTestDashboard : MonoBehaviour
    {
        [System.Serializable]
        public struct ReminderTask
        {
            public string reminderTask;
            [Range(0, 61)] public float timeRemainingDue;
            public bool Equals(ReminderTask obj)
            {
                return this.reminderTask == obj.reminderTask && this.timeRemainingDue == obj.timeRemainingDue;
            }
        }
        [Header("System Reference")]
        public InkyContextController contextController;

        [Header("Test Input")]
        public List<ReminderTask> tasks = new List<ReminderTask>();
        private string newReminderText = "new reminder";
        private float newTimeRemainingMins = 30f;

        // Scroll state for the task list in the debug window
        private Vector2 scrollPos;

        [Header("UI Settings")]
        public bool showDebugUI = true;

        // Smaller, less intrusive window
        private Rect windowRect = new Rect(20, 20, 280, 400);
        private GUIStyle textAreaStyle;
        private GUIStyle labelStyle;
        private ReminderTask latestTask;
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
            if (contextController == null)
            {
                Debug.LogError("[TestDashboard] Missing Context Controller!");
                return;
            }

            if (tasks == null || tasks.Count == 0)
            {
                Debug.LogWarning("[TestDashboard] No tasks in list!");
                return;
            }

            ReminderTask latest = tasks.Aggregate((i, j) => i.timeRemainingDue < j.timeRemainingDue ? i : j);
            // foreach (var task in tasks)
            // {
            //     if (task.timeRemainingDue > latest.timeRemainingDue)
            //         Debug.Log($"Latest {latest.reminderTask} will be {task.reminderTask} now");
            //     latest = task;
            //     latestTask = latest;
            // }

            Debug.Log($"[TestDashboard] Sending latest task: '{latest.reminderTask}' at {latest.timeRemainingDue:F1} mins");
            contextController.ReceiveReminder(latest.reminderTask, latest.timeRemainingDue);
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

            // --- Task List ---
            GUILayout.Label("Tasks:", labelStyle);

            float latestTime = tasks.Count > 0 ? tasks.Min(t => t.timeRemainingDue) : -1f;

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(160));
            for (int i = 0; i < tasks.Count; i++)
            {
                bool isLatest = Mathf.Approximately(tasks[i].timeRemainingDue, latestTime);
                string prefix = isLatest ? "★ " : "   ";

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{prefix}[{tasks[i].timeRemainingDue:F1}m] {tasks[i].reminderTask}", labelStyle);
                if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(20)))
                {
                    tasks.RemoveAt(i);
                    GUILayout.EndHorizontal();
                    break; // avoid modifying list mid-loop
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(8);

            // --- Add New Task ---
            GUILayout.Label("New Reminder:", labelStyle);
            newReminderText = GUILayout.TextArea(newReminderText, textAreaStyle, GUILayout.Height(35));

            GUILayout.Label("Time (mins): " + newTimeRemainingMins.ToString("F1"), labelStyle);
            newTimeRemainingMins = GUILayout.HorizontalSlider(newTimeRemainingMins, 0f, 60f);

            if (GUILayout.Button("Add Task", GUILayout.Height(22)))
            {
                tasks.Add(new ReminderTask { reminderTask = newReminderText, timeRemainingDue = newTimeRemainingMins });
            }

            GUILayout.Space(8);

            // --- Actions ---
            if (GUILayout.Button("Send Latest Reminder", GUILayout.Height(25)))
                SendTestReminder();

            if (GUILayout.Button("Update Time Linearly", GUILayout.Height(25)))
            {
                if (contextController != null && tasks.Count > 0)
                {
                    float latest = tasks.Max(t => t.timeRemainingDue);
                    contextController.SetTimeRemaining(latest);
                }
            }

            GUI.DragWindow();
        }
    }
}
