using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;

namespace Raytracer;

public class MeshLoader //uses 1 global BVH
{
    public List<Triangle> triangles;
    public List<Material> materials;
    public List<BVHNode> BVHs;

    public Quality q;

    public MeshLoader(Quality q)
    {
        this.q = q;

        triangles = new List<Triangle>();
        materials = new List<Material>();
        BVHs = new List<BVHNode>();
    }

    #region Mesh Loading
    public void loadSTL(string filename, Material material, Matrix4x4 transform)
    {
        using BinaryReader r = new BinaryReader(File.OpenRead(filename));

        r.ReadBytes(80); //header
        int triCount = (int)r.ReadUInt32();
        triangles.EnsureCapacity(triangles.Count + triCount);

        for (int i = 0; i < triCount; i++)
        {
            r.ReadSingle(); r.ReadSingle(); r.ReadSingle(); //normals, will calculate by myself

            Vector3 A = Vector4.Transform(new Vector4(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), 1), transform).AsVector3();
            Vector3 B = Vector4.Transform(new Vector4(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), 1), transform).AsVector3();
            Vector3 C = Vector4.Transform(new Vector4(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), 1), transform).AsVector3();
            Triangle tri = new Triangle(A, B, C, materials.Count);

            triangles.Add(tri);
            r.ReadUInt16(); //attribute byte count -> useless
        }
        materials.Add(material);
    }

    public void loadSTL(string filename, Material material, Vector3 position, Vector3 rotation, float scale)
    {
        rotation *= MathF.PI / 180f;
        Matrix4x4 transform = Matrix4x4.Identity;
        transform *= Matrix4x4.CreateScale(scale);
        transform *= Matrix4x4.CreateRotationZ(rotation.Z);
        transform *= Matrix4x4.CreateRotationY(rotation.Y);
        transform *= Matrix4x4.CreateRotationX(rotation.X);
        transform *= Matrix4x4.CreateTranslation(position);
        loadSTL(filename, material, transform);

    }

    public void loadSphere(int res, Material material, Matrix4x4 transform) //default radius = 1
    {
        Triangle[] subdivide(Triangle t)
        {
            Vector3 midAB = Vector3.Normalize(((Vector3)t.A + (Vector3)t.B) / 2);
            Vector3 midAC = Vector3.Normalize(((Vector3)t.A + (Vector3)t.C) / 2);
            Vector3 midBC = Vector3.Normalize(((Vector3)t.B + (Vector3)t.C) / 2);
            Triangle[] triangles = new Triangle[4]
            {
                new Triangle(t.A, midAB, midAC, t.materialID),
                new Triangle(midAB, t.B, midBC, t.materialID),
                new Triangle(midAC, midBC, t.C, t.materialID),
                new Triangle(midAB, midBC, midAC, t.materialID)
            };
            return triangles;
        }

        if (res < 0) { throw new ArgumentException(); }
        float phi = (1 + MathF.Sqrt(5)) / 2;
        Vector3[] origCorners = new Vector3[12]
        {
            new Vector3(-1,  phi, 0), new Vector3(1,  phi, 0),
            new Vector3(-1, -phi, 0), new Vector3(1, -phi, 0),
            new Vector3(0, -1,  phi), new Vector3(0,  1,  phi),
            new Vector3(0, -1, -phi), new Vector3(0,  1, -phi),
            new Vector3(phi, 0, -1), new Vector3(phi, 0,  1),
            new Vector3(-phi, 0, -1), new Vector3(-phi, 0,  1)
        };
        int[,] origFaces = new int[20, 3]
        {
            { 0, 11, 5 }, { 0, 5, 1 }, { 0, 1, 7 }, { 0, 7, 10 },
            { 0, 10, 11 }, { 1, 5, 9 }, { 5, 11, 4 }, { 11, 10, 2 },
            { 10, 7, 6 }, { 7, 1, 8 }, { 3, 9, 4 }, { 3, 4, 2 },
            { 3, 2, 6 }, { 3, 6, 8 }, { 3, 8, 9 }, { 4, 9, 5 },
            { 2, 4, 11 }, { 6, 2, 10 }, { 8, 6, 7 }, { 9, 8, 1 }
        };
        List<Triangle> faces = new List<Triangle>();
        for (int i = 0; i < origCorners.Length; i++) { origCorners[i] = Vector3.Normalize(origCorners[i]); }
        for (int i = 0; i < origFaces.GetLength(0); i++)
        {
            Vector3 A = origCorners[origFaces[i, 0]];
            Vector3 B = origCorners[origFaces[i, 1]];
            Vector3 C = origCorners[origFaces[i, 2]];
            faces.Add(new Triangle(A, B, C, materials.Count));
        }
        for (int i = 0; i < res; i++)
        {
            List<Triangle> newFaces = new List<Triangle>();
            for (int j = 0; j < faces.Count; j++)
            {
                Triangle[] t = subdivide(faces[j]);
                newFaces.Add(t[0]); newFaces.Add(t[1]);
                newFaces.Add(t[2]); newFaces.Add(t[3]);
            }
            faces = newFaces;
        }
        for (int i = 0; i < faces.Count; i++)
        {
            Triangle t = faces[i];
            t.A = Vector3.Transform(t.A, transform);
            t.B = Vector3.Transform(t.B, transform);
            t.C = Vector3.Transform(t.C, transform);
            triangles.Add(t);
        }
        materials.Add(material);
    }

    public void loadPlate(bool doubleside, Material material, Matrix4x4 transform) //default = 1x1 facing up 
    {
        Vector3 A = Vector3.Transform(new Vector3(0.5f, 0.5f, 0), transform);
        Vector3 B = Vector3.Transform(new Vector3(-0.5f, 0.5f, 0), transform);
        Vector3 C = Vector3.Transform(new Vector3(-0.5f, -0.5f, 0), transform);
        Vector3 D = Vector3.Transform(new Vector3(0.5f, -0.5f, 0), transform);

        triangles.Add(new Triangle(A, B, D, materials.Count));
        triangles.Add(new Triangle(B, C, D, materials.Count));
        if (doubleside)
        {
            triangles.Add(new Triangle(D, B, A, materials.Count));
            triangles.Add(new Triangle(D, C, B, materials.Count));
        }
        materials.Add(material);
    }
    #endregion

    #region Include
    private void include(ref BBox bbox, Vector3 point)
    {
        bbox.max = Vector3.Max(bbox.max, point);
        bbox.min = Vector3.Min(bbox.min, point);
    }

    private void include(ref BBox bbox, Triangle tri)
    {
        include(ref bbox, tri.A);
        include(ref bbox, tri.B);
        include(ref bbox, tri.C);
    }

    private void include(ref BBox bbox, BBox other)
    {
        include(ref bbox, other.min);
        include(ref bbox, other.max);
    }
    #endregion

    #region BVH Building
    public void buildBVH() //for entire scene
    {
        BVHs.Clear();
        if (triangles.Count == 0) { BVHs.Add(new BVHNode(new BBox(Vector3.Zero, Vector3.Zero), -1, 0)); }
        BBox sceneBBox = new BBox();
        for (int i = 0; i < triangles.Count; i++) { include(ref sceneBBox, triangles[i]); }
        BVHs.Add(new BVHNode(sceneBBox, 0, triangles.Count)); //index is triStartIndex -> 0
        addBVH(0);
    }

    private void addBVH(int rootIndex, int depth = 0)
    {
        BVHNode root = BVHs[rootIndex];
        if (depth >= 31 || root.triCount <= q.trisPerLeaf) { return; } //max tris in bbox

        BVHNode left = new BVHNode(root.bbox.inverse, root.index, 0);
        BVHNode right = new BVHNode(root.bbox.inverse, root.index, 0);

        (int axis, float pos, float cost) = SAH(root);

        if (axis == -1 || cost >= nodeCost(root)) { return; }

        for (int i = 0; i < root.triCount; i++) //partitioning
        {
            Triangle t = triangles[i + root.index];
            float centre = ((Vector3)t.centre)[axis];
            if (centre < pos)
            {
                include(ref left.bbox, t);
                int swapIndex = root.index + left.triCount;

                Triangle temp = triangles[swapIndex];
                triangles[swapIndex] = t;
                triangles[i + root.index] = temp;

                left.triCount++;
            }
            else { include(ref right.bbox, t); }
        }
        right.triCount = root.triCount - left.triCount;
        right.index = root.index + left.triCount;
        if (left.triCount == 0 || right.triCount == 0) { return; }

        int leftID = BVHs.Count;
        BVHs.Add(left);
        BVHs.Add(right);
        root.triCount = -1;
        root.index = leftID;
        BVHs[rootIndex] = root; //value semantics -> i only got a copy

        addBVH(leftID, depth + 1);
        addBVH(leftID + 1, depth + 1);
    }

