# NekoSune World UI Builder

A beginner-friendly, data-driven world-space UI authoring addon for the NekoSune World Hub.

## Package

- Branch: `world-ui-builder`
- Package: `com.nekosune.world-ui-builder`
- Menu: `NekoSune -> World -> UI Builder`

Installing this package also installs the lightweight `com.nekosune.worlds` Hub. The UI Builder registers itself automatically in the World Hub.

## Goal

World-space UI is easy to get wrong when you are new to Unity or social-VR SDKs. The Builder creates the Canvas/layout for you, explains what it generated, and keeps the design in a portable JSON blueprint instead of forcing the whole UI to be hard-coded in C#.

A beginner should be able to think in terms of:

```text
Button: Enable Mirror
Action: Enable Object
Target: Mirror
```

instead of first needing to understand every Unity Event, RectTransform, Canvas component and platform SDK class.

## Included templates

- Blank panel
- World settings
- Mirror controls
- Teleport menu
- Media / music controls
- Image gallery
- Supporter / Patreon-style wall
- Shop / catalog
- Social + links panel
- Credits / About
- Player interaction / comfort controls
- Admin / debug controls
- Rules / welcome panel
- Event schedule

Templates are editable starting points. Elements can be reordered, added and removed. Every layout can be exported/imported as a `.json` blueprint.

## UI elements

The visual editor can add:

- headings
- text
- buttons
- toggles
- sliders
- local or runtime image slots
- cards
- dividers
- spacers

Action metadata supports common beginner intent such as page navigation, object enable/disable/toggle, Animator bool/trigger, audio play/stop, teleport, respawn, external-link cards, JSON refresh and custom actions. Safe ordinary Unity actions are wired automatically; platform-specific actions stay clearly marked for VRChat Udon or ChilloutVR CCK runtime wiring.

## Theme builder

The UI is not locked to one hard-coded NekoSune style. Included presets are:

- Neko Dark
- Light
- Neon
- Glass
- Pastel
- Terminal
- Custom

Creators can edit background/card/control/primary/accent/text/link colours, spacing, padding, base font size and button height. Theme data is part of the exported JSON blueprint.

## JSON content

The built-in feed schema uses:

```json
{
  "items": [
    {
      "title": "Supporter name",
      "subtitle": "Gold tier",
      "description": "Thank you for supporting the world!",
      "imageUrl": "https://example.com/image.png",
      "url": "https://example.com",
      "value": "gold"
    }
  ]
}
```

The Builder can import a local `.json` TextAsset or download a JSON snapshot in the Unity Editor and bake it into the UI. Editor snapshots are portable because the result is normal Unity UI and can be used for VRChat or ChilloutVR.

Examples are included under `Templates/` for:

- supporters / Patreon-style credits
- shops/catalogs
- event schedules
- image galleries
- staff/creator lists
- leaderboards
- links/support pages

This makes the same builder useful for supporter halls, creator walls, event posters, menus, stores/catalogs, community information panels and other data-driven displays.

## Images

Image elements generate real `RawImage` slots.

- **Local Texture:** baked directly into the world.
- **Remote image URL:** stored as slot metadata for a platform-specific runtime image loader.
- JSON feed cards with `imageUrl` create remote image placeholders automatically.

For VRChat, runtime image URLs normally need to be declared as `VRCUrl` values before upload; arbitrary strings downloaded inside JSON cannot simply become unrestricted `VRCUrl` instances at runtime.

## VRChat runtime starter scripts

`Generate VRChat Runtime Starter Pack` writes optional UdonSharp helpers under:

```text
Assets/NekoSune/WorldUI/Generated/
```

for:

- `VRCStringDownloader` + `VRCJson` JSON feeds
- `VRCImageDownloader` + predeclared `VRCUrl` image slots

There is also:

```text
NekoSune -> World -> UI Builder -> Generate VRChat Player Action Starter
```

which creates a small UdonSharp helper with beginner methods for:

- Toggle / Enable / Disable object
- Respawn player
- Teleport player
- Toggle Animator bool
- Animator trigger
- Play / Stop audio

The package itself still has no hard UdonSharp compile dependency because these scripts are generated into the creator's project only when requested.

## Links / Patreon / shops

A shop template is a **catalog/support panel**, not a payment processor. Social-VR worlds should not pretend to process Patreon/store payments inside the world.

The Builder renders title, description, price/tier text, image slots and visible URL/link information. Creators can add a QR texture using an Image element. If a platform does not expose a safe generic browser-open action, the Builder keeps the URL visible instead of inventing a fake browser API.

## Platform setup

### VRChat

When the VRChat Worlds SDK is installed, the Builder attempts to add the current `VRCUiShape` / `VRC_UIShape` component through reflection and disables Unity Navigation on generated controls.

### ChilloutVR

When CCK 3 or CCK 4 is installed, the Builder attempts to add `CVRCanvasWrapper`. CCK remains optional, so the package still compiles without it. UI actions that require `CVRInteractable` remain clearly identified instead of pretending an ordinary Unity callback is equivalent to every CCK runtime action.

## UI Doctor

The built-in Doctor checks common beginner problems such as:

- Canvas not using World Space
- missing GraphicRaycaster
- missing VRChat UI Shape / CVR Canvas Wrapper
- Unity Navigation left enabled
- tiny interaction targets
- buttons with no action
- link cards without URLs
- platform-specific actions that still need runtime wiring
- very small text

`Fix Safe Setup` can repair basic Canvas/Raycaster/platform-wrapper/navigation setup without rewriting custom logic.

## Beginner learning mode

The help section explains:

- Canvas + RectTransform
- layout groups
- GraphicRaycaster
- VRChat UI Shape
- CVR Canvas Wrapper
- Buttons / Toggles / Sliders
- JSON snapshots versus live data
- local versus runtime images
- platform-specific player actions
- why link/shop templates are informational rather than in-world payment processors

The goal is to help creators understand the generated structure instead of hiding it permanently.

## Notes

- Arbitrary remote data is platform-limited. VRChat has trusted URL rules and rate limits for remote strings/images.
- Runtime image URLs in VRChat normally need to be declared as `VRCUrl` values before upload.
- CCK 3 and CCK 4 are detected at runtime through reflection, so neither CCK is a hard VPM dependency.
- Always test generated UI at real VR viewing distance and in the actual client before publishing.
