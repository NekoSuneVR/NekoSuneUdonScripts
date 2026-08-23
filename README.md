# NekoSune World UI Builder

A beginner-friendly, data-driven world-space UI authoring addon for the NekoSune World Hub.

## Package

- Branch: `world-ui-builder`
- Package: `com.nekosune.world-ui-builder`
- Menu: `NekoSune -> World -> UI Builder`

Installing this package also installs the lightweight `com.nekosune.worlds` Hub. The UI Builder registers itself automatically in the World Hub.

## Why this exists

World-space UI is easy to get wrong when you are new to Unity or social-VR SDKs. This builder creates the Canvas/layout for you, explains what it generated, and keeps the design in a portable JSON blueprint instead of forcing the whole UI to be hard-coded in C#.

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

Templates are only starting points. Every layout can be edited and exported/imported as JSON.

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
      "url": "https://example.com"
    }
  ]
}
```

The Builder can import a local `.json` TextAsset or download a JSON snapshot in the Unity Editor and bake it into the UI. This works for both VRChat and ChilloutVR because the result is normal Unity UI.

For VRChat projects, `Generate VRChat Runtime Starter Pack` writes optional UdonSharp starter scripts into `Assets/NekoSune/WorldUI/Generated/` for `VRCStringDownloader`/`VRCJson` text feeds and predeclared `VRCUrl` image slots.

## Links / Patreon / shops

A shop template is a **catalog/support panel**, not a payment processor. Social-VR worlds should not pretend to process Patreon/store payments inside the world. The Builder renders title, description, price/tier text, image slots and URL/QR placeholders. If a platform does not expose a safe generic browser-open action, the Builder keeps the URL visible instead of inventing an unsafe API.

## Platform setup

### VRChat

When the VRChat Worlds SDK is installed, the builder attempts to add the current `VRCUiShape`/`VRC_UIShape` component through reflection and disables Unity Navigation on generated controls.

### ChilloutVR

When CCK 3 or CCK 4 is installed, the builder attempts to add `CVRCanvasWrapper` and uses the CCK's current UI interaction path where supported. CCK is optional; the package still compiles without it.

## UI Doctor

The built-in doctor checks common beginner problems such as:

- Canvas not using World Space
- missing GraphicRaycaster
- missing VRChat UI shape / CVR canvas wrapper
- Unity Navigation left enabled
- tiny interaction targets
- no actions / missing targets in blueprint data
- link cards without a visible URL or QR/image hint
- remote JSON configuration without a URL

## Beginner learning mode

The right-hand help section explains what Canvas, RectTransform, GraphicRaycaster, VRC UI Shape, CVR Canvas Wrapper, Buttons, Toggles, JSON feeds and platform actions actually do. The goal is to help creators learn the generated structure rather than hide it permanently.

## Notes

- Arbitrary remote data is platform-limited. VRChat has trusted URL rules and rate limits for remote strings/images.
- Runtime image URLs in VRChat normally need to be declared as `VRCUrl` values before upload; arbitrary URL strings from JSON cannot simply be converted to `VRCUrl` at runtime.
- CCK 3 and CCK 4 are detected at runtime through reflection, so neither CCK is a hard VPM dependency.
