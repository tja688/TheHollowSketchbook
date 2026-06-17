using UnityEngine;

[DisallowMultipleComponent]
public sealed class RetroRenderPresetApplier : MonoBehaviour
{
    public RetroRenderPreset preset;
    public RetroRenderRuntimeTargets targets;
    public bool applyOnAwake = true;
    public bool applyOnEnable;
    public bool logAppliedPreset;

    private bool appliedOnce;

    private void Awake()
    {
        if (applyOnAwake)
        {
            ApplyPreset();
        }
    }

    private void OnEnable()
    {
        if (applyOnEnable)
        {
            ApplyPreset();
        }
    }

    public void ApplyPreset()
    {
        if (preset == null)
        {
            return;
        }

        RetroRenderRuntimeTargets resolvedTargets = ResolveTargets();
        if (resolvedTargets == null)
        {
            Debug.LogWarning($"[{nameof(RetroRenderPresetApplier)}] No runtime targets found for preset `{preset.name}`.", this);
            return;
        }

        preset.ApplyTo(resolvedTargets);
        appliedOnce = true;

        if (logAppliedPreset)
        {
            Debug.Log($"[{nameof(RetroRenderPresetApplier)}] Applied preset `{preset.name}`.", this);
        }
    }

    private RetroRenderRuntimeTargets ResolveTargets()
    {
        if (targets != null)
        {
            return targets;
        }

        targets = GetComponent<RetroRenderRuntimeTargets>();
        if (targets != null)
        {
            return targets;
        }

        targets = FindObjectOfType<RetroRenderRuntimeTargets>(true);
        return targets;
    }

    public bool HasAppliedOnce => appliedOnce;
}
