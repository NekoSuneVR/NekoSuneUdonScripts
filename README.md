# NekoSune World Player Tools

Beginner-friendly VRChat player interaction utilities for the NekoSune World Hub.

## Player Teleport Builder

The teleport module now supports both fixed world destinations and player-to-player movement.

Generated controls include:

- **Player To Move** selector
- **Target Player** selector
- **Teleport Me → Target**
- **Request Player → Target**
- **Request Player → Me**
- world destination selector
- **Teleport Me → Destination**
- **Request Player → Destination**
- refresh player list
- explicit **Allow Teleport Requests** consent toggle
- editable destination Transforms
- editable restricted `BoxCollider` areas

## Local player → another player

`Teleport Me → Target` reads the selected target player's current position/rotation and teleports your local player next to them using the configurable `playerArrivalOffset`.

The default offset is:

```text
0, 0, -1.25
```

so the two players do not occupy exactly the same position.

## Asking another player to move

VRChat only lets each client call `VRCPlayerApi.TeleportTo` for its own local player. NekoSune therefore cannot directly force another remote client to move.

`Request Player → Target`, `Request Player → Me`, and `Request Player → Destination` send parameterized `[NetworkCallable]` events. Every client receives the event, but only the client whose player ID matches the requested subject handles it.

That client then checks its own local `allowRemoteTeleportRequests` setting. The setting defaults **OFF**.

If consent is ON, that player's own client performs the teleport. If consent is OFF, the request is ignored.

## Restricted player-teleport areas

The generated demo creates:

```text
Neko Player Teleport Restricted Areas
├── Restricted Area - Example A
└── Restricted Area - Example B
```

Each area is a trigger `BoxCollider`. Move, rotate and resize these in the Scene view to cover locations where player-to-player arrival must be blocked, for example:

- staff rooms
- game rounds already in progress
- VIP/economy areas
- puzzle interiors
- private stages
- spawn-only sections

Before a player-to-player teleport, the receiving client checks both:

1. the target player's current position;
2. the calculated arrival point beside that player.

If either point is inside a configured restricted box, the player-to-player teleport is rejected.

Fixed world destinations remain a separate system, so creators can decide exactly which world markers should exist.

## UdonSharp repair

The Builder now uses **AUTO-WIRE / REPAIR SELECTED TELEPORT UI**. It verifies/repairs the generated `NekoPlayerTeleportSystem.asset` program asset and attaches the runtime using UdonSharp's editor component API rather than normal Unity `AddComponent`.

If a program asset has to be created, let Unity/UdonSharp compile and click Auto-Wire again.

## Demo

1. Open `NekoSune > World > Player Teleport Builder`.
2. Enter three starter destination names.
3. Click **BUILD PLAYER TELEPORT DEMO**.
4. Move the destination Transforms where you want them.
5. Move/resize the generated restricted-area BoxColliders.
6. Wait for Unity/UdonSharp to compile.
7. Select `Neko Player Teleport UI`.
8. Click **AUTO-WIRE / REPAIR SELECTED TELEPORT UI**.
9. If the program asset was repaired, wait and click Auto-Wire again.
10. Use VRChat Build & Test with two or more clients to test player-to-player consent requests.

This package requires VRChat Worlds SDK 3.8.1+ for parameterized `[NetworkCallable]` events.
