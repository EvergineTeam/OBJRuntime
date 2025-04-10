// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics;
using System.IO;
using OBJRuntime.DataTypes;
using System.Collections.Generic;
using OBJRuntime.Readers;
using System.Linq;
using Evergine.Mathematics;

namespace OBJTests
{
    public class OBJLoaderTests
    {
        private AssetsDirectory assetsDirectory;

        public OBJLoaderTests()
        {
            this.assetsDirectory = new AssetsDirectory();
        }

        [Fact]
        public void CheckVertexValues()
        {
            // Arrange
            var expectedVertices = new Vector3[]
            {
                new Vector3(0.000000f, 2.000000f, 2.000000f),
                new Vector3(0.000000f, 0.000000f, 2.000000f),
                new Vector3(2.000000f, 0.000000f, 2.000000f),
                new Vector3(2.000000f, 2.000000f, 2.000000f),
                new Vector3(0.000000f, 2.000000f, 0.000000f),
                new Vector3(0.000000f, 0.000000f, 0.000000f),
                new Vector3(2.000000f, 0.000000f, 0.000000f),
                new Vector3(2.000000f, 2.000000f, 0.000000f)
            };

            var attrib = new OBJAttrib();
            var shapes = new List<OBJShape>();
            var materials = new List<OBJMaterial>();
            var warning = "";
            var error = "";

            // Act
            using (var streamObj = assetsDirectory.Open("Cube.obj"))
            {
                using (StreamReader srObj = new StreamReader(streamObj))
                {
                    bool ok = OBJLoader.Load(srObj, ref attrib, shapes, materials, ref warning, ref error, null, true, true);

                    // Assert
                    Assert.True(ok);
                    Assert.Equal(expectedVertices.Length, attrib.Vertices.Count);
                    for (int i = 0; i < expectedVertices.Length; i++)
                    {
                        Assert.Equal(expectedVertices[i], attrib.Vertices[i]);
                    }
                }
            }
        }

        [Fact]
        public void CheckShapeNames()
        {
            // Arrange
            var attrib = new OBJAttrib();
            var shapes = new List<OBJShape>();
            var materials = new List<OBJMaterial>();
            var warning = "";
            var error = "";

            // Act
            using (var streamObj = assetsDirectory.Open("Cube.obj"))
            {
                using (StreamReader srObj = new StreamReader(streamObj))
                {
                    bool ok = OBJLoader.Load(srObj, ref attrib, shapes, materials, ref warning, ref error, null, true, true);

                    // Assert
                    Assert.True(ok);
                    Assert.Equal(6, shapes.Count);
                    Assert.Equal("front cube", shapes[0].Name);
                    Assert.Equal("back cube", shapes[1].Name);
                    Assert.Equal("right cube", shapes[2].Name);
                    Assert.Equal("top cube", shapes[3].Name);
                    Assert.Equal("left cube", shapes[4].Name);
                    Assert.Equal("bottom cube", shapes[5].Name);
                }
            }
        }

        [Fact]
        public void CheckFaceTessellation()
        {
            // Arrange
            var expectedVertices = new float[] { 0, 1, 2, 0, 2, 3 };

            var attrib = new OBJAttrib();
            var shapes = new List<OBJShape>();
            var materials = new List<OBJMaterial>();
            var warning = "";
            var error = "";

            // Act
            using (var streamObj = assetsDirectory.Open("Cube.obj"))
            {
                using (StreamReader srObj = new StreamReader(streamObj))
                {
                    bool ok = OBJLoader.Load(srObj, ref attrib, shapes, materials, ref warning, ref error, null, true, true);

                    // Assert
                    Assert.True(ok);

                    var indices = shapes[0].Mesh.Indices;
                    Assert.Equal(expectedVertices.Length, indices.Count);
                    for (int i = 0; i < expectedVertices.Length; i++)
                    {
                        Assert.Equal(expectedVertices[i], indices[i].VertexIndex);
                    }
                }
            }
        }

