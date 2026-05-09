using UnityEngine;

/// <summary>
/// Optional: swap renderer materials when <see cref="Water.CameraIsUnderwater"/> toggles.
/// Pre-builds material arrays once to avoid per-frame allocations.
/// </summary>
[DisallowMultipleComponent]
public class WaterVolume : MonoBehaviour
{
    public bool swapChildRenderers = true;
    [Tooltip("Applied while the main camera is underwater.")]
    public Material underwaterMaterial;

    Renderer[] _renderers;
    Material[][] _originals;
    Material[][] _underwaterStacks;
    bool _cached;
    bool _lastUnder;

    void OnEnable()
    {
        _cached = false;
        _lastUnder = !Water.CameraIsUnderwater;
        BuildCache();
    }

    void OnValidate()
    {
        _cached = false;
    }

    void LateUpdate()
    {
        if (!swapChildRenderers || underwaterMaterial == null)
            return;

        BuildCache();
        bool under = Water.CameraIsUnderwater;
        if (under == _lastUnder)
            return;

        _lastUnder = under;
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;
            r.sharedMaterials = under ? _underwaterStacks[i] : _originals[i];
        }
    }

    void OnDisable()
    {
        RestoreAll();
    }

    void BuildCache()
    {
        if (_cached) return;
        _renderers = swapChildRenderers ? GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        _originals = new Material[_renderers.Length][];
        _underwaterStacks = new Material[_renderers.Length][];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _originals[i] = _renderers[i].sharedMaterials;
            int len = _originals[i].Length;
            var stack = new Material[len];
            for (int j = 0; j < len; j++)
                stack[j] = underwaterMaterial;
            _underwaterStacks[i] = stack;
        }

        _cached = true;
    }

    void RestoreAll()
    {
        if (!_cached || _originals == null || _renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null && _originals[i] != null)
                _renderers[i].sharedMaterials = _originals[i];
        }
    }
}
