using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace YAPS.ContextSystem
{
    public class ReminderTestDashboard : MonoBehaviour
    {
        // ── JSON response shapes (must match server output) ──────────────────
        [System.Serializable]
        private class ApiTask
        {
            public int    id;
            public string text;
            public string priority;
            public float  timeRemainingMins;   // 0 when absent → check hasTime
            public bool   done;
        }

        [System.Serializable]
        private class ApiResponse
        {
            public int       count;
            public ApiTask[] tasks;
            // latestDueTask omitted — we recalculate it locally
        }

        // ── Task struct (unchanged from your version) ─────────────────────────
        [System.Serializable]
        public struct ReminderTask
        {
            public string reminderTask;
            [Range(0, 61)] public float timeRemainingDue;

            // Track origin so we can show a badge in the UI
            public bool fromAPI;

            public bool Equals(ReminderTask obj) =>
                reminderTask == obj.reminderTask &&
                Mathf.Approximately(timeRemainingDue, obj.timeRemainingDue);
        }

        // ── Inspector fields ──────────────────────────────────────────────────
        [Header("System Reference")]
        public InkyContextController contextController;

        [Header("API Settings")]
        public string apiUrl = "http://localhost:3001/api/tasks";
        public bool   fetchOnStart = true;

        [Header("Test Input")]
        public List<ReminderTask> tasks = new List<ReminderTask>();

        [Header("UI Settings")]
        public bool showDebugUI = true;

        // ── Private state ─────────────────────────────────────────────────────
        private string    newReminderText    = "new reminder";
        private float     newTimeRemainingMins = 30f;
        private Vector2   scrollPos;
        private Rect      windowRect = new Rect(20, 20, 300, 460);
        private GUIStyle  textAreaStyle;
        private GUIStyle  labelStyle;
        private GUIStyle  mutedLabelStyle;

        private bool   _fetching       = false;
        private string _fetchStatus    = "";   // shown in UI

        // ─────────────────────────────────────────────────────────────────────
        void Start()
        {
            if (contextController == null)
                contextController = FindFirstObjectByType<InkyContextController>();

            if (fetchOnStart)
                StartCoroutine(FetchTasksFromAPI());
        }

        // ── API fetch ─────────────────────────────────────────────────────────
        private IEnumerator FetchTasksFromAPI()
        {
            if (_fetching) yield break;
            _fetching    = true;
            _fetchStatus = "Fetching…";

            using var req = UnityWebRequest.Get(apiUrl);
            req.SetRequestHeader("Accept", "application/json");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                _fetchStatus = $"Error: {req.error}";
                Debug.LogWarning($"[TestDashboard] API fetch failed: {req.error}");
                _fetching = false;
                yield break;
            }

            // JsonUtility can't deserialise a root array, but our server returns
            // { "tasks": [...] } so a wrapper class works fine.
            var response = JsonUtility.FromJson<ApiResponse>(req.downloadHandler.text);

            if (response?.tasks == null)
            {
                _fetchStatus = "Parse error";
                _fetching    = false;
                yield break;
            }

            int added = 0;
            foreach (var apiTask in response.tasks)
            {
                // Skip completed tasks and tasks with no meaningful time
                if (apiTask.done) continue;

                var incoming = new ReminderTask
                {
                    reminderTask     = apiTask.text,
                    timeRemainingDue = apiTask.timeRemainingMins,
                    fromAPI          = true,
                };

                // Deduplicate: skip if an identical task already exists locally
                bool duplicate = tasks.Any(t =>
                    t.reminderTask == incoming.reminderTask &&
                    Mathf.Approximately(t.timeRemainingDue, incoming.timeRemainingDue));

                if (!duplicate)
                {
                    tasks.Add(incoming);
                    added++;
                }
            }

            _fetchStatus = added > 0
                ? $"Fetched {added} new task(s)"
                : $"Up to date ({response.count} total)";

            Debug.Log($"[TestDashboard] API sync: {_fetchStatus}");
            _fetching = false;
        }

        // ── Send reminder (uses task due SOONEST — lowest timeRemainingDue) ───
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

            // Aggregate picks the task with the SMALLEST timeRemainingDue (due soonest)
            ReminderTask latest = tasks.Aggregate((i, j) =>
                i.timeRemainingDue < j.timeRemainingDue ? i : j);

            Debug.Log($"[TestDashboard] Sending: '{latest.reminderTask}' " +
                      $"at {latest.timeRemainingDue:F1} mins");
            contextController.ReceiveReminder(latest.reminderTask, latest.timeRemainingDue);
        }

        // ── IMGUI ─────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            if (!showDebugUI) return;

            if (textAreaStyle == null)
            {
                textAreaStyle    = new GUIStyle(GUI.skin.textArea)  { wordWrap = true, fontSize = 12 };
                labelStyle       = new GUIStyle(GUI.skin.label)     { fontSize = 12, fontStyle = FontStyle.Bold };
                mutedLabelStyle  = new GUIStyle(GUI.skin.label)     { fontSize = 11, fontStyle = FontStyle.Normal };
                mutedLabelStyle.normal.textColor = Color.gray;
            }

            windowRect = GUILayout.Window(0, windowRect, DrawWindow, "Inky Context Tester");
        }

        private void DrawWindow(int windowID)
        {
            GUILayout.Space(4);

            // ── Task list ────────────────────────────────────────────────────
            GUILayout.Label("Tasks:", labelStyle);

            float soonestTime = tasks.Count > 0 ? tasks.Min(t => t.timeRemainingDue) : -1f;

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(170));
            for (int i = 0; i < tasks.Count; i++)
            {
                bool isSoonest = Mathf.Approximately(tasks[i].timeRemainingDue, soonestTime);
                string prefix  = isSoonest ? "★ " : "   ";
                string origin  = tasks[i].fromAPI ? " [API]" : "";

                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    $"{prefix}[{tasks[i].timeRemainingDue:F1}m] {tasks[i].reminderTask}{origin}",
                    labelStyle);
                if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(20)))
                {
                    tasks.RemoveAt(i);
                    GUILayout.EndHorizontal();
                    break;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(6);

            // ── Add new task ─────────────────────────────────────────────────
            GUILayout.Label("New Reminder:", labelStyle);
            newReminderText = GUILayout.TextArea(newReminderText, textAreaStyle, GUILayout.Height(35));

            GUILayout.Label("Time (mins): " + newTimeRemainingMins.ToString("F1"), labelStyle);
            newTimeRemainingMins = GUILayout.HorizontalSlider(newTimeRemainingMins, 0f, 60f);

            if (GUILayout.Button("Add Task", GUILayout.Height(22)))
                tasks.Add(new ReminderTask
                {
                    reminderTask     = newReminderText,
                    timeRemainingDue = newTimeRemainingMins,
                    fromAPI          = false,
                });

            GUILayout.Space(6);

            // ── API controls ─────────────────────────────────────────────────
            GUI.enabled = !_fetching;
            if (GUILayout.Button(_fetching ? "Fetching…" : "Fetch from API", GUILayout.Height(22)))
                StartCoroutine(FetchTasksFromAPI());
            GUI.enabled = true;

            if (!string.IsNullOrEmpty(_fetchStatus))
                GUILayout.Label(_fetchStatus, mutedLabelStyle);

            GUILayout.Space(6);

            // ── Actions ──────────────────────────────────────────────────────
            if (GUILayout.Button("Send Latest Reminder", GUILayout.Height(25)))
                SendTestReminder();

            if (GUILayout.Button("Update Time Linearly", GUILayout.Height(25)))
            {
                if (contextController != null && tasks.Count > 0)
                    contextController.SetTimeRemaining(tasks.Max(t => t.timeRemainingDue));
            }

            GUI.DragWindow();
        }
    }
}
