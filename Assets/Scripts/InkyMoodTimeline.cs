using System.Collections;
using UnityEngine;

/// <summary>
/// Drives Inky's animator <c>Mood</c> over time based on <see cref="AnnoyanceModeSelector"/>:
/// Regular: calm → restless → urgent (0 → 1 → 2).
/// PleaseBotherMe: calm → distressed → urgent (0 → 3 → 2).
/// IDontWantToKnow: calm → restless (0 → 1).
/// Turn off <see cref="ObjectDistanceFromAnim.useDebugMood"/> on the follower so this is not overridden.
/// </summary>
public class InkyMoodTimeline : MonoBehaviour
{
    [Header("References")]
    [Tooltip("If null, uses AnnoyanceModeSelector.Instance at runtime.")]
    public AnnoyanceModeSelector modeSource;

    [Tooltip("Uses SetMood so speech bubble and placement stay in sync.")]
    public ObjectDistanceFromAnim inkyFollower;

    [Header("Timing (seconds)")]
    public float calmHoldDuration = 20f;
    public float middleHoldDuration = 25f;

    Coroutine _timelineRoutine;
    AnnoyanceModeSelector _subscribedSource;
    bool _listening;

    void OnEnable()
    {
        TrySubscribeToModeSource();
    }

    void OnDisable()
    {
        if (_listening && _subscribedSource != null)
            _subscribedSource.onModeChanged.RemoveListener(OnAnnoyanceModeChanged);
        _listening = false;
        StopTimeline();
        _subscribedSource = null;
    }

    void Start()
    {
        TrySubscribeToModeSource();
        var src = ResolveModeSource();
        if (src != null && _timelineRoutine == null)
            RestartTimeline(src.CurrentMode);
    }

    void TrySubscribeToModeSource()
    {
        if (_listening) return;
        _subscribedSource = ResolveModeSource();
        if (_subscribedSource != null)
        {
            _subscribedSource.onModeChanged.AddListener(OnAnnoyanceModeChanged);
            _listening = true;
        }
    }

    AnnoyanceModeSelector ResolveModeSource()
    {
        if (modeSource != null) return modeSource;
        if (AnnoyanceModeSelector.Instance != null) return AnnoyanceModeSelector.Instance;
        return Object.FindFirstObjectByType<AnnoyanceModeSelector>();
    }

    void OnAnnoyanceModeChanged(AnnoyanceModeSelector.Mode mode)
    {
        RestartTimeline(mode);
    }

    public void RestartTimeline(AnnoyanceModeSelector.Mode mode)
    {
        StopTimeline();
        if (!isActiveAndEnabled || inkyFollower == null)
            return;
        _timelineRoutine = StartCoroutine(RunTimeline(mode));
    }

    void StopTimeline()
    {
        if (_timelineRoutine != null)
        {
            StopCoroutine(_timelineRoutine);
            _timelineRoutine = null;
        }
    }

    IEnumerator RunTimeline(AnnoyanceModeSelector.Mode mode)
    {
        inkyFollower.SetMood(0);

        switch (mode)
        {
            case AnnoyanceModeSelector.Mode.Regular:
                yield return WaitSeconds(calmHoldDuration);
                inkyFollower.SetMood(1);
                yield return WaitSeconds(middleHoldDuration);
                inkyFollower.SetMood(2);
                break;

            case AnnoyanceModeSelector.Mode.PleaseBotherMe:
                yield return WaitSeconds(calmHoldDuration);
                inkyFollower.SetMood(2);
                yield return WaitSeconds(middleHoldDuration);
                inkyFollower.SetMood(3);
                break;

            case AnnoyanceModeSelector.Mode.IDontWantToKnow:
                yield return WaitSeconds(calmHoldDuration);
                inkyFollower.SetMood(1);
                break;
        }

        _timelineRoutine = null;
    }

    static IEnumerator WaitSeconds(float seconds)
    {
        if (seconds > 0f)
            yield return new WaitForSecondsRealtime(seconds);
    }
}
