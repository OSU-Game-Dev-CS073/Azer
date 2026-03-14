using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ImpactParticleSetup : MonoBehaviour
{
    [Header("Particle Settings")]
    public Color particleColor = Color.white;
    public float particleSize = 0.3f;
    public int particleCount = 15;
    public float particleSpeed = 3f;
    public float particleLifetime = 0.4f;
    
    [Header("Shape")]
    public float spreadRadius = 0.4f;
    
    [Header("Material")]
    public Material customMaterial; // Optional: assign your own material
    
    void Start()
    {
        SetupParticleSystem();
    }
    
    void SetupParticleSystem()
    {
        // Get or add ParticleSystem
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps == null) ps = gameObject.AddComponent<ParticleSystem>();
        
        // Stop any playing particles
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        // --- MAIN MODULE ---
        var main = ps.main;
        main.duration = 0.2f;
        main.loop = false;
        main.prewarm = false;
        main.startDelay = 0f;
        main.startLifetime = particleLifetime;
        main.startSpeed = particleSpeed;
        main.startSize = particleSize;
        main.startColor = particleColor;
        main.gravityModifier = 0.5f; // Slight gravity for that Hollow Knight feel
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;
        main.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;
        
        // --- EMISSION MODULE ---
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.rateOverDistance = 0;
        
        // Add a single burst
        emission.SetBursts(
            new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, (short)particleCount)
            }
        );
        
        // --- SHAPE MODULE ---
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = spreadRadius;
        shape.radiusThickness = 0f; // Emit from center only
        shape.angle = 25f; // Slight spread angle
        
        // --- COLOR OVER LIFETIME MODULE ---
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        // Create gradient: starts bright, fades to transparent
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(particleColor, 0.7f),
                new GradientColorKey(particleColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        
        // --- SIZE OVER LIFETIME MODULE ---
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        
        // AnimationCurve: start small, expand, shrink
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.3f, 1.2f);
        sizeCurve.AddKey(0.7f, 0.8f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // --- ROTATION OVER LIFETIME MODULE ---
        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.separateAxes = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-180f, 180f);
        
        // --- RENDERER MODULE (CRITICAL - THIS FIXES PINK SQUARES) ---
        var renderer = GetComponent<ParticleSystemRenderer>();
        if (renderer == null) renderer = gameObject.AddComponent<ParticleSystemRenderer>();
        
        // Set up the material
        if (customMaterial != null)
        {
            renderer.material = customMaterial;
        }
        else
        {
            // Create a proper particle material
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            
            Material mat = new Material(shader);
            mat.mainTexture = CreateDefaultParticleTexture(); // Create a small white circle texture
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            
            renderer.material = mat;
        }
        
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.None;
        renderer.minParticleSize = 0.1f;
        renderer.maxParticleSize = 1f;
        renderer.alignment = ParticleSystemRenderSpace.World;
        
        // --- TEXTURE SHEET ANIMATION (for sprite sheets if you want) ---
        var textureSheet = ps.textureSheetAnimation;
        textureSheet.enabled = false; // Disable by default
        
        // Final cleanup
        ps.Clear(true);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        Debug.Log("Particle System setup complete with proper material!");
    }
    
    // Creates a simple white circle texture if no material is assigned
    private Texture2D CreateDefaultParticleTexture()
    {
        Texture2D texture = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 32; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16));
                if (dist < 14)
                {
                    // Smooth edge
                    float alpha = 1f - Mathf.Clamp01((dist - 10f) / 4f);
                    colors[y * 32 + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    colors[y * 32 + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        
        return texture;
    }
    
    // Public method to test the particles
    public void PlayTestBurst()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            Debug.Log("Test burst played!");
        }
    }
    
    // Visualize in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spreadRadius);
    }
}