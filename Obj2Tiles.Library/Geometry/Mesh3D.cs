﻿using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Obj2Tiles.Library.Materials;

namespace Obj2Tiles.Library.Geometry;

public class Mesh3D
{
    public IReadOnlyList<Vertex3D> Vertices;
    public IReadOnlyList<Vertex2D> TextureVertices;
    public IReadOnlyList<Face<Vertex3D>> Faces;
    public IReadOnlyList<Material> Materials;

    public const string DefaultName = "Mesh";

    public string Name { get; set; } = DefaultName;

    public Box3D Bounds
    {
        get
        {
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var minZ = double.MaxValue;

            var maxX = double.MinValue;
            var maxY = double.MinValue;
            var maxZ = double.MinValue;

            foreach (var v in Vertices)
            {
                minX = minX < v.x ? minX : v.x;
                minY = minY < v.y ? minY : v.y;
                minZ = minZ < v.z ? minZ : v.z;

                maxX = v.x > maxX ? maxX : v.x;
                maxY = v.y > maxY ? maxY : v.y;
                maxZ = v.z > maxZ ? maxZ : v.z;
            }

            return new Box3D(minX, minY, minZ, maxX, maxY, maxZ);
        }
    }

    public Vertex3D GetBaricenter()
    {
        var x = 0.0;
        var y = 0.0;
        var z = 0.0;

        foreach (var v in Vertices)
        {
            x += v.x;
            y += v.y;
            z += v.z;
        }

        x /= Vertices.Count;
        y /= Vertices.Count;
        z /= Vertices.Count;

        return new Vertex3D(x, y, z);
    }

    public Mesh3D(string file)
    {
        InternalLoad(file);
    }

    public Mesh3D(IReadOnlyList<Vertex3D> vertices, IReadOnlyList<Vertex2D> textureVertices,
        IReadOnlyList<Face<Vertex3D>> faces, IReadOnlyList<Material> materials)
    {
        Vertices = vertices;
        TextureVertices = textureVertices;
        Faces = faces;
        Materials = materials;
    }

