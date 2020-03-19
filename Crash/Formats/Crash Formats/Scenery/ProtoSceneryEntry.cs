using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;

namespace Crash
{
    public sealed class ProtoSceneryEntry : Entry
    {
        private List<ProtoSceneryPolygon> polygons;
        private List<ProtoSceneryVertex> vertices;
        private List<OldModelStruct> structs;

        public ProtoSceneryEntry(byte[] info,IEnumerable<ProtoSceneryPolygon> polygons,IEnumerable<ProtoSceneryVertex> vertices,IEnumerable<OldModelStruct> structs,short? pad,int eid) : base(eid)
        {
            Info = info ?? throw new ArgumentNullException("info");
            this.polygons = new List<ProtoSceneryPolygon>(polygons);
            this.vertices = new List<ProtoSceneryVertex>(vertices);
            this.structs = new List<OldModelStruct>(structs);
            Pad = pad;
        }

        public override int Type => 3;
        public byte[] Info { get; }
        public short? Pad { get; }

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

        public IList<ProtoSceneryPolygon> Polygons => polygons;
        public IList<ProtoSceneryVertex> Vertices => vertices;
        public IList<OldModelStruct> Structs => structs;

        public override UnprocessedEntry Unprocess()
        {
            byte[][] items = new byte [3][];
            items[0] = Info;
            items[1] = new byte [polygons.Count * 12];
            for (int i = 0;i < polygons.Count;i++)
            {
                polygons[i].Save().CopyTo(items[1],i * 12);
            }
            items[2] = new byte [vertices.Count * 6 + (!Pad.HasValue ? 0 : 2)];
            for (int i = 0;i < vertices.Count;i++)
            {
                vertices[i].Save().CopyTo(items[2],i * 6);
            }
            if (Pad.HasValue)
            {
                BitConv.ToInt16(items[2],vertices.Count*6,Pad.Value);
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
                    foreach (ProtoSceneryVertex vertex in vertices)
                    {
                        obj.WriteLine("v {0} {1} {2}",vertex.X + XOffset,vertex.Y + YOffset, vertex.Z + ZOffset);
                    }
                    obj.WriteLine();
                    obj.WriteLine("# Polygons");
                    foreach (ProtoSceneryPolygon polygon in polygons)
                    {
                        obj.WriteLine("f {0} {1} {2}", (polygon.VertexA + 1 + start), (polygon.VertexB + 1 + start) , (polygon.VertexC + 1 + start));
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
                    foreach (ProtoSceneryVertex vertex in vertices)
                    {
                        ply.WriteLine("{0} {1} {2} 255 0 255", vertex.X + XOffset, vertex.Y + YOffset, vertex.Z + ZOffset);
                    }
                    ply.WriteLine();
                    ply.WriteLine("# Polygons");
                    foreach (ProtoSceneryPolygon polygon in polygons)
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
                    foreach (ProtoSceneryVertex vertex in vertices)
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
                    xmlwriter.WriteAttributeString("name","Z");
                    xmlwriter.WriteAttributeString("type","float");
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("source");
                    xmlwriter.WriteStartElement("float_array");
                    xmlwriter.WriteAttributeString("count",(vertices.Count * 3).ToString());
                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteStartElement("technique_common");
                    xmlwriter.WriteStartElement("accessor");
                    xmlwriter.WriteAttributeString("count",vertices.Count.ToString());
                    xmlwriter.WriteAttributeString("stride","3");
                    xmlwriter.WriteStartElement("param");
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
                    foreach (ProtoSceneryPolygon polygon in polygons)
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