#warning its supposed to be centroid bounds
    private (int axis, float pos, float cost) SAH(BVHNode node)
    {
        float bestCost = float.MaxValue;
        int bestAxis = -1; //not valid
        float bestPos = 0;

        Vector3 diag = (Vector3)node.bbox.max - (Vector3)node.bbox.min;
        for (int i = 0; i < 3; i++)
        {
            if (diag[i] <= 1e-6f) { continue; }

            BVHNode[] bins = new BVHNode[q.binsPerAxis]; //just need bbox and numTris
            for (int j = 0; j < bins.Length; j++) { bins[j] = new BVHNode(node.bbox.inverse, 0, 0); }
            float step = diag[i] / bins.Length;
            float start = ((Vector3)node.bbox.min)[i];
            for (int j = 0; j < node.triCount; j++) //populate bins
            {
                float centre = ((Vector3)triangles[j + node.index].centre)[i];
                int binID = Math.Clamp((int)((centre - start) / step), 0, bins.Length - 1);
                BVHNode bin = bins[binID];
                bin.triCount++;
                include(ref bin.bbox, triangles[j + node.index]);
                bins[binID] = bin;
            }

            for (int j = 1; j < bins.Length; j++) //calculate cost for each plane
            {
                float pos = start + step * j;
                BVHNode l = new BVHNode(node.bbox.inverse, 0, 0);
                BVHNode r = new BVHNode(node.bbox.inverse, 0, 0);
                for (int k = 0; k < j; k++)
                {
                    if (bins[k].triCount == 0) { continue; } //else empty box has default bounds
                    include(ref l.bbox, bins[k].bbox);
                    l.triCount += bins[k].triCount;
                }
                for (int k = j; k < bins.Length; k++)
                {
                    if (bins[k].triCount == 0) { continue; }
                    include(ref r.bbox, bins[k].bbox);
                    r.triCount += bins[k].triCount;
                }
                if (l.triCount == 0 || r.triCount == 0) { continue; }
                float cost = nodeCost(l) + nodeCost(r);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestPos = pos;
                    bestAxis = i;
                }
            }
        }

        return (bestAxis, bestPos, bestCost);
    }

#warning add traversal cost -> cost of doing another AABB intersection test
    private float nodeCost(BVHNode node)
    {
        Vector3 diag = (Vector3)node.bbox.max - (Vector3)node.bbox.min;
        return node.triCount * (diag.X * diag.Y + diag.Y * diag.Z + diag.Z * diag.X);
    }

    #endregion

}