    private int SplitWithTexture(IVertexUtils utils, double q, out Mesh3D left, out Mesh3D right)
    {
        var leftVertices = new Dictionary<Vertex3D, int>(Vertices.Count);
        var rightVertices = new Dictionary<Vertex3D, int>(Vertices.Count);

        var leftFaces = new List<Face<Vertex3D>>(Faces.Count);
        var rightFaces = new List<Face<Vertex3D>>(Faces.Count);

        var leftTextureVertices = new Dictionary<Vertex2D, int>(TextureVertices.Count);
        var rightTextureVertices = new Dictionary<Vertex2D, int>(TextureVertices.Count);

        var count = 0;

        foreach (var face in Faces)
        {

            var aSide = utils.GetDimension(face.A) < q;
            var bSide = utils.GetDimension(face.B) < q;
            var cSide = utils.GetDimension(face.C) < q;

            if (aSide)
            {
                if (bSide)
                {
                    if (cSide)
                    {
                        // All on the left

                        var indexALeft = leftVertices.AddIndex(face.A);
                        var indexBLeft = leftVertices.AddIndex(face.B);
                        var indexCLeft = leftVertices.AddIndex(face.C);

                        var indexATextureLeft = leftTextureVertices!.AddIndex(face.TA);
                        var indexBTextureLeft = leftTextureVertices!.AddIndex(face.TB);
                        var indexCTextureLeft = leftTextureVertices!.AddIndex(face.TC);

                        leftFaces.Add(new Face<Vertex3D>(indexALeft, indexBLeft, indexCLeft, face.A, face.B, face.C,
                            indexATextureLeft, indexBTextureLeft, indexCTextureLeft,
                            face.MaterialIndex!.Value, face.TA!, face.TB!, face.TC!));

                    }
                    else
                    {
                        IntersectRight2DWithTexture(utils, q, face.IndexC, face.IndexA, face.IndexB, leftVertices,
                            rightVertices,
                            face.TextureIndexC!.Value, face.TextureIndexA!.Value, face.TextureIndexB!.Value,
                            leftTextureVertices, rightTextureVertices, face.MaterialIndex!.Value, leftFaces, rightFaces
                        );
                        count++;
                    }
                }
                else
                {
                    if (cSide)
                    {
                        IntersectRight2DWithTexture(utils, q, face.IndexB, face.IndexC, face.IndexA, leftVertices,
                            rightVertices,
                            face.TextureIndexB!.Value, face.TextureIndexC!.Value, face.TextureIndexA!.Value,
                            leftTextureVertices, rightTextureVertices, face.MaterialIndex!.Value, leftFaces,
                            rightFaces);
                        count++;
                    }
                    else
                    {
                        IntersectLeft2DWithTexture(utils, q, face.IndexA, face.IndexB, face.IndexC, leftVertices,
                            rightVertices,
                            face.TextureIndexA!.Value, face.TextureIndexB!.Value, face.TextureIndexC!.Value,
                            leftTextureVertices, rightTextureVertices, face.MaterialIndex!.Value, leftFaces,
                            rightFaces);
                        count++;
                    }
                }
            }
            else
            {
                if (bSide)
                {
                    if (cSide)
                    {
                        IntersectRight2DWithTexture(utils, q, face.IndexA, face.IndexB, face.IndexC, leftVertices,
                            rightVertices,
                            face.TextureIndexA!.Value, face.TextureIndexB!.Value, face.TextureIndexC!.Value,
                            leftTextureVertices, rightTextureVertices, face.MaterialIndex!.Value, leftFaces,
                            rightFaces);
                        count++;
                    }
                    else
                    {
                        IntersectLeft2DWithTexture(utils, q, face.IndexB, face.IndexC, face.IndexA, leftVertices,
                            rightVertices,
                            face.TextureIndexB!.Value, face.TextureIndexC!.Value, face.TextureIndexA!.Value,
                            leftTextureVertices, rightTextureVertices, face.MaterialIndex!.Value, leftFaces,
                            rightFaces);
                        count++;
                    }
                }
                else
                {
                    if (cSide)
                    {
                        IntersectLeft2DWithTexture(utils, q, face.IndexC, face.IndexA, face.IndexB, leftVertices,
                            rightVertices,
                            face.TextureIndexC!.Value, face.TextureIndexA!.Value, face.TextureIndexB!.Value,
                            leftTextureVertices, rightTextureVertices, face.MaterialIndex!.Value, leftFaces,
                            rightFaces);
                        count++;
                    }
                    else
                    {
                        // All on the right
                        
                        var indexARight = rightVertices.AddIndex(face.A);
                        var indexBRight = rightVertices.AddIndex(face.B);
                        var indexCRight = rightVertices.AddIndex(face.C);

                        var indexATextureRight = rightTextureVertices!.AddIndex(face.TA);
                        var indexBTextureRight = rightTextureVertices!.AddIndex(face.TB);
                        var indexCTextureRight = rightTextureVertices!.AddIndex(face.TC);

                        rightFaces.Add(new Face<Vertex3D>(indexARight, indexBRight, indexCRight, face.A, face.B, face.C,
                            indexATextureRight, indexBTextureRight, indexCTextureRight,
                            face.MaterialIndex!.Value, face.TA!, face.TB!, face.TC!));

                    }
                }
            }
        }

        var orderedLeftVertices = leftVertices.OrderBy(x => x.Value).Select(x => x.Key).ToList();
        var orderedRightVertices = rightVertices.OrderBy(x => x.Value).Select(x => x.Key).ToList();

        var orderedLeftTextureVertices = leftTextureVertices.OrderBy(x => x.Value).Select(x => x.Key).ToList();
        var orderedRightTextureVertices = rightTextureVertices.OrderBy(x => x.Value).Select(x => x.Key).ToList();

        // Aggiungere split texture
        left = new Mesh3D(orderedLeftVertices, orderedLeftTextureVertices, leftFaces, Materials);
        right = new Mesh3D(orderedRightVertices, orderedRightTextureVertices, rightFaces, Materials);

        return count;
    }


    public int Split(IVertexUtils utils, double q, out Mesh3D left, out Mesh3D right)
    {
        return TextureVertices.Any()
            ? SplitWithTexture(utils, q, out left, out right)
            : SplitWithoutTexture(utils, q, out left, out right);
    }

