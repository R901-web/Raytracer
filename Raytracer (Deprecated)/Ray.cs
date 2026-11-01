using ComputeSharp;
using RaytracerOld;
using System.Runtime.Versioning;

namespace RaytracerOld;

[SupportedOSPlatform("windows")]
public struct Ray
{
    public float3 orig;
    public float3 dir;
    public float3 throughput;
    public float3 radiance;

    public Ray(float3 origin, float3 direction)
    {
        this.orig = origin;
        this.dir = Hlsl.Normalize(direction); //used on GPU
        throughput = float3.One;
        radiance = float3.Zero;
    }
}

[SupportedOSPlatform("windows")]
public struct HitInfo
{
    public Bool hit;
    public float3 hitPos;
    public float dist;
    public float3 normal;
    public MaterialInfo material;

    public HitInfo()
    {
        hit = false;
        dist = float.MaxValue;
        hitPos = float.MaxValue * float3.One;
        normal = float3.Zero;
        material = new MaterialInfo();
    }

    public HitInfo(float t, float3 hitPoint, float3 normal, MaterialInfo material)
    {
        hit = true;
        this.dist = t;
        this.hitPos = hitPoint;
        this.normal = normal;
        this.material = material;
    }
}