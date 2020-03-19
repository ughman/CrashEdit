using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;

namespace Crash
{
    public sealed class OldSceneryEntry : Entry
    {
        private List<OldSceneryPolygon> polygons;
        private List<OldSceneryVertex> vertices;
        private List<OldModelStruct> structs;

        public OldSceneryEntry(byte[] info,IEnumerable<OldSceneryPolygon> polygons,IEnumerable<OldSceneryVertex> vertices,IEnumerable<OldModelStruct> structs,byte[] extradata,int eid) : base(eid)
        {
            Info = info ?? throw new ArgumentNullException("info");
            this.polygons = new List<OldSceneryPolygon>(polygons);
            this.vertices = new List<OldSceneryVertex>(vertices);
            this.structs = new List<OldModelStruct>(structs);
            ExtraData = extradata;
        }

        public override int Type => 3;
        public byte[] Info { get; }
        public byte[] ExtraData { get; }

        public int XOffset
        {
            get => BitConv.FromInt32(Info, 0);
            set => BitConv.ToInt32(Info, 0, value);
        }

        public int YOffset
        {
            get => BitConv.FromInt32(Info, 4);
            set => BitConv.ToInt32(Info, 4, value);
        }

        public int ZOffset
        {
            get => BitConv.FromInt32(Info, 8);
            set => BitConv.ToInt32(Info, 8, value);
        }

        public IList<OldSceneryPolygon> Polygons => polygons;
        public IList<OldSceneryVertex> Vertices => vertices;
        public IList<OldModelStruct> Structs => structs;

        public override UnprocessedEntry Unprocess()
        {
            byte[][] items = new byte [ExtraData == null ? 3 : 4][];
            items[0] = Info;
            items[1] = new byte [polygons.Count * 8];
            for (int i = 0;i < polygons.Count;i++)
            {
                polygons[i].Save().CopyTo(items[1],i * 8);
            }
            items[2] = new byte [vertices.Count * 8];
            for (int i = 0;i < vertices.Count;i++)
            {
                vertices[i].Save().CopyTo(items[2],i * 8);
            }
            if (ExtraData != null)
            {
                items[3] = ExtraData;
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
                    foreach (OldSceneryVertex vertex in vertices)
                    {
                        obj.WriteLine("v {0} {1} {2}",vertex.X + XOffset,vertex.Y + YOffset,vertex.Z + ZOffset);
                    }
                    obj.WriteLine();
                    obj.WriteLine("# Polygons");
                    foreach (OldSceneryPolygon polygon in polygons)
                    {
                        obj.WriteLine("f {0} {1} {2}",(polygon.VertexA + 1 + start),(polygon.VertexB + 1 + start),(polygon.VertexC + 1 + start));
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
                    ply.WriteLine("ply");
                    ply.WriteLine("format ascii 1.0");
                    ply.WriteLine("element vertex {0}", Vertices.Count);
                    ply.WriteLine("property int x");
                    ply.WriteLine("property int y");
                    ply.WriteLine("property int z");
                    ply.WriteLine("property uchar red");
                    ply.WriteLine("property uchar green");
                    ply.WriteLine("property uchar blue");
                    ply.WriteLine("element face {0}", polygons.Count);
                    ply.WriteLine("property list uchar int vertex_index");
                    ply.WriteLine("end_header");
                    ply.WriteLine("# Vertices");
                    foreach (OldSceneryVertex vertex in vertices)
                    {
                        ply.WriteLine("{0} {1} {2} {3} {4} {5}", vertex.X + XOffset, vertex.Y + YOffset, vertex.Z + ZOffset, vertex.Red, vertex.Green, vertex.Blue);
                    }
                    ply.WriteLine();
                    ply.WriteLine("# Polygons");
                    foreach (OldSceneryPolygon polygon in polygons)
                    {
                        ply.WriteLine("{0} {1} {2} 255 0 255", (polygon.VertexA + 1 + start), (polygon.VertexB + 1 + start), (polygon.VertexC + 1 + start));
                    }
                }
                return stream.ToArray();
            }
        }