    private void IntersectLeft2DWithTexture(IVertexUtils utils, double q, int indexVL, int indexVR1, int indexVR2,
        IDictionary<Vertex3D, int> leftVertices, IDictionary<Vertex3D, int> rightVertices,
        int indexTextureVL, int indexTextureVR1, int indexTextureVR2,
        IDictionary<Vertex2D, int> leftTextureVertices, IDictionary<Vertex2D, int> rightTextureVertices,
        int materialIndex, ICollection<Face<Vertex3D>> leftFaces, ICollection<Face<Vertex3D>> rightFaces)
    {
        var vL = Vertices[indexVL];
        var vR1 = Vertices[indexVR1];
        var vR2 = Vertices[indexVR2];

        var tVL = TextureVertices[indexTextureVL];
        var tVR1 = TextureVertices[indexTextureVR1];
        var tVR2 = TextureVertices[indexTextureVR2];

        var indexVLLeft = leftVertices.AddIndex(vL);
        var indexTextureVLLeft = leftTextureVertices.AddIndex(tVL);

        if (Math.Abs(utils.GetDimension(vR1) - q) < double.Epsilon &&
            Math.Abs(utils.GetDimension(vR2) - q) < double.Epsilon)
        {
            // Right Vertices are on the line

            var indexVR1Left = leftVertices.AddIndex(vR1);
            var indexVR2Left = leftVertices.AddIndex(vR2);

            var indexTextureVR1Left = leftTextureVertices.AddIndex(tVR1);
            var indexTextureVR2Left = leftTextureVertices.AddIndex(tVR2);

            leftFaces.Add(new Face<Vertex3D>(indexVLLeft, indexVR1Left, indexVR2Left, vL, vR1, vR2,
                indexTextureVLLeft, indexTextureVR1Left, indexTextureVR2Left, materialIndex,
                tVL, tVR1, tVR2));

            return;
        }

        var indexVR1Right = rightVertices.AddIndex(vR1);
        var indexVR2Right = rightVertices.AddIndex(vR2);

        // a on the left, b and c on the right

        // Prima intersezione
        var t1 = utils.CutEdge(vL, vR1, q);
        var indexT1Left = leftVertices.AddIndex(t1);
        var indexT1Right = rightVertices.AddIndex(t1);

        // Seconda intersezione
        var t2 = utils.CutEdge(vL, vR2, q);
        var indexT2Left = leftVertices.AddIndex(t2);
        var indexT2Right = rightVertices.AddIndex(t2);

        // Split texture
        var indexTextureVR1Right = rightTextureVertices.AddIndex(tVR1);
        var indexTextureVR2Right = rightTextureVertices.AddIndex(tVR2);

        var perc1 = GetIntersectionPerc(vL, vR1, t1);

        // Prima intersezione texture
        var t1t = utils.CutEdgePerc(tVL, tVR1, perc1);
        var indexTextureT1Left = leftTextureVertices.AddIndex(t1t);
        var indexTextureT1Right = rightTextureVertices.AddIndex(t1t);

        var perc2 = GetIntersectionPerc(vL, vR2, t2);

        // Seconda intersezione texture
        var t2t = utils.CutEdgePerc(tVL, tVR2, perc2);
        var indexTextureT2Left = leftTextureVertices.AddIndex(t2t);
        var indexTextureT2Right = rightTextureVertices.AddIndex(t2t);

        var lface = new Face<Vertex3D>(indexVLLeft, indexT1Left, indexT2Left, vL, t1, t2,
            indexTextureVLLeft, indexTextureT1Left, indexTextureT2Left, materialIndex,
            tVL, t1t, t2t);
        leftFaces.Add(lface);

        var rface1 = new Face<Vertex3D>(indexT1Right, indexVR1Right, indexVR2Right, t1, vR1, vR2,
            indexTextureT1Right, indexTextureVR1Right, indexTextureVR2Right, materialIndex,
            t1t, tVR1, tVR2);
        rightFaces.Add(rface1);

        var rface2 = new Face<Vertex3D>(indexT1Right, indexVR2Right, indexT2Right, t1, vR2, t2,
            indexTextureT1Right, indexTextureVR2Right, indexTextureT2Right, materialIndex,
            t1t, tVR2, t2t);
        rightFaces.Add(rface2);
    }


