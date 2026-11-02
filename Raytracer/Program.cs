using ComputeSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.Versioning;

namespace Raytracer;

[SupportedOSPlatform("windows")]
public class Program
{
    static void Main(string[] args)
    {
        Stopwatch s = Stopwatch.StartNew();

        Camera c = new Camera(new float3(0, -5, 0.5f), new float3(0, 1, 0.1f), 45);
        Quality q = new Quality(8, 4, Aspect.HD, 200, 10, 5);

        MeshLoader loader = new MeshLoader(q);
        Material lightSquare = new Material(1, 0.95f * Vector3.One, Vector3.Zero, 0.25f, 0, Vector3.One);
        Material darkSquare = new Material(1, 0.05f * Vector3.One, Vector3.Zero, 0.25f, 0, Vector3.One);
        Material whitePiece = new Material(1, 0.75f * Vector3.One, Vector3.Zero, 0.05f, 0, Vector3.One);
        Material blackPiece = new Material(1, 0.25f * Vector3.One, Vector3.Zero, 0.05f, 0, Vector3.One);
        Material light = new Material(1, Vector3.One, 5 * Vector3.One);

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                float3 position = new float3(-3.5f + i, 3.5f - j, 0f);
                loader.loadPlate(false, (i + j) % 2 == 0 ? darkSquare : lightSquare, Matrix4x4.CreateTranslation(position));
            }
        }

        loader.loadSTL("..\\..\\..\\Models\\rook.stl", blackPiece, new float3(3.5f, 3.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\king.stl", blackPiece, new float3(3.5f, 1.5f, 0f), new float3(0, 0, 90f), 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\rook.stl", blackPiece, new float3(3.5f, -3.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\pawn.stl", blackPiece, new float3(2.5f, 3.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\knight.stl", blackPiece, new float3(2.5f, 0.5f, 0f), new float3(0, 0, -90), 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\bishop.stl", blackPiece, new float3(2.5f, -0.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\pawn.stl", blackPiece, new float3(2.5f, -2.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\bishop.stl", blackPiece, new float3(1.5f, 1.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\pawn.stl", blackPiece, new float3(1.5f, -3.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\pawn.stl", blackPiece, new float3(0.5f, 2.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\knight.stl", blackPiece, new float3(0.5f, 0.5f, 0f), new float3(0, 0, -90), 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\pawn.stl", blackPiece, new float3(0.5f, -1.5f, 0f), float3.Zero, 0.03f);

        loader.loadSTL("..\\..\\..\\Models\\pawn.stl", whitePiece, new float3(-0.5f, 1.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\pawn.stl", whitePiece, new float3(-0.5f, 0.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\queen.stl", whitePiece, new float3(-1.5f, 0.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\knight.stl", whitePiece, new float3(-1.5f, -1.5f, 0f), new float3(0, 0, 90), 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\bishop.stl", whitePiece, new float3(-1.5f, -2.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\pawn.stl", whitePiece, new float3(-2.5f, 2.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\pawn.stl", whitePiece, new float3(-2.5f, -1.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\pawn.stl", whitePiece, new float3(-2.5f, -2.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\pawn.stl", whitePiece, new float3(-2.5f, -3.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\rook.stl", whitePiece, new float3(-3.5f, 3.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\Models\\king.stl", whitePiece, new float3(-3.5f, -2.5f, 0f), float3.Zero, 0.03f);

        loader.loadSphere(3, light, Matrix4x4.CreateScale(75) * Matrix4x4.CreateTranslation(125 * Vector3.UnitZ));
        loader.buildBVH();

        Console.WriteLine($"Meshes: {loader.materials.Count}"); //1 material/mesh
        Console.WriteLine($"Triangles: {loader.triangles.Count}");
        Console.WriteLine($"Time: {(float)s.ElapsedMilliseconds / 1000} s\n");

        using GraphicsDevice gpu = GraphicsDevice.GetDefault();
        using ReadOnlyBuffer<BVHNode> BVHs = gpu.AllocateReadOnlyBuffer(loader.BVHs.ToArray());
        using ReadOnlyBuffer<Triangle> triangles = gpu.AllocateReadOnlyBuffer(loader.triangles.ToArray());
        using ReadOnlyBuffer<Material> materials = gpu.AllocateReadOnlyBuffer(loader.materials.ToArray());

        using ReadWriteTexture2D<float4> accumulate = gpu.AllocateReadWriteTexture2D<float4>(q.width, q.height);
        using ReadWriteTexture2D<Rgba32, float4> picture = gpu.AllocateReadWriteTexture2D<Rgba32, float4>(q.width, q.height);

        for (int i = 0; i < q.raysPerPixel; i += q.batchSize)
        {
            gpu.For(q.width, q.height, new Raytrace(c, q, BVHs, triangles, materials, accumulate));
            q.batchesDone++;
            Console.WriteLine($"Completed: {q.batchesDone * q.batchSize * 100f / q.raysPerPixel}%");
            Console.WriteLine($"Time: {(float)s.ElapsedMilliseconds / 1000}s\n");
        }
        gpu.For(q.width, q.height, new ToImage(q, accumulate, picture));
        picture.Save("raytrace.png");

        s.Stop();        
    }
}
