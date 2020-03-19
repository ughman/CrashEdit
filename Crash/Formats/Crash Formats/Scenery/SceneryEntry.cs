using System.IO;
using System.Collections.Generic;

namespace Crash
{
    public sealed class SceneryEntry : Entry
    {
        private List<SceneryVertex> vertices;
        private List<SceneryTriangle> triangles;
        private List<SceneryQuad> quads;
        private List<ModelTexture> textures;
        private List<SceneryColor> colors;
        private List<ModelExtendedTexture> animatedtextures;

        public SceneryEntry(byte[] info,IEnumerable<SceneryVertex> vertices,IEnumerable<SceneryTriangle> triangles,IEnumerable<SceneryQuad> quads,IEnumerable<ModelTexture> textures,IEnumerable<SceneryColor> colors,IEnumerable<ModelExtendedTexture> animatedtextures,int eid)
            : base(eid)
        {
            Info = info;
            this.vertices = new List<SceneryVertex>(vertices);
            this.triangles = new List<SceneryTriangle>(triangles);
            this.quads = new List<SceneryQuad>(quads);
            this.textures = new List<ModelTexture>(textures);
            this.colors = new List<SceneryColor>(colors);
            this.animatedtextures = new List<ModelExtendedTexture>(animatedtextures);
        }

        public override int Type => 3;
        public byte[] Info { get; }
        public IList<SceneryVertex> Vertices => vertices;
        public IList<SceneryTriangle> Triangles => triangles;
        public IList<SceneryQuad> Quads => quads;
        public IList<ModelTexture> Textures => textures;
        public IList<SceneryColor> Colors => colors;
        public IList<ModelExtendedTexture> AnimatedTextures => animatedtextures;

        public int XOffset
        {
            get => BitConv.FromInt32(Info,0);
            set => BitConv.ToInt32(Info,0,value);
        }

        public int YOffset
        {
            get => BitConv.FromInt32(Info,4);
            set => BitConv.ToInt32(Info,4,value);
        }

        public int ZOffset
        {
            get => BitConv.FromInt32(Info,8);
            set => BitConv.ToInt32(Info,8,value);
        }

        public override UnprocessedEntry Unprocess()
        {
            byte[][] items = new byte [7][];
            items[0] = Info;
            items[1] = new byte [Aligner.Align(vertices.Count * 6,4)];
            for (int i = 0;i < vertices.Count;i++)
            {
                vertices[i].SaveXY().CopyTo(items[1],(vertices.Count - 1 - i) * 4);
                vertices[i].SaveZ().CopyTo(items[1],vertices.Count * 4 + i * 2);
            }
            items[2] = new byte [Aligner.Align(triangles.Count * 6,4)];
            for (int i = 0;i < triangles.Count;i++)
            {
                triangles[i].SaveA().CopyTo(items[2],(triangles.Count - 1 - i) * 4);
                triangles[i].SaveB().CopyTo(items[2],triangles.Count * 4 + i * 2);
            }
            items[3] = new byte [quads.Count * 8];
            for (int i = 0;i < quads.Count;i++)
            {
                quads[i].Save().CopyTo(items[3],i * 8);
            }
            items[4] = new byte[textures.Count * 12];
            for (int i = 0; i < textures.Count; i++)
            {
                textures[i].Save().CopyTo(items[4], i * 12);
            }
            items[5] = new byte[colors.Count * 4];
            for (int i = 0; i < colors.Count; i++)
            {
                items[5][i * 4] = Colors[i].Red;
                items[5][i * 4 + 1] = Colors[i].Green;
                items[5][i * 4 + 2] = Colors[i].Blue;
                items[5][i * 4 + 3] = Colors[i].Extra;
            }
            items[6] = new byte[animatedtextures.Count * 4];
            for (int i = 0; i < animatedtextures.Count; i++)
            {
                animatedtextures[i].Save().CopyTo(items[6], i * 4);
            }
            return new UnprocessedEntry(items,EID,Type);
        }