    private void IntersectRight2DWithTexture(IVertexUtils utils, double q, int indexVR, int indexVL1, int indexVL2,
        IDictionary<Vertex3D, int> leftVertices, IDictionary<Vertex3D, int> rightVertices,
        int indexTextureVR, int indexTextureVL1, int indexTextureVL2,
        IDictionary<Vertex2D, int> leftTextureVertices, IDictionary<Vertex2D, int> rightTextureVertices,
        int materialIndex, ICollection<Face<Vertex3D>> leftFaces, ICollection<Face<Vertex3D>> rightFaces)
    {
        var vR = Vertices[indexVR];
        var vL1 = Vertices[indexVL1];
        var vL2 = Vertices[indexVL2];

        var tVR = TextureVertices[indexTextureVR];
        var tVL1 = TextureVertices[indexTextureVL1];
        var tVL2 = TextureVertices[indexTextureVL2];

        var indexVRRight = rightVertices.AddIndex(vR);
        var indexTextureVRRight = rightTextureVertices.AddIndex(tVR);

        if (Math.Abs(utils.GetDimension(vL1) - q) < double.Epsilon &&
            Math.Abs(utils.GetDimension(vL2) - q) < double.Epsilon)
        {
            // Left Vertices are on the line

            var indexVL1Right = rightVertices.AddIndex(vL1);
            var indexVL2Right = rightVertices.AddIndex(vL2);

            var indexTextureVL1Right = rightTextureVertices.AddIndex(tVL1);
            var indexTextureVL2Right = rightTextureVertices.AddIndex(tVL2);

            rightFaces.Add(new Face<Vertex3D>(indexVRRight, indexVL1Right, indexVL2Right, vR, vL1, vL2,
                indexTextureVRRight, indexTextureVL1Right, indexTextureVL2Right, materialIndex,
                tVR, tVL1, tVL2));

            return;
        }

        var indexVL1Left = leftVertices.AddIndex(vL1);
        var indexVL2Left = leftVertices.AddIndex(vL2);

        // a on the right, b and c on the left

        // Prima intersezione
        var t1 = utils.CutEdge(vR, vL1, q);
        var indexT1Left = leftVertices.AddIndex(t1);
        var indexT1Right = rightVertices.AddIndex(t1);

        // Seconda intersezione
        var t2 = utils.CutEdge(vR, vL2, q);
        var indexT2Left = leftVertices.AddIndex(t2);
        var indexT2Right = rightVertices.AddIndex(t2);

        // Split texture
        var indexTextureVL1Left = leftTextureVertices.AddIndex(tVL1);
        var indexTextureVL2Left = leftTextureVertices.AddIndex(tVL2);

        var perc1 = GetIntersectionPerc(vR, vL1, t1);

        // Prima intersezione texture
        var t1t = utils.CutEdgePerc(tVR, tVL1, perc1);
        var indexTextureT1Left = leftTextureVertices.AddIndex(t1t);
        var indexTextureT1Right = rightTextureVertices.AddIndex(t1t);

        var perc2 = GetIntersectionPerc(vR, vL2, t2);

        // Seconda intersezione texture
        var t2t = utils.CutEdgePerc(tVR, tVL2, perc2);
        var indexTextureT2Left = leftTextureVertices.AddIndex(t2t);
        var indexTextureT2Right = rightTextureVertices.AddIndex(t2t);

        var rface = new Face<Vertex3D>(indexVRRight, indexT1Right, indexT2Right, vR, t1, t2,
            indexTextureVRRight, indexTextureT1Right, indexTextureT2Right, materialIndex,
            tVR, t1t, t2t);
        rightFaces.Add(rface);

        var lface1 = new Face<Vertex3D>(indexT2Left, indexVL1Left, indexVL2Left, t2, vL1, vL2,
            indexTextureT2Left, indexTextureVL1Left, indexTextureVL2Left, materialIndex,
            t2t, tVL1, tVL2);
        leftFaces.Add(lface1);

        var lface2 = new Face<Vertex3D>(indexT2Left, indexT1Left, indexVL1Left, t2, t1, vL1,
            indexTextureT2Left, indexTextureT1Left, indexTextureVL1Left, materialIndex,
            t2t, t1t, tVL1);
        leftFaces.Add(lface2);
        
    }

    public void TrimTextures()
    {
        var facesMapper = from face in Faces
            where face.MaterialIndex != null
            group face by face.MaterialIndex.Value
            into grp
            select new
            {
                MaterialIndex = grp.Key,
                Faces = grp.ToList()
            };

        foreach (var g in facesMapper)
        {
            var material = Materials[g.MaterialIndex];
            var clusters = new List<Face<Vertex3D>[]>();

            var materialFaces = g.Faces;

            var currentCluster = new List<Face<Vertex3D>> { materialFaces.First() };
            materialFaces.RemoveAt(0);

            while (materialFaces.Count > 0)
            {
                var cnt = currentCluster.Count;

                // Scorriamo tutte le facce del cluster
                for (var index = 0; index < currentCluster.Count; index++)
                {
                    var face = currentCluster[index];

                    // Scorriamo tutte le facce rimanenti
                    for (var n = 0; n < materialFaces.Count; n++)
                    {
                        var f = materialFaces[n];
                        if (face.IsTextureAdjacent(f))
                        {
                            currentCluster.Add(f);
                            materialFaces.RemoveAt(n);
                        }
                    }
                }

                // Non ho aggiunto nessuna faccia
                if (cnt == currentCluster.Count)
                {
                    // Aggiungo il cluster
                    clusters.Add(currentCluster.ToArray());

                    // Andiamo avanti con il prossimo cluster
                    currentCluster = new List<Face<Vertex3D>> { materialFaces.First() };
                    materialFaces.RemoveAt(0);
                }
            }

            clusters.Add(currentCluster.ToArray());

            Debug.WriteLine($"Material {g.MaterialIndex} has {clusters.Count} clusters");

            for (var index = 0; index < clusters.Count; index++)
            {
                var cluster = clusters[index];
                Debug.WriteLine("Working on cluster " + index);

                double maxX = double.MinValue, maxY = double.MinValue;
                double minX = double.MaxValue, minY = double.MaxValue;

                var boxes = new List<Box2D>();

                foreach (var face in cluster)
                {
                    maxX = Math.Max(Math.Max(Math.Max(maxX, face.TC!.x), face.TB!.x), face.TA!.x);
                    maxY = Math.Max(Math.Max(Math.Max(maxY, face.TC!.y), face.TB!.y), face.TA!.y);

                    minX = Math.Min(Math.Min(Math.Min(minX, face.TC!.x), face.TB!.x), face.TA!.x);
                    minY = Math.Min(Math.Min(Math.Min(minY, face.TC!.y), face.TB!.y), face.TA!.y);

                    boxes.Add(new Box2D(minX, minY, maxX, maxY));
                }

                var size = Math.Sqrt(boxes.Sum(b => b.Area));
            }

            //throw new NotImplementedException();
            // Aggiungere alla lista dei cluster il materiale corrispondente
            // Unificare i materiali
            // Incasellare i cluster in rettangoli
            // Aggiungere i rettangoli dal più grande al più piccolo al nuovo file di texture
            // Aggiornare tutte le coordinate delle texture
        }
    }

    #region No texture

    public int SplitWithoutTexture(IVertexUtils utils, double q, out Mesh3D left, out Mesh3D right)
    {
        var leftVertices = new Dictionary<Vertex3D, int>(Vertices.Count);
        var rightVertices = new Dictionary<Vertex3D, int>(Vertices.Count);

        var leftFaces = new List<Face<Vertex3D>>(Faces.Count);
        var rightFaces = new List<Face<Vertex3D>>(Faces.Count);

        var count = 0;

        foreach (var face in Faces)
        {

            var aSide = face.A.x < q;
            var bSide = face.B.x < q;
            var cSide = face.C.x < q;

            if (aSide)
            {
                if (bSide)
                {
                    if (cSide)
                    {
                        
                        // All on the left

                        var indexALeft = leftVertices.AddIndex(face.A);
                        var indexBLeft = leftVertices.AddIndex(face.B);
                        var indexCLeft = leftVertices.AddIndex(face.C);

                        leftFaces.Add(new Face<Vertex3D>(indexALeft, indexBLeft, indexCLeft, face.A, face.B, face.C));

                    }
                    else
                    {
                        IntersectRight2D(utils, q, face.IndexC, face.IndexB, face.IndexA, leftVertices, rightVertices,
                            leftFaces, rightFaces);
                        count++;
                    }
                }
                else
                {
                    if (cSide)
                    {
                        IntersectRight2D(utils, q, face.IndexB, face.IndexA, face.IndexC, leftVertices, rightVertices,
                            leftFaces, rightFaces);
                        count++;
                    }
                    else
                    {
                        IntersectLeft2D(utils, q, face.IndexA, face.IndexB, face.IndexC, leftVertices, rightVertices,
                            leftFaces, rightFaces);
                        count++;
                    }
                }
            }
            else
            {
                if (bSide)
                {
                    if (cSide)
                    {
                        IntersectRight2D(utils, q, face.IndexA, face.IndexB, face.IndexC, leftVertices, rightVertices,
                            leftFaces, rightFaces);
                        count++;
                    }
                    else
                    {
                        IntersectLeft2D(utils, q, face.IndexB, face.IndexA, face.IndexC, leftVertices, rightVertices,
                            leftFaces, rightFaces);
                        count++;
                    }
                }
                else
                {
                    if (cSide)
                    {
                        IntersectLeft2D(utils, q, face.IndexC, face.IndexA, face.IndexB, leftVertices, rightVertices,
                            leftFaces, rightFaces);
                        count++;
                    }
                    else
                    {
                        // All on the right
                        
                        var indexARight = rightVertices.AddIndex(face.A);
                        var indexBRight = rightVertices.AddIndex(face.B);
                        var indexCRight = rightVertices.AddIndex(face.C);
                        rightFaces.Add(
                            new Face<Vertex3D>(indexARight, indexBRight, indexCRight, face.A, face.B, face.C));

                    }
                }
            }
        }

        var orderedLeftVertices = leftVertices.OrderBy(x => x.Value).Select(x => x.Key).ToList();
        var orderedRightVertices = rightVertices.OrderBy(x => x.Value).Select(x => x.Key).ToList();

        // Aggiungere split texture
        left = new Mesh3D(orderedLeftVertices, TextureVertices, leftFaces, Materials);
        right = new Mesh3D(orderedRightVertices, TextureVertices, rightFaces, Materials);

        return count;
    }

    private void IntersectLeft2D(IVertexUtils utils, double q, int indexVL, int indexVR1, int indexVR2,
        IDictionary<Vertex3D, int> leftVertices,
        IDictionary<Vertex3D, int> rightVertices, ICollection<Face<Vertex3D>> leftFaces,
        ICollection<Face<Vertex3D>> rightFaces)
    {
        var vL = Vertices[indexVL];
        var vR1 = Vertices[indexVR1];
        var vR2 = Vertices[indexVR2];

        var indexVLLeft = leftVertices.AddIndex(vL);

        if (Math.Abs(utils.GetDimension(vR1) - q) < double.Epsilon &&
            Math.Abs(utils.GetDimension(vR2) - q) < double.Epsilon)
        {
            // Right Vertices are on the line

            var indexVR1Left = leftVertices.AddIndex(vR1);
            var indexVR2Left = leftVertices.AddIndex(vR2);

            leftFaces.Add(new Face<Vertex3D>(indexVLLeft, indexVR1Left, indexVR2Left, vL, vR1, vR2));
            return;
        }

        var indexVR1Right = rightVertices.AddIndex(vR1);
        var indexVR2Right = rightVertices.AddIndex(vR2);

        // a on the left, b and c on the right

        // Prima intersezione
        var t1 = utils.CutEdge(vL, vR1, q);
        var indexT1Left = leftVertices.AddIndex(t1);
        var indexT1Right = rightVertices.AddIndex(t1);

        // Seconda intersezione
        var t2 = utils.CutEdge(vL, vR2, q);
        var indexT2Left = leftVertices.AddIndex(t2);
        var indexT2Right = rightVertices.AddIndex(t2);

        var lface = new Face<Vertex3D>(indexVLLeft, indexT1Left, indexT2Left, vL, t1, t2);
        leftFaces.Add(lface);

        var rface1 = new Face<Vertex3D>(indexT1Right, indexVR1Right, indexVR2Right, t1, vR1, vR2);
        rightFaces.Add(rface1);

        var rface2 = new Face<Vertex3D>(indexT2Right, indexT1Right, indexVR2Right, t2, t1, vR2);
        rightFaces.Add(rface2);
        
    }

    private void IntersectRight2D(IVertexUtils utils, double q, int indexVR, int indexVL1, int indexVL2,
        IDictionary<Vertex3D, int> leftVertices, IDictionary<Vertex3D, int> rightVertices,
        ICollection<Face<Vertex3D>> leftFaces, ICollection<Face<Vertex3D>> rightFaces)
    {
        var vR = Vertices[indexVR];
        var vL1 = Vertices[indexVL1];
        var vL2 = Vertices[indexVL2];

        var indexVRRight = rightVertices.AddIndex(vR);

        if (Math.Abs(utils.GetDimension(vL1) - q) < double.Epsilon &&
            Math.Abs(utils.GetDimension(vL2) - q) < double.Epsilon)
        {
            // Left Vertices are on the line
            var indexVL1Right = rightVertices.AddIndex(vL1);
            var indexVL2Right = rightVertices.AddIndex(vL2);

            rightFaces.Add(new Face<Vertex3D>(indexVRRight, indexVL1Right, indexVL2Right, vR, vL1, vL2));

            return;
        }

        var indexVL1Left = leftVertices.AddIndex(vL1);
        var indexVL2Left = leftVertices.AddIndex(vL2);

        // a on the right, b and c on the left

        // Prima intersezione
        var t1 = utils.CutEdge(vR, vL1, q);
        var indexT1Left = leftVertices.AddIndex(t1);
        var indexT1Right = rightVertices.AddIndex(t1);

        // Seconda intersezione
        var t2 = utils.CutEdge(vR, vL2, q);
        var indexT2Left = leftVertices.AddIndex(t2);
        var indexT2Right = rightVertices.AddIndex(t2);

        var rface = new Face<Vertex3D>(indexVRRight, indexT2Right, indexT1Right, vR, t2, t1);
        rightFaces.Add(rface);

        var lface1 = new Face<Vertex3D>(indexT2Left, indexVL2Left, indexVL1Left, t2, vL2, vL1);
        leftFaces.Add(lface1);

        var lface2 = new Face<Vertex3D>(indexT2Left, indexVL1Left, indexT1Left, t2, vL1, t1);
        leftFaces.Add(lface2);

    }

    #endregion

    #region Utils

    /// <summary>
    /// Gets the distance of P from A (in percent) relative to segment AB
    /// </summary>
    /// <param name="a">Edge start</param>
    /// <param name="b">Edge end</param>
    /// <param name="p">Point on the segment</param>
    /// <returns></returns>
    private static double GetIntersectionPerc(Vertex3D a, Vertex3D b, Vertex3D p)
    {
        var edge1Length = a.Distance(b);
        var subEdge1Length = a.Distance(p);
        var perc1 = subEdge1Length / edge1Length;
        return perc1;
    }

    private static readonly CultureInfo en = CultureInfo.GetCultureInfo("en-US");

    public void WriteObj(string path)
    {
        var materialsPath = Path.ChangeExtension(path, "mtl");

        using (var writer = new StreamWriter(path))
        {
            writer.WriteLine("mtllib {0}", Path.GetFileName(materialsPath));
            writer.Write("o ");
            writer.WriteLine(string.IsNullOrWhiteSpace(Name) ? DefaultName : Name);

            CultureInfo.CurrentCulture = en;

            foreach (var vertex in Vertices)
            {
                writer.Write("v ");
                writer.Write(vertex.x);
                writer.Write(" ");
                writer.Write(vertex.y);
                writer.Write(" ");
                writer.WriteLine(vertex.z);
            }

            foreach (var textureVertex in TextureVertices)
            {
                writer.Write("vt ");
                writer.Write(textureVertex.x);
                writer.Write(" ");
                writer.WriteLine(textureVertex.y);
            }

            var materialFaces = from face in Faces
                group face by face.MaterialIndex
                into g
                select g;

            // NOTE: Se ci sono gruppi di facce senza materiali vanno messe all'inizio
            foreach (var grp in materialFaces.OrderBy(item => item.Key))
            {
                if (grp.Key != null)
                    writer.WriteLine($"usemtl {Materials[grp.Key.Value].Name}");

                foreach (var face in grp)
                    writer.WriteLine(face.ToObj());
            }
        }

        using (var writer = new StreamWriter(Path.ChangeExtension(path, "mtl")))
        {
            foreach (var material in Materials)
            {
                writer.WriteLine(material.ToMtl());

                /*
                if (!string.IsNullOrWhiteSpace(material.Texture))
                {
                    var texturePath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, material.Texture);

                    File.Copy(texturePath, Path.ChangeExtension(path, Path.GetExtension(texturePath)), true);
                }*/
            }
        }
    }

    private void InternalLoad(string file)
    {
        using var reader = new StreamReader(file);

        var vertices = new List<Vertex3D>();
        var textureVertices = new List<Vertex2D>();
        var faces = new List<Face<Vertex3D>>();
        var materials = new List<Material>();
        var materialsDict = new Dictionary<string, int>();
        string currentMaterial = null;

        while (true)
        {
            var line = reader.ReadLine();

            if (line == null) break;

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var segs = line.Split(' ');

            switch (segs[0])
            {
                case "v" when segs.Length >= 4:
                    vertices.Add(new Vertex3D(
                        double.Parse(segs[1], en),
                        double.Parse(segs[2], en),
                        double.Parse(segs[3], en)));
                    break;
                case "vt" when segs.Length >= 3:
                    textureVertices.Add(new Vertex2D(
                        double.Parse(segs[1], en),
                        double.Parse(segs[2], en)));
                    break;
                case "vn" when segs.Length == 3:
                    // Skipping normals
                    break;
                case "usemtl" when segs.Length == 2:
                {
                    if (!materialsDict.ContainsKey(segs[1]))
                        throw new Exception($"Material {segs[1]} not found");

                    currentMaterial = segs[1];
                    break;
                }
                case "f" when segs.Length == 4:
                {
                    var vertexIndices = new[]
                    {
                        segs[1].Split('/').Select(int.Parse).ToArray(),
                        segs[2].Split('/').Select(int.Parse).ToArray(),
                        segs[3].Split('/').Select(int.Parse).ToArray()
                    };

                    Face<Vertex3D> face = null;

                    switch (vertexIndices[0].Length)
                    {
                        case 1:

                            face = new Face<Vertex3D>(
                                vertexIndices[0][0] - 1,
                                vertexIndices[1][0] - 1,
                                vertexIndices[2][0] - 1,
                                vertices[vertexIndices[0][0] - 1],
                                vertices[vertexIndices[1][0] - 1],
                                vertices[vertexIndices[2][0] - 1]);

                            break;

                        case 2:

                            face = new Face<Vertex3D>(
                                vertexIndices[0][0] - 1,
                                vertexIndices[1][0] - 1,
                                vertexIndices[2][0] - 1,
                                vertices[vertexIndices[0][0] - 1],
                                vertices[vertexIndices[1][0] - 1],
                                vertices[vertexIndices[2][0] - 1],
                                vertexIndices[0][1] - 1,
                                vertexIndices[1][1] - 1,
                                vertexIndices[2][1] - 1,
                                materialsDict[currentMaterial],
                                textureVertices[vertexIndices[0][1] - 1],
                                textureVertices[vertexIndices[1][1] - 1],
                                textureVertices[vertexIndices[2][1] - 1]);

                            break;

                        case 3:

                            // We ignore normals for now
                            face = new Face<Vertex3D>(
                                vertexIndices[0][0] - 1,
                                vertexIndices[1][0] - 1,
                                vertexIndices[2][0] - 1,
                                vertices[vertexIndices[0][0] - 1],
                                vertices[vertexIndices[1][0] - 1],
                                vertices[vertexIndices[2][0] - 1],
                                vertexIndices[0][1] - 1,
                                vertexIndices[1][1] - 1,
                                vertexIndices[2][1] - 1,
                                materialsDict[currentMaterial],
                                textureVertices[vertexIndices[0][1] - 1],
                                textureVertices[vertexIndices[1][1] - 1],
                                textureVertices[vertexIndices[2][1] - 1]);

                            break;

                        default:
                            throw new InvalidOperationException($"Invalid vertex index: '{line}'");
                    }

                    faces.Add(face);
                    break;
                }
                case "mtllib" when segs.Length == 2:
                {
                    var mtlFileName = segs[1];
                    var mtlFilePath = Path.Combine(Path.GetDirectoryName(file) ?? string.Empty, mtlFileName);

                    var mats = Material.ReadMtl(mtlFilePath);

                    foreach (var mat in mats)
                    {
                        materials.Add(mat);
                        materialsDict.Add(mat.Name, materials.Count - 1);
                    }

                    break;
                }
                case "l" or "cstype" or "deg" or "bmat" or "step" or "curv" or "curv2" or "surf" or "parm" or "trim" or "end" or "hole" or "scrv" or "sp" or "con":
                    
                    throw new NotSupportedException("Element not supported: '" + line + "'");
            }
        }

        Vertices = vertices;
        Faces = faces;
        Materials = materials;
        TextureVertices = textureVertices;
    }

