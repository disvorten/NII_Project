using UnityEngine;

public class Setup_Scene : MonoBehaviour
{
    [SerializeField] private GameObject VLC;
    [SerializeField] private GameObject WebRTC;
    [SerializeField] private GameObject Sphere;
    [SerializeField] private GameObject SphereVLC;

    private void Awake()
    {
        if (PlayerPrefs.GetInt("options", 0) == 0)
        {
            WebRTC.SetActive(true);
            Sphere.SetActive(true);
            VLC.SetActive(false);
            SphereVLC.SetActive(false);
        }
        else
        {
            WebRTC.SetActive(false);
            Sphere.SetActive(false);
            VLC.SetActive(true);
            SphereVLC.SetActive(true);
        }
    }
}
