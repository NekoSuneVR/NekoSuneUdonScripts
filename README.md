# NekoSune World Player Tools

Beginner-friendly VRChat player interaction utilities for the NekoSune World Hub.

## Player Teleport Builder

The first module builds a world-space destination UI with:

- player selector
- destination selector
- **Teleport Me**
- **Request Selected Player**
- refresh player list
- explicit **Allow Teleport Requests** consent toggle
- editable destination Transforms

## Why consent is required

VRChat only allows `VRCPlayerApi.TeleportTo` to teleport the local player. To request a remote player's movement, this package sends a parameterized Udon network event containing the target player ID and destination index.

Every client receives the event, but only the matching target player handles it. The target client then checks its own local `allowRemoteTeleportRequests` flag. The flag defaults **OFF**.

If enabled, the target client teleports its own local player. If disabled, the request is ignored.

This requires VRChat Worlds SDK 3.8.1+ because the request uses `[NetworkCallable]` events with parameters.

## Demo

1. Open `NekoSune > World > Player Teleport Builder`.
2. Enter three starter destination names.
3. Click **BUILD PLAYER TELEPORT DEMO**.
4. Move the generated destination Transforms where you want them.
5. Wait for UdonSharp to compile the generated script.
6. Select `Neko Player Teleport UI`.
7. Click **AUTO-WIRE SELECTED TELEPORT UI**.
8. Use VRChat Build & Test with two or more clients to test consent-based remote requests.
