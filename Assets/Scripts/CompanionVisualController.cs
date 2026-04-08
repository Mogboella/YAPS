using UnityEngine;

namespace YAPS
{
    [ExecuteAlways]
    public class CompanionVisualController : MonoBehaviour
    {
        [Header("Urgency Settings")]
        [Range(0f, 1f)]
        public float urgencyScore = 0f;
        public float lerpSpeed = 2f;

        [Header("Color Settings")]
        public Color calmColor = new Color(0.2f, 0.8f, 1.0f); // Bright Blue
        public Color urgentColor = new Color(1.0f, 0.0f, 0.0f); // Pure Red

        [Header("References")]
        public Animator animator;
        public Renderer companionRenderer;
        public int bodyMaterialIndex = 0; // The index of the body material slot
        public ParticleSystem stressVFX;  // The "Stress Storm" system

        [Header("Visual Feedback")]
        public float minScale = 1f;
        public float maxScale = 1.4f;

        private float currentVisualUrgency = 0f;
        private MaterialPropertyBlock _propBlock;
        private static readonly int AnxietyLevelHash = Animator.StringToHash("AnxietyLevel");
        
        // Property IDs
        private static readonly int ColorHash = Shader.PropertyToID("_BaseColor"); 
        private static readonly int LegacyColorHash = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorHash = Shader.PropertyToID("_EmissionColor");

        void OnEnable()
        {
            _propBlock = new MaterialPropertyBlock();
            ApplyAllVisuals();
        }

        void Update()
        {
            if (Application.isPlaying)
                currentVisualUrgency = Mathf.Lerp(currentVisualUrgency, urgencyScore, Time.deltaTime * lerpSpeed);
            else
                currentVisualUrgency = urgencyScore;

            ApplyAllVisuals();
            UpdateVFX();
        }

        public void ApplyAllVisuals()
        {
            // 1. Scale
            float targetScale = Mathf.Lerp(minScale, maxScale, currentVisualUrgency);
            transform.localScale = Vector3.one * targetScale;

            // 2. Color (Using MaterialPropertyBlock to avoid dirtying assets)
            if (companionRenderer != null)
            {
                if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
                companionRenderer.GetPropertyBlock(_propBlock);
                
                Color lerpedColor = Color.Lerp(calmColor, urgentColor, currentVisualUrgency);
                
                // Set both Base Color and Emission for that "Glow" feel
                _propBlock.SetColor(ColorHash, lerpedColor);
                _propBlock.SetColor(LegacyColorHash, lerpedColor);
                _propBlock.SetColor(EmissionColorHash, lerpedColor * (currentVisualUrgency * 0.5f));
                
                // ONLY apply to the body material index!
                companionRenderer.SetPropertyBlock(_propBlock, bodyMaterialIndex);
            }

            // 3. Animation
            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat(AnxietyLevelHash, currentVisualUrgency);
                if (!Application.isPlaying) {
                    animator.Update(Time.deltaTime); 
                }
            }
        }

        private void UpdateVFX() {
            if (stressVFX == null) return;

            var emission = stressVFX.emission;
            var main = stressVFX.main;

            // Density: 0 -> 50 particles/sec
            emission.rateOverTime = currentVisualUrgency * 50f;

            // Appearance: Matches Inky's Color
            Color targetColor = Color.Lerp(calmColor, urgentColor, currentVisualUrgency);
            main.startColor = new ParticleSystem.MinMaxGradient(targetColor);

            // Intensity: Increase speed (more "frantic")
            main.startSpeed = 0.5f + (currentVisualUrgency * 2.0f);
            
            // Size: Bigger cloud when more urgent
            main.startSize = 0.2f + (currentVisualUrgency * 0.4f);

            // Optimization: If urgency is 0, sleep the system
            if (currentVisualUrgency < 0.05f) {
                if (stressVFX.isPlaying) stressVFX.Stop();
            } else {
                if (!stressVFX.isPlaying) stressVFX.Play();
            }
        }

        void OnValidate() { ApplyAllVisuals(); }

        /// <summary>Maps Inky animator Mood (0–3) to urgency-driven color, scale, VFX, and AnxietyLevel.</summary>
        public void SetUrgencyFromMood(int mood)
        {
            urgencyScore = mood switch
            {
                0 => 0f,
                1 => 0.4f,
                2 => 1f,
                3 => 0.75f,
                _ => 0f
            };
        }

        /// <summary>Animation event from inky_urgent / inky_distressed clips (same name as in the .anim events).</summary>
        public void TriggerStressBurst()
        {
            if (stressVFX == null) return;
            int n = Mathf.Clamp(Mathf.RoundToInt(16f + 32f * Mathf.Max(urgencyScore, 0.35f)), 8, 64);
            stressVFX.Emit(n);
        }
    }
}
