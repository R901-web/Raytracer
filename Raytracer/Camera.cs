using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.AccessControl;
using System.Text;

namespace Raytracer;

public enum Aspect 
{ 
    HD, FHD, QHD, UHD, //16:9
    WHD, WFHD, WQHD, WUHD //21:9
}

public struct Camera
{
    public float3 pos;
    public float3 forward;
    public float3 right;
    public float3 up; //forward cross left = up

    public float scale; //from vertical FOV

    public Camera(float3 pos, float3 forward, float fov)
    {
        this.pos = pos;
        this.forward = forward;

        fov *= MathF.PI * 0.0055555556f;
        scale = MathF.Tan(fov * 0.5f);

        this.forward = Vector3.Normalize(forward);
        bool degenerate = 1 - MathF.Abs(Vector3.Dot(this.forward, Vector3.UnitZ)) <= 1e-6f;
        right = degenerate ? -Vector3.UnitY : Vector3.Normalize(Vector3.Cross(this.forward, Vector3.UnitZ));
        up = Vector3.Normalize(Vector3.Cross(right, this.forward));
    }
}

public struct Quality
{
    public int binsPerAxis;
    public int trisPerLeaf;

    public int width;
    public int height;
    public float aspect;

    public int raysPerPixel;
    public int bouncePerRay;
    public int batchSize;
    public int batchesDone;

    private Quality(int binsPerAxis, int trisPerLeaf, int raysPerPixel, int bouncePerRay, int batchSize)
    {
        this.binsPerAxis = binsPerAxis;
        this.trisPerLeaf = trisPerLeaf;
        this.raysPerPixel = raysPerPixel;
        this.bouncePerRay = bouncePerRay;
        this.batchSize = batchSize;
        batchesDone = 0;
    }

    public Quality(int binsPerAxis, int trisPerLeaf, int width, int height, int raysPerPixel, int bouncePerRay, int batchSize) : this(binsPerAxis, trisPerLeaf, raysPerPixel, bouncePerRay, batchSize)
    {
        this.width = width;
        this.height = height;
        aspect = (float) width / height;
    }

    public Quality(int binsPerAxis, int trisPerLeaf, Aspect aspect, int raysPerPixel, int bouncePerRay, int batchSize) : this(binsPerAxis, trisPerLeaf, raysPerPixel, bouncePerRay, batchSize)
    {
        switch (aspect)
        {
            case Aspect.HD: width = 1280; height = 720; break;
            case Aspect.FHD: width = 1920; height = 1080; break;
            case Aspect.QHD: width = 2560; height = 1440; break;
            case Aspect.UHD: width = 3840; height = 2160; break;

            case Aspect.WHD: width = 1680; height = 720; break;
            case Aspect.WFHD: width = 2520; height = 1080; break;
            case Aspect.WQHD: width = 3360; height = 1440; break;
            case Aspect.WUHD: width = 5040; height = 2160; break;
        }
        this.aspect = (float) width / height;
    }
}
