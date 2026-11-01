using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace RaytracerOld;

public class MeshLoader
{
    public List<Triangle> triangles;
    public List<BVHNode> BVHs;
    public List<Mesh> meshes;
    public const int BVHDepth = 31; //32 - 1

    public MeshLoader()
    {
        triangles = new List<Triangle>();
        BVHs = new List<BVHNode>();
        meshes = new List<Mesh>();
    }

    public void loadSTL(string filename, MaterialInfo material, float3 position, float3 rotation, float scale)
    {
        if (scale <= 0) { throw new Exception(); }

        using BinaryReader r = new BinaryReader(File.OpenRead(filename));

        r.ReadBytes(80); //80 byte header
        uint triangleCount = r.ReadUInt32();
        triangles.EnsureCapacity(triangles.Count + (int)triangleCount);
        int startIndex = triangles.Count;
        float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
        float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);

        for (int i = 0; i < triangleCount; i++)
        {
            r.ReadSingle(); //normal -> self calculate
            r.ReadSingle();
            r.ReadSingle();

            float3 a = (Vector3)rotateVec(new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()) * scale, rotation) + (Vector3)position;
            float3 b = (Vector3)rotateVec(new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()) * scale, rotation) + (Vector3)position;
            float3 c = (Vector3)rotateVec(new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()) * scale, rotation) + (Vector3)position;
            triangles.Add(new Triangle(a, b, c, meshes.Count));

            min.X = MathF.Min(MathF.Min(min.X, a.X), MathF.Min(b.X, c.X));
            min.Y = MathF.Min(MathF.Min(min.Y, a.Y), MathF.Min(b.Y, c.Y));
            min.Z = MathF.Min(MathF.Min(min.Z, a.Z), MathF.Min(b.Z, c.Z));

            max.X = MathF.Max(MathF.Max(max.X, a.X), MathF.Max(b.X, c.X));
            max.Y = MathF.Max(MathF.Max(max.Y, a.Y), MathF.Max(b.Y, c.Y));
            max.Z = MathF.Max(MathF.Max(max.Z, a.Z), MathF.Max(b.Z, c.Z));

            r.ReadUInt16(); //attribute byte count -> doesnt do anything
        }

        meshes.Add(new Mesh(BVHs.Count, material));
        BVHs.Add(new BVHNode(startIndex, (int)triangleCount, min, max));
        addBVH(BVHs.Count - 1);
    }

    //start with icosahedron -> divide each face into resolution + 1 segments -> join new vertices -> inflate into sphere
    public void loadSphere(float radius, int resolution, MaterialInfo material, float3 position)
    {
        if (radius <= 0 || resolution < 0) { throw new Exception(); }
        float phi = (1 + MathF.Sqrt(5)) / 2;

        int triangleCount = 20 * (resolution + 1) * (resolution + 1);
        triangles.EnsureCapacity(triangles.Count + triangleCount);
        int startIndex = triangles.Count;

        float3[] origCorners = new float3[12] //12 corners, 20 faces, 30 edges
        {
            new float3(-1,  phi, 0), new float3(1,  phi, 0),
            new float3(-1, -phi, 0), new float3(1, -phi, 0),
            new float3(0, -1,  phi), new float3(0,  1,  phi),
            new float3(0, -1, -phi), new float3(0,  1, -phi),
            new float3(phi, 0, -1), new float3(phi, 0,  1),
            new float3(-phi, 0, -1), new float3(-phi, 0,  1)
        };
        for (int i = 0; i < 12; i++) { origCorners[i] = Vector3.Normalize(origCorners[i]); }
        int[] faces = new int[60] //3 vertex/face * 20 faces -> in ccw order so normals point out
        {
            0,11,5, 0,5,1,
            0,1,7, 0,7,10,
            0,10,11, 1,5,9,
            5,11,4, 11,10,2,
            10,7,6, 7,1,8,
            3,9,4, 3,4,2,
            3,2,6, 3,6,8,
            3,8,9, 4,9,5,
            2,4,11, 6,2,10,
            8,6,7, 9,8,1
        };

        for (int i = 0; i < faces.Length; i += 3)
        {
            float3 a = origCorners[faces[i]];
            float3 b = origCorners[faces[i + 1]];
            float3 c = origCorners[faces[i + 2]];

            float3[,] grid = new float3[resolution + 2, resolution + 2]; //new points from resolution + 2 endpoints
            for (int j = 0; j < resolution + 2; j++)
            {
                for (int k = 0; k < resolution + 2 - j; k++)
                {
                    float wa = (float)(resolution + 1 - j - k) / (resolution + 1);
                    float wb = (float)j / (resolution + 1);
                    float wc = (float)k / (resolution + 1);

                    float3 p = new float3((wa * a.X) + (wb * b.X) + (wc * c.X), (wa * a.Y) + (wb * b.Y) + (wc * c.Y), (wa * a.Z) + (wb * b.Z) + (wc * c.Z));
                    p = Vector3.Normalize(p);
                    grid[j, k] = ((Vector3)p * radius) + (Vector3)position;
                }
            }

            for (int j = 0; j < resolution + 1; j++)
            {
                for (int k = 0; k < resolution + 1 - j; k++)
                {
                    float3 v0 = grid[j, k];
                    float3 v1 = grid[j + 1, k];
                    float3 v2 = grid[j, k + 1];
                    triangles.Add(new Triangle(v0, v1, v2, meshes.Count));
                    if (j + k < resolution)
                    {
                        float3 v3 = grid[j + 1, k + 1];
                        triangles.Add(new Triangle(v1, v3, v2, meshes.Count));
                    }
                }
            }
        }

        float3 min = new float3(position.X - radius, position.Y - radius, position.Z - radius);
        float3 max = new float3(position.X + radius, position.Y + radius, position.Z + radius);

        meshes.Add(new Mesh(BVHs.Count, material));
        BVHs.Add(new BVHNode(startIndex, triangleCount, min, max));
        addBVH(BVHs.Count - 1);
    }

    public void loadPlate(float x, float y, bool doubleSide, MaterialInfo material, float3 position, float3 rotation) //default is normal face up (Z)
    {
        if (x <= 0 || y <= 0) { throw new Exception(); }

        float3 a = (Vector3)rotateVec(new float3(-x / 2, y / 2, 0), rotation) + (Vector3)position;
        float3 b = (Vector3)rotateVec(new float3(-x / 2, -y / 2, 0), rotation) + (Vector3)position;
        float3 c = (Vector3)rotateVec(new float3(x / 2, -y / 2, 0), rotation) + (Vector3)position;
        float3 d = (Vector3)rotateVec(new float3(x / 2, y / 2, 0), rotation) + (Vector3)position;
        float3 min = new float3(MathF.Min(MathF.Min(d.X, a.X), MathF.Min(b.X, c.X)), MathF.Min(MathF.Min(d.Y, a.Y), MathF.Min(b.Y, c.Y)), MathF.Min(MathF.Min(d.Z, a.Z), MathF.Min(b.Z, c.Z)));
        float3 max = new float3(MathF.Max(MathF.Max(d.X, a.X), MathF.Max(b.X, c.X)), MathF.Max(MathF.Max(d.Y, a.Y), MathF.Max(b.Y, c.Y)), MathF.Max(MathF.Max(d.Z, a.Z), MathF.Max(b.Z, c.Z)));
        int startIndex = triangles.Count;

        triangles.Add(new Triangle(a, b, c, meshes.Count));
        triangles.Add(new Triangle(a, c, d, meshes.Count));
        if (doubleSide)
        {
            triangles.Add(new Triangle(c, b, a, meshes.Count));
            triangles.Add(new Triangle(d, c, a, meshes.Count));
        }

        meshes.Add(new Mesh(BVHs.Count, material));
        BVHs.Add(new BVHNode(startIndex, doubleSide ? 4 : 2, min, max));
        addBVH(BVHs.Count - 1);
    }

    public void loadBox(float x, float y, float z, MaterialInfo material, float3 position, float3 rotation)
    {
        if (x <= 0 || y <= 0 || z <= 0) { throw new Exception(); }

        float3[] vertices = new float3[8]
        {
            new float3(-x / 2, -y / 2, -z / 2), // 0
            new float3(x / 2, -y / 2, -z / 2), // 1
            new float3(x / 2, y / 2, -z / 2), // 2
            new float3(-x / 2, y / 2, -z / 2), // 3
            new float3(-x / 2, -y / 2, z / 2), // 4
            new float3(x / 2, -y / 2, z / 2), // 5
            new float3(x / 2, y / 2, z / 2), // 6
            new float3(-x / 2, y / 2, z / 2)  // 7
        };
        float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
        float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);
        for (int i = 0; i < 8; i++)
        {
            vertices[i] = (Vector3)rotateVec(vertices[i], rotation) + (Vector3)position;
            min.X = MathF.Min(vertices[i].X, min.X);
            min.Y = MathF.Min(vertices[i].Y, min.Y);
            min.Z = MathF.Min(vertices[i].Z, min.Z);

            max.X = MathF.Max(vertices[i].X, max.X);
            max.Y = MathF.Max(vertices[i].Y, max.Y);
            max.Z = MathF.Max(vertices[i].Z, max.Z);
        }
        int[] indices = new int[36] //2 triangles/face * 6 faces * 3 points/triangle
        {
            0, 2, 1,  0, 3, 2, //-z
            4, 5, 6,  4, 6, 7, //z
            0, 1, 5,  0, 5, 4, //-y
            3, 7, 6,  3, 6, 2, //y
            0, 4, 7,  0, 7, 3, //-x
            1, 2, 6,  1, 6, 5 //x
        };
        int startIndex = triangles.Count;
        for (int i = 0; i < 36; i += 3) { triangles.Add(new Triangle(vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]], meshes.Count)); }

        meshes.Add(new Mesh(BVHs.Count, material));
        BVHs.Add(new BVHNode(startIndex, 12, min, max));
        addBVH(BVHs.Count - 1); //guarantee to be < 16 depth
    }

    private float3 rotateVec(float3 v, float3 rotation)
    {
        float3 rotateVec(float3 v, float3 axis, float angle) //rodrigues rotation formula
        {
            float c = MathF.Cos(angle);
            float s = MathF.Sin(angle);
            return ((Vector3)v * c) + (Vector3.Cross(axis, v) * s) + ((Vector3)axis * Vector3.Dot(axis, v) * (1 - c));
        }

        float3 x = new float3(1, 0, 0);
        float3 y = new float3(0, 1, 0);
        float3 z = new float3(0, 0, 1);
        rotation = (Vector3)rotation * MathF.PI / 180f; //degree to radian
        v = rotateVec(v, z, rotation.Z); //yaw
        v = rotateVec(v, y, rotation.Y); //pitch -> assume its pointing in x dirn
        v = rotateVec(v, x, rotation.X); //roll
        return v;
    }

    private void addBVH(int rootIndex, int depth = 0)
    {
        BVHNode root = BVHs[rootIndex];
        if (depth >= BVHDepth || root.triCount < 4) { return; } //2^31 ~ 2 bn bounding boxes, dont split if too small

        BVHNode left = new BVHNode(root.index, 0, root.max, root.min); //root index is still for triCount, not set to children yet
        BVHNode right = new BVHNode(root.index, root.triCount, root.max, root.min);

        (int axis, float split, float cost) = SAH(root);
        if (cost >= nodeCost(root)) { return; } //dont split

        for (int i = root.index; i < root.index + root.triCount; i++)
        {
            float centre = axis == 0 ? triangles[i].centre.X : axis == 1 ? triangles[i].centre.Y : triangles[i].centre.Z;
            if (centre < split) //partitioning -> quicksort
            {
                include(ref left, triangles[i]);
                int swapIndex = root.index + left.triCount;
                (triangles[swapIndex], triangles[i]) = (triangles[i], triangles[swapIndex]);
                left.triCount++;
                right.index++;
                right.triCount--;
            }
            else { include(ref right, triangles[i]); }
        }
        if (left.triCount == 0 || right.triCount == 0) { return; } //if either is empty -> this is a leaf

        int leftID = BVHs.Count;
        BVHs.Add(left);
        BVHs.Add(right);
        root.triCount = 0; //mark as non-leaf
        root.index = leftID; //if a root has child, change index from triIndex to childIndex
        BVHs[rootIndex] = root;
        addBVH(leftID, depth + 1);
        addBVH(leftID + 1, depth + 1);
    }

    private void include(ref BVHNode mesh, Triangle t)
    {
        void include(ref BVHNode mesh, float3 p)
        {
            mesh.min.X = MathF.Min(mesh.min.X, p.X);
            mesh.min.Y = MathF.Min(mesh.min.Y, p.Y);
            mesh.min.Z = MathF.Min(mesh.min.Z, p.Z);

            mesh.max.X = MathF.Max(mesh.max.X, p.X);
            mesh.max.Y = MathF.Max(mesh.max.Y, p.Y);
            mesh.max.Z = MathF.Max(mesh.max.Z, p.Z);
        }
        include(ref mesh, t.A);
        include(ref mesh, t.B);
        include(ref mesh, t.C);
    }

    private (int axis, float dist, float cost) SAH(BVHNode mesh)
    {
        float cost(BVHNode node, int axis, float dist)
        {
            BVHNode l = new BVHNode(0, 0, node.max, node.min);
            BVHNode r = new BVHNode(0, 0, node.max, node.min);
            for (int i = 0; i < node.triCount; i++)
            {
                Triangle t = triangles[node.index + i];
                float centre = axis == 0 ? t.centre.X : axis == 1 ? t.centre.Y : t.centre.Z;
                if (centre < dist) { l.triCount++; include(ref l, t); }
                else { r.triCount++; include(ref r, t); }
            }
            return nodeCost(l) + nodeCost(r);
        }

        float minCost = float.MaxValue;
        int axis = 0;
        float dist = 0;

        for (int i = 0; i < 3; i++)
        {
            float min = i == 0 ? mesh.min.X : i == 1 ? mesh.min.Y : mesh.min.Z;
            float max = i == 0 ? mesh.max.X : i == 1 ? mesh.max.Y : mesh.max.Z;
            for (int j = 0; j < 5; j++)
            {
                float split = min + (max - min) * (j + 1) / 6f;
                float splitCost = cost(mesh, i, split);
                if (splitCost < minCost)
                {
                    minCost = splitCost;
                    dist = split;
                    axis = i;
                }
            }
        }

        return (axis, dist, minCost);
    }

    private float nodeCost(BVHNode node)
    {
        float x = node.max.X - node.min.X; //dist measured from origin -> give plane perpendicular to axis at dist from origin
        float y = node.max.Y - node.min.Y;
        float z = node.max.Z - node.min.Z;
        return (x * y + x * z + y * z) * node.triCount; //1/2 SA * triCount
    }
}