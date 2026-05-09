# HDRP water package (`Assets/Water`)

**Project detection:** `ProjectVersion.txt` → Unity **6000.0.x**; `Packages/manifest.json` → **HDRP 17.0.4** (no Built-in/URP packages). This water system is therefore **HDRP-only** (no Built-in `GrabPass`; refraction samples the HDRP color pyramid / opaque buffer stack).

## Files

| File | Purpose |
|------|---------|
| `WaterSurface.shader` | Transparent surface: Schlick Fresnel, planar reflection RT, screen refraction + chromatic split, depth-masked distortion, dual scrolling normals with distance fade, sun specular, forward scatter tint, absorption, projected caustics. |
| `WaterUnder.shader` | Opaque forward pass for seabed / props: view-distance extinction, in-scatter toward sun, projected caustics (global texture). |
| `Water.cs` | Planar reflection camera (HDRP), globals for sun/absorption/scatter/caustics, underwater camera flag, assigns reflection RT to the surface material. |
| `WaterVolume.cs` | Optional material swap on child renderers when the camera is underwater. |
| `Materials/WaterSurface_HDRP.mat` | Default surface material (assign to your water mesh). |
| `Materials/WaterUnder_HDRP.mat` | Default underwater material. |

## Setup (quick)

1. Create a **horizontal plane** (or quad) for the water. **Scale** defines world size; **local +Y** is the water normal used for the plane equation and reflections.
2. Assign **`Materials/WaterSurface_HDRP`** to the mesh renderer.
3. Add **`Water`** to the same GameObject (requires a `Renderer`).
4. Assign **`causticsTexture`** on `Water` (any tiling noise / caustic pattern; also bound globally for `WaterUnder`).
5. Set **Sun**: leave `sunLight` empty to use `RenderSettings.sun`, or assign your directional light.
6. **Reflection self-culling**: while the reflection RT is rendered, this water `Renderer` is temporarily disabled so the plane does not draw into its own reflection (no need for a dedicated layer). Use `excludeFromReflection` to strip additional layers (e.g. UI, particles).
7. For submerged meshes, assign **`Materials/WaterUnder_HDRP`** (or duplicate it). Globals (`_Water_*`, `_WaterCausticsTex`) are driven by `Water` while it is enabled.

## HDRP project settings

- **Opaque texture / color pyramid** must be available to transparent shaders that sample `_ColorPyramidTexture`. In HDRP defaults this is usually on; if refraction is black, check **Frame Settings** (Default Frame Settings / your camera’s custom frame settings) and enable options related to **low-resolution transparent** / **color buffer copy** for your pipeline version.
- **Depth texture** is used for absorption and refraction masking. Ensure depth is written for opaque geometry before transparent water renders (default transparent queue is late enough in most setups).

## Troubleshooting

- **Invalid `.meta` GUID warnings:** Unity expects a **32-character lowercase hex** GUID (same format as `uuid.uuid4().hex` in Python). If a `.meta` is hand-edited, bad indentation under `MonoImporter` (e.g. `assetBundleName` indented under `userData`) can also confuse the importer—match Unity’s default two-space YAML layout.
- **Water looks white:** HDRP directional lights often use **very high intensity (lux)**. `Water.cs` clamps `_Water_MainLightColor` with **`maxSunColorComponent`** (default 10) so specular and caustics do not blow out. Raise or lower that value on the `Water` component; also tune **`_ReflectionStrength`** and **`_SpecularIntensity`** on the material.

## Known limitations

- **Planar reflection** is a single infinite plane; it does not model waves geometrically for reflection (only shader normals move). Oblique near-plane clipping is **not** implemented yet; very steep grazing angles may show redundant geometry in the reflection RT—tune culling layers and resolution (`reflectionResolution`) as needed.
- **Multiple `Water` instances** each create their own reflection camera; `Water.CameraIsUnderwater` is a single static flag (last `Water` instance wins each frame). Use one ocean driver per scene, or extend the script for multi-zone logic.
- **Performance**: `Camera.Render()` for reflections costs GPU time; lower `reflectionResolution` on slower targets.
- **Built-in / URP**: not included in this package; shaders use HDRP includes and `ForwardOnly` passes.

## Attribution

Behavior is **inspired by** community “optically realistic water” approaches (Fresnel, chromatic refraction, planar reflections, extinction). This code was **written for this repository** and is not a port of any specific third-party asset.