        public byte[] ToOBJ(int start = 0)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter obj = new StreamWriter(stream))
                {
                    obj.WriteLine("# Vertices");
                    foreach (SceneryVertex vertex in vertices)
                    {
                        if (vertex.Color < Colors.Count)
                        {
                            SceneryColor color = Colors[vertex.Color];
                            obj.WriteLine("v {0} {1} {2} {3} {4} {5}", vertex.X + XOffset / 16, vertex.Y + YOffset / 16, vertex.Z + ZOffset / 16, color.Red / 255.0, color.Green / 255.0, color.Blue / 255.0);
                        }
                        else
                        {
                            obj.WriteLine("v {0} {1} {2}", vertex.X + XOffset / 16, vertex.Y + YOffset / 16, vertex.Z + ZOffset / 16);
                        }
                    }
                    obj.WriteLine();
                    obj.WriteLine("# Triangles");
                    foreach (SceneryTriangle triangle in triangles)
                    {
                        obj.WriteLine("f {0} {1} {2}", (triangle.VertexA + 1 + start), (triangle.VertexB + 1 + start), (triangle.VertexC + 1 + start));
                    }
                    obj.WriteLine();
                    obj.WriteLine("# Quads");
                    foreach (SceneryQuad quad in quads)
                    {
                        obj.WriteLine("f {0} {1} {2} {3}", (quad.VertexA + 1 + start), (quad.VertexB + 1 + start), (quad.VertexC + 1 + start), (quad.VertexD + 1 + start));
                    }
                }
                return stream.ToArray();
            }
        }

        public byte[] ToPLY(int start = 0)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter ply = new StreamWriter(stream))
                {
                    int polycount = 0;
                    foreach (SceneryTriangle triangle in triangles)
                    {
                        if (triangle.VertexA < vertices.Count - 1 && triangle.VertexB < vertices.Count - 1 && triangle.VertexC < vertices.Count - 1)
                        {
                            ++polycount;
                        }
                    }
                    foreach (SceneryQuad quad in quads)
                    {
                        if (quad.VertexA < vertices.Count - 1 && quad.VertexB < vertices.Count - 1 && quad.VertexC < vertices.Count - 1 && quad.VertexD < vertices.Count - 1)
                        {
                            ++polycount;
                        }
                    }
                    ply.WriteLine("ply");
                    ply.WriteLine("format ascii 1.0");
                    ply.WriteLine("element vertex {0}",Vertices.Count);
                    ply.WriteLine("property int x");
                    ply.WriteLine("property int y");
                    ply.WriteLine("property int z");
                    ply.WriteLine("property uchar red");
                    ply.WriteLine("property uchar green");
                    ply.WriteLine("property uchar blue");
                    ply.WriteLine("element face {0}",polycount);
                    ply.WriteLine("property list uchar int vertex_index");
                    ply.WriteLine("end_header");
                    foreach (SceneryVertex vertex in vertices)
                    {
                        if (vertex.Color < Colors.Count)
                        {
                            SceneryColor color = Colors[vertex.Color];
                            ply.WriteLine("{0} {1} {2} {3} {4} {5}", vertex.X + XOffset / 16, vertex.Y + YOffset / 16, vertex.Z + ZOffset / 16, color.Red, color.Green, color.Blue);
                        }
                        else
                        {
                            ply.WriteLine("{0} {1} {2} 255 0 255", vertex.X + XOffset / 16, vertex.Y + YOffset / 16, vertex.Z + ZOffset / 16);
                        }
                    }
                    foreach (SceneryTriangle triangle in triangles)
                    {
                        if (triangle.VertexA < vertices.Count - 1 && triangle.VertexB < vertices.Count - 1 && triangle.VertexC < vertices.Count - 1)
                        {
                            ply.WriteLine("3 {0} {1} {2}", triangle.VertexA + start, triangle.VertexB + start, triangle.VertexC + start);
                        }
                    }
                    foreach (SceneryQuad quad in quads)
                    {
                        if (quad.VertexA < vertices.Count - 1 && quad.VertexB < vertices.Count - 1 && quad.VertexC < vertices.Count - 1 && quad.VertexD < vertices.Count - 1)
                        {
                            ply.WriteLine("4 {0} {1} {2} {3}", quad.VertexA + start, quad.VertexB + start, quad.VertexC + start, quad.VertexD + start);
                        }
                    }
                }
                return stream.ToArray();
            }
        }

        /*public byte[] ToCOLLADA()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter textwriter = new StreamWriter(stream))
                using (XmlTextWriter xmlwriter = new XmlTextWriter(textwriter))
                {
                    xmlwriter.WriteStartDocument();
                    xmlwriter.WriteStartElement("COLLADA");
                    xmlwriter.WriteAttributeString("xmlns", "http://www.collada.org/2005/11/COLLADASchema");
                    xmlwriter.WriteAttributeString("version", "1.4.1");
                    xmlwriter.WriteStartElement("library_geometries");
                    xmlwriter.WriteStartElement("geometry");
                    xmlwriter.WriteStartElement("mesh");
                    xmlwriter.WriteStartElement("source");
                    xmlwriter.WriteAttributeString("id", "positions");
                    xmlwriter.WriteStartElement("float_array");
                    xmlwriter.WriteAttributeString("id", "positions-array");
                    xmlwriter.WriteAttributeString("count", (vertices.Count * 3).ToString());
                    foreach (NewSceneryVertex vertex in vertices)
                    {
                        xmlwriter.WriteValue(vertex.X + XOffset / 4);
                        xmlwriter.WriteWhitespace(" ");
                        xmlwriter.WriteValue(vertex.Y + YOffset / 4);
                        xmlwriter.WriteWhitespace(" ");
                        xmlwriter.WriteValue(vertex.Z + ZOffset / 4);
                        xmlwriter.WriteWhitespace(" ");
                    }
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("technique_common");
                    xmlwriter.WriteStartElement("accessor");
                    xmlwriter.WriteAttributeString("source", "#positions-array");
                    xmlwriter.WriteAttributeString("count", vertices.Count.ToString());
                    xmlwriter.WriteAttributeString("stride", "3");
                    xmlwriter.WriteStartElement("param");
                    xmlwriter.WriteAttributeString("name", "X");
                    xmlwriter.WriteAttributeString("type", "float");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("param");
                    xmlwriter.WriteAttributeString("name", "Y");
                    xmlwriter.WriteAttributeString("type", "float");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("param");
                    xmlwriter.WriteAttributeString("name", "Y");
                    xmlwriter.WriteAttributeString("type", "float");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("source");
                    xmlwriter.WriteAttributeString("id", "colors");
                    xmlwriter.WriteStartElement("float_array");
                    xmlwriter.WriteAttributeString("id", "colors-array");
                    xmlwriter.WriteAttributeString("count", (vertices.Count * 3).ToString());
                    foreach (NewSceneryVertex vertex in vertices)
                    {
                        if (vertex.Color < Colors.Count)
                        {
                            SceneryColor color = Colors[vertex.Color];
                            xmlwriter.WriteValue(color.Red / 256.0);
                            xmlwriter.WriteWhitespace(" ");
                            xmlwriter.WriteValue(color.Green / 256.0);
                            xmlwriter.WriteWhitespace(" ");
                            xmlwriter.WriteValue(color.Blue / 256.0);
                            xmlwriter.WriteWhitespace(" ");
                        }
                        else
                        {
                            xmlwriter.WriteValue(256.0 / 256.0);
                            xmlwriter.WriteWhitespace(" ");
                            xmlwriter.WriteValue(0 / 256.0);
                            xmlwriter.WriteWhitespace(" ");
                            xmlwriter.WriteValue(256.0 / 256.0);
                            xmlwriter.WriteWhitespace(" ");
                        }
                    }
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("technique_common");
                    xmlwriter.WriteStartElement("accessor");
                    xmlwriter.WriteAttributeString("source", "#colors-array");
                    xmlwriter.WriteAttributeString("count", vertices.Count.ToString());
                    xmlwriter.WriteAttributeString("stride", "3");
                    xmlwriter.WriteStartElement("param");
                    xmlwriter.WriteAttributeString("name", "R");
                    xmlwriter.WriteAttributeString("type", "float");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("param");
                    xmlwriter.WriteAttributeString("name", "G");
                    xmlwriter.WriteAttributeString("type", "float");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("param");
                    xmlwriter.WriteAttributeString("name", "B");
                    xmlwriter.WriteAttributeString("type", "float");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("vertices");
                    xmlwriter.WriteAttributeString("id", "vertices");
                    xmlwriter.WriteStartElement("input");
                    xmlwriter.WriteAttributeString("semantic", "POSITION");
                    xmlwriter.WriteAttributeString("source", "positions");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("input");
                    xmlwriter.WriteAttributeString("semantic", "COLOR");
                    xmlwriter.WriteAttributeString("source", "colors");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("triangles");
                    xmlwriter.WriteAttributeString("count", triangles.Count.ToString());
                    xmlwriter.WriteStartElement("input");
                    xmlwriter.WriteAttributeString("semantic", "VERTEX");
                    xmlwriter.WriteAttributeString("source", "vertices");
                    xmlwriter.WriteAttributeString("offset", "0");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("p");
                    foreach (SceneryTriangle triangle in triangles)
                    {
                        xmlwriter.WriteValue(triangle.VertexA);
                        xmlwriter.WriteWhitespace(" ");
                        xmlwriter.WriteValue(triangle.VertexB);
                        xmlwriter.WriteWhitespace(" ");
                        xmlwriter.WriteValue(triangle.VertexC);
                        xmlwriter.WriteWhitespace(" ");
                    }
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndDocument();
                }
                return stream.ToArray();
            }
        }*/
    }
}
