using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;

public class WebRTC_Controller : MonoBehaviour
{
    public string url = "http://localhost:8889/stream/whep";

    private RTCPeerConnection pc;
    private MediaStream receiveStream;
    public Renderer sphere;
    [SerializeField] private bool is_forward;

    void Start()
    {
        if (is_forward)
        {
            url = PlayerPrefs.GetString("forward_url");
        }
        else
        {
            url = PlayerPrefs.GetString("back_url");
        }
            pc = new RTCPeerConnection();
        receiveStream = new MediaStream();

        pc.OnTrack = e =>
        {
            receiveStream.AddTrack(e.Track);
        };

        receiveStream.OnAddTrack = e =>
        {
            if (e.Track is VideoStreamTrack videoTrack)
            {
                videoTrack.OnVideoReceived += (tex) =>
                {
                    Debug.Log("get img");
                    sphere.material.mainTexture = tex;
                };
            }
            //else if (e.Track is AudioStreamTrack audioTrack)
            //{
            //    audioSource.SetTrack(audioTrack);
            //    audioSource.loop = true;
            //    audioSource.Play();
            //}
        };

        RTCRtpTransceiverInit init = new RTCRtpTransceiverInit();
        init.direction = RTCRtpTransceiverDirection.RecvOnly;
        pc.AddTransceiver(TrackKind.Audio, init);
        pc.AddTransceiver(TrackKind.Video, init);

        StartCoroutine(WebRTC.Update());
        StartCoroutine(createOffer());
    }

    private IEnumerator createOffer()
    {
        var op = pc.CreateOffer();
        yield return op;
        if (op.IsError)
        {
            Debug.LogError("CreateOffer() failed");
            yield break;
        }

        yield return setLocalDescription(op.Desc);
    }

    private IEnumerator setLocalDescription(RTCSessionDescription offer)
    {
        var op = pc.SetLocalDescription(ref offer);
        yield return op;
        if (op.IsError)
        {
            Debug.LogError("SetLocalDescription() failed");
            yield break;
        }

        yield return postOffer(offer);
    }

    private IEnumerator postOffer(RTCSessionDescription offer)
    {
        var uri = new UriBuilder(url);
        var content = new StringContent(offer.sdp);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/sdp");
        //HttpClientHandler httpClientHandler = new();
        //httpClientHandler.UseProxy = true;
        var client = new HttpClient();
        //client.DefaultRequestHeaders.Add("Host", uri.Host);
        var task = System.Threading.Tasks.Task.Run(async () => {
            var res = await client.PostAsync(uri.Uri, content);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadAsStringAsync();
        });
        Debug.Log(task.Result);
        yield return new WaitUntil(() => task.IsCompleted);
        if (task.Exception != null)
        {
            Debug.LogError(task.Exception);
            yield break;
        }

        yield return setRemoteDescription(task.Result);
    }

    private IEnumerator setRemoteDescription(string answer)
    {
        RTCSessionDescription desc = new RTCSessionDescription();
        desc.type = RTCSdpType.Answer;
        desc.sdp = answer;
        var op = pc.SetRemoteDescription(ref desc);
        yield return op;
        if (op.IsError)
        {
            Debug.LogError("SetRemoteDescription() failed");
            yield break;
        }

        yield break;
    }

    void OnDestroy()
    {
        pc?.Close();
        pc?.Dispose();
        receiveStream?.Dispose();
    }
}

