using UnityEngine;

public class BuildDustParticles : MonoBehaviour
{
    private static Mesh _cachedSphereMesh;
    private static Material _cachedMaterial;

    public static void Spawn(Vector3 position)
    {
        GameObject obj = new GameObject("BuildDustFX");
        obj.transform.position = position;
        obj.AddComponent<BuildDustParticles>().Setup();
    }

    void Setup()
    {
        var ps = gameObject.AddComponent<ParticleSystem>();

        // 立刻停止，避免"播放中修改 duration"的警告
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 1.2f;
        main.startSpeed = 6f;
        main.startSize = 1.2f;
        main.startColor = new Color(0.55f, 0.4f, 0.25f, 1f);
        main.gravityModifier = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
          new ParticleSystem.Burst(0f, 60)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.5f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0f)
        ));

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;

        renderer.mesh = GetCachedSphereMesh();
        renderer.sharedMaterial = GetCachedMaterial();

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        ps.Play();
        Destroy(gameObject, 2f);
    }

    private static Mesh GetCachedSphereMesh()
    {
        if (_cachedSphereMesh != null) return _cachedSphereMesh;

        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _cachedSphereMesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Destroy(temp);
        return _cachedSphereMesh;
    }

    private static Material GetCachedMaterial()
    {
        if (_cachedMaterial != null) return _cachedMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        Color c = new Color(0.55f, 0.4f, 0.25f, 1f);
        mat.SetColor("_BaseColor", c);
        mat.SetColor("_Color", c);

        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        _cachedMaterial = mat;
        return _cachedMaterial;
    }
}