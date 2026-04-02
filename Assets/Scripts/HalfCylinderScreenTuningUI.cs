using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public sealed class HalfCylinderScreenTuningUI : MonoBehaviour
{
    public HalfCylinderScreenTuningController controller;

    private bool isDebug = false;

    [SerializeField] private GameObject debugOnlyObject1;
    [SerializeField] private GameObject debugOnlyObject2;

    [Header("Sliders")]
    public Slider radius;
    public Slider height;
    public Slider roundHeight;
    public Slider roundInset;

    [Header("Slider value labels (optional)")]
    public TMP_Text radiusValue;
    public TMP_Text heightValue;
    public TMP_Text roundHeightValue;
    public TMP_Text roundInsetValue;

    [Header("Toggles")]
    public Toggle showUI;
    public Toggle capTop;
    public Toggle capBottom;
    public Toggle capLeft;
    public Toggle capRight;
    public Toggle roundTop;
    public Toggle roundBottom;

    [Header("Buttons (optional)")]
    public Button resetToDefaults;

    bool _syncing;
    bool _wired;

    void RefreshDebugGate()
    {
        isDebug = PlayerPrefs.GetInt("Is_debug", 0) != 0;
        Debug.Log(isDebug);
        if (debugOnlyObject1 != null) debugOnlyObject1.SetActive(isDebug);
        if (debugOnlyObject2 != null) debugOnlyObject2.SetActive(isDebug);

        if (!isDebug && controller != null)
        {
            controller.enableTuningUI = false;
            controller.ApplyUiActive();
        }
    }

    private void Awake()
    {
        RefreshDebugGate();

        if (!isDebug)
        {
            // Prevent any possible UI flash (OnEnable can run before Start).
            enabled = false;
            return;
        }

        enabled = true;
    }
    void OnEnable()
    {
        RefreshDebugGate();
        if (!isDebug)
        {
            enabled = false;
            return;
        }
        Wire();
        PullFromController();
    }

    void LateUpdate()
    {
        // If something else toggles these objects on, force them back off when debug is disabled.
        if (!isDebug)
        {
            if (debugOnlyObject1 != null && debugOnlyObject1.activeSelf) debugOnlyObject1.SetActive(false);
            if (debugOnlyObject2 != null && debugOnlyObject2.activeSelf) debugOnlyObject2.SetActive(false);
        }
    }

    void OnDisable()
    {
        Unwire();
    }

    void Wire()
    {
        if (_wired) return;
        if (controller == null) return;

        if (showUI != null) showUI.onValueChanged.AddListener(OnShowUI);
        if (radius != null) radius.onValueChanged.AddListener(v => OnRadius(v));
        if (height != null) height.onValueChanged.AddListener(v => OnHeight(v));
        if (roundHeight != null) roundHeight.onValueChanged.AddListener(v => OnRoundHeight(v));
        if (roundInset != null) roundInset.onValueChanged.AddListener(v => OnRoundInset(v));

        if (capTop != null) capTop.onValueChanged.AddListener(v => OnCapTop(v));
        if (capBottom != null) capBottom.onValueChanged.AddListener(v => OnCapBottom(v));
        if (capLeft != null) capLeft.onValueChanged.AddListener(v => OnCapLeft(v));
        if (capRight != null) capRight.onValueChanged.AddListener(v => OnCapRight(v));

        if (roundTop != null) roundTop.onValueChanged.AddListener(v => OnRoundTop(v));
        if (roundBottom != null) roundBottom.onValueChanged.AddListener(v => OnRoundBottom(v));

        if (resetToDefaults != null) resetToDefaults.onClick.AddListener(OnResetToDefaults);
        _wired = true;
    }

    void Unwire()
    {
        if (!_wired) return;

        if (showUI != null) showUI.onValueChanged.RemoveListener(OnShowUI);
        if (radius != null) radius.onValueChanged.RemoveAllListeners();
        if (height != null) height.onValueChanged.RemoveAllListeners();
        if (roundHeight != null) roundHeight.onValueChanged.RemoveAllListeners();
        if (roundInset != null) roundInset.onValueChanged.RemoveAllListeners();

        if (capTop != null) capTop.onValueChanged.RemoveAllListeners();
        if (capBottom != null) capBottom.onValueChanged.RemoveAllListeners();
        if (capLeft != null) capLeft.onValueChanged.RemoveAllListeners();
        if (capRight != null) capRight.onValueChanged.RemoveAllListeners();
        if (roundTop != null) roundTop.onValueChanged.RemoveAllListeners();
        if (roundBottom != null) roundBottom.onValueChanged.RemoveAllListeners();

        if (resetToDefaults != null) resetToDefaults.onClick.RemoveListener(OnResetToDefaults);
        _wired = false;
    }

    public void PullFromController()
    {
        if (controller == null) return;
        _syncing = true;

        if (showUI != null) showUI.SetIsOnWithoutNotify(controller.enableTuningUI);
        if (radius != null) radius.SetValueWithoutNotify(controller.radius);
        if (height != null) height.SetValueWithoutNotify(controller.height);
        if (roundHeight != null) roundHeight.SetValueWithoutNotify(controller.roundHeight);
        if (roundInset != null) roundInset.SetValueWithoutNotify(controller.roundInset);

        if (capTop != null) capTop.SetIsOnWithoutNotify(controller.capTop);
        if (capBottom != null) capBottom.SetIsOnWithoutNotify(controller.capBottom);
        if (capLeft != null) capLeft.SetIsOnWithoutNotify(controller.capLeft);
        if (capRight != null) capRight.SetIsOnWithoutNotify(controller.capRight);

        if (roundTop != null) roundTop.SetIsOnWithoutNotify(controller.roundTop);
        if (roundBottom != null) roundBottom.SetIsOnWithoutNotify(controller.roundBottom);

        UpdateLabels();
        _syncing = false;
    }

    void OnShowUI(bool v)
    {
        if (_syncing || controller == null) return;
        controller.enableTuningUI = v;
        controller.ApplyUiActive();
    }

    void OnRadius(float v)
    {
        if (_syncing || controller == null) return;
        controller.radius = Mathf.Max(0.01f, v);
        controller.Apply();
        UpdateLabels();
    }

    void OnHeight(float v)
    {
        if (_syncing || controller == null) return;
        controller.height = Mathf.Max(0.01f, v);
        controller.Apply();
        UpdateLabels();
    }

    void OnRoundHeight(float v)
    {
        if (_syncing || controller == null) return;
        controller.roundHeight = Mathf.Max(0f, v);
        controller.Apply();
        UpdateLabels();
    }

    void OnRoundInset(float v)
    {
        if (_syncing || controller == null) return;
        controller.roundInset = Mathf.Max(0f, v);
        controller.Apply();
        UpdateLabels();
    }

    void OnCapTop(bool v) { if (!_syncing && controller != null) { controller.capTop = v; controller.Apply(); } }
    void OnCapBottom(bool v) { if (!_syncing && controller != null) { controller.capBottom = v; controller.Apply(); } }
    void OnCapLeft(bool v) { if (!_syncing && controller != null) { controller.capLeft = v; controller.Apply(); } }
    void OnCapRight(bool v) { if (!_syncing && controller != null) { controller.capRight = v; controller.Apply(); } }
    void OnRoundTop(bool v) { if (!_syncing && controller != null) { controller.roundTop = v; controller.Apply(); } }
    void OnRoundBottom(bool v) { if (!_syncing && controller != null) { controller.roundBottom = v; controller.Apply(); } }

    void OnResetToDefaults()
    {
        if (controller == null) return;
        controller.ResetToDefaults();
        PullFromController();
    }

    void UpdateLabels()
    {
        if (controller == null) return;
        if (radiusValue != null) radiusValue.text = controller.radius.ToString("0.###");
        if (heightValue != null) heightValue.text = controller.height.ToString("0.###");
        if (roundHeightValue != null) roundHeightValue.text = controller.roundHeight.ToString("0.###");
        if (roundInsetValue != null) roundInsetValue.text = controller.roundInset.ToString("0.###");
    }
}

