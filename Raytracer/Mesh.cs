using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Text;

namespace Raytracer;

public struct BVHNode 
{
    public BBox bbox;
    public int index; //childIndex or triIndex
    public int triCount;

    public BVHNode(BBox bbox, int index, int triCount)
    {
        this.bbox = bbox;
        this.index = index;
        this.triCount = triCount;
    }
}

public struct BVHArr
{
    #region Elements
    public int e0; public int e1; public int e2; public int e3;
    public int e4; public int e5; public int e6; public int e7;
    public int e8; public int e9; public int e10; public int e11;
    public int e12; public int e13; public int e14; public int e15;
    public int e16; public int e17; public int e18; public int e19;
    public int e20; public int e21; public int e22; public int e23;
    public int e24; public int e25; public int e26; public int e27;
    public int e28; public int e29; public int e30; public int e31;
    #endregion

    public int get(int index)
    {
        switch (index)
        {
            case 0: return e0; case 1: return e1;
            case 2: return e2; case 3: return e3;
            case 4: return e4; case 5: return e5;
            case 6: return e6; case 7: return e7;
            case 8: return e8; case 9: return e9;
            case 10: return e10; case 11: return e11;
            case 12: return e12; case 13: return e13;
            case 14: return e14; case 15: return e15;
            case 16: return e16; case 17: return e17;
            case 18: return e18; case 19: return e19;
            case 20: return e20; case 21: return e21;
            case 22: return e22; case 23: return e23;
            case 24: return e24; case 25: return e25;
            case 26: return e26; case 27: return e27;
            case 28: return e28; case 29: return e29;
            case 30: return e30; case 31: return e31;
            default: return -1;
        }
    }

    public void set(int index, int value)
    {
        switch (index)
        {
            case 0: e0 = value; break;
            case 1: e1 = value; break;
            case 2: e2 = value; break;
            case 3: e3 = value; break;
            case 4: e4 = value; break;
            case 5: e5 = value; break;
            case 6: e6 = value; break;
            case 7: e7 = value; break;
            case 8: e8 = value; break;
            case 9: e9 = value; break;
            case 10: e10 = value; break;
            case 11: e11 = value; break;
            case 12: e12 = value; break;
            case 13: e13 = value; break;
            case 14: e14 = value; break;
            case 15: e15 = value; break;
            case 16: e16 = value; break;
            case 17: e17 = value; break;
            case 18: e18 = value; break;
            case 19: e19 = value; break;
            case 20: e20 = value; break;
            case 21: e21 = value; break;
            case 22: e22 = value; break;
            case 23: e23 = value; break;
            case 24: e24 = value; break;
            case 25: e25 = value; break;
            case 26: e26 = value; break;
            case 27: e27 = value; break;
            case 28: e28 = value; break;
            case 29: e29 = value; break;
            case 30: e30 = value; break;
            case 31: e31 = value; break;
        }
    }

}

public struct BBox
{
    public float3 min;
    public float3 max;
    public BBox inverse { get { return new BBox(max, min); } }

    public BBox()
    {
        this.min = Vector3.One * float.MaxValue;
        this.max = Vector3.One * float.MinValue;
    }

    public BBox(float3 min, float3 max)
    {
        this.min = min;
        this.max = max;
    }
}

public struct Triangle
{
    public float3 A; 
    public float3 B;
    public float3 C;
    public float3 centre { get { return ((Vector3)A + (Vector3)B + (Vector3)C) * 0.33333333f; }  }
    public float3 normal;
    public int materialID;

    public Triangle(float3 A, float3 B, float3 C, int materialID)
    {
        this.A = A;
        this.B = B;
        this.C = C;
        this.materialID = materialID;

        Vector3 BA = (Vector3)A - (Vector3)B;
        Vector3 BC = (Vector3)C - (Vector3)B;
        normal = Vector3.Normalize(Vector3.Cross(BC, BA));
    }
}

public struct Material
{
    public float roughness;
    public float3 albedo;
    public float3 emission;
    public float coatOpaque;
    public float coatRoughness;
    public float3 coatAlbedo;

    public Material(float roughness, float3 albedo, float3 emission, float coatOpaque, float coatRoughness, float3 coatAlbedo)
    {
        this.roughness = roughness;
        this.albedo = albedo;
        this.emission = emission;
        this.coatOpaque = coatOpaque;
        this.coatRoughness = coatRoughness;
        this.coatAlbedo = coatAlbedo;
    }

    public Material(float roughness, float3 albedo, float3 emission) : this(roughness, albedo, emission, 0, 0, float3.Zero) { }
}
