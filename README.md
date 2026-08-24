# NekoSune World Gallery

A beginner-friendly VRChat image gallery/slideshow addon for the NekoSune World Hub.

## Sources

The same runtime can be driven in several ways:

- local `Texture[]` baked with the world
- raw `string[]` titles/subtitles
- embedded JSON pasted directly into the inspector
- an array of raw JSON-object strings (`string[] rawJsonRows`)
- predeclared `VRCUrl[]` downloaded lazily with `VRCImageDownloader`
- optional remote JSON downloaded with `VRCStringDownloader` and parsed with `VRCJson`

## Remote JSON mapper

The Builder now exposes the remote JSON configuration directly:

```text
Remote JSON URL
Root array path
Title field
Subtitle field
Image index field
Image URL/path field
Predeclared image URLs
Optional path aliases
```

`Root array path` supports:

- a root JSON array
- a normal wrapper such as `items`
- dotted/nested paths such as `payload.gallery.images`

The target array can contain object rows, numeric image indexes, or raw string paths/URLs.

Examples:

```json
[
  "/gallery/a.png",
  "/gallery/b.png"
]
```

```json
{
  "payload": {
    "gallery": {
      "images": [
        "/gallery/a.png",
        "/gallery/b.png"
      ]
    }
  }
}
```

```json
{
  "items": [
    {"title":"Image A", "path":"/gallery/a.png"},
    {"title":"Image B", "path":"/gallery/b.png"}
  ]
}
```

For object rows set `Image URL/path field` to `path` in the last example.

## Mapping downloaded path strings to images

VRChat does not allow arbitrary runtime `VRCUrl` construction from strings downloaded in JSON. NekoSune therefore maps JSON strings onto creator-predeclared `VRCUrl[]` entries.

You can map by:

1. numeric `imageIndex`;
2. exact full URL;
3. a relative path that matches the end of the predeclared URL;
4. a creator-defined entry in `imageUrlMapKeys[]` / **Optional aliases**.

Example Builder configuration:

```text
Predeclared image URLs:
https://cdn.example.com/gallery/a.png
https://cdn.example.com/gallery/b.png

Optional aliases:
/gallery/a.png
/gallery/b.png
```

The remote JSON may then contain only `/gallery/a.png` and `/gallery/b.png` while VRChat still receives the actual predeclared `VRCUrl` values.

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

## Repairing an older generated gallery

If `Assets/NekoSune/Gallery/Generated/NekoImageGalleryRuntime.cs` came from an older package and has a compile error, you do not need to rebuild the UI.

1. Update NekoSune World Gallery.
2. Open `NekoSune > World > Image Gallery`.
3. Click **REPAIR / COPY LATEST GALLERY RUNTIME**.
4. Let Unity/UdonSharp finish compiling.
5. Select the existing `Neko Image Gallery` root.
6. Click **AUTO-WIRE / REPAIR SELECTED GALLERY**.

The Auto-Wire repair also verifies/repairs the generated `UdonSharpProgramAsset` before attaching the runtime.

## Demo

Open `NekoSune > World > Image Gallery`, configure the source/mapping if desired, and choose **BUILD DEMO IMAGE GALLERY**. After Unity/UdonSharp compiles the generated script, select the gallery root and choose **AUTO-WIRE / REPAIR SELECTED GALLERY**.
