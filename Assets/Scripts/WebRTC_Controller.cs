using System;
using System.Collections;
using System.Linq;
using System.Net.Http;
using TMPro;
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

    [Tooltip("UI Text: пинг, FPS приложения и FPS входящего видео (если есть в WebRTC stats).")]
    [SerializeField] private  TMP_Text statsOutputText;
    [SerializeField, Min(0.1f)] private float statsRefreshIntervalSeconds = 0.5f;

    private Coroutine statsDisplayCoroutine;
    private float smoothedAppFps = 60f;

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
                    Texture2D texture2D = (tex as Texture2D);
                    Debug.Log(texture2D.GetPixel(0, 0));
                    Debug.Log(texture2D.GetPixel(0, 0));
                    sphere.material.mainTexture = texture2D;
                };
            }
        };

        RTCRtpTransceiverInit init = new RTCRtpTransceiverInit();
        init.direction = RTCRtpTransceiverDirection.RecvOnly;
        pc.AddTransceiver(TrackKind.Video, init);

        StartCoroutine(WebRTC.Update());
        StartCoroutine(createOffer());

        StartStatsDisplay();
    }

    void Update()
    {
        if (statsOutputText == null)
            return;
        float dt = Time.unscaledDeltaTime;
        if (dt > 0.0001f)
        {
            float instant = 1f / dt;
            smoothedAppFps = Mathf.Lerp(smoothedAppFps, instant, Mathf.Clamp01(dt * 12f));
        }
    }

    /// <summary>
    /// Запускает периодическое обновление текста со статистикой. Первый проход выполняется сразу (без задержки).
    /// Вызывается из Start; можно вызвать снова после смены statsOutputText.
    /// </summary>
    public void StartStatsDisplay()
    {
        if (statsOutputText == null || !isActiveAndEnabled)
            return;
        if (statsDisplayCoroutine != null)
            StopCoroutine(statsDisplayCoroutine);
        statsDisplayCoroutine = StartCoroutine(StatsDisplayLoop());
    }


    IEnumerator StatsDisplayLoop()
    {
        var wait = new WaitForSeconds(statsRefreshIntervalSeconds);
        while (statsOutputText != null && pc != null)
        {
            double? pingMs = null;
            double? videoFps = null;

            var op = pc.GetStats();
            yield return op;
            if (!op.IsError && op.Value != null)
            {
                var report = op.Value;

                foreach (var transport in report.Stats.Values.OfType<RTCTransportStats>())
                {
                    if (string.IsNullOrEmpty(transport.selectedCandidatePairId))
                        continue;
                    if (!report.Stats.TryGetValue(transport.selectedCandidatePairId, out var pairEntry))
                        continue;
                    if (pairEntry is RTCIceCandidatePairStats pair && pair.currentRoundTripTime > 0)
                    {
                        pingMs = pair.currentRoundTripTime * 1000.0;
                        break;
                    }
                }

                foreach (var inbound in report.Stats.Values.OfType<RTCInboundRTPStreamStats>())
                {
                    if (inbound.kind == "video" && inbound.framesPerSecond > 0)
                    {
                        videoFps = inbound.framesPerSecond;
                        break;
                    }
                }
            }

            WriteStatsToOutput(pingMs, smoothedAppFps, videoFps);
            yield return wait;
        }

        statsDisplayCoroutine = null;
    }

    void WriteStatsToOutput(double? pingMs, float appFps, double? videoFps)
    {
        if (statsOutputText == null)
            return;
        string pingLine = pingMs.HasValue ? $"{pingMs.Value:F0} ms" : "—";
        string videoLine = videoFps.HasValue ? $"{videoFps.Value:F1}" : "—";
        statsOutputText.text =
            $"Ping: {pingLine}\nFPS видео: {videoLine}";
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
        if (statsDisplayCoroutine != null)
        {
            StopCoroutine(statsDisplayCoroutine);
            statsDisplayCoroutine = null;
        }

        pc?.Close();
        pc?.Dispose();
        receiveStream?.Dispose();
    }


    
}

