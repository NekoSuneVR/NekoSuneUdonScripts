# NekoSune World YouTube Proxy

A VRChat World addon that routes **YouTube** through the stable NekoSuneTools relay while leaving every non-YouTube URL on the video player's normal path.

## Package

- Branch: `world-youtube-proxy`
- VPM package: `com.nekosune.world-youtube-proxy`
- Menu: `NekoSune > World > YouTube Proxy`

## One-click setup

Open the tool and press:

```text
ONE CLICK AUTO SETUP WHOLE SCENE
```

That single action:

1. copies the runtime template to `Assets/NekoSune/YouTubeProxy/Generated/NekoYouTubeProxyPlayer.cs`
2. waits through Unity's script/domain reload automatically
3. creates/associates `Assets/NekoSune/YouTubeProxy/Generated/NekoYouTubeProxyPlayer.asset`
4. requests a synchronous UdonSharp compile when the installed UdonSharp version exposes it
5. scans the scene for known video-player families
6. finds the player's existing `VRCUrlInputField`
7. finds nested stock AVPro/Unity video sources
8. adds/configures the Neko bridge
9. converts serialized creator-time YouTube default/playlist URLs to Neko relay URLs
10. gives detected URL inputs a Neko relay hint

There is also:

```text
ONE CLICK SETUP SELECTED PLAYER
```

for a single player/prefab instance.

## Auto-detected players

The scanner uses reflection and hierarchy/type detection, so these packages are **optional** and are not added as hard VPM dependencies.

| Family | One-click behavior |
| --- | --- |
| VRChat stock AVPro / Unity Video | Adds bridge, fills source/input references, optional synchronized start URL |
| VideoTXL | Detects `Texel.*`, its URL input, defaults/playlists, nested video sources |
| ProTV | Detects ProTV/ArchiTech hierarchy/types and ProTV-based prefabs |
| RiskiPlayer | Detected as ProTV-based player |
| USharpVideo | Detects `UdonSharp.Video.*` and the existing control-handler URL field |
| USharpVideoModernUI | Detected with the USharpVideo family |
| YamaPlayer | Detects `Yamadev.YamaStream.*` and its URL input fields |
| VizVid | Detects `JLChnToZ.VRC.VVMW.*` and its UI URL fields |
| ZPlayer | Detects ZPlayer types/hierarchy |
| iwaSync3 | Detects iwaSync types/hierarchy |
| KineL Video Player | Detects KineL types/hierarchy |
| TopazChat | Detects Topaz player types/hierarchy |
| UdonVR Video Player | Detects `UdonVR.Takato.VideoPlayer.*` |
| JT Playlist | Detects JT Playlist hierarchy/types |
| Generic player fallback | Detects video-looking prefabs containing a `VRCUrlInputField` |

Community video players keep ownership of their own queue, lock, synchronization and playback logic. NekoSune does **not** bypass those systems with a second competing playback path. It configures their existing URL/default data and adds a non-racing bridge marker/configuration.

## Stable relay URL

The canonical relay format is:

```text
https://tools.nekosunevr.co.uk/v/{youtubeVideoId}?vrc=1
```

`vrc=1` is always the **final** query parameter.

Examples:

```text
Auto:
https://tools.nekosunevr.co.uk/v/O9qAGM_JVGI?vrc=1

1080:
https://tools.nekosunevr.co.uk/v/O9qAGM_JVGI?q=1080&vrc=1

720:
https://tools.nekosunevr.co.uk/v/O9qAGM_JVGI?q=720&vrc=1
```

Do not store/synchronize temporary `/api/youtube-relay/...` URLs in the world.

## YouTube only

Passive bridge logic only treats these specially:

```text
youtube.com/*
youtu.be/*
https://tools.nekosunevr.co.uk/v/*
```

These continue through the original player unchanged:

```text
Vimeo
Twitch
direct MP4
direct HLS / m3u8
RTSP where supported
radio/video CDN URLs
any other non-YouTube media URL
```

## Automatic creator-time URL conversion

One-click setup inspects serialized `VRCUrl` and `VRCUrl[]` fields on detected player prefabs. Existing YouTube defaults/playlists such as:

```text
https://www.youtube.com/watch?v=O9qAGM_JVGI
```

are converted in the Unity editor to:

```text
https://tools.nekosunevr.co.uk/v/O9qAGM_JVGI?vrc=1
```

Non-YouTube entries in the same fields are left untouched.

If an optional creator start URL is entered in the setup window, the tool also tries obvious community-player default URL/playlist fields. Stock players receive it directly on the generated Neko bridge.

## Why runtime normal YouTube links cannot be silently rewritten

VRChat allows `VRCUrl(string)` construction only at **editor time**. At runtime, arbitrary user-entered URLs come from `VRCUrlInputField`.

Therefore pure Udon cannot safely do this after a player types a normal YouTube URL:

```text
https://youtube.com/watch?v=O9qAGM_JVGI
        ↓ create a different VRCUrl at runtime
https://tools.nekosunevr.co.uk/v/O9qAGM_JVGI?vrc=1
```

For runtime proxy playback the user should enter the complete stable relay URL, for example:

```text
https://tools.nekosunevr.co.uk/v/O9qAGM_JVGI?vrc=1
```

The one-click installer automatically adds this format as the detected URL input's hint/placeholder where possible.

## Generated UdonSharp files

The package runtime is shipped as a template and generated into the project:

```text
Assets/NekoSune/YouTubeProxy/Generated/NekoYouTubeProxyPlayer.cs
Assets/NekoSune/YouTubeProxy/Generated/NekoYouTubeProxyPlayer.asset
```

This avoids the package-directory UdonSharp program-asset creation problem and lets the one-click setup resume after Unity's domain reload using `SessionState`.

The program asset repair path uses the installed UdonSharp editor APIs when available and otherwise creates a `UdonSharpProgramAsset`, assigns its `sourceCsScript`, force-imports it and requests compilation.

## VRChat video rate limiting

For stock bridge-owned playback the runtime:

- waits at least 5.1 seconds between bridge-owned `PlayURL` requests
- queues rather than spamming
- retries after approximately 5, 10 and 20 seconds
- stops after three retries

Community players keep their own rate limiting/retry logic because they own playback.

## Allow Untrusted URLs

`tools.nekosunevr.co.uk` may require the player's **Allow Untrusted URLs** setting unless VRChat accepts the domain under the current world/video URL rules.

## Files

```text
Templates/Runtime/NekoYouTubeProxyPlayer.cs.txt
Editor/NekoYouTubeProxySetupWindow.cs
Editor/NekoSune.WorldYouTubeProxy.Editor.asmdef
```

## Testing note

Repository publishing cannot run Unity, UdonSharp or VRChat Build & Test. Always verify the installed release in a clean VRChat World project before publishing a production world.
