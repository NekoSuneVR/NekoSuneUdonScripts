# NekoSune World YouTube Proxy

A VRChat World addon that bridges the stable NekoSuneTools YouTube relay into VRChat video players.

## Package

- Branch: `world-youtube-proxy`
- VPM package: `com.nekosune.world-youtube-proxy`
- Menu: `NekoSune > World > YouTube Proxy`

## Stable relay contract

Use the stable NekoSuneTools URL as the canonical video URL:

```text
https://tools.nekosunevr.co.uk/v/{youtubeVideoId}?vrc=1
```

Examples:

```text
https://tools.nekosunevr.co.uk/v/O9qAGM_JVGI?vrc=1
https://tools.nekosunevr.co.uk/v/O9qAGM_JVGI?vrc=1&q=1080
https://tools.nekosunevr.co.uk/v/O9qAGM_JVGI?vrc=1&q=720
```

The relay decides server-side whether the YouTube target is a normal VOD or a live stream. Worlds should not store temporary `/api/youtube-relay/...` URLs.

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

For example:

```text
VRCUrl variable: url
Play event: Play
Stop event: Stop
```

Community video prefabs use different variable/event names, so these values are configurable instead of hard-coded.

## Quick setup

Open:

```text
NekoSune > World > YouTube Proxy
```

Then either:

```text
ADD / REPAIR BRIDGE ON SELECTED PLAYER
```

or:

```text
ADD BRIDGES TO ALL STOCK VRCHAT VIDEO PLAYERS
```

The setup window auto-wires stock AVPro/Unity components and tries to find a `VRCUrlInputField` in the same player hierarchy.

## Creator start URL

Creators can paste a normal YouTube URL in the Unity editor, for example:

```text
https://www.youtube.com/watch?v=O9qAGM_JVGI
```

The editor tool extracts the 11-character video ID and stores this editor-created `VRCUrl` on the bridge:

```text
https://tools.nekosunevr.co.uk/v/O9qAGM_JVGI?vrc=1
```

This can optionally play on world start and can be synchronized.

## Runtime URL input limitation

VRChat does **not** allow Udon to freely construct a new `VRCUrl` from an arbitrary string at runtime. A user-created `VRCUrl` normally comes from `VRCUrlInputField`.

That means a pure Udon component cannot transparently do this for an arbitrary runtime entry:

```text
https://www.youtube.com/watch?v=O9qAGM_JVGI
        ↓ impossible to construct a different VRCUrl in pure Udon
https://tools.nekosunevr.co.uk/v/O9qAGM_JVGI?vrc=1
```

The package therefore supports the VRChat-safe flows:

- creator converts normal YouTube URL to proxy URL in the editor
- user pastes a complete NekoSune `/v/...` URL into a `VRCUrlInputField`
- setup tool prefills a `VRCUrlInputField` with:

```text
https://tools.nekosunevr.co.uk/v/VIDEO_ID?vrc=1
```

and the user replaces `VIDEO_ID`
- optional direct-YouTube fallback can be enabled, but that bypasses the NekoSune proxy

The package deliberately does not fake a runtime `new VRCUrl(dynamicString)` API that VRChat does not support.

## URL synchronization

`NekoYouTubeProxyPlayer` can synchronize the stable `VRCUrl` itself using manual Udon synchronization.

Only the stable `/v/...` URL is synchronized. Temporary relay tokens are never treated as canonical world state.

Disable `synchronizeUrl` if the community player already owns URL synchronization and you only want the bridge as a local adapter.

## VRChat URL rate limit

VRChat globally limits a user to roughly one new video-player URL every five seconds.

The bridge therefore:

- waits at least 5.1 seconds between new `PlayURL` requests on that bridge
- queues instead of spamming
- retries video errors after approximately 5, 10 and 20 seconds
- stops after three retries

If a world runs many independent video players simultaneously, creators should still stagger their startup because the VRChat limit is global across the user's video players, not only this component.

## Allow Untrusted URLs

`tools.nekosunevr.co.uk` may require the player's **Allow Untrusted URLs** setting unless the domain is accepted by the world's/current VRChat URL rules.

A redirect later in the relay chain does not make the initial short-link domain automatically trusted.

## Start URL quality

The editor helper supports:

```text
auto  = prefer 1080p, fallback 720p
1080  = prefer/force the 1080 profile
720   = 720p profile
```

The Udon side does not need to determine whether the result is MP4 or HLS.

## Source ownership

The world package depends only on the stable public endpoint:

```text
https://tools.nekosunevr.co.uk/v/{youtubeVideoId}?vrc=1
```

It does not depend on the relay's internal `/info`, GoogleVideo, temporary MP4 token or temporary HLS token URLs.

## Files

```text
Runtime/NekoYouTubeProxyPlayer.cs
Editor/NekoYouTubeProxySetupWindow.cs
Editor/NekoSune.WorldYouTubeProxy.Editor.asmdef
```

## Testing note

The package source is designed for VRChat Worlds SDK/UdonSharp, but repository publishing does not execute a Unity or VRChat client compile. Always test the installed package with a clean world project and VRChat Build & Test before publishing a production world.
