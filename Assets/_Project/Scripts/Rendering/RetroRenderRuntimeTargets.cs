using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class RetroRenderRuntimeTargets : MonoBehaviour
{
    private const string RetroFakeLitShaderName = "CardDungeon/RetroFakeLit";

    [Header("Fullscreen Pass Materials")]
    public Material phase07Material;
    public Material phase08Material;
    public VolumeProfile volumeProfile;

    [Header("Phase 05 Materials")]
    public bool collectSceneRetroFakeLitMaterials = true;
    public List<Material> retroFakeLitMaterials = new List<Material>();
    public List<Renderer> retroFakeLitRenderers = new List<Renderer>();

    [Header("Scene Light Roles")]
    public List<RetroRenderLightRoleTarget> lightRoles = new List<RetroRenderLightRoleTarget>();

    public IEnumerable<Material> ResolveRetroFakeLitMaterials()
    {
        HashSet<Material> resolved = new HashSet<Material>();

        AddMaterials(retroFakeLitMaterials, resolved);
        AddRendererMaterials(retroFakeLitRenderers, resolved);

        if (collectSceneRetroFakeLitMaterials)
        {
            Renderer[] sceneRenderers = FindObjectsOfType<Renderer>(true);
            foreach (Renderer sceneRenderer in sceneRenderers)
            {
                AddRendererMaterials(sceneRenderer, resolved);
            }
        }

        return resolved;
    }

    public IEnumerable<RetroRenderResolvedLightRole> ResolveLightRoles()
    {
        if (lightRoles == null)
        {
            yield break;
        }

        foreach (RetroRenderLightRoleTarget lightRole in lightRoles)
        {
            if (lightRole == null || lightRole.light == null || string.IsNullOrWhiteSpace(lightRole.roleName))
            {
                continue;
            }

            yield return new RetroRenderResolvedLightRole(lightRole.roleName, lightRole.light);
        }
    }

    private static void AddMaterials(IEnumerable<Material> source, HashSet<Material> resolved)
    {
        if (source == null)
        {
            return;
        }

        foreach (Material material in source)
        {
            AddMaterial(material, resolved);
        }
    }

    private static void AddRendererMaterials(IEnumerable<Renderer> renderers, HashSet<Material> resolved)
    {
        if (renderers == null)
        {
            return;
        }

        foreach (Renderer renderer in renderers)
        {
            AddRendererMaterials(renderer, resolved);
        }
    }

    private static void AddRendererMaterials(Renderer renderer, HashSet<Material> resolved)
    {
        if (renderer == null)
        {
            return;
        }

        foreach (Material material in renderer.sharedMaterials)
        {
            AddMaterial(material, resolved);
        }
    }

    private static void AddMaterial(Material material, HashSet<Material> resolved)
    {
        if (material == null || material.shader == null || material.shader.name != RetroFakeLitShaderName)
        {
            return;
        }

        resolved.Add(material);
    }
}

[Serializable]
public sealed class RetroRenderLightRoleTarget
{
    public string roleName;
    public Light light;
}

public readonly struct RetroRenderResolvedLightRole
{
    public readonly string RoleName;
    public readonly Light Light;

    public RetroRenderResolvedLightRole(string roleName, Light light)
    {
        RoleName = roleName;
        Light = light;
    }
}
