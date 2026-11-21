using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class MirrorGlassController : MonoBehaviour
{
    [Header("Основные настройки")]
    [Range(0f, 1f)] public float reflectivity = 0.7f;
    [Range(0f, 1f)] public float glassOpacity = 0.15f;
    public Color glassTint = new Color(0.88f, 0.97f, 0.94f, 1f);

    [Header("Кривое зеркало (Искажение)")]
    [Range(0f, 0.5f)] public float distortion = 0.08f;
    [Range(1f, 30f)] public float distortionScale = 6.0f;

    [Header("Несколько стекол и физика")]
    [Range(0f, 0.15f)] public float multiGlassLayers = 0.03f;
    [Range(0.5f, 10f)] public float fresnelPower = 2.5f;
    [Range(0f, 1f)] public float smoothness = 0.98f;

    [Header("Запотевание")]
    [Range(0f, 1f)] public float dirtAmount = 0.0f;
    public Color dirtColor = new Color(0.8f, 0.85f, 0.88f, 1f);

    [Header("Настройки отражения")]
    public bool realtimeReflection = true;
    public int resolution = 256;
    public LayerMask reflectionLayers = -1;

    private Material _material;
    private ReflectionProbe _probe;
    private RenderTexture _cubeRT;
    private static readonly int DirtColorID = Shader.PropertyToID("_DirtColor");

    private static readonly int ReflectivityID = Shader.PropertyToID("_Reflectivity");
    private static readonly int GlassOpacityID = Shader.PropertyToID("_GlassOpacity");
    private static readonly int GlassTintID = Shader.PropertyToID("_GlassTint");
    private static readonly int DistortionID = Shader.PropertyToID("_Distortion");
    private static readonly int DistortionScaleID = Shader.PropertyToID("_DistortionScale");
    private static readonly int MultiGlassID = Shader.PropertyToID("_MultiGlassLayers");
    private static readonly int FresnelPowerID = Shader.PropertyToID("_FresnelPower");
    private static readonly int SmoothnessID = Shader.PropertyToID("_Smoothness");
    private static readonly int DirtAmountID = Shader.PropertyToID("_DirtAmount");
    private static readonly int CustomCubeID = Shader.PropertyToID("_CustomReflectionCube");

    private void OnEnable()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            _material = Application.isPlaying ? rend.material : rend.sharedMaterial;
        }

        SetupProbe();
    }

    private void Update()
    {
        UpdateShaderProperties();

        if (realtimeReflection && _probe != null && _cubeRT != null)
        {
            _probe.RenderProbe(_cubeRT);
            if (_material != null)
            {
                _material.SetTexture(CustomCubeID, _cubeRT);
            }
        }
    }

    public void UpdateShaderProperties()
    {
        if (_material == null) return;

        _material.SetFloat(ReflectivityID, reflectivity);
        _material.SetFloat(GlassOpacityID, glassOpacity);
        _material.SetColor(GlassTintID, glassTint);
        _material.SetFloat(DistortionID, distortion);
        _material.SetFloat(DistortionScaleID, distortionScale);
        _material.SetFloat(MultiGlassID, multiGlassLayers);
        _material.SetFloat(FresnelPowerID, fresnelPower);
        _material.SetFloat(SmoothnessID, smoothness);
        _material.SetFloat(DirtAmountID, dirtAmount);
        _material.SetColor(DirtColorID, dirtColor);
    }

    private void SetupProbe()
    {
        if (_cubeRT == null || _cubeRT.width != resolution)
        {
            if (_cubeRT != null) _cubeRT.Release();
            _cubeRT = new RenderTexture(resolution, resolution, 16);
            _cubeRT.dimension = TextureDimension.Cube;
            _cubeRT.useMipMap = true;
            _cubeRT.autoGenerateMips = true;
            _cubeRT.Create();
        }

        _probe = GetComponent<ReflectionProbe>();
        if (_probe == null)
        {
            _probe = gameObject.AddComponent<ReflectionProbe>();
        }

        _probe.enabled = true;
        _probe.mode = ReflectionProbeMode.Realtime;
        _probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        _probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        _probe.resolution = resolution;
        _probe.size = new Vector3(20f, 20f, 20f);
        _probe.cullingMask = reflectionLayers;
        _probe.nearClipPlane = 0.9f;

        _probe.RenderProbe(_cubeRT);
        if (_material != null)
        {
            _material.SetTexture(CustomCubeID, _cubeRT);
        }
    }

    private void OnDisable()
    {
        if (_probe != null) _probe.enabled = false;
        if (_cubeRT != null) _cubeRT.Release();
    }
}