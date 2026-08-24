# NekoSune World Starter Games

UI mini-game examples for beginners learning VRChat UdonSharp + Persistence.

## Included demos

### Neko Flappy
- world-space UI game board
- Jump input + FLAP button
- moving UI pipe obstacles
- persistent best score
- persistent run count
- simple medal counter

### Neko Clicker
- persistent coins
- lifetime clicks
- persistent upgrade level
- upgrade purchase loop

### Neko Idle
- persistent coins / production rate / upgrade level / lifetime production
- local per-frame accumulation
- PlayerData writes batched every 10 seconds instead of every frame

## Build flow

1. Open `NekoSune → World → Starter Game Kit`.
2. Pick a starter and click **BUILD SELECTED STARTER**.
3. Wait for Unity/UdonSharp to compile the generated script.
4. Keep/select the generated `Neko Starter - ...` root.
5. Click **AUTO-WIRE SELECTED STARTER**.
6. Test with VRChat Build & Test.

All generated scripts live in `Assets/NekoSune/StarterGames/Generated/` and are intentionally readable/editable.
