# NekoSune World Avatar Search

A beginner-friendly VRChat world avatar browser/search builder for the NekoSune World Hub.

## Demo endpoint

The included demo preset uses:

```text
https://vrcavatarsearch.nekosunevr.co.uk/vrcx_search?search=Rindo
```

The default mapper expects the VRCX-style root-array fields `id`, `name`, `authorName`, `description`, `thumbnailImageUrl`, and `releaseStatus`.

## Flexible JSON adapter

The runtime can also read APIs wrapped in `avatars`, `results`, `items`, or `data`, and tries common aliases such as:

- `id`, `avatarId`, `avatar_id`
- `name`, `avatarName`, `title`
- `authorName`, `author`, `creatorName`
- `description`, `desc`, `summary`
- `releaseStatus`, `status`, `visibility`
- `thumbnailImageUrl`, `thumbnailUrl`, `imageUrl`

All preferred keys remain editable in the generated behaviour.

## Search input limitation

VRChat Udon cannot construct an arbitrary `VRCUrl` from a normal runtime string. The package therefore supports creator-predeclared preset/demo searches plus a `VRCUrlInputField` for a complete user-entered API URL.

If an API domain is not trusted by VRChat, the player must enable **Allow Untrusted URLs**.

## Avatar switching

Results store the avatar ID and use a `VRCAvatarPedestal`:

1. **PREVIEW** calls `SwitchAvatar(id)` on the preview pedestal.
2. **USE AVATAR** switches the pedestal then calls `SetAvatarUse(Networking.LocalPlayer)`.

VRChat continues to enforce the normal public/private/Marketplace ownership rules.

## Demo workflow

1. Open `NekoSune > World > Avatar Search Builder`.
2. Click **BUILD AVATAR SEARCH DEMO**.
3. Wait for UdonSharp to compile the generated runtime script.
4. Keep the generated UI selected.
5. Click **AUTO-WIRE SELECTED SEARCH UI**.
6. Test with VRChat Build & Test.
