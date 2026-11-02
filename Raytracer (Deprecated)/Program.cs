using ComputeSharp;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static System.Net.Mime.MediaTypeNames;

namespace RaytracerOld;

[SupportedOSPlatform("windows")]
public class Program
{
    private static void Main(string[] args)
    {
        Stopwatch s = Stopwatch.StartNew();
        //Camera c = new Camera(new float3(-12f, 0, 3.125f), new float3(1, 0, -0.2f), 30, 1);
        //Camera c = new Camera(new float3(0, 0, 18), new float3(0, 0, -1), 30, 1);
        //Camera c = new Camera(new float3(-4, 0.5f, 0.5f), new float3(1, 0, 0.1f), 45, 1);
        Camera c = new Camera(new float3(0, -5, 0.5f), new float3(0, 1, 0.1f), 45, 1);
        c.bouncePerRay = 1;
        c.raysPerPixel = 5;
        c.batchSize = Math.Min(c.raysPerPixel, 5);

        MeshLoader loader = new MeshLoader();
        // Materials
        MaterialInfo lightSquare = new MaterialInfo(new float3(0.95f, 0.95f, 0.95f), float3.Zero, 1f, 0.25f, float3.One, 0);
        MaterialInfo darkSquare = new MaterialInfo(new float3(0.05f, 0.05f, 0.05f), float3.Zero, 1f, 0.25f, float3.One, 0);
        MaterialInfo whitePiece = new MaterialInfo(new float3(0.75f, 0.75f, 0.75f), float3.Zero, 1f, 0.05f, float3.One, 0);
        MaterialInfo blackPiece = new MaterialInfo(new float3(0.25f, 0.25f, 0.25f), float3.Zero, 1f, 0.05f, float3.One, 0);
        MaterialInfo light = new MaterialInfo(float3.Zero, new float3(5f, 5f, 5f), 1f, 0, float3.Zero, 0);

        MaterialInfo red = new MaterialInfo(new float3(1, 0, 0), float3.Zero, 1f, 0, float3.Zero, 0);
        MaterialInfo green = new MaterialInfo(new float3(0, 1, 0), float3.Zero, 1f, 0, float3.Zero, 0);
        MaterialInfo blue = new MaterialInfo(new float3(0, 0, 1), float3.Zero, 1f, 0, float3.Zero, 0);
        MaterialInfo dimLight = new MaterialInfo(float3.Zero, new float3(2.5f, 2.5f, 1.75f), 1f, 0, float3.Zero, 0);
        MaterialInfo mirror = new MaterialInfo(new float3(0.7f, 0.7f, 0.7f), float3.Zero, 0.15f, 0, float3.Zero, 0);

        /*
        loader.loadPlate(5f, 8f, false, white, new float3(8.5f, 0f, 0f), new float3(0f, -90f, 0f)); //back wall
        loader.loadPlate(5f, 5f, false, red, new float3(6f, 4f, 0f), new float3(90f, 0f, 0f)); //left wall
        loader.loadPlate(5f, 5f, false, green, new float3(6f, -4f, 0f), new float3(-90f, 0f, 0f)); //right wall
        loader.loadPlate(5f, 8f, false, blue, new float3(6f, 0f, -2.5f), float3.Zero); //floor
        loader.loadPlate(5f, 8f, false, white, new float3(6f, 0f, 2.5f), new float3(180f, 0f, 0f)); //ceiling
        loader.loadPlate(2f, 3f, false, dimLight, new float3(6f, 0f, 2.3f), new float3(180f, 0f, 0f)); //light
        loader.loadPlate(5f, 8f, false, white, new float3(3.5f, 0, 0), new float3(0f, 90f, 0f)); //front wall
        */

        /*
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\pawn.stl", white, new float3(6f, 3.214f, -2.5f), float3.Zero, 0.045f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\rook.stl", white, new float3(6f, 1.929f, -2.5f), float3.Zero, 0.045f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\knight.stl", white, new float3(6f, 0.643f, -2.5f), float3.Zero, 0.045f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\bishop.stl", white, new float3(6f, -0.643f, -2.5f), float3.Zero, 0.045f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\queen.stl", white, new float3(6f, -1.929f, -2.5f), float3.Zero, 0.045f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\king.stl", white, new float3(6f, -3.214f, -2.5f), float3.Zero, 0.045f);
        */

#warning spotlight with 1 strong light + planes around?
        //loader.loadPlate(2, 2, true, red, new float3(3.5f, 3.5f, 0.1f), float3.Zero);
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                float3 position = new float3(-3.5f + i, 3.5f - j, 0f);
                loader.loadPlate(1, 1, false, (i + j) % 2 == 0 ? darkSquare : lightSquare, position, float3.Zero);
            }
        }
        loader.loadBox(0.2f, 8.4f, 0.2f, darkSquare, new float3(-4.1f, 0, 0f), float3.Zero);
        loader.loadBox(0.2f, 8.4f, 0.2f, darkSquare, new float3(4.1f, 0, 0f), float3.Zero);
        loader.loadBox(8.4f, 0.2f, 0.2f, darkSquare, new float3(0, -4.1f, 0), float3.Zero);
        loader.loadBox(8.4f, 0.2f, 0.2f, darkSquare, new float3(0, 4.1f, 0f), float3.Zero);

        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\rook.stl", blackPiece, new float3(3.5f, 3.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\king.stl", blackPiece, new float3(3.5f, 1.5f, 0f), new float3(0, 0, 90f), 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\rook.stl", blackPiece, new float3(3.5f, -3.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\pawn.stl", blackPiece, new float3(2.5f, 3.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\knight.stl", blackPiece, new float3(2.5f, 0.5f, 0f), new float3(0, 0, -90), 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\bishop.stl", blackPiece, new float3(2.5f, -0.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\pawn.stl", blackPiece, new float3(2.5f, -2.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\bishop.stl", blackPiece, new float3(1.5f, 1.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\pawn.stl", blackPiece, new float3(1.5f, -3.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\pawn.stl", blackPiece, new float3(0.5f, 2.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\knight.stl", blackPiece, new float3(0.5f, 0.5f, 0f), new float3(0, 0, -90), 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\pawn.stl", blackPiece, new float3(0.5f, -1.5f, 0f), float3.Zero, 0.03f);

        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\pawn.stl", whitePiece, new float3(-0.5f, 1.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\pawn.stl", whitePiece, new float3(-0.5f, 0.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\queen.stl", whitePiece, new float3(-1.5f, 0.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\knight.stl", whitePiece, new float3(-1.5f, -1.5f, 0f), new float3(0, 0, 90), 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\bishop.stl", whitePiece, new float3(-1.5f, -2.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\pawn.stl", whitePiece, new float3(-2.5f, 2.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\pawn.stl", whitePiece, new float3(-2.5f, -1.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\pawn.stl", whitePiece, new float3(-2.5f, -2.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\pawn.stl", whitePiece, new float3(-2.5f, -3.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\rook.stl", whitePiece, new float3(-3.5f, 3.5f, 0f), float3.Zero, 0.03f);
        loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\king.stl", whitePiece, new float3(-3.5f, -2.5f, 0f), float3.Zero, 0.03f);

        loader.loadSphere(75, 10, light, new float3(0, 0, 125f));

        //loader.loadBox(2, 2, 3, white, new float3(5.25f, -2f, -1f), new float3(0, 0, -30f));
        //loader.loadBox(2, 5, 2, white, new float3(6f, 1f, -1.5f), new float3(0, 0, 22.5f));

        //loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\dogSit.stl", mirror, new float3(4.75f, 1.5f, -2.5f), new float3(0, 0, -45f), 0.05f);
        //loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\dog.stl", mirror, new float3(4.75f, -1.5f, -2.5f), new float3(0, 0, -135f), 0.05f);

        //loader.loadSTL("..\\..\\..\\..\\Raytracer\\Models\\stanford.stl", mirror, new float3(6, 0, -2.5f), new float3(0, 0, -90), 0.05f);


        Console.WriteLine($"Meshes: {loader.meshes.Count}");
        Console.WriteLine($"Triangles: {loader.triangles.Count}");
        Console.WriteLine($"Time: {(float)s.ElapsedMilliseconds / 1000} s\n");
        int minTris = int.MaxValue;
        int maxTris = int.MinValue;
        foreach (BVHNode node in loader.BVHs)
        {
            maxTris = Math.Max(node.triCount, maxTris);
            minTris = Math.Min(node.triCount != 0 ? node.triCount : minTris, minTris);
        }
        Console.WriteLine(minTris);
        Console.WriteLine(maxTris);
        Console.WriteLine(loader.BVHs.Count);

        using GraphicsDevice gpu = GraphicsDevice.GetDefault();

        using ReadOnlyBuffer<Triangle> scene = gpu.AllocateReadOnlyBuffer<Triangle>(loader.triangles.ToArray());
        using ReadOnlyBuffer<BVHNode> BVHs = gpu.AllocateReadOnlyBuffer(loader.BVHs.ToArray());
        using ReadOnlyBuffer<Mesh> meshes = gpu.AllocateReadOnlyBuffer(loader.meshes.ToArray());
        using ReadWriteTexture2D<float4> accumulate = gpu.AllocateReadWriteTexture2D<float4>(c.width, c.height);
        using ReadWriteTexture2D<Rgba32, float4> rayTrace = gpu.AllocateReadWriteTexture2D<Rgba32, float4>(c.width, c.height);

        for (int i = 0; i < c.raysPerPixel; i += c.batchSize)
        {
            gpu.For(c.width, c.height, new RayTrace(c, scene, BVHs, meshes, accumulate));
            c.batchesDone++;
            Console.WriteLine($"Completed: {c.batchesDone * c.batchSize * 100f / c.raysPerPixel}%");
            Console.WriteLine($"Time: {(float)s.ElapsedMilliseconds / 1000}s\n");
        }
        gpu.For(c.width, c.height, new ToImage(1f / c.raysPerPixel, accumulate, rayTrace));


        Console.WriteLine($"Time: {(float)s.ElapsedMilliseconds / 1000} s");
        s.Stop();
        rayTrace.Save("raytrace.png");
    }
}
