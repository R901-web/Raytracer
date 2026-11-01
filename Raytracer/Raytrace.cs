using ComputeSharp;
using System;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;

namespace Raytracer;

[SupportedOSPlatform("windows")]
[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
[GeneratedComputeShaderDescriptor]
public readonly partial struct Raytrace(Camera c, Quality q, ReadOnlyBuffer<BVHNode> BVHs, ReadOnlyBuffer<Triangle> triangles, ReadOnlyBuffer<Material> materials, ReadWriteTexture2D<float4> output) : IComputeShader
{
    public void Execute()
    {
        int x = ThreadIds.X;
        int y = ThreadIds.Y;
        uint seed = (uint)((x * 98321832) + (y * 23998312) + (q.batchesDone * 19829831) + 987987);
        seed ^= seed << 13;

        int reps = Hlsl.Min(q.raysPerPixel - (q.batchesDone * q.batchSize), q.batchSize);
        float3 colour = float3.Zero;
        for (int i = 0; i < reps; i++)
        {
            float px = ((x + randFloat(ref seed)) / q.width * 2f) - 1f; //anti-aliasing
            float py = 1f - ((y + randFloat(ref seed)) / q.height * 2f);
            float3 dir = Hlsl.Normalize(c.forward + (c.right * (px * q.aspect * c.scale)) + (c.up * (py * c.scale))); //to prevent circle artifact -> look at error shader, no. of normalize change error

            Ray ray = new Ray(c.pos, dir);
            colour += traceRay(ray, ref seed);
        }

        output[x, y] += new float4(colour, 1);
    }

    public float3 traceRay(Ray ray, ref uint seed)
    {
        for (int i = 0; i < q.bouncePerRay; i++)
        {
            HitInfo hit = rayScene(ray);
            if (hit.dist == float.MaxValue) 
            {
                //float3 skyColour = Hlsl.Lerp(0.9f * float3.One, new float3(0.27f, 0.67f, 0.91f), (ray.dir.Z + 1) / 2);
                //ray.radiance += skyColour * ray.throughput;
                break; 
            }

            ray.orig = hit.hitPos + hit.normal * 1e-6f;
            bool isCoat = randFloat(ref seed) <= hit.material.coatOpaque;

#warning do explicit microfacet simulation?
            float3 diffuseDir = randHemisphere(hit.normal, ref seed);
            float3 specDir = reflect(ray.dir, hit.normal);
            ray.dir = Hlsl.Normalize(Hlsl.Lerp(specDir, diffuseDir, isCoat ? hit.material.coatRoughness : hit.material.roughness));

            ray.radiance += hit.material.emission * ray.throughput;
            ray.throughput *= isCoat ? hit.material.coatAlbedo : hit.material.albedo;

        }
        return ray.radiance;
    }

    public HitInfo rayScene(Ray ray)
    {
        HitInfo sceneHit = new HitInfo();
        BVHArr stack = new BVHArr();
        int top = 0;
        if (rayAABB(ray, BVHs[0].bbox) < float.MaxValue) { stack.set(top++, 0); }
        while (top != 0)
        {
            BVHNode node = BVHs[stack.get(--top)];
            if (node.triCount > -1) //leaf node
            {
                for (int i = 0; i < node.triCount; i++)
                {
                    HitInfo hit = rayTriangle(ray, triangles[node.index + i]);
                    if (hit.dist < sceneHit.dist) { sceneHit = hit; }
                }
            }
            else
            {
                BVHNode l = BVHs[node.index];
                BVHNode r = BVHs[node.index + 1];
                float lDist = rayAABB(ray, l.bbox);
                float rDist = rayAABB(ray, r.bbox);
                if (lDist > rDist)
                {
                    if (lDist < sceneHit.dist) { stack.set(top++, node.index); } //max value of sceneHit.dist = float.maxValue -> this also checks if it hit
                    if (rDist < sceneHit.dist) { stack.set(top++, node.index + 1); }
                }
                else
                {
                    if (rDist < sceneHit.dist) { stack.set(top++, node.index + 1); }
                    if (lDist < sceneHit.dist) { stack.set(top++, node.index); }
                }
            }
        }
        return sceneHit;
    }

    #region Ray Intersection
    public float rayAABB(Ray ray, BBox bbox)
    {
        float3 min = bbox.min;
        float3 max = bbox.max;
        float tMin = 1e-6f;
        float tMax = float.MaxValue;
        // X axis
        if (ray.dir.X == 0) { if (ray.orig.X < min.X || ray.orig.X > max.X) { return float.MaxValue; } }
        else
        {
            float inv = 1.0f / ray.dir.X;
            float t1 = (min.X - ray.orig.X) * inv;
            float t2 = (max.X - ray.orig.X) * inv;
            if (t1 > t2)
            {
                float temp = t1;
                t1 = t2;
                t2 = temp;
            }
            tMin = Hlsl.Max(tMin, t1);
            tMax = Hlsl.Min(tMax, t2);
            if (tMin > tMax) { return float.MaxValue; }
        }
        // Y axis
        if (ray.dir.Y == 0) { if (ray.orig.Y < min.Y || ray.orig.Y > max.Y) { return float.MaxValue; } }
        else
        {
            float inv = 1.0f / ray.dir.Y;
            float t1 = (min.Y - ray.orig.Y) * inv;
            float t2 = (max.Y - ray.orig.Y) * inv;
            if (t1 > t2)
            {
                float temp = t1;
                t1 = t2;
                t2 = temp;
            }
            tMin = Hlsl.Max(tMin, t1);
            tMax = Hlsl.Min(tMax, t2);
            if (tMin > tMax) { return float.MaxValue; }
        }
        // Z axis
        if (ray.dir.Z == 0) { if (ray.orig.Z < min.Z || ray.orig.Z > max.Z) { return float.MaxValue; } }
        else
        {
            float inv = 1.0f / ray.dir.Z;
            float t1 = (min.Z - ray.orig.Z) * inv;
            float t2 = (max.Z - ray.orig.Z) * inv;
            if (t1 > t2)
            {
                float temp = t1;
                t1 = t2;
                t2 = temp;
            }
            tMin = Hlsl.Max(tMin, t1);
            tMax = Hlsl.Min(tMax, t2);
            if (tMin > tMax) { return float.MaxValue; }
        }

        return tMin;
    }

    private HitInfo rayTriangle(Ray r, Triangle t)
    {
        float3 edge1 = t.B - t.A;
        float3 edge2 = t.C - t.A;

        float3 pvec = Hlsl.Cross(r.dir, edge2);
        float det = Hlsl.Dot(pvec, edge1);
        if (det <= 1e-6f) { return new HitInfo(); } //ray parallel triangle -> backface culling

        //checking if within bounds
        float invDet = 1.0f / det; //faster to multiply than divide
        float3 tvec = r.orig - t.A;
        float u = Hlsl.Dot(tvec, pvec) * invDet;
        if (u < 0.0f || u > 1.0f) { return new HitInfo(); }
        float3 qvec = Hlsl.Cross(tvec, edge1);
        float v = Hlsl.Dot(r.dir, qvec) * invDet;
        if (v < 0.0f || u + v > 1.0f) { return new HitInfo(); }

        float dist = Hlsl.Dot(edge2, qvec) * invDet;
        if (dist <= 1e-6f) { return new HitInfo(); } //prevent behind ray or self intersect

        float3 hitPoint = r.orig + (r.dir * dist);

        return new HitInfo(dist, hitPoint, t.normal, materials[t.materialID]);
    }
    #endregion

    #region Random + Reflect
    public float randFloat(ref uint seed) //PCG
    {
        seed = (seed * 747796405) + 2891336453;
        uint result = ((seed >> ((int)(seed >> 28) + 4)) ^ seed) * 277803737;
        result ^= result >> 22;
        return (float)result / uint.MaxValue;
    }

    public float3 randHemisphere(float3 centre, ref uint seed)
    {
        float r1 = randFloat(ref seed);
        float r2 = randFloat(ref seed);

        float radius = Hlsl.Sqrt(r1);
        float phi = 2 * 3.1415926535f * r2;

        float x = radius * Hlsl.Cos(phi);
        float y = radius * Hlsl.Sin(phi);
        float z = Hlsl.Sqrt(Hlsl.Max(0.0f, 1.0f - r1));

        float3 helper = Hlsl.Abs(centre.Z) < 0.999f ? new float3(0, 0, 1) : new float3(0, 1, 0);
        float3 tangent = Hlsl.Normalize(Hlsl.Cross(helper, centre));
        float3 bitangent = Hlsl.Cross(centre, tangent);

        return Hlsl.Normalize((tangent * x) + (bitangent * y) + (centre * z));
    }

    public float3 reflect(float3 direction, float3 normal)
    {
        return direction - 2 * Hlsl.Dot(direction, normal) * normal;
    }
    #endregion
}

[SupportedOSPlatform("windows")]
[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
[GeneratedComputeShaderDescriptor]
public readonly partial struct ToImage(Quality q, ReadWriteTexture2D<float4> input, ReadWriteTexture2D<Rgba32, float4> output) : IComputeShader
{
    public void Execute()
    {
        int x = ThreadIds.X;
        int y = ThreadIds.Y;
        output[x, y] = new float4(input[x, y].XYZ * 1f / q.raysPerPixel, 1);
    }
}
