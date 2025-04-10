﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System.Collections.Generic;
using System.IO;
using System.Globalization;
using OBJRuntime.DataTypes;
using Evergine.Mathematics;
using System.Runtime.CompilerServices;
using Evergine.Framework;
using Evergine.Common.IO;

namespace OBJRuntime.Readers
{
    /// <summary>
    /// Main OBJ loading logic in a static class, adapted from your code snippet.
    /// </summary>
    public static class OBJLoader
    {
        public static bool Load(
            StreamReader inStream,
            ref OBJAttrib attrib,
            List<OBJShape> shapes,
            List<OBJMaterial> materials,
            ref string warning,
            ref string error,
            AssetsDirectory assetsDirectory,
            string workingDirectory,
            bool triangulate,
            bool defaultVcolsFallback)
        {
            // We'll parse the entire .obj text from the provided stream.
            // This is roughly a direct line-by-line translation from the original.
            // We skip some advanced parts like TINYOBJLOADER_USE_MAPBOX_EARCUT for brevity.

            attrib.Vertices.Clear();
            attrib.VertexWeights.Clear();
            attrib.Normals.Clear();
            attrib.Texcoords.Clear();
            attrib.TexcoordWs.Clear();
            attrib.Colors.Clear();
            attrib.SkinWeights.Clear();
            shapes.Clear();

            var v = new List<Vector3>();
            var vertexWeights = new List<float>();  // for the optional w in 'v' lines
            var vn = new List<Vector3>();
            var vt = new List<Vector2>();
            var vc = new List<Vector3>();  // optional vertex colors
            var vw = new List<OBJSkinWeight>(); // extension: vertex skin weights

            int materialId = -1;
            uint currentSmoothingId = 0;

            bool foundAllColors = true;

            // We'll accumulate face/lines/points in a "PrimGroup" then flush to shape when
            // we see group or object changes. 
            PrimGroup primGroup = new PrimGroup();
            string currentGroupName = "";

            // For storing materials that we've loaded from .mtl
            var materialMap = new Dictionary<string, int>();
            var materialFilenames = new HashSet<string>();

            int lineNo = 0;
            while (!inStream.EndOfStream)
            {
                lineNo++;
                string line = inStream.ReadLine();
                if (line == null)
                    break;
                line = line.TrimEnd(); // remove trailing spaces
                if (line.Length < 1)
                    continue;

                // skip leading spaces
                if (line.StartsWith("#"))
                    continue; // comment

                // parse tokens
                var tokens = Helpers.Tokenize(line);
                if (tokens.Count < 1)
                    continue;

                var cmd = tokens[0];
                if (cmd == "v")
                {
                    // vertex
                    if (tokens.Count >= 4)
                    {
                        float x = 0, y = 0, z = 0;
                        float r = 1, g = 1, b = 1; // either color or 'w'

                        // parse x,y,z
                        Helpers.TryParseFloat(tokens[1], out x);
                        Helpers.TryParseFloat(tokens[2], out y);
                        Helpers.TryParseFloat(tokens[3], out z);

                        // if we have 4 tokens => might be w or color
                        // if we have 7 tokens => x y z r g b
                        int count = tokens.Count - 1; // ignoring the "v"
                        if (count == 4)
                        {
                            // interpret the 4th as 'w'
                            Helpers.TryParseFloat(tokens[4], out r);

                            // store w into vertexWeights
                            v.Add(new Vector3(x, y, z));
                            vertexWeights.Add(r);
                            foundAllColors = false;
                        }
                        else if (count == 6)
                        {
                            // x y z r g b ...
                            Helpers.TryParseFloat(tokens[4], out r);
                            Helpers.TryParseFloat(tokens[5], out g);
                            Helpers.TryParseFloat(tokens[6], out b);

                            v.Add(new Vector3(x, y, z));
                            vertexWeights.Add(1.0f); // default w=1
                            vc.Add(new Vector3(r, g, b));

                        }
                        else
                        {
                            // just x,y,z
                            v.Add(new Vector3(x, y, z));
                            vertexWeights.Add(1.0f);
                            foundAllColors = false;
                        }
                    }
                }
                else if (cmd == "vn") // normal
                {
                    if (tokens.Count >= 4)
                    {
                        float x = 0, y = 0, z = 0;

                        Helpers.TryParseFloat(tokens[1], out x);
                        Helpers.TryParseFloat(tokens[2], out y);
                        Helpers.TryParseFloat(tokens[3], out z);
                        vn.Add(new Vector3(x, y, z));
                    }
                }
                else if (cmd == "vt") // texcoord
                {
                    // vt u v [w]
                    if (tokens.Count >= 2)
                    {
                        float u = 0, vv = 0, w = 0;
                        Helpers.TryParseFloat(tokens[1], out u);

                        if (tokens.Count > 2)
                            Helpers.TryParseFloat(tokens[2], out vv);

                        if (tokens.Count > 3)
                            Helpers.TryParseFloat(tokens[3], out w);

                        vt.Add(new Vector2(u, vv));
                        // we won't store w in vt directly, but we can store it in TexcoordWs if needed.
                    }
                }
                else if (cmd == "vw")
                {
                    // extension for vertex weights
                    // e.g. "vw 0 0 0.25 1 0.25 2 0.5"
                    // first token is "vw", second is vertex id:
                    if (tokens.Count > 1)
                    {
                        OBJSkinWeight sw = new OBJSkinWeight();
                        int vid = 0;
                        if (int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out vid))
                        {
                            sw.VertexId = vid;
                            int idx = 2;
                            while (idx + 1 < tokens.Count)
                            {
                                int j = 0;
                                float w = 0.0f;

                                if (!int.TryParse(tokens[idx], out j))
                                    break;

                                if (!Helpers.TryParseFloat(tokens[idx + 1], out w))
                                    break;

                                OBJJointAndWeight jw = new OBJJointAndWeight();
                                jw.JointId = j;
                                jw.Weight = w;
                                sw.WeightValues.Add(jw);
                                idx += 2;
                            }
                            vw.Add(sw);
                        }
                    }
                }
                else if (cmd == "f")
                {
                    // face
                    if (tokens.Count < 2)
                        continue;

                    Face f = new Face() { SmoothingGroupId = currentSmoothingId };

                    // parse
                    for (int i = 1; i < tokens.Count; i++)
                    {
                        var vix = Helpers.ParseRawTriple(tokens[i]);
                        f.VertexIndices.Add(vix);
                    }

                    primGroup.FaceGroup.Add(f);
                }
                else if (cmd == "l")
                {
                    // line
                    if (tokens.Count < 2)
                        continue;

                    var lineGroup = new LineElm();
                    for (int i = 1; i < tokens.Count; i++)
                    {
                        var vix = Helpers.ParseRawTriple(tokens[i]);
                        lineGroup.VertexIndices.Add(vix);
                    }

                    primGroup.LineGroup.Add(lineGroup);
                }
                else if (cmd == "p")
                {
                    // points
                    if (tokens.Count < 2)
                        continue;

                    var pointGroup = new PointsElm();
                    for (int i = 1; i < tokens.Count; i++)
                    {
                        var vix = Helpers.ParseRawTriple(tokens[i]);
                        pointGroup.VertexIndices.Add(vix);
                    }

                    primGroup.PointsGroup.Add(pointGroup);
                }
                else if (cmd == "usemtl")
                {
                    // flush the current primGroup into a shape
                    ExportGroupsToShape(
                        shapes,
                        ref primGroup,
                        currentGroupName,
                        materialId,
                        v,
                        triangulate,
                        ref warning);

                    primGroup.Clear();

                    // read the next material name
                    string matName = tokens.Count > 1 ? tokens[1] : "";
                    if (!materialMap.TryGetValue(matName, out materialId))
                    {
                        // not found
                        materialId = -1;
                        warning += $"material [{matName}] not found.\n";
                    }
                }
                else if (cmd == "mtllib")
                {
                    if (tokens.Count > 1)
                    {
                        // We can have multiple filenames in the line: "mtllib file1 file2"
                        // We'll parse them all.                        
                        for (int i = 1; i < tokens.Count; i++)
                        {
                            string filename = tokens[i];
                            if (materialFilenames.Contains(filename))
                                continue; // skip repeated

                            string filePath = Path.Combine(workingDirectory, filename);
                            if (assetsDirectory != null && assetsDirectory.Exists(filePath))
                            {
                                using (var streamMtl = assetsDirectory.Open(filePath))
                                {
                                    var mtlReader = new OBJMaterialStreamReader(streamMtl);
                                    if (!mtlReader.Read(filename, materials, materialMap, out string warnMtl, out string errMtl))
                                    {
                                        // failed
                                        warning += warnMtl;
                                        error += errMtl;
                                    }
                                    else
                                    {
                                        warning += warnMtl;
                                    }
                                    materialFilenames.Add(filename);
                                }
                            }
                        }
                    }
                }
                else if (cmd == "g")
                {
                    // flush current group -> shape
                    ExportGroupsToShape(
                        shapes,
                        ref primGroup,
                        currentGroupName,
                        materialId,
                        v,
                        triangulate,
                        ref warning);

                    if (primGroup.HasData())
                    {
                        // create a new shape if there's leftover
                        // but typically ExportGroupsToShape() empties it if it is valid
                    }

                    // parse the new group name
                    if (tokens.Count > 1)
                    {
                        // we combine them if multiple
                        currentGroupName = "";
                        for (int i = 1; i < tokens.Count; i++)
                        {
                            if (i > 1)
                                currentGroupName += " ";

                            currentGroupName += tokens[i];
                        }
                    }
                    else
                    {
                        currentGroupName = "";
                    }
                }
                else if (cmd == "o")
                {
                    // flush
                    ExportGroupsToShape(
                        shapes,
                        ref primGroup,
                        currentGroupName,
                        materialId,
                        v,
                        triangulate,
                        ref warning);

                    // new shape
                    if (tokens.Count > 1)
                    {
                        currentGroupName = line.Substring(1).Trim();
                    }
                    else
                    {
                        currentGroupName = "";
                    }
                }
                else if (cmd == "s")
                {
                    // smoothing group
                    if (tokens.Count > 1)
                    {
                        if (tokens[1] == "off") currentSmoothingId = 0;
                        else
                        {
                            if (!uint.TryParse(tokens[1], out currentSmoothingId))
                            {
                                currentSmoothingId = 0;
                            }
                        }
                    }
                }
                // else unknown
            }