        public byte[] ToCOLLADA()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter textwriter = new StreamWriter(stream))
                using (XmlTextWriter xmlwriter = new XmlTextWriter(textwriter))
                {
                    xmlwriter.WriteStartDocument();
                    xmlwriter.WriteStartElement("COLLADA");
                    xmlwriter.WriteAttributeString("xmlns","http://www.collada.org/2005/11/COLLADASchema");
                    xmlwriter.WriteAttributeString("version","1.4.1");
                    xmlwriter.WriteStartElement("library_geometries");
                    xmlwriter.WriteStartElement("geometry");
                    xmlwriter.WriteStartElement("mesh");
                    xmlwriter.WriteStartElement("source");
                    xmlwriter.WriteAttributeString("id","positions");
                    xmlwriter.WriteStartElement("float_array");
                    xmlwriter.WriteAttributeString("id","positions-array");
                    xmlwriter.WriteAttributeString("count",(vertices.Count * 3).ToString());
                    foreach (OldSceneryVertex vertex in vertices)
                    {
                        xmlwriter.WriteValue(vertex.X);
                        xmlwriter.WriteWhitespace(" ");
                        xmlwriter.WriteValue(vertex.Y);
                        xmlwriter.WriteWhitespace(" ");
                        xmlwriter.WriteValue(vertex.Z);
                        xmlwriter.WriteWhitespace(" ");
                    }
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("technique_common");
                    xmlwriter.WriteStartElement("accessor");
                    xmlwriter.WriteAttributeString("source","#positions-array");
                    xmlwriter.WriteAttributeString("count",vertices.Count.ToString());
                    xmlwriter.WriteAttributeString("stride","3");
                    xmlwriter.WriteStartElement("param");
                    xmlwriter.WriteAttributeString("name","X");
                    xmlwriter.WriteAttributeString("type","float");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("param");
                    xmlwriter.WriteAttributeString("name","Y");
                    xmlwriter.WriteAttributeString("type","float");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("param");
                    xmlwriter.WriteAttributeString("name","Y");
                    xmlwriter.WriteAttributeString("type","float");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("source");
                    xmlwriter.WriteAttributeString("id","colors");
                    xmlwriter.WriteStartElement("float_array");
                    xmlwriter.WriteAttributeString("id","colors-array");
                    xmlwriter.WriteAttributeString("count",(vertices.Count * 3).ToString());
                    foreach (OldSceneryVertex vertex in vertices)
                    {
                        xmlwriter.WriteValue(vertex.Red / 256.0);
                        xmlwriter.WriteWhitespace(" ");
                        xmlwriter.WriteValue(vertex.Green / 256.0);
                        xmlwriter.WriteWhitespace(" ");
                        xmlwriter.WriteValue(vertex.Blue / 256.0);
                        xmlwriter.WriteWhitespace(" ");
                    }
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("technique_common");
                    xmlwriter.WriteStartElement("accessor");
                    xmlwriter.WriteAttributeString("source","#colors-array");
                    xmlwriter.WriteAttributeString("count",vertices.Count.ToString());
                    xmlwriter.WriteAttributeString("stride","3");
                    xmlwriter.WriteStartElement("param");
                    xmlwriter.WriteAttributeString("name","R");
                    xmlwriter.WriteAttributeString("type","float");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("param");
                    xmlwriter.WriteAttributeString("name","G");
                    xmlwriter.WriteAttributeString("type","float");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("param");
                    xmlwriter.WriteAttributeString("name","B");
                    xmlwriter.WriteAttributeString("type","float");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("vertices");
                    xmlwriter.WriteAttributeString("id","vertices");
                    xmlwriter.WriteStartElement("input");
                    xmlwriter.WriteAttributeString("semantic","POSITION");
                    xmlwriter.WriteAttributeString("source","positions");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("input");
                    xmlwriter.WriteAttributeString("semantic","COLOR");
                    xmlwriter.WriteAttributeString("source","colors");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("triangles");
                    xmlwriter.WriteAttributeString("count",polygons.Count.ToString());
                    xmlwriter.WriteStartElement("input");
                    xmlwriter.WriteAttributeString("semantic","VERTEX");
                    xmlwriter.WriteAttributeString("source","vertices");
                    xmlwriter.WriteAttributeString("offset","0");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("p");
                    foreach (OldSceneryPolygon polygon in polygons)
                    {
                        xmlwriter.WriteValue(polygon.VertexA);
                        xmlwriter.WriteWhitespace(" ");
                        xmlwriter.WriteValue(polygon.VertexB);
                        xmlwriter.WriteWhitespace(" ");
                        xmlwriter.WriteValue(polygon.VertexC);
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
        }
    }
}
