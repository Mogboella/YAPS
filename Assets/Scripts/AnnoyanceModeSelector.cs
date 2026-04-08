using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Assign the dropdown in the inspector or put this on the same GameObject as TMP_Dropdown.
/// Regular: calm → restless → urgent (0 → 1 → 2).
/// PleaseBotherMe: calm → distressed → urgent (0 → 3 → 2).
/// IDontWantToKnow: calm → restless (0 → 1).
/// </summary>
public class AnnoyanceModeSelector : MonoBehaviour
{
    public enum Mode
    {
        Regular = 0,
        PleaseBotherMe = 1,
        IDontWantToKnow = 2
    }

    [System.Serializable]
    public class ModeEvent : UnityEvent<Mode> { }

    [SerializeField]
    Mode defaultMode = Mode.Regular;

    [Tooltip("Optional. If empty, uses TMP_Dropdown on this GameObject. Listener is registered in OnEnable — no need to use On Value Changed unless you prefer.")]
    [SerializeField]
    TMP_Dropdown dropdown;

    [Tooltip("Invoked whenever the dropdown selection changes (after mapping index → mode).")]
    public ModeEvent onModeChanged;

    public Mode CurrentMode { get; private set; }

    /// <summary>Optional singleton for gameplay code: last enabled selector in the scene.</summary>
    public static AnnoyanceModeSelector Instance { get; private set; }

#if UNITY_EDITOR
    void Reset()
    {
        dropdown = GetComponent<TMP_Dropdown>();
    }
#endif

    void Awake()
    {
        CurrentMode = defaultMode;
    }

    void OnEnable()
    {
        Instance = this;

        var d = dropdown != null ? dropdown : GetComponent<TMP_Dropdown>();
        if (d != null)
            d.onValueChanged.AddListener(OnAnnoyanceDropdownChanged);
    }

    void OnDisable()
    {
        if (Instance == this)
            Instance = null;

        var d = dropdown != null ? dropdown : GetComponent<TMP_Dropdown>();
        if (d != null)
            d.onValueChanged.RemoveListener(OnAnnoyanceDropdownChanged);
    }

    void Start()
    {
        var d = dropdown != null ? dropdown : GetComponent<TMP_Dropdown>();
        if (d != null)
            OnAnnoyanceDropdownChanged(d.value);
    }

    /// <summary>Optional Inspector wiring: TMP_Dropdown → On Value Changed → drag the GameObject (not the script), pick <b>Dynamic int</b> → this method.</summary>
    public void OnAnnoyanceDropdownChanged(int index)
    {
        CurrentMode = index switch
        {
            0 => Mode.Regular,
            1 => Mode.PleaseBotherMe,
            2 => Mode.IDontWantToKnow,
            _ => Mode.Regular
        };

        onModeChanged?.Invoke(CurrentMode);
    }

    /// <summary>Use if you set the dropdown from code and want listeners to run.</summary>
    public void SetMode(Mode mode)
    {
        CurrentMode = mode;
        onModeChanged?.Invoke(CurrentMode);
    }
}
