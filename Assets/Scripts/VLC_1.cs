using LibVLCSharp;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class VLC_1 : MonoBehaviour
{
    LibVLC _libVLC;
    MediaPlayer _mediaPlayerSphere1;
    MediaPlayer _mediaPlayerSphere2;
    Texture2D texSphere1, texSphere2;
    public Renderer sphere1, sphere2;
    bool playing;
    public string url1;
    public string url2;
    public bool change_size = false;
    public int width = 1920;
    public int height = 1080;

    void Awake()
    {
        Core.Initialize(Application.dataPath);
        _libVLC = new LibVLC(enableDebugLogs: true);

    }

    void Start() => PlayPause();

    void OnDisable()
    {
        _mediaPlayerSphere2?.Dispose();
        _mediaPlayerSphere1?.Dispose();
        _libVLC?.Dispose();
    }

    public void PlayPause()
    {
        url1 = PlayerPrefs.GetString("forward_url");
        url2 = PlayerPrefs.GetString("back_url");
        if (_mediaPlayerSphere1 == null)
        {
            // keep default projection for the screen, thus with viewpoint navigation enabled
            _mediaPlayerSphere1 = new MediaPlayer(_libVLC);
            _mediaPlayerSphere1.SetProjectionMode(VideoProjection.Rectangular);
        }
        if (_mediaPlayerSphere2 == null)
        {
            // disable default projection for the sphere, no libvlc viewpoint navigation
            _mediaPlayerSphere2 = new MediaPlayer(_libVLC);
            _mediaPlayerSphere2.SetProjectionMode(VideoProjection.Rectangular);
        }

        Debug.Log("[VLC] Toggling Play Pause!");

        if (_mediaPlayerSphere1.IsPlaying || _mediaPlayerSphere2.IsPlaying)
        {
            _mediaPlayerSphere1.Pause();
            _mediaPlayerSphere2.Pause();
        }
        else
        {
            playing = true;
            var media = new Media(new Uri(url2));
            MediaConfiguration mcfg = new()
            {
                NetworkCaching = 0
            };
            if (_mediaPlayerSphere1.Media == null)
            {
                //media.AddOption(":no-audio");
                media.AddOption(mcfg);
                Task.Run(async () =>
                {
                    var result = await media.ParseAsync(_libVLC, MediaParseOptions.ParseNetwork);
                    //Debug.Log(media.TrackList(TrackType.Video)[0].Data.Video.Projection == VideoProjection.Equirectangular
                        //? "The video is a 360 video" : "The video was not identified as a 360 video by VLC");
                });
                _mediaPlayerSphere1.Media = _mediaPlayerSphere1.Media = media;
            }
            _mediaPlayerSphere1.Play();
            if (_mediaPlayerSphere2.Media == null)
            {
                //media.AddOption(":no-audio");
                media.AddOption(mcfg);
                Task.Run(async () =>
                {
                    var result = await media.ParseAsync(_libVLC, MediaParseOptions.ParseNetwork);
                    //Debug.Log(media.TrackList(TrackType.Video)[0].Data.Video.Projection == VideoProjection.Equirectangular
                     //   ? "The video is a 360 video" : "The video was not identified as a 360 video by VLC");
                });
                _mediaPlayerSphere2.Media = _mediaPlayerSphere2.Media = media;
            }
            _mediaPlayerSphere2.Play();
        }
    }

    void Update()
    {
        if (!playing) return;
        UpdateTexture(ref texSphere1, _mediaPlayerSphere1, sphere1);
        UpdateTexture(ref texSphere2, _mediaPlayerSphere2, sphere2);
    }

    void UpdateTexture(ref Texture2D texture, MediaPlayer player, Renderer renderer)
    {
        if (texture == null)
        {
            uint width_temp = 0;
            uint height_temp = 0;
            if (change_size)
            {
                width_temp = (uint)width;
                height_temp = (uint)height;
            }
            else
            {
                player.Size(0, ref width_temp, ref height_temp);
            }
                var texPtr = player.GetTexture(width_temp, height_temp, out bool updated);

            if (width_temp != 0 && height_temp != 0 && updated && texPtr != IntPtr.Zero)
            {
                Debug.Log($"Creating texture with height {height_temp} and width {width_temp}");
                texture = Texture2D.CreateExternalTexture((int)width_temp, (int)height_temp, TextureFormat.ARGB32, false, true, texPtr);
                renderer.material.mainTexture = texture;
            }
        }
        else
        {
            var texPtr = player.GetTexture((uint)texture.width, (uint)texture.height, out bool updated);
            if (updated) texture.UpdateExternalTexture(texPtr);
        }
    }

}

