using Crash;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CrashEdit
{
    public sealed class ProtoZoneEntryViewer : ProtoSceneryEntryViewer
    {
        private static byte[] stipplea;
        private static byte[] stippleb;

        static ProtoZoneEntryViewer()
        {
            stipplea = new byte[128];
            stippleb = new byte[128];
            for (int i = 0; i < 128; i += 8)
            {
                stipplea[i + 0] = 0x55;
                stipplea[i + 1] = 0x55;
                stipplea[i + 2] = 0x55;
                stipplea[i + 3] = 0x55;
                stipplea[i + 4] = 0xAA;
                stipplea[i + 5] = 0xAA;
                stipplea[i + 6] = 0xAA;
                stipplea[i + 7] = 0xAA;
                stippleb[i + 0] = 0xAA;
                stippleb[i + 1] = 0xAA;
                stippleb[i + 2] = 0xAA;
                stippleb[i + 3] = 0xAA;
                stippleb[i + 4] = 0x55;
                stippleb[i + 5] = 0x55;
                stippleb[i + 6] = 0x55;
                stippleb[i + 7] = 0x55;
            }
        }

        private ProtoZoneEntry entry;
        private ProtoZoneEntry[] linkedentries;
        private bool renderoctree;
        private int[] octreedisplaylists;
        private Dictionary<short,Color> octreevalues;
        private int octreeselection;
        private bool deletelists;
        private bool polygonmode;
        private bool allentries;
        private Form frmoctree;
        private bool flipoctree;

        public ProtoZoneEntryViewer(ProtoZoneEntry entry,ProtoSceneryEntry[] linkedsceneryentries,TextureChunk[][] texturechunks,ProtoZoneEntry[] linkedentries) : base(linkedsceneryentries,texturechunks)
        {
            this.entry = entry;
            this.linkedentries = linkedentries;
            renderoctree = false;
            octreedisplaylists = new int[linkedentries.Length + 1];
            for (int i = 0; i < octreedisplaylists.Length; i++)
            {
                octreedisplaylists[i] = -1;
            }
            octreevalues = new Dictionary<short,Color>();
            octreeselection = -1;
            deletelists = false;
            polygonmode = false;
            allentries = false;
            flipoctree = false;
        }
        public ProtoZoneEntryViewer(List<ProtoZoneEntry> entries, ProtoSceneryEntry[] linkedsceneryentries, TextureChunk[][] texturechunks) : base(linkedsceneryentries, texturechunks)
        {
            this.entry = entries[0];
            entries.Remove(this.entry);
            this.linkedentries = entries.ToArray();
            renderoctree = false;
            octreedisplaylists = new int[linkedentries.Length + 1];
            for (int i = 0; i < octreedisplaylists.Length; i++)
            {
                octreedisplaylists[i] = -1;
            }
            octreevalues = new Dictionary<short, Color>();
            octreeselection = -1;
            deletelists = false;
            polygonmode = false;
            allentries = false;
            flipoctree = false;
        }

        protected override IEnumerable<IPosition> CorePositions
        {
            get
            {
                int xoffset = BitConv.FromInt32(entry.Layout,0);
                int yoffset = BitConv.FromInt32(entry.Layout,4);
                int zoffset = BitConv.FromInt32(entry.Layout,8);
                yield return new Position(xoffset,yoffset,zoffset);
                int x2 = BitConv.FromInt32(entry.Layout,12);
                int y2 = BitConv.FromInt32(entry.Layout,16);
                int z2 = BitConv.FromInt32(entry.Layout,20);
                yield return new Position(x2 + xoffset,y2 + yoffset,z2 + zoffset);
                foreach (ProtoEntity entity in entry.Entities)
                {
                    int x = entity.StartX/4 + xoffset;
                    int y = entity.StartY/4 + yoffset;
                    int z = entity.StartZ/4 + zoffset;
                    yield return new Position(x,y,z);
                    foreach (ProtoEntityPosition delta in entity.Deltas)
                    {
                        x += delta.X*2;
                        y += delta.Y*2;
                        z += delta.Z*2;
                        yield return new Position(x,y,z);
                    }
                }
                foreach (OldCamera camera in entry.Cameras)
                {
                    foreach (OldCameraPosition position in camera.Positions)
                    {
                        int x = position.X + xoffset;
                        int y = position.Y + yoffset;
                        int z = position.Z + zoffset;
                        yield return new Position(x,y,z);
                    }
                }
            }
        }

        protected override int CameraRangeMinimum => 800;

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.X:
                case Keys.C:
                case Keys.R:
                case Keys.V:
                case Keys.F:
                case Keys.O:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.X:
                    renderoctree = !renderoctree;
                    break;
                case Keys.C:
                    if (frmoctree == null || frmoctree.IsDisposed)
                    {
                        frmoctree = new Form();
                        frmoctree.FormClosed += (object sender, FormClosedEventArgs ee) =>
                        {
                            octreeselection = -1;
                            deletelists = true;
                        };
                        UpdateOctreeFormList();
                        frmoctree.Show();
                    }
                    else
                    {
                        frmoctree.Select();
                    }
                    break;
                case Keys.R:
                    deletelists = true;
                    UpdateOctreeFormList();
                    break;
                case Keys.V:
                    polygonmode = !polygonmode;
                    break;
                case Keys.F:
                    allentries = !allentries;
                    UpdateOctreeFormList();
                    break;
                case Keys.O:
                    flipoctree = !flipoctree;
                    deletelists = true;
                    break;
            }
        }

        internal void UpdateOctreeFormList()
        {
            if (frmoctree != null && !frmoctree.IsDisposed)
            {
                frmoctree.Controls.Clear();
                ListView lst = new ListView();
                lst.Dock = DockStyle.Fill;
                foreach (KeyValuePair<short,Color> color in octreevalues)
                {
                    ListViewItem lsi = new ListViewItem();
                    lsi.Text = string.Format("{2:X2}:{1:X2}:{0:X1}",color.Key >> 1 & 0x7,color.Key >> 4 & 0x3F,color.Key >> 10 & 0x3F);
                    lsi.BackColor = color.Value;
                    lsi.ForeColor = color.Value.GetBrightness() >= 0.5 ? Color.Black : Color.White;
                    lsi.Tag = color.Key;
                    lst.Items.Add(lsi);
                }
                lst.SelectedIndexChanged += delegate (object sender,EventArgs ee)
                {
                    if (lst.SelectedItems.Count == 0)
                    {
                        octreeselection = -1;
                    }
                    else
                    {
                        octreeselection = (ushort)(short)lst.SelectedItems[0].Tag;
                    }
                    deletelists = true;
                };
                frmoctree.Controls.Add(lst);
            }
        }

        protected override void RenderObjects()
        {
            GL.Disable(EnableCap.Texture2D);
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.RgbScale, 1.0f);
            RenderEntry(entry,ref octreedisplaylists[0]);
            GL.Enable(EnableCap.PolygonStipple);
            for (int i = 0; i < linkedentries.Length; i++)
            {
                ProtoZoneEntry linkedentry = linkedentries[i];
                if (linkedentry == entry)
                    continue;
                if (linkedentry == null)
                    continue;
                RenderLinkedEntry(linkedentry,ref octreedisplaylists[i + 1]);
            }
            GL.Disable(EnableCap.PolygonStipple);
            if (deletelists)
                deletelists = false;
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.RgbScale, 2.0f);
            GL.Enable(EnableCap.Texture2D);
            base.RenderObjects();
        }

        private void RenderEntry(ProtoZoneEntry entry,ref int octreedisplaylist)
        {
            int xoffset = BitConv.FromInt32(entry.Layout,0);
            int yoffset = BitConv.FromInt32(entry.Layout,4);
            int zoffset = BitConv.FromInt32(entry.Layout,8);
            int x2 = BitConv.FromInt32(entry.Layout,12);
            int y2 = BitConv.FromInt32(entry.Layout,16);
            int z2 = BitConv.FromInt32(entry.Layout,20);
            GL.PushMatrix();
            GL.Translate(xoffset,yoffset,zoffset);
            if (deletelists)
            {
                GL.DeleteLists(octreedisplaylist,1);
                octreedisplaylist = -1;
            }
            if (renderoctree)
            {
                if (!polygonmode)
                    GL.PolygonMode(MaterialFace.FrontAndBack,PolygonMode.Line);
                if (octreedisplaylist == -1)
                {
                    octreedisplaylist = GL.GenLists(1);
                    GL.NewList(octreedisplaylist,ListMode.CompileAndExecute);
                    GL.PushMatrix();
                    int xmax = (ushort)BitConv.FromInt16(entry.Layout,0x1E);
                    int ymax = (ushort)BitConv.FromInt16(entry.Layout,0x20);
                    if (ymax == 0) ymax = xmax;
                    int zmax = (ushort)BitConv.FromInt16(entry.Layout,0x22);
                    if (zmax == 0) zmax = ymax;
                    RenderOctree(entry.Layout,0x1C,0,flipoctree ? -y2 : 0,flipoctree ? -z2 : 0,x2,y2,z2,xmax,ymax,zmax);
                    GL.PopMatrix();
                    GL.EndList();
                }
                else
                {
                    GL.CallList(octreedisplaylist);
                }
                GL.PolygonMode(MaterialFace.FrontAndBack,PolygonMode.Fill);
            }
            GL.Color3(Color.White);
            GL.Begin(PrimitiveType.LineStrip);
            GL.Vertex3(0,0,0);
            GL.Vertex3(x2,0,0);
            GL.Vertex3(x2,y2,0);
            GL.Vertex3(0,y2,0);
            GL.Vertex3(0,0,0);
            GL.Vertex3(0,0,z2);
            GL.Vertex3(x2,0,z2);
            GL.Vertex3(x2,y2,z2);
            GL.Vertex3(0,y2,z2);
            GL.Vertex3(0,0,z2);
            GL.Vertex3(x2,0,z2);
            GL.Vertex3(x2,0,0);
            GL.Vertex3(x2,y2,0);
            GL.Vertex3(x2,y2,z2);
            GL.Vertex3(0,y2,z2);
            GL.Vertex3(0,y2,0);
            GL.End();
            GL.Scale(4,4,4);
            foreach (ProtoEntity entity in entry.Entities)
            {
                RenderEntity(entity);
            }
            foreach (OldCamera camera in entry.Cameras)
            {
                RenderCamera(camera);
            }
            GL.PopMatrix();
        }

        private void RenderLinkedEntry(ProtoZoneEntry entry,ref int octreedisplaylist)
        {
            int xoffset = BitConv.FromInt32(entry.Layout,0);
            int yoffset = BitConv.FromInt32(entry.Layout,4);
            int zoffset = BitConv.FromInt32(entry.Layout,8);
            int x2 = BitConv.FromInt32(entry.Layout,12);
            int y2 = BitConv.FromInt32(entry.Layout,16);
            int z2 = BitConv.FromInt32(entry.Layout,20);
            GL.PushMatrix();
            GL.Translate(xoffset,yoffset,zoffset);
            if (allentries)
            {
                GL.Disable(EnableCap.PolygonStipple);
                if (deletelists)
                {
                    GL.DeleteLists(octreedisplaylist,1);
                    octreedisplaylist = -1;
                }
                if (renderoctree)
                {
                    if (!polygonmode)
                        GL.PolygonMode(MaterialFace.FrontAndBack,PolygonMode.Line);
                    if (octreedisplaylist == -1)
                    {
                        octreedisplaylist = GL.GenLists(1);
                        GL.NewList(octreedisplaylist,ListMode.CompileAndExecute);
                        GL.PushMatrix();
                        int xmax = (ushort)BitConv.FromInt16(entry.Layout,0x1E);
                        int ymax = (ushort)BitConv.FromInt16(entry.Layout,0x20);
                        if (ymax == 0) ymax = xmax;
                        int zmax = (ushort)BitConv.FromInt16(entry.Layout,0x22);
                        if (zmax == 0) zmax = ymax;
                        RenderOctree(entry.Layout,0x1C,0,flipoctree ? -y2 : 0,flipoctree ? -z2 : 0,x2,y2,z2,xmax,ymax,zmax);
                        GL.PopMatrix();
                        GL.EndList();
                    }
                    else
                    {
                        GL.CallList(octreedisplaylist);
                    }
                    GL.PolygonMode(MaterialFace.FrontAndBack,PolygonMode.Fill);
                }
                GL.Enable(EnableCap.PolygonStipple);
            }
            GL.Scale(4,4,4);
            foreach (ProtoEntity entity in entry.Entities)
            {
                RenderEntity(entity);
            }
            foreach (OldCamera camera in entry.Cameras)
            {
                RenderCamera(camera);
            }
            GL.PopMatrix();
        }

        private void RenderOctree(byte[] data,int offset,double x,double y,double z,double w,double h,double d,int xmax,int ymax,int zmax)
        {
            int value = (ushort)BitConv.FromInt16(data,offset);
            if ((value & 1) != 0)
            {
                if (flipoctree)
                {
                    GL.Scale(1, -1, -1);
                }
                Color color;
                if (!octreevalues.TryGetValue((short)value,out color))
                {
                    byte[] colorbuf = new byte[3];
                    Random random = new Random(value);
                    random.NextBytes(colorbuf);
                    color = Color.FromArgb(255,colorbuf[0],colorbuf[1],colorbuf[2]);
                    octreevalues.Add((short)value,color);
                }
                if (octreeselection != -1 && octreeselection != value)
                    return;
                Color c1 = Color.FromArgb((color.R + 4) % 256,(color.G + 4) % 256,(color.B + 4) % 256);
                Color c2 = Color.FromArgb((color.R + 8) % 256,(color.G + 8) % 256,(color.B + 8) % 256);
                Color c3 = Color.FromArgb((color.R + 12) % 256,(color.G + 12) % 256,(color.B + 12) % 256);
                Color c4 = Color.FromArgb((color.R + 16) % 256,(color.G + 16) % 256,(color.B + 16) % 256);
                GL.Color3(color);
                GL.Begin(PrimitiveType.Quads);
                // Bottom
                GL.Color3(c1);
                GL.Vertex3(x + 0,y + 0,z + 0);
                GL.Color3(c2);
                GL.Vertex3(x + w,y + 0,z + 0);
                GL.Color3(c3);
                GL.Vertex3(x + w,y + 0,z + d);
                GL.Color3(c4);
                GL.Vertex3(x + 0,y + 0,z + d);

                // Top
                GL.Color3(c1);
                GL.Vertex3(x + 0,y + h,z + 0);
                GL.Color3(c2);
                GL.Vertex3(x + w,y + h,z + 0);
                GL.Color3(c3);
                GL.Vertex3(x + w,y + h,z + d);
                GL.Color3(c4);
                GL.Vertex3(x + 0,y + h,z + d);

                // Left
                GL.Color3(c1);
                GL.Vertex3(x + 0,y + 0,z + 0);
                GL.Color3(c2);
                GL.Vertex3(x + 0,y + h,z + 0);
                GL.Color3(c3);
                GL.Vertex3(x + 0,y + h,z + d);
                GL.Color3(c4);
                GL.Vertex3(x + 0,y + 0,z + d);

                // Right
                GL.Color3(c1);
                GL.Vertex3(x + w,y + 0,z + 0);
                GL.Color3(c2);
                GL.Vertex3(x + w,y + h,z + 0);
                GL.Color3(c3);
                GL.Vertex3(x + w,y + h,z + d);
                GL.Color3(c4);
                GL.Vertex3(x + w,y + 0,z + d);

                // Front
                GL.Color3(c1);
                GL.Vertex3(x + 0,y + 0,z + 0);
                GL.Color3(c2);
                GL.Vertex3(x + w,y + 0,z + 0);
                GL.Color3(c3);
                GL.Vertex3(x + w,y + h,z + 0);
                GL.Color3(c4);
                GL.Vertex3(x + 0,y + h,z + 0);

                // Back
                GL.Color3(c1);
                GL.Vertex3(x + 0,y + 0,z + d);
                GL.Color3(c2);
                GL.Vertex3(x + w,y + 0,z + d);
                GL.Color3(c3);
                GL.Vertex3(x + w,y + h,z + d);
                GL.Color3(c4);
                GL.Vertex3(x + 0,y + h,z + d);
                GL.End();
                if (flipoctree)
                {
                    GL.Scale(1, -1, -1);
                }
            }
            else if (value != 0)
            {
                RenderOctreeX(data,ref value,x,y,z,w,h,d,xmax,ymax,zmax);
            }
        }

        private void RenderOctreeX(byte[] data,ref int offset,double x,double y,double z,double w,double h,double d,int xmax,int ymax,int zmax)
        {
            if (xmax > 0)
            {
                RenderOctreeY(data,ref offset,x + 0 / 2,y,z,w / 2,h,d,xmax - 1,ymax,zmax);
                RenderOctreeY(data,ref offset,x + w / 2,y,z,w / 2,h,d,xmax - 1,ymax,zmax);
            }
            else
            {
                RenderOctreeY(data,ref offset,x,y,z,w,h,d,xmax - 1,ymax,zmax);
            }
        }

        private void RenderOctreeY(byte[] data,ref int offset,double x,double y,double z,double w,double h,double d,int xmax,int ymax,int zmax)
        {
            if (ymax > 0)
            {
                RenderOctreeZ(data,ref offset,x,y + 0 / 2,z,w,h / 2,d,xmax,ymax - 1,zmax);
                RenderOctreeZ(data,ref offset,x,y + h / 2,z,w,h / 2,d,xmax,ymax - 1,zmax);
            }
            else
            {
                RenderOctreeZ(data,ref offset,x,y,z,w,h,d,xmax,ymax - 1,zmax);
            }
        }

        private void RenderOctreeZ(byte[] data,ref int offset,double x,double y,double z,double w,double h,double d,int xmax,int ymax,int zmax)
        {
            if (zmax > 0)
            {
                RenderOctree(data,offset,x,y,z + 0 / 2,w,h,d / 2,xmax,ymax,zmax - 1);
                offset += 2;
                RenderOctree(data,offset,x,y,z + d / 2,w,h,d / 2,xmax,ymax,zmax - 1);
                offset += 2;
            }
            else
            {
                RenderOctree(data,offset,x,y,z,w,h,d,xmax,ymax,zmax - 1);
                offset += 2;
            }
        }

        private void RenderEntity(ProtoEntity entity)
        {
            GL.PolygonStipple(stipplea);
            if (entity.Deltas.Count == 0)
            {
                GL.PushMatrix();
                GL.Translate(entity.StartX/4,entity.StartY/4,entity.StartZ/4);
                switch (entity.Type)
                {
                    case 0x3:
                        RenderPickup(entity.Subtype);
                        break;
                    default:
                        GL.Color3(Color.White);
                        LoadTexture(OldResources.PointTexture);
                        RenderSprite();
                        break;
                }
                GL.PopMatrix();
            }
            else
            {
                GL.Color3(Color.Blue);
                GL.PushMatrix();
                GL.Begin(PrimitiveType.LineStrip);
                int curx = entity.StartX / 4;
                int cury = entity.StartY / 4;
                int curz = entity.StartZ / 4;
                GL.Vertex3(curx,cury,curz);
                foreach (ProtoEntityPosition delta in entity.Deltas)
                {
                    curx += delta.X*2;
                    cury += delta.Y*2;
                    curz += delta.Z*2;
                    GL.Vertex3(curx,cury,curz);
                }
                GL.End();
                curx = entity.StartX / 4;
                cury = entity.StartY / 4;
                curz = entity.StartZ / 4;
                GL.Color3(Color.Red);
                LoadTexture(OldResources.PointTexture);
                GL.PushMatrix();
                GL.Translate(curx,cury,curz);
                RenderSprite();
                GL.PopMatrix();
                GL.Vertex3(curx,cury,curz);
                foreach (ProtoEntityPosition delta in entity.Deltas)
                {
                    GL.PushMatrix();
                    curx += delta.X*2;
                    cury += delta.Y*2;
                    curz += delta.Z*2;
                    GL.Translate(curx,cury,curz);
                    RenderSprite();
                    GL.PopMatrix();
                }
                GL.PopMatrix();
            }
        }

        private void RenderCamera(OldCamera camera)
        {
            GL.PolygonStipple(stippleb);
            GL.Color3(Color.Green);
            GL.PushMatrix();
            GL.Begin(PrimitiveType.LineStrip);
            foreach (OldCameraPosition position in camera.Positions)
            {
                GL.Vertex3(position.X / 4,position.Y / 4,position.Z / 4);
            }
            GL.End();
            GL.Color3(Color.Yellow);
            LoadTexture(OldResources.PointTexture);
            foreach (OldCameraPosition position in camera.Positions)
            {
                GL.PushMatrix();
                GL.Translate(position.X / 4,position.Y / 4,position.Z / 4);
                RenderSprite();
                GL.PopMatrix();
            }
            GL.PopMatrix();
        }

        private void RenderSprite()
        {
            GL.Enable(EnableCap.Texture2D);
            GL.PushMatrix();
            GL.Rotate(-rotx,0,1,0);
            GL.Rotate(-roty,1,0,0);
            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0,0);
            GL.Vertex2(-50,+50);
            GL.TexCoord2(1,0);
            GL.Vertex2(+50,+50);
            GL.TexCoord2(1,1);
            GL.Vertex2(+50,-50);
            GL.TexCoord2(0,1);
            GL.Vertex2(-50,-50);
            GL.End();
            GL.PopMatrix();
            GL.Disable(EnableCap.Texture2D);
        }

        private void RenderPickup(int subtype)
        {
            GL.Translate(0,50,0);
            GL.Color3(Color.White);
            LoadPickupTexture(subtype);
            RenderSprite();
        }

        private void LoadPickupTexture(int subtype)
        {
            switch (subtype)
            {
                case 5: // Life
                    LoadTexture(OldResources.LifeTexture);
                    break;
                case 6: // Mask
                    LoadTexture(OldResources.MaskTexture);
                    break;
                case 16: // Apple
                    LoadTexture(OldResources.AppleTexture);
                    break;
                default:
                    LoadTexture(OldResources.UnknownPickupTexture);
                    break;
            }
        }
    }
}
