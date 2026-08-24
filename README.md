# NekoSune World YouTube Proxy

A VRChat World addon that bridges the stable NekoSuneTools YouTube relay into VRChat video players while leaving non-YouTube URLs alone.

## Package

- Branch: `world-youtube-proxy`
- VPM package: `com.nekosune.world-youtube-proxy`
- Menu: `NekoSune > World > YouTube Proxy`

## Stable relay contract

Use the stable NekoSuneTools URL as the canonical YouTube URL:

```text
https://tools.nekosunevr.co.uk/v/{youtubeVideoId}?vrc=1
```

`vrc=1` is always kept as the final query parameter.

Examples:

```text
Auto:
https://tools.nekosunevr.co.uk/v/O9qAGM_JVGI?vrc=1

1080:
https://tools.nekosunevr.co.uk/v/O9qAGM_JVGI?q=1080&vrc=1

720:
https://tools.nekosunevr.co.uk/v/O9qAGM_JVGI?q=720&vrc=1
```

The relay decides server-side whether the YouTube target is a normal VOD or a live stream. Worlds should not store temporary `/api/youtube-relay/...` URLs.

## YouTube-only interception

Passive watching only reacts to:

```text
youtube.com/*
youtu.be/*
https://tools.nekosunevr.co.uk/v/*
```

Everything else is deliberately left to the existing video player's normal path:

```text
Vimeo
Twitch
direct MP4
direct HLS / m3u8
radio/video CDN URLs
other supported media URLs
```

The bridge does not duplicate `PlayURL` for those URLs when it is passively watching an existing player.

## Supported player targets

### Stock VRChat components

- `VRCAVProVideoPlayer`
- `VRCUnityVideoPlayer`

If both are assigned, the bridge prefers AVPro. AVPro is the recommended target when the same integration must support normal videos and YouTube Live.

### Community/custom Udon players

The bridge also has a generic adapter:

1. assign the target `UdonBehaviour`
2. enter the program variable that receives a `VRCUrl`
3. enter the custom event that loads/plays it
4. optionally enter a stop event

Example:

```text
VRCUrl variable: url
Play event: Play
Stop event: Stop
```

## Reliable UdonSharp runtime installation

The package does not rely on its UdonSharp runtime compiling directly from `Packages/`.

Instead the setup tool copies the runtime template to:

```text
Assets/NekoSune/YouTubeProxy/Generated/NekoYouTubeProxyPlayer.cs
```

That lets Unity and UdonSharp compile it as a normal project script and create/associate a valid UdonSharp program asset.

On first install:

```text
NekoSune > World > YouTube Proxy
```

Then:

```text
1. INSTALL / REPAIR GENERATED UDON RUNTIME
2. wait for Unity/UdonSharp compilation to finish
3. select the video player object
4. ADD / REPAIR BRIDGE ON SELECTED PLAYER
```

If the UdonSharp program asset is missing, the repair flow attempts to create/refresh it and asks you to wait for compilation before attaching the behaviour.

## Scene-wide setup

You can also use:

```text
ADD BRIDGES TO ALL STOCK VRCHAT VIDEO PLAYERS
```

The setup window scans the scene for stock AVPro/Unity video players and tries to find a `VRCUrlInputField` in each player hierarchy.

## Creator start URL

Creators can paste a normal YouTube URL in the Unity editor:

```text
https://www.youtube.com/watch?v=O9qAGM_JVGI
```

The editor extracts the 11-character video ID and stores an editor-created relay `VRCUrl` such as:

```text
https://tools.nekosunevr.co.uk/v/O9qAGM_JVGI?vrc=1
```

This can optionally play on world start and can be synchronized.

## Runtime URL input limitation

VRChat does not allow pure Udon to freely create a brand-new `VRCUrl` from an arbitrary rewritten string at runtime.

So a player-entered normal YouTube URL cannot safely be transformed in pure Udon from:

```text
https://www.youtube.com/watch?v=O9qAGM_JVGI
```

into:

```text
https://tools.nekosunevr.co.uk/v/O9qAGM_JVGI?vrc=1
```

The supported VRChat-safe flows are:

- creator converts the normal YouTube URL in the Unity editor
- player pastes a complete NekoSune `/v/...?...&vrc=1` URL into `VRCUrlInputField`
- setup tool prefills the input field with:

```text
https://tools.nekosunevr.co.uk/v/VIDEO_ID?vrc=1
```

and the player replaces `VIDEO_ID`
- optional direct-YouTube fallback can be enabled, but this bypasses the relay

The package does not invent an unsupported runtime `new VRCUrl(dynamicString)` path.

## URL synchronization

`NekoYouTubeProxyPlayer` can synchronize the stable `VRCUrl` using manual Udon synchronization.

Only the stable `/v/...?...&vrc=1` URL is canonical. Temporary relay tokens are never treated as world state.

Disable `synchronizeUrl` when a community player already owns URL synchronization and the NekoSune bridge is only being used as a local adapter.

## VRChat URL rate limit

The bridge:

- waits at least 5.1 seconds between new bridge-owned `PlayURL` requests
- queues instead of spamming
- retries video errors after approximately 5, 10 and 20 seconds
- stops after three retries

If a world runs many independent video players simultaneously, creators should still stagger startup because VRChat's URL limit is global per user.

## Allow Untrusted URLs

`tools.nekosunevr.co.uk` may require the player's **Allow Untrusted URLs** setting unless the domain is accepted by the current VRChat/world URL rules.

## Files

```text
Templates/Runtime/NekoYouTubeProxyPlayer.cs.txt
Editor/NekoYouTubeProxySetupWindow.cs
Editor/NekoSune.WorldYouTubeProxy.Editor.asmdef
```

Generated in the Unity project:

```text
Assets/NekoSune/YouTubeProxy/Generated/NekoYouTubeProxyPlayer.cs
Assets/NekoSune/YouTubeProxy/Generated/NekoYouTubeProxyPlayer.asset
```

## Testing note

Repository publishing does not execute a Unity or VRChat client compile. Test the installed package in a clean VRChat World project and use VRChat Build & Test before publishing a production world.
