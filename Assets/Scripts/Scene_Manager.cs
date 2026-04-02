using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Scene_Manager : MonoBehaviour
{
    [SerializeField] private TMP_InputField forward_url;
    [SerializeField] private TMP_InputField back_url;
    [SerializeField] private TMP_Dropdown options;
    [SerializeField] private Button start_button;
    [SerializeField] private Toggle Is_debug;
    void Start()
    {
        start_button.onClick.AddListener(OpenScene);
        forward_url.onValueChanged.AddListener(Change_Forward_Url);
        back_url.onValueChanged.AddListener(Change_Back_Url);
        options.onValueChanged.AddListener(Change_Options);
        Is_debug.onValueChanged.AddListener(Change_Debug);
        forward_url.text = PlayerPrefs.GetString("forward_url", "http://192.168.50.89:8889/stream1/whep");
        back_url.text = PlayerPrefs.GetString("back_url", "http://192.168.50.89:8889/stream2/whep");
        options.value = PlayerPrefs.GetInt("options", 0);
        Is_debug.isOn = PlayerPrefs.GetInt("Is_debug", 0) != 0;
    }

    private void OpenScene()
    {
        SceneManager.LoadScene("SampleScene");
    }

    private void Change_Forward_Url(string url)
    {
        PlayerPrefs.SetString("forward_url", url);
    }

    private void Change_Back_Url(string url)
    {
        PlayerPrefs.SetString("back_url", url);
    }

    private void Change_Options(int number)
    {
        PlayerPrefs.SetInt("options", number);
    }
    private void Change_Debug(bool value)
    {
        PlayerPrefs.SetInt("Is_debug", value ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        start_button.onClick.RemoveAllListeners();
        forward_url.onValueChanged.RemoveAllListeners();
        back_url.onValueChanged.RemoveAllListeners();
    }
}
