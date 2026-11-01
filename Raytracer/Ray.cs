using ComputeSharp;

namespace Raytracer;

public struct Ray
{
    public float3 orig; //for homogeneous coords
    public float3 dir;
    public float3 throughput;
    public float3 radiance;

    public Ray(float3 orig, float3 dir)
    {
        this.orig = orig;
        this.dir = Hlsl.Normalize(dir);
        throughput = float3.One;
        radiance = float3.Zero;
    }
}

public struct HitInfo
{
    public float dist;
    public float3 hitPos;
    public float3 normal;
    public Material material;

    public HitInfo()
    {
        dist = float.MaxValue;
        hitPos = float3.Zero;
        normal = float3.Zero;
        material = new Material();
    }

    public HitInfo(float dist, float3 hitPos, float3 normal, Material material)
    {
        this.dist = dist;
        this.hitPos = hitPos;
        this.normal = normal;
        this.material = material;
    }
}