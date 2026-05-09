using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
#if UNITY_RENDER_PIPELINE_HIGH_DEFINITION
using UnityEngine.Rendering.HighDefinition;
#endif

/// <summary>
/// HDRP water driver: planar reflection camera, globals for underwater shaders, sun, caustics,
/// camera above/below plane test. Attach to the water plane (renderer + this script).
/// Inspired by planar optically-realistic water techniques; written for this project (HDRP 17 / Unity 6).
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public class Water : MonoBehaviour
{
    public const string GlobalCameraUnderwater = "_Water_CameraUnderwater";
    public const string GlobalMainLightDir = "_Water_MainLightDir";
    public const string GlobalMainLightColor = "_Water_MainLightColor";
    public const string GlobalAbsorption = "_Water_Absorption";
    public const string GlobalScatterColor = "_Water_ScatterColor";
    public const string GlobalScatterIntensity = "_Water_ScatterIntensity";
    public const string GlobalCausticsST = "_Water_Caustics_ST";
    public const string GlobalCausticsIntensity = "_Water_CausticsIntensity";
    public const string GlobalCausticsTex = "_WaterCausticsTex";
    public const string GlobalPlanarReflVP = "_PlanarReflWorldToClip";

    static readonly int IdPlanarReflectionTexture = Shader.PropertyToID("_PlanarReflectionTexture");

    /// <summary>True when the main camera is below the water plane (local +Y is up).</summary>
    public static bool CameraIsUnderwater { get; private set; }

    [Header("References")]
    [Tooltip("Defaults to Camera.main when null.")]
    public Camera targetCamera;
    [Tooltip("Defaults to RenderSettings.sun when null.")]
    public Light sunLight;
    [Tooltip("Caustics pattern; assigned as global for surface + underwater shaders.")]
    public Texture causticsTexture;

    [Header("Reflection")]
    [Min(64)] public int reflectionResolution = 512;
    [Tooltip("Layers rendered into the reflection.")]
    public LayerMask reflectedLayers = ~0;
    [Tooltip("Extra layers to exclude from the reflection pass.")]
    public LayerMask excludeFromReflection = 0;

    [Header("Sun (HDRP lux is huge — clamp for this non-physical shader)")]
    [Tooltip("Clamps _Water_MainLightColor so specular / caustics do not wash out the water (HDRP directional intensity is often 10k+).")]
    [Min(0.5f)] public float maxSunColorComponent = 10f;

    [Header("Optical globals (extinction / scatter)")]
    public Vector3 volumeAbsorption = new Vector3(0.45f, 0.78f, 0.95f);
    public Color scatterColor = new Color(0.25f, 0.55f, 0.7f, 1f);
    [Range(0f, 4f)] public float scatterIntensity = 0.9f;
    public Vector4 causticsST = new Vector4(0.25f, 0.25f, 0.04f, 0.03f);
    [Range(0f, 3f)] public float causticsIntensity = 1f;

    Camera _reflectionCamera;
    RenderTexture _reflectionRt;
#if UNITY_RENDER_PIPELINE_HIGH_DEFINITION
    HDAdditionalCameraData _reflectionHd;
#endif
    Renderer _renderer;
    Material _surfaceMaterial;
    static int _reflectionDepth;

    void OnEnable()
    {
        _renderer = GetComponent<Renderer>();
        CacheSurfaceMaterial();
        EnsureReflectionResources();
    }

    void OnDisable()
    {
        ReleaseReflectionResources();
    }

    void LateUpdate()
    {
        UpdateGlobalsAndReflection();
    }

    void CacheSurfaceMaterial()
    {
        if (_renderer == null) _renderer = GetComponent<Renderer>();
        _surfaceMaterial = _renderer != null ? _renderer.sharedMaterial : null;
    }

    void EnsureReflectionResources()
    {
        if (_reflectionCamera != null)
            return;

        var go = new GameObject("WaterPlanarReflectionCamera")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        go.transform.SetParent(transform, false);
        _reflectionCamera = go.AddComponent<Camera>();
        _reflectionCamera.enabled = false;
        _reflectionCamera.cameraType = CameraType.Game;
        _reflectionCamera.clearFlags = CameraClearFlags.SolidColor;
        _reflectionCamera.backgroundColor = Color.clear;
        _reflectionCamera.allowMSAA = false;
        _reflectionCamera.forceIntoRenderTexture = true;

#if UNITY_RENDER_PIPELINE_HIGH_DEFINITION
        _reflectionHd = go.GetComponent<HDAdditionalCameraData>();
        if (_reflectionHd == null)
            _reflectionHd = go.AddComponent<HDAdditionalCameraData>();
        _reflectionHd.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
#endif

        AllocateReflectionTarget();
    }

    void AllocateReflectionTarget()
    {
        int w = Mathf.ClosestPowerOfTwo(Mathf.Clamp(reflectionResolution, 64, 2048));
        int h = w;
        if (_reflectionRt != null && _reflectionRt.width == w && _reflectionRt.height == h)
            return;

        if (_reflectionRt != null)
            _reflectionRt.Release();

        var desc = new RenderTextureDescriptor(w, h, GraphicsFormat.B10G11R11_UFloatPack32, GraphicsFormat.D32_SFloat)
        {
            msaaSamples = 1,
            useMipMap = false,
            autoGenerateMips = false,
            dimension = TextureDimension.Tex2D
        };
        _reflectionRt = new RenderTexture(desc) { name = "WaterPlanarReflection" };
        _reflectionRt.Create();
        _reflectionCamera.targetTexture = _reflectionRt;
    }

    void ReleaseReflectionResources()
    {
        if (_reflectionCamera != null && _reflectionCamera.gameObject != null)
        {
            DestroyImmediate(_reflectionCamera.gameObject);
            _reflectionCamera = null;
#if UNITY_RENDER_PIPELINE_HIGH_DEFINITION
            _reflectionHd = null;
#endif
        }

        if (_reflectionRt != null)
        {
            _reflectionRt.Release();
            if (Application.isPlaying)
                Destroy(_reflectionRt);
            else
                DestroyImmediate(_reflectionRt);
            _reflectionRt = null;
        }
    }

    void OnValidate()
    {
        reflectionResolution = Mathf.ClosestPowerOfTwo(Mathf.Clamp(reflectionResolution, 64, 2048));
        if (_reflectionCamera != null && isActiveAndEnabled)
            AllocateReflectionTarget();
    }

    void UpdateGlobalsAndReflection()
    {
        var cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null || _reflectionCamera == null || _reflectionRt == null)
            return;

        CacheSurfaceMaterial();

        var sun = sunLight != null ? sunLight : RenderSettings.sun;
        Vector3 lightDir = sun != null ? -sun.transform.forward : Vector3.up;
        Color lightCol = sun != null ? sun.color * sun.intensity : Color.white;
        var lc = new Vector3(lightCol.r, lightCol.g, lightCol.b);
        float m = Mathf.Max(lc.x, Mathf.Max(lc.y, lc.z));
        if (m > maxSunColorComponent && m > 1e-6f)
            lc *= maxSunColorComponent / m;

        UpdateUnderwaterState(cam);

        Shader.SetGlobalFloat(GlobalCameraUnderwater, CameraIsUnderwater ? 1f : 0f);
        Shader.SetGlobalVector(GlobalMainLightDir, lightDir);
        Shader.SetGlobalVector(GlobalMainLightColor, new Vector4(lc.x, lc.y, lc.z, 1f));
        Shader.SetGlobalVector(GlobalAbsorption, volumeAbsorption);
        Shader.SetGlobalVector(GlobalScatterColor, scatterColor);
        Shader.SetGlobalFloat(GlobalScatterIntensity, scatterIntensity);
        Shader.SetGlobalVector(GlobalCausticsST, causticsST);
        Shader.SetGlobalFloat(GlobalCausticsIntensity, causticsIntensity);
        Shader.SetGlobalTexture(GlobalCausticsTex, causticsTexture != null ? causticsTexture : Texture2D.whiteTexture);

        if (_reflectionDepth > 0)
            return;

        SyncReflectionCamera(cam);
        RenderReflection();

        // Bias clip space [-w,w] to UV [0,1] for sampling the reflection RT.
        Matrix4x4 scaleOffset = Matrix4x4.TRS(new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity, new Vector3(0.5f, 0.5f, 0.5f));
        Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(_reflectionCamera.projectionMatrix, true);
        Matrix4x4 worldToClip = gpuProj * _reflectionCamera.worldToCameraMatrix;
        Shader.SetGlobalMatrix(GlobalPlanarReflVP, scaleOffset * worldToClip);

        if (_surfaceMaterial != null)
            _surfaceMaterial.SetTexture(IdPlanarReflectionTexture, _reflectionRt);
    }

    void UpdateUnderwaterState(Camera cam)
    {
        Vector4 pl = PlaneEquationWorld();
        Vector3 p = cam.transform.position;
        float signedDist = pl.x * p.x + pl.y * p.y + pl.z * p.z + pl.w;
        CameraIsUnderwater = signedDist < 0f;
    }

    void SyncReflectionCamera(Camera src)
    {
        _reflectionCamera.CopyFrom(src);
        _reflectionCamera.targetTexture = _reflectionRt;
        int mask = reflectedLayers.value & ~excludeFromReflection.value;
        _reflectionCamera.cullingMask = mask;
        _reflectionCamera.clearFlags = CameraClearFlags.SolidColor;
        _reflectionCamera.backgroundColor = Color.clear;
        _reflectionCamera.enabled = false;

        Vector4 plane = PlaneEquationWorld();
        Matrix4x4 reflWorld = CalculateReflectionMatrix(plane);
        _reflectionCamera.worldToCameraMatrix = src.worldToCameraMatrix * reflWorld;
        _reflectionCamera.projectionMatrix = src.projectionMatrix;
    }

    Vector4 PlaneEquationWorld()
    {
        Vector3 n = transform.up.normalized;
        float d = -Vector3.Dot(n, transform.position);
        return new Vector4(n.x, n.y, n.z, d);
    }

    void RenderReflection()
    {
        _reflectionDepth++;
        bool wasRendererOn = _renderer != null && _renderer.enabled;
        if (_renderer != null)
            _renderer.enabled = false;
        try
        {
            GL.invertCulling = true;
            _reflectionCamera.Render();
        }
        finally
        {
            GL.invertCulling = false;
            _reflectionDepth--;
            if (_renderer != null)
                _renderer.enabled = wasRendererOn;
        }
    }

    /// <summary>Homogeneous reflection matrix across plane n·x+d=0 (classic planar water).</summary>
    public static Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
    {
        float x = plane.x, y = plane.y, z = plane.z, w = plane.w;
        return new Matrix4x4(
            new Vector4(1f - 2f * x * x, -2f * x * y, -2f * x * z, -2f * x * w),
            new Vector4(-2f * x * y, 1f - 2f * y * y, -2f * y * z, -2f * y * w),
            new Vector4(-2f * x * z, -2f * y * z, 1f - 2f * z * z, -2f * z * w),
            new Vector4(0f, 0f, 0f, 1f)
        );
    }
}