        [Fact]
        public void CheckDiffuseMaterials()
        {
            // Arrange
            var expectedColor1 = new float[] { 1, 1, 1 };
            var expectedColor2 = new float[] { 1, 0, 0 };
            var expectedColor3 = new float[] { 0, 1, 0 };
            var expectedColor4 = new float[] { 0, 0, 1 };
            var expectedColor5 = new float[] { 1, 1, 1 };

            var attrib = new OBJAttrib();
            var shapes = new List<OBJShape>();
            var materials = new List<OBJMaterial>();
            var warning = "";
            var error = "";

            // Act
            using (var streamObj = assetsDirectory.Open("Cube.obj"))
            using (var streamMtl = assetsDirectory.Open("Cube.mtl"))
            {
                using (StreamReader srObj = new StreamReader(streamObj))
                {
                    var mtlReader = new OBJMaterialStreamReader(streamMtl);
                    bool ok = OBJLoader.Load(srObj, ref attrib, shapes, materials, ref warning, ref error, mtlReader, true, true);

                    // Assert
                    Assert.True(ok);
                    Assert.Equal(5, materials.Count);
                    Assert.True(expectedColor1.SequenceEqual(materials[0].Diffuse));
                    Assert.True(expectedColor2.SequenceEqual(materials[1].Diffuse));
                    Assert.True(expectedColor3.SequenceEqual(materials[2].Diffuse));
                    Assert.True(expectedColor4.SequenceEqual(materials[3].Diffuse));
                    Assert.True(expectedColor5.SequenceEqual(materials[4].Diffuse));
                }
            }
        }

        [Fact]
        public void CheckVertexWeights()
        {
            // Arrange
            var expectedW = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f };

            var attrib = new OBJAttrib();
            var shapes = new List<OBJShape>();
            var materials = new List<OBJMaterial>();
            var warning = "";
            var error = "";

            // Act
            using (var streamObj = assetsDirectory.Open("cube-vertex-w-component.obj"))
            {
                using (StreamReader srObj = new StreamReader(streamObj))
                {
                    bool ok = OBJLoader.Load(srObj, ref attrib, shapes, materials, ref warning, ref error, null, true, true);

                    // Assert
                    Assert.True(ok);
                    Assert.True(attrib.VertexWeights.SequenceEqual(expectedW));
                }
            }
        }

        [Fact]
        public void TestPointCloud()
        {
            // Arrange
            var expectedVertices = new Vector3[]
            {
                new Vector3(-0.207717f, -0.953997f, 2.554110f),
                new Vector3(-0.275607f, -0.965401f, 2.541530f),
                new Vector3(-0.270155f, -0.963170f, 2.548000f) };
            var expectedNormals = new float[] { -0.281034f, -0.057252f, 0.957989f, -0.139126f, -0.135672f, 0.980937f, -0.163133f, -0.131576f, 0.977791f };

            var attrib = new OBJAttrib();
            var shapes = new List<OBJShape>();
            var materials = new List<OBJMaterial>();
            var warning = "";
            var error = "";

            // Act
            using (var streamObj = assetsDirectory.Open("point-cloud.obj"))
            {
                using (StreamReader srObj = new StreamReader(streamObj))
                {
                    bool ok = OBJLoader.Load(srObj, ref attrib, shapes, materials, ref warning, ref error, null, true, true);

                    // Assert
                    Assert.True(ok);
                    Assert.Equal(9, attrib.Vertices.Count);
                    Assert.Empty(shapes);
                    Assert.True(attrib.Vertices.SequenceEqual(expectedVertices));
                    Assert.True(attrib.Normals.SequenceEqual(expectedNormals));
                }
            }
        }

