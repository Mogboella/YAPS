using System.Collections;
using UnityEngine;

namespace YAPS.ContextSystem
{
    [RequireComponent(typeof(ContextExtractor), typeof(PropManager))]
    public class InkyContextController : MonoBehaviour
    {
        [Header("Script References")]
        [Tooltip("Reference to the existing Visual Controller to drive anxiety/colors.")]
        public CompanionVisualController visualController;
        
        private ContextExtractor extractor;
        private PropManager propManager;

        [Header("Behavior Settings")]
        [Tooltip("How fast Inky rotates to match current look target.")]
        public float rotationSpeed = 3.0f;

        // Internal State
        private ReminderData activeReminder;
        private bool hasActiveReminder = false;
        private float currentLookAtUserTimer = 0f;
        private bool isLookingAtUser = false;
        private Transform mainCameraTransform;
        private Vector3 initialForward;

        void Awake()
        {
            extractor = GetComponent<ContextExtractor>();
            propManager = GetComponent<PropManager>();
            
            if (visualController == null)
            {
                visualController = GetComponent<CompanionVisualController>();
                if (visualController == null)
                {
                    Debug.LogWarning("[InkyContextController] Visual controller not assigned!");
                }
            }

            if (Camera.main != null) mainCameraTransform = Camera.main.transform;
            initialForward = transform.forward;
        }

        public void ReceiveReminder(string reminderText, float timeRemaining)
        {
            hasActiveReminder = true;
            activeReminder = new ReminderData(reminderText, timeRemaining);
            
            // Extract meaning
            extractor.ExtractContext(reminderText, OnContextExtracted);
        }

        private void OnContextExtracted(ContextCategory category, string keyword)
        {
            activeReminder.extractedCategory = category;
            activeReminder.extractedKeyword = keyword;
            
            // Spawn the relevant prop
            propManager.SpawnProp(category, keyword);
        }

        void Update()
        {
            if (!hasActiveReminder) return;

            // 1. Update Urgency based on Time Remaining
            UpdateUrgencyLevel();

            // 2. Handle Looking Behavior (Prop vs User)
            HandleLookBehavior();
        }

        private void UpdateUrgencyLevel()
        {
            if (visualController == null) return;

            float time = activeReminder.timeRemainingMinutes;
            float targetUrgency = 0f;

            if (time > 30f)
            {
                // Idle
                targetUrgency = 0.0f;
            }
            else if (time <= 30f && time > 10f)
            {
                // Mild
                // Lerp between 0.1 and 0.4 as time goes from 30 down to 10
                float t = 1.0f - ((time - 10f) / 20f); 
                targetUrgency = Mathf.Lerp(0.1f, 0.4f, t);
            }
            else if (time <= 10f && time > 5f)
            {
                // Active
                float t = 1.0f - ((time - 5f) / 5f);
                targetUrgency = Mathf.Lerp(0.4f, 0.8f, t);
            }
            else
            {
                // Persistent
                targetUrgency = 1.0f;
            }

            // Feed it into the existing visual controller
            visualController.urgencyScore = targetUrgency;
        }

        private void HandleLookBehavior()
        {
            GameObject prop = propManager.GetCurrentProp();
            if (prop == null) return;

            // Simple timer logic to occasionally look at user
            currentLookAtUserTimer -= Time.deltaTime;
            if (currentLookAtUserTimer <= 0)
            {
                isLookingAtUser = !isLookingAtUser;
                // Switch look target every 3-8 seconds
                currentLookAtUserTimer = Random.Range(3f, 8f); 
            }

            Vector3 targetLookDirection;

            if (isLookingAtUser && mainCameraTransform != null)
            {
                // Look at camera (User)
                targetLookDirection = mainCameraTransform.position - transform.position;
            }
            else
            {
                // Look at Prop
                targetLookDirection = prop.transform.position - transform.position;
            }

            // Keep rotation flat on the Y axis to avoid tilting the character weirdly
            targetLookDirection.y = 0;

            if (targetLookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(targetLookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
        }

        /// <summary>
        /// Call this externally if simulating time passing linearly.
        /// </summary>
        public void SetTimeRemaining(float mins)
        {
            if (hasActiveReminder)
            {
                activeReminder.timeRemainingMinutes = mins;
            }
        }
    }
}
