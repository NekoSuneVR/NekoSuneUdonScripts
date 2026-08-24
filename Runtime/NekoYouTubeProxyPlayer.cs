using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDKBase;
using VRC.Udon;

namespace NekoSune.WorldYouTubeProxy
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class NekoYouTubeProxyPlayer : UdonSharpBehaviour
    {
        [Header("Stock VRChat players")]
        public VRCAVProVideoPlayer avProPlayer;
        public VRCUnityVideoPlayer unityPlayer;

        [Header("Community / custom player adapter")]
        public UdonBehaviour customPlayer;
        public string customUrlVariable = "url";
        public string customPlayEvent = "Play";
        public string customStopEvent = "Stop";

        [Header("Input / status")]
        public VRCUrlInputField proxyInput;
        public Text statusText;
        [Tooltip("Watch the assigned VRCUrlInputField for YouTube/NekoSune relay URLs only. Non-YouTube URLs are left completely alone for the existing video player to handle normally.")]
        public bool autoWatchInput = true;

        [Header("Start URL")]
        public VRCUrl startUrl = VRCUrl.Empty;
        public bool playStartUrl;
        [Tooltip("Optional compatibility switch for a proxy-owned start URL. Disabled by default so adding this component never interrupts an existing player's normal non-YouTube autoplay.")]
        public bool stopNativePlayerOnBridgeStart = false;

        [Header("Networking")]
        public bool synchronizeUrl = true;

        [Header("YouTube compatibility")]
        [Tooltip("Fallback only. If enabled, a normal youtube.com/youtu.be URL is played directly when it cannot be converted to a NekoSune proxy VRCUrl at runtime. Non-YouTube URLs are never changed by this option.")]
        public bool allowDirectYouTubeFallback;

        [UdonSynced] private VRCUrl syncedUrl = VRCUrl.Empty;

        private VRCUrl _pendingUrl = VRCUrl.Empty;
        private VRCUrl _activeUrl = VRCUrl.Empty;
        private float _nextAllowedPlayTime;
        private bool _playQueued;
        private int _retryIndex;
        private string _lastInputValue = "";
        private float _nextInputPoll;

        private const float MinimumUrlInterval = 5.1f;
        private const float InputPollInterval = 0.25f;

        private void Start()
        {
            if (stopNativePlayerOnBridgeStart)
            {
                if (avProPlayer != null) avProPlayer.Stop();
                if (unityPlayer != null) unityPlayer.Stop();
            }

            if (proxyInput != null && !VRCUrl.IsNullOrEmpty(proxyInput.GetUrl()))
                _lastInputValue = proxyInput.GetUrl().Get();

            SetStatus("NekoSune YouTube Proxy ready. Only YouTube is intercepted.");
            if (playStartUrl && !VRCUrl.IsNullOrEmpty(startUrl)) SubmitUrl(startUrl);
        }

        private void Update()
        {
            if (!autoWatchInput || proxyInput == null || Time.time < _nextInputPoll) return;
            _nextInputPoll = Time.time + InputPollInterval;

            VRCUrl current = proxyInput.GetUrl();
            string value = VRCUrl.IsNullOrEmpty(current) ? "" : current.Get();
            if (value == _lastInputValue) return;

            _lastInputValue = value;
            if (string.IsNullOrEmpty(value)) return;

            // Passive component mode: only intercept YouTube/proxy URLs. Everything else is
            // deliberately ignored so the existing video player's own URL logic remains intact.
            if (IsYouTube(value) || IsNekoProxy(value))
            {
                SubmitUrl(current);
            }
            else
            {
                SetStatus("Non-YouTube URL left to the original video player.");
            }
        }

        // Explicit button/API mode. If a creator wires their UI directly to this bridge,
        // non-YouTube URLs are passed through unchanged because the bridge owns submission.
        public void PlayFromInput()
        {
            if (proxyInput == null)
            {
                SetStatus("No VRCUrlInputField assigned.");
                return;
            }

            SubmitUrl(proxyInput.GetUrl());
        }

        public void PlayStartUrl()
        {
            SubmitUrl(startUrl);
        }

        public void StopVideo()
        {
            _playQueued = false;
            _retryIndex = 0;

            if (avProPlayer != null) avProPlayer.Stop();
            if (unityPlayer != null) unityPlayer.Stop();
            if (customPlayer != null && !string.IsNullOrEmpty(customStopEvent))
                customPlayer.SendCustomEvent(customStopEvent);

            SetStatus("Stopped.");
        }

        public void SubmitUrl(VRCUrl url)
        {
            if (VRCUrl.IsNullOrEmpty(url))
            {
                SetStatus("URL is empty.");
                return;
            }

            string raw = url.Get();

            if (IsNekoProxy(raw))
            {
                SetStatus("NekoSune YouTube relay URL detected.");
                PublishAndQueue(url);
                return;
            }

            if (IsYouTube(raw))
            {
                if (allowDirectYouTubeFallback)
                {
                    SetStatus("YouTube URL detected. Proxy conversion is unavailable for this runtime VRCUrl, so direct fallback is being used.");
                    PublishAndQueue(url);
                }
                else
                {
                    SetStatus("YouTube URL detected. Use the NekoSune /v/VIDEO_ID?vrc=1 URL so YouTube goes through the proxy.");
                }
                return;
            }

            SetStatus("Non-YouTube URL passing through unchanged.");
            PublishAndQueue(url);
        }

        private void PublishAndQueue(VRCUrl url)
        {
            if (synchronizeUrl)
            {
                if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
                syncedUrl = url;
                RequestSerialization();
            }

            QueuePlay(url, false);
        }

        private void QueuePlay(VRCUrl url, bool retry)
        {
            if (VRCUrl.IsNullOrEmpty(url)) return;

            _pendingUrl = url;
            if (!retry) _retryIndex = 0;

            float wait = _nextAllowedPlayTime - Time.time;
            if (wait <= 0f)
            {
                PlayPending();
                return;
            }

            if (_playQueued) return;

            _playQueued = true;
            SetStatus("Queued for VRChat video URL rate limit...");
            SendCustomEventDelayedSeconds("PlayPending", Mathf.Max(0.1f, wait));
        }

        public void PlayPending()
        {
            _playQueued = false;
            if (VRCUrl.IsNullOrEmpty(_pendingUrl)) return;

            float wait = _nextAllowedPlayTime - Time.time;
            if (wait > 0f)
            {
                _playQueued = true;
                SendCustomEventDelayedSeconds("PlayPending", Mathf.Max(0.1f, wait));
                return;
            }

            _activeUrl = _pendingUrl;
            _nextAllowedPlayTime = Time.time + MinimumUrlInterval;

            if (avProPlayer != null)
            {
                SetStatus(IsYouTubeOrProxy(_activeUrl.Get()) ? "Loading YouTube through AVPro..." : "Loading URL through AVPro...");
                avProPlayer.PlayURL(_activeUrl);
                return;
            }

            if (unityPlayer != null)
            {
                SetStatus(IsYouTubeOrProxy(_activeUrl.Get()) ? "Loading YouTube through Unity Video..." : "Loading URL through Unity Video...");
                unityPlayer.PlayURL(_activeUrl);
                return;
            }

            if (customPlayer != null)
            {
                if (!string.IsNullOrEmpty(customUrlVariable))
                    customPlayer.SetProgramVariable(customUrlVariable, _activeUrl);
                if (!string.IsNullOrEmpty(customPlayEvent))
                    customPlayer.SendCustomEvent(customPlayEvent);

                SetStatus(IsYouTubeOrProxy(_activeUrl.Get())
                    ? "Forwarded YouTube URL to community player adapter."
                    : "Forwarded URL unchanged to community player adapter.");
                return;
            }

            SetStatus("No video player is assigned.");
        }

        public override void OnDeserialization()
        {
            if (!synchronizeUrl || VRCUrl.IsNullOrEmpty(syncedUrl)) return;
            QueuePlay(syncedUrl, false);
        }

        public override void OnVideoReady()
        {
            SetStatus("Video ready.");
        }

        public override void OnVideoStart()
        {
            _retryIndex = 0;
            SetStatus("Playing.");
        }

        public override void OnVideoEnd()
        {
            SetStatus("Video ended.");
        }

        public override void OnVideoError(VideoError videoError)
        {
            if (VRCUrl.IsNullOrEmpty(_activeUrl))
            {
                SetStatus("Video error: " + videoError);
                return;
            }

            if (_retryIndex >= 3)
            {
                SetStatus("Video error: " + videoError + ". Retry limit reached.");
                return;
            }

            float delay = _retryIndex == 0 ? 5f : (_retryIndex == 1 ? 10f : 20f);
            _retryIndex++;
            _pendingUrl = _activeUrl;
            SetStatus("Video error: " + videoError + ". Retrying in " + delay + "s...");
            SendCustomEventDelayedSeconds("RetryPending", delay);
        }

        public void RetryPending()
        {
            QueuePlay(_pendingUrl, true);
        }

        private bool IsNekoProxy(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            return value.StartsWith("https://tools.nekosunevr.co.uk/v/");
        }

        private bool IsYouTube(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            return value.IndexOf("youtube.com") >= 0 || value.IndexOf("youtu.be") >= 0;
        }

        private bool IsYouTubeOrProxy(string value)
        {
            return IsNekoProxy(value) || IsYouTube(value);
        }

        private void SetStatus(string value)
        {
            if (statusText != null) statusText.text = value;
            Debug.Log("[NekoSune YouTube Proxy] " + value);
        }
    }
}
