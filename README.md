# NekoSune World Gallery

A beginner-friendly VRChat image gallery/slideshow addon for the NekoSune World Hub.

## Sources

The same runtime can be driven in several ways:

- local `Texture[]` baked with the world
- raw `string[]` titles/subtitles
- embedded JSON pasted directly into the inspector
- an array of raw JSON-object strings (`string[] rawJsonRows`)
- predeclared `VRCUrl[]` downloaded lazily with `VRCImageDownloader`
- optional remote JSON metadata downloaded with `VRCStringDownloader` and parsed with `VRCJson`

JSON may be a root array or wrapped in `items`, `images`, `gallery`, or `data`. The runtime exposes editable field names for title, subtitle, image index, and image URL.

VRChat does not currently allow arbitrary runtime `VRCUrl` construction from JSON strings. For remote JSON galleries, predeclare the image URLs and map JSON entries to them with `imageIndex` or a matching `imageUrl` string. Raw strings/embedded JSON can freely control captions and mapping, but remote image addresses still have to obey VRChat's `VRCUrl` security model.

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
