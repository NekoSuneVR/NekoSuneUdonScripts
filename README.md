# NekoSune World Gallery

A beginner-friendly VRChat image gallery/slideshow addon for the NekoSune World Hub.

## Sources

- Local `Texture[]` baked with the world.
- Predeclared `VRCUrl[]` downloaded lazily with `VRCImageDownloader`.
- Optional JSON metadata parsed with `VRCJson`.

JSON may be a root array or wrapped in `items`, `images`, `gallery`, or `data`. The runtime exposes editable field names for title, subtitle, image index, and image URL.

VRChat does not currently allow arbitrary runtime `VRCUrl` construction from JSON strings. For remote JSON galleries, predeclare the image URLs and map JSON entries to them with `imageIndex` or a matching `imageUrl` string.

## Effects

- Cross fade
- Slide left
- Slide right
- Slide up
- Zoom
- Spin + zoom
- Shader wipe
- Shader dissolve
- Shader radial reveal

The transform effects use two `RawImage` layers. Shader effects use `Shaders/NekoGalleryTransition.shader` and a third UI layer.

## Demo

Open `NekoSune > World > Image Gallery` and choose **BUILD DEMO IMAGE GALLERY**. After Unity/UdonSharp compiles the generated script, select the gallery root and choose **AUTO-WIRE SELECTED GALLERY**.