        [Fact]
        public void TestPoints()
        {
            // Arrange
            var attrib = new OBJAttrib();
            var shapes = new List<OBJShape>();
            var materials = new List<OBJMaterial>();
            var warning = "";
            var error = "";

            // Act
            using (var streamObj = assetsDirectory.Open("testpoints.obj"))
            {
                using (StreamReader srObj = new StreamReader(streamObj))
                {
                    bool ok = OBJLoader.Load(srObj, ref attrib, shapes, materials, ref warning, ref error, null, true, true);

                    // Assert
                    Assert.True(ok);
                    Assert.Equal(24, attrib.Vertices.Count);
                    Assert.Single(shapes);
                    Assert.Equal(24, shapes[0].Points.Indices.Count);
                    Assert.Equal(3, shapes[0].Points.Indices[15].VertexIndex);
                }
            }
        }

        [Fact]
        public void TestLines()
        {
            // Arrange
            var attrib = new OBJAttrib();
            var shapes = new List<OBJShape>();
            var materials = new List<OBJMaterial>();
            var warning = "";
            var error = "";

            // Act
            using (var streamObj = assetsDirectory.Open("testlines.obj"))
            {
                using (StreamReader srObj = new StreamReader(streamObj))
                {
                    bool ok = OBJLoader.Load(srObj, ref attrib, shapes, materials, ref warning, ref error, null, true, true);

                    // Assert
                    Assert.True(ok);
                    Assert.Equal(24, attrib.Vertices.Count);
                    Assert.Single(shapes);
                    Assert.Equal(24, shapes[0].Lines.Indices.Count);
                    Assert.Equal(7, shapes[0].Lines.Indices[17].VertexIndex);
                }
            }
        }

        [Fact]
        public void CheckTexcoords()
        {
            // Arrange
            var expectedTexcoords = new float[] { 1.0f, 2.0f, 3.0f,
                                                 2.0f, 3.0f, 1.0f,
                                                 3.0f, 1.0f, 2.0f,
                                                 1.0f, 2.0f, 3.0f};

            var attrib = new OBJAttrib();
            var shapes = new List<OBJShape>();
            var materials = new List<OBJMaterial>();
            var warning = "";
            var error = "";

            // Act
            using (var streamObj = assetsDirectory.Open("multiple-spaces.obj"))
            {
                using (StreamReader srObj = new StreamReader(streamObj))
                {
                    bool ok = OBJLoader.Load(srObj, ref attrib, shapes, materials, ref warning, ref error, null, true, true);

                    // Assert
                    Assert.True(ok);
                    Assert.Equal(12, attrib.Vertices.Count);
                    Assert.Equal(8, attrib.Texcoords.Count);
                    Assert.True(attrib.Texcoords.SequenceEqual(expectedTexcoords));
                    Assert.Single(shapes);
                }
            }
        }

        [Fact]
        public void CheckNormals()
        {
            // Arrange
            var expectedNormals = new float[] {  0.0f,  0.0f,  1.0f,
                                                   0.0f,  0.0f, -1.0f,
                                                   0.0f,  1.0f,  0.0f,
                                                   0.0f, -1.0f,  0.0f,
                                                   1.0f,  0.0f,  0.0f,
                                                  -1.0f,  0.0f,  0.0f};

            var attrib = new OBJAttrib();
            var shapes = new List<OBJShape>();
            var materials = new List<OBJMaterial>();
            var warning = "";
            var error = "";

            // Act
            using (var streamObj = assetsDirectory.Open("cube-normals.obj"))
            {
                using (StreamReader srObj = new StreamReader(streamObj))
                {
                    bool ok = OBJLoader.Load(srObj, ref attrib, shapes, materials, ref warning, ref error, null, true, true);

                    // Assert
                    Assert.True(ok);
                    Assert.Equal(24, attrib.Vertices.Count);
                    Assert.Equal(18, attrib.Normals.Count);
                    Assert.True(attrib.Normals.SequenceEqual(expectedNormals));
                    Assert.Single(shapes);
                }
            }
        }
    }
}