            // flush last primGroup
            ExportGroupsToShape(
                shapes,
                ref primGroup,
                currentGroupName,
                materialId,
                v,
                triangulate,
                ref warning);

            // Store them in the final Attrib
            if (!foundAllColors && !defaultVcolsFallback)
            {
                vc.Clear();
            }

            attrib.Vertices.AddRange(v);
            attrib.VertexWeights.AddRange(vertexWeights);
            attrib.Normals.AddRange(vn);
            attrib.Texcoords.AddRange(vt);
            attrib.TexcoordWs.AddRange(new float[vt.Count]); // not used currently
            attrib.Colors.AddRange(vc);
            attrib.SkinWeights.AddRange(vw);

            return true;
        }

        private static void ExportGroupsToShape(
            List<OBJShape> shapes,
            ref PrimGroup primGroup,
            string groupName,
            int materialId,
            List<Vector3> v,
            bool triangulate,
            ref string warning)
        {
            if (!primGroup.HasData())
                return;

            OBJShape shape = new OBJShape();
            shape.Name = groupName;

            // faceGroup => shape.Mesh
            foreach (var face in primGroup.FaceGroup)
            {
                int nVerts = face.VertexIndices.Count;
                if (nVerts < 3)
                {
                    warning += "Degenerate face found.\n";
                    continue;
                }
                if (triangulate && nVerts > 3)
                {
                    // naive ear clipping or fan approach. We'll do a fan approach for brevity:
                    // Face (v0, v1, v2, v3, ...) => (v0,v1,v2),(v0,v2,v3), ...
                    // Not robust for complex polygons, but typical for quads etc.

                    var baseIndex = face.VertexIndices[0];
                    for (int i = 1; i < nVerts - 1; i++)
                    {
                        OBJIndex i1 = face.VertexIndices[i];
                        OBJIndex i2 = face.VertexIndices[i + 1];
                        shape.Mesh.Indices.Add(baseIndex);
                        shape.Mesh.Indices.Add(i1);
                        shape.Mesh.Indices.Add(i2);

                        shape.Mesh.NumFaceVertices.Add(3);
                        shape.Mesh.MaterialIds.Add(materialId);
                        shape.Mesh.SmoothingGroupIds.Add(face.SmoothingGroupId);
                    }
                }
                else
                {
                    // store as is
                    foreach (var idx in face.VertexIndices)
                    {
                        shape.Mesh.Indices.Add(idx);
                    }
                    shape.Mesh.NumFaceVertices.Add((uint)nVerts);
                    shape.Mesh.MaterialIds.Add(materialId);
                    shape.Mesh.SmoothingGroupIds.Add(face.SmoothingGroupId);
                }
            }

            // lines
            foreach (var line in primGroup.LineGroup)
            {
                foreach (var idx in line.VertexIndices)
                {
                    shape.Lines.Indices.Add(idx);
                }
                shape.Lines.NumLineVertices.Add(line.VertexIndices.Count);
            }
            // points
            foreach (var pts in primGroup.PointsGroup)
            {
                foreach (var idx in pts.VertexIndices)
                {
                    shape.Points.Indices.Add(idx);
                }
            }

            shapes.Add(shape);
            primGroup.Clear();
        }

        private class Face
        {
            public uint SmoothingGroupId = 0;
            public List<OBJIndex> VertexIndices = new List<OBJIndex>();
        }

        private class LineElm
        {
            public List<OBJIndex> VertexIndices = new List<OBJIndex>();
        }

        private class PointsElm
        {
            public List<OBJIndex> VertexIndices = new List<OBJIndex>();
        }

        private class PrimGroup
        {
            public List<Face> FaceGroup = new List<Face>();
            public List<LineElm> LineGroup = new List<LineElm>();
            public List<PointsElm> PointsGroup = new List<PointsElm>();

            public void Clear()
            {
                FaceGroup.Clear();
                LineGroup.Clear();
                PointsGroup.Clear();
            }

            public bool HasData()
            {
                return FaceGroup.Count > 0 || LineGroup.Count > 0 || PointsGroup.Count > 0;
            }
        }
    }
}