    #endregion

    #region Splitters

    private static readonly IVertexUtils yutils = new VertexUtilsY();
    private static readonly IVertexUtils xutils = new VertexUtilsX();
    private static readonly IVertexUtils zutils = new VertexUtilsZ();

    public static async Task<int> RecurseSplitXY(Mesh3D mesh, int depth, ConcurrentBag<Mesh3D> meshes)
    {
        var center = mesh.GetBaricenter();

        var count = mesh.Split(xutils, center.x, out var left, out var right);
        count += left.Split(yutils, center.y, out var topleft, out var bottomleft);
        count += right.Split(yutils, center.y, out var topright, out var bottomright);

        var nextDepth = depth - 1;

        if (nextDepth == 0)
        {
            if (topleft.Faces.Any()) meshes.Add(topleft);
            if (bottomleft.Faces.Any()) meshes.Add(bottomleft);
            if (topright.Faces.Any()) meshes.Add(topright);
            if (bottomright.Faces.Any()) meshes.Add(bottomright);

            return count;
        }

        var tasks = new List<Task<int>>();

        if (topleft.Faces.Any()) tasks.Add(RecurseSplitXY(topleft, nextDepth, meshes));
        if (bottomleft.Faces.Any()) tasks.Add(RecurseSplitXY(bottomleft, nextDepth, meshes));
        if (topright.Faces.Any()) tasks.Add(RecurseSplitXY(topright, nextDepth, meshes));
        if (bottomright.Faces.Any()) tasks.Add(RecurseSplitXY(bottomright, nextDepth, meshes));

        await Task.WhenAll(tasks);

        return count + tasks.Sum(t => t.Result);
    }

    public static async Task<int> RecurseSplitXYZ(Mesh3D mesh, int depth, ConcurrentBag<Mesh3D> meshes)
    {

        var center = mesh.GetBaricenter();

        var count = mesh.Split(xutils, center.x, out var left, out var right);
        count += left.Split(yutils, center.y, out var topleft, out var bottomleft);
        count += right.Split(yutils, center.y, out var topright, out var bottomright);

        count += topleft.Split(zutils, center.z, out var topleftnear, out var topleftfar);
        count += bottomleft.Split(zutils, center.z, out var bottomleftnear, out var bottomleftfar);

        count += topright.Split(zutils, center.z, out var toprightnear, out var toprightfar);
        count += bottomright.Split(zutils, center.z, out var bottomrightnear, out var bottomrightfar);

        var nextDepth = depth - 1;

        if (nextDepth == 0)
        {
            if (topleftnear.Faces.Any()) meshes.Add(topleftnear);
            if (topleftfar.Faces.Any()) meshes.Add(topleftfar);
            if (bottomleftnear.Faces.Any()) meshes.Add(bottomleftnear);
            if (bottomleftfar.Faces.Any()) meshes.Add(bottomleftfar);

            if (toprightnear.Faces.Any()) meshes.Add(toprightnear);
            if (toprightfar.Faces.Any()) meshes.Add(toprightfar);
            if (bottomrightnear.Faces.Any()) meshes.Add(bottomrightnear);
            if (bottomrightfar.Faces.Any()) meshes.Add(bottomrightfar);

            return count;
        }

        var tasks = new List<Task<int>>();

        if (topleftnear.Faces.Any()) tasks.Add(RecurseSplitXYZ(topleftnear, nextDepth, meshes));
        if (topleftfar.Faces.Any()) tasks.Add(RecurseSplitXYZ(topleftfar, nextDepth, meshes));
        if (bottomleftnear.Faces.Any()) tasks.Add(RecurseSplitXYZ(bottomleftnear, nextDepth, meshes));
        if (bottomleftfar.Faces.Any()) tasks.Add(RecurseSplitXYZ(bottomleftfar, nextDepth, meshes));

        if (toprightnear.Faces.Any()) tasks.Add(RecurseSplitXYZ(toprightnear, nextDepth, meshes));
        if (toprightfar.Faces.Any()) tasks.Add(RecurseSplitXYZ(toprightfar, nextDepth, meshes));
        if (bottomrightnear.Faces.Any()) tasks.Add(RecurseSplitXYZ(bottomrightnear, nextDepth, meshes));
        if (bottomrightfar.Faces.Any()) tasks.Add(RecurseSplitXYZ(bottomrightfar, nextDepth, meshes));

        await Task.WhenAll(tasks);

        return count + tasks.Sum(t => t.Result);
    }

    #endregion
}