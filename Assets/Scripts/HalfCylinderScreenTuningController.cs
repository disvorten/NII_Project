using UnityEngine;

[DisallowMultipleComponent]
public sealed class HalfCylinderScreenTuningController : MonoBehaviour
{
    [Header("Persistence")]
    public bool persist = true;
    public bool persistInEditor = false;
    public string playerPrefsKey = "HalfCylinderScreenTuning.v1";

    [Header("Runtime toggle (for prod)")]
    public bool enableTuningUI = true;
    public GameObject tuningUIRoot;

    [Header("Targets (sync)")]
    public HalfCylinderScreenMesh[] targets;

    [Header("Parameters")]
    [Min(0.01f)] public float radius = 2f;
    [Min(0.01f)] public float height = 2f;
    [Range(10f, 360f)] public float arcDegrees = 180f;
    [Min(3)] public int arcSegments = 64;
    [Min(1)] public int heightSegments = 8;

    public bool capTop;
    public bool capBottom;
    public bool capLeft;
    public bool capRight;

    public bool roundTop;
    public bool roundBottom;
    [Min(0f)] public float roundHeight = 0.35f;
    [Min(0f)] public float roundInset = 0.35f;

    public bool normalsInward = true;
    [Range(-180f, 180f)] public float yawDegrees;
    public bool flipU;
    public bool flipV;

    [System.Serializable]
    sealed class Saved
    {
        public bool enableTuningUI;

        public float radius;
        public float height;
        public float arcDegrees;
        public int arcSegments;
        public int heightSegments;

        public bool capTop;
        public bool capBottom;
        public bool capLeft;
        public bool capRight;

        public bool roundTop;
        public bool roundBottom;
        public float roundHeight;
        public float roundInset;

        public bool normalsInward;
        public float yawDegrees;
        public bool flipU;
        public bool flipV;
    }

    static Saved Defaults => new Saved
    {
        enableTuningUI = true,

        radius = 0.456f,
        height = 0.692f,
        arcDegrees = 180f,
        arcSegments = 150,
        heightSegments = 100,

        capTop = true,
        capBottom = true,
        capLeft = false,
        capRight = false,

        roundTop = true,
        roundBottom = true,
        roundHeight = 0.23f,
        roundInset = 0.211f,

        normalsInward = true,
        yawDegrees = 0f,
        flipU = false,
        flipV = false,
    };

    void OnEnable()
    {
        Load();
        Apply();
        ApplyUiActive();
    }

    void OnValidate()
    {
        radius = Mathf.Max(0.01f, radius);
        height = Mathf.Max(0.01f, height);
        arcDegrees = Mathf.Clamp(arcDegrees, 10f, 360f);
        arcSegments = Mathf.Max(3, arcSegments);
        heightSegments = Mathf.Max(1, heightSegments);
        roundHeight = Mathf.Max(0f, roundHeight);
        roundInset = Mathf.Max(0f, roundInset);

        ApplyUiActive();
        Apply();
    }

    public void Load()
    {
        if (!persist) return;
        if (!Application.isPlaying && !persistInEditor) return;
        if (string.IsNullOrWhiteSpace(playerPrefsKey)) return;
        if (!PlayerPrefs.HasKey(playerPrefsKey)) return;

        string json = PlayerPrefs.GetString(playerPrefsKey, "");
        if (string.IsNullOrWhiteSpace(json)) return;

        Saved s;
        try
        {
            s = JsonUtility.FromJson<Saved>(json);
        }
        catch
        {
            return;
        }

        if (s == null) return;

        enableTuningUI = s.enableTuningUI;

        radius = s.radius;
        height = s.height;
        arcDegrees = s.arcDegrees;
        arcSegments = s.arcSegments;
        heightSegments = s.heightSegments;

        capTop = s.capTop;
        capBottom = s.capBottom;
        capLeft = s.capLeft;
        capRight = s.capRight;

        roundTop = s.roundTop;
        roundBottom = s.roundBottom;
        roundHeight = s.roundHeight;
        roundInset = s.roundInset;

        normalsInward = s.normalsInward;
        yawDegrees = s.yawDegrees;
        flipU = s.flipU;
        flipV = s.flipV;
    }

    public void ResetToDefaults()
    {
        // Ensure saved runtime state can't immediately overwrite defaults (especially on device).
        ClearSaved();

        var d = Defaults;

        enableTuningUI = d.enableTuningUI;

        radius = d.radius;
        height = d.height;
        arcDegrees = d.arcDegrees;
        arcSegments = d.arcSegments;
        heightSegments = d.heightSegments;

        capTop = d.capTop;
        capBottom = d.capBottom;
        capLeft = d.capLeft;
        capRight = d.capRight;

        roundTop = d.roundTop;
        roundBottom = d.roundBottom;
        roundHeight = d.roundHeight;
        roundInset = d.roundInset;

        normalsInward = d.normalsInward;
        yawDegrees = d.yawDegrees;
        flipU = d.flipU;
        flipV = d.flipV;

        ApplyUiActive();
        Apply();
    }

    public void ClearSaved()
    {
        if (string.IsNullOrWhiteSpace(playerPrefsKey)) return;
        PlayerPrefs.DeleteKey(playerPrefsKey);
        PlayerPrefs.Save();
    }

    public void Save()
    {
        if (!persist) return;
        if (!Application.isPlaying && !persistInEditor) return;
        if (string.IsNullOrWhiteSpace(playerPrefsKey)) return;

        var s = new Saved
        {
            enableTuningUI = enableTuningUI,

            radius = radius,
            height = height,
            arcDegrees = arcDegrees,
            arcSegments = arcSegments,
            heightSegments = heightSegments,

            capTop = capTop,
            capBottom = capBottom,
            capLeft = capLeft,
            capRight = capRight,

            roundTop = roundTop,
            roundBottom = roundBottom,
            roundHeight = roundHeight,
            roundInset = roundInset,

            normalsInward = normalsInward,
            yawDegrees = yawDegrees,
            flipU = flipU,
            flipV = flipV,
        };

        string json = JsonUtility.ToJson(s);
        PlayerPrefs.SetString(playerPrefsKey, json);
        PlayerPrefs.Save();
    }

    public void ApplyUiActive()
    {
        // Hard gate: if debug is off, never show tuning UI (even if saved enableTuningUI=true).
        if (Application.isPlaying && PlayerPrefs.GetInt("Is_debug", 0) == 0)
        {
            enableTuningUI = false;
            if (tuningUIRoot != null) tuningUIRoot.SetActive(false);
            return;
        }

        if (tuningUIRoot != null)
            tuningUIRoot.SetActive(enableTuningUI);
        Save();
    }

    public void Apply()
    {
        if (targets == null) return;
        for (int i = 0; i < targets.Length; i++)
        {
            var t = targets[i];
            if (t == null) continue;

            t.radius = radius;
            t.height = height;
            t.arcDegrees = arcDegrees;
            t.arcSegments = arcSegments;
            t.heightSegments = heightSegments;

            t.capTop = capTop;
            t.capBottom = capBottom;
            t.capLeft = capLeft;
            t.capRight = capRight;

            t.roundTop = roundTop;
            t.roundBottom = roundBottom;
            t.roundHeight = roundHeight;
            t.roundInset = roundInset;

            t.normalsInward = normalsInward;
            t.yawDegrees = yawDegrees;
            t.flipU = flipU;
            t.flipV = flipV;

            t.RebuildNow();
        }

        Save();
    }
}

