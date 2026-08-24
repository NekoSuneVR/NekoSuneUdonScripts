# NekoSune World Gameplay

Beginner-focused VRChat world gameplay builders for the NekoSune World Hub.

## Included

- Persistence Builder for PlayerData schemas
- automatic `OnPlayerRestored` safety gate
- typed Get/Set/Add helpers
- persistent storage usage callbacks
- starter schemas for clicker, idle/incremental, Flappy-style high-score and RPG/inventory systems
- AI Navigation patrol starter with NavMeshAgent waypoints

Generated UdonSharp source is written into `Assets/NekoSune/Gameplay/Generated/` so creators can read and learn from it.

VRChat Persistence stores per-player data on VRChat servers. The builder prefixes keys to reduce collisions and intentionally waits for `OnPlayerRestored` before accessing PlayerData.
