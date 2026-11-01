using ComputeSharp;
using RaytracerOld;
using System.Runtime.Versioning;

namespace RaytracerOld;

[SupportedOSPlatform("windows")]
[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
[GeneratedComputeShaderDescriptor]
public readonly partial struct RayTrace : IComputeShader
{
    public readonly Camera camera;
    public readonly ReadOnlyBuffer<Triangle> scene;
    public readonly ReadOnlyBuffer<BVHNode> BVHs;
    public readonly ReadOnlyBuffer<Mesh> meshes;
    public readonly ReadWriteTexture2D<float4> texture;

    public RayTrace(Camera camera, ReadOnlyBuffer<Triangle> scene, ReadOnlyBuffer<BVHNode> BVHs, ReadOnlyBuffer<Mesh> meshes, ReadWriteTexture2D<float4> texture)
    {
        this.camera = camera;
        this.scene = scene;
        this.BVHs = BVHs;
        this.meshes = meshes;
        this.texture = texture;
    }

    public void Execute()
    {
        int x = ThreadIds.X;
        int y = ThreadIds.Y;
        uint seed = (uint)((x * 98321832) + (y * 23998312) + (camera.batchesDone * 19829831) + 987987);
        seed ^= seed << 13;

        float aspect = (float)camera.width / camera.height;
        float scale = Hlsl.Tan(camera.fov * 0.5f);

        int reps = Hlsl.Min(camera.raysPerPixel - (camera.batchesDone * camera.batchSize), camera.batchSize);
        float3 colour = float3.Zero;
        for (int i = 0; i < reps; i++)
        {
            float px = ((x + randFloat(ref seed)) / camera.width * 2f) - 1f; //anti-aliasing
            float py = 1f - ((y + randFloat(ref seed)) / camera.height * 2f);
            float3 dir = Hlsl.Normalize(camera.forward + (camera.right * (px * aspect * scale)) + (camera.up * (py * scale))); //to prevent circle artifact -> look at error shader, no. of normalize change error

            Ray ray = new Ray(camera.pos, dir);
            colour += traceRay(ray, ref seed);
        }

        texture[ThreadIds.X, ThreadIds.Y] += new float4(colour, 0);
    }

    private float3 traceRay(Ray ray, ref uint seed)
    {
        for (int i = 0; i <= camera.bouncePerRay; i++) //n bounce = n + 1 segments
        {
            HitInfo hit = rayCollide(ray);
            if (!hit.hit)
            {
                //float3 skyColour = Hlsl.Lerp(0.9f * float3.One, new float3(0.27f, 0.67f, 0.91f), (ray.dir.Z + 1) / 2);
                //ray.radiance += skyColour * ray.throughput;
                break;
            }
            ray.orig = hit.hitPos + (hit.normal * 1e-6f);
            bool isCoat = randFloat(ref seed) <= hit.material.coatOpaque; //1 -> completely opaque, 0 -> transparent

#warning change to explicit microfacet simulation? angle of microfacet from 2d gaussian distribution -> if reflected ray still pointing into object just keep on reflecting
            float3 diffuseDir = randHemisphere(hit.normal, ref seed); //cosine-weighted, because at angle rays are more spread out
            float3 specDir = reflect(ray.dir, hit.normal);
            ray.dir = Hlsl.Normalize(Hlsl.Lerp(specDir, diffuseDir, isCoat ? hit.material.coatRoughness : hit.material.roughness));

            ray.radiance += hit.material.emission * ray.throughput;
            ray.throughput *= isCoat ? hit.material.coatAlbedo : hit.material.albedo;
        }
        return ray.radiance;
    }

    private HitInfo rayCollide(Ray ray)
    {
        HitInfo sceneHit = new HitInfo();

        BVHArrPtr32 stack = new BVHArrPtr32();
        int top = 0;
        for (int i = 0; i < meshes.Length; i++)
        {
            stack.set(top++, meshes[i].BVHIndex);
            while (top != 0)
            {
                int meshID = stack.get(--top);
                BVHNode mesh = BVHs[meshID];

                if (mesh.triCount > 0) //leaf node
                {
                    for (int j = 0; j < mesh.triCount; j++)
                    {
                        HitInfo hit = rayTriangle(ray, scene[j + mesh.index]);
                        if ((bool)hit.hit && hit.dist < sceneHit.dist) { sceneHit = hit; }
                    }
                }
                else
                {
                    BVHNode left = BVHs[mesh.index];
                    BVHNode right = BVHs[mesh.index + 1];
                    float lDist = rayAABB(ray, left.min, left.max);
                    float rDist = rayAABB(ray, right.min, right.max);
                    if (lDist > rDist)
                    {
                        if (lDist < sceneHit.dist) { stack.set(top++, mesh.index); } //max value of sceneHit.dist = float.maxValue -> this also checks if it hit
                        if (rDist < sceneHit.dist) { stack.set(top++, mesh.index + 1); }
                    }
                    else
                    {
                        if (rDist < sceneHit.dist) { stack.set(top++, mesh.index + 1); }
                        if (lDist < sceneHit.dist) { stack.set(top++, mesh.index); }
                    }
                }
            }
        }
        return sceneHit;
    }

    private float3 reflect(float3 ray, float3 normal) //ray, normal both normalized
    {
        float3 proj = Hlsl.Dot(ray, normal) * normal; //dot < 0 -> point opp. normal -> into sphere
        return ray - (2 * proj);
    }

    private float3 randHemisphere(float3 centre, ref uint seed) //cosine-weighted about normal
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

    private float randFloat(ref uint seed) //0 to 1 -> its a PCG
    {
        seed = (seed * 747796405) + 2891336453;
        uint result = ((seed >> ((int)(seed >> 28) + 4)) ^ seed) * 277803737;
        result ^= result >> 22;
        return (float)result / uint.MaxValue;
    }

    private HitInfo rayTriangle(Ray r, Triangle t)
    {
        float3 edge1 = t.B - t.A;
        float3 edge2 = t.C - t.A;

        float3 pvec = Hlsl.Cross(r.dir, edge2);
        float det = Hlsl.Dot(pvec, edge1);
        if (det < 1e-6f) { return new HitInfo(); } //ray parallel triangle -> backface culling

        //checking if within bounds
        float invDet = 1.0f / det; //faster to multiply than divide
        float3 tvec = r.orig - t.A;
        float u = Hlsl.Dot(tvec, pvec) * invDet;
        if (u < 0.0f || u > 1.0f) { return new HitInfo(); }
        float3 qvec = Hlsl.Cross(tvec, edge1);
        float v = Hlsl.Dot(r.dir, qvec) * invDet;
        if (v < 0.0f || u + v > 1.0f) { return new HitInfo(); }

        float dist = Hlsl.Dot(edge2, qvec) * invDet;
        if (dist < 1e-6f) { return new HitInfo(); } //prevent behind ray or self intersect

        float3 hitPoint = r.orig + (r.dir * dist);

        return new HitInfo(dist, hitPoint, t.normal, meshes[t.meshID].material);
    }

    private float rayAABB(Ray ray, float3 min, float3 max)
    {
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
}

[SupportedOSPlatform("windows")]
[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
[GeneratedComputeShaderDescriptor]
public readonly partial struct ToImage(float invRPP, ReadWriteTexture2D<float4> accumulate, ReadWriteTexture2D<Rgba32, float4> output) : IComputeShader
{
    public void Execute()
    {
        int x = ThreadIds.X;
        int y = ThreadIds.Y;
        output[x, y] = new float4(accumulate[x, y].XYZ * invRPP, 1);
    }
}
