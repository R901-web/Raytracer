using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace RaytracerOld;

[SupportedOSPlatform("windows")]
public struct Camera
{
    public float3 pos;

    public float3 forward;
    public float3 right;
    public float3 up;

    public float fov; //in radians
    public int width;
    public int height;

    public int raysPerPixel;
    public int bouncePerRay;
    public int batchSize; //ray/pixel every time call compute shader
    public int batchesDone;

    /* World Coords
     * X out of page
     * Y to the right
     * Z upwards
     */

    #region Constructors
    public Camera(float3 pos, float3 forward, float fov, int width, int height, int raysPerPixel, int bouncePerRay, int batchSize)
    {
        this.pos = pos;
        this.fov = fov * MathF.Tau / 360;
        this.width = width;
        this.height = height;
        this.bouncePerRay = bouncePerRay;
        this.raysPerPixel = raysPerPixel;
        this.batchSize = batchSize;
        this.batchesDone = 0;

        this.forward = Vector3.Normalize(forward);
        right = forward.X == 0 && forward.Y == 0 ? -Vector3.UnitY : Vector3.Normalize(Vector3.Cross(forward, float3.UnitZ)); //if parallel to up -> as if originally in x dirn
        up = Vector3.Normalize(Vector3.Cross(right, forward));
    }
    public Camera(float3 forward, float fov, int width, int height, int raysPerPixel, int bouncePerRay, int batchSize) : this(float3.Zero, forward, fov, width, height, raysPerPixel, bouncePerRay, batchSize) { }

    public Camera(float3 pos, float3 forward, float fov, int preset) : this(pos, forward, fov, 0, 0, 12, 5, 12)
    {
        int w = 0, h = 0;
        if (preset == 1) { w = 1280; h = 720; } //HD
        else if (preset == 2) { w = 1920; h = 1080; } //Full HD
        else if (preset == 3) { w = 2560; h = 1440; } //2K

        width = w; //if invalid -> set resolution to 0, 0
        height = h;
    }
    public Camera(float3 forward, float fov, int preset) : this(float3.Zero, forward, fov, preset) { }
    #endregion

    #region Rotation
    private static float3 rotateVec(float3 v, float3 axis, float angle) //rodrigues rotation formula
    {
        float c = MathF.Cos(angle);
        float s = MathF.Sin(angle);
        return ((Vector3)v * c) + (Vector3.Cross(axis, v) * s) + ((Vector3)axis * Vector3.Dot(axis, v) * (1 - c));
    }

    public void rotate(float yaw, float pitch, float roll)
    {
        yaw *= MathF.PI / 180f; //to radians
        pitch *= MathF.PI / 180f;
        roll *= MathF.PI / 180f;

        forward = rotateVec(forward, up, yaw); //yaw
        right = rotateVec(right, up, yaw);
        forward = rotateVec(forward, right, pitch); //pitch
        up = rotateVec(up, right, pitch);
        right = rotateVec(right, forward, roll); //roll
        up = rotateVec(up, forward, roll);

        //orthogonalize basis vectors
        forward = Vector3.Normalize(forward);
        right = Vector3.Normalize(Vector3.Cross(forward, up));
        up = Vector3.Normalize(Vector3.Cross(right, forward));
    }
    #endregion
}
