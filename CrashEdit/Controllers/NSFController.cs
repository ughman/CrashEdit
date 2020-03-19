using Crash;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Forms;
using System.IO;
using System.Linq;

namespace CrashEdit
{
    public sealed class NSFController : Controller
    {
        public NSFController(NSF nsf,GameVersion gameversion)
        {
            NSF = nsf;
            GameVersion = gameversion;
            foreach (Chunk chunk in nsf.Chunks)
            {
                AddNode(CreateChunkController(chunk));
            }
            AddMenu(Crash.UI.Properties.Resources.NSFController_AcAddNormalChunk,Menu_Add_NormalChunk);
            if (GameVersion != GameVersion.Crash2 && GameVersion != GameVersion.Crash3 && GameVersion != GameVersion.Crash1)
                AddMenu(Crash.UI.Properties.Resources.NSFController_AcAddOldSoundChunk,Menu_Add_OldSoundChunk);
            AddMenu(Crash.UI.Properties.Resources.NSFController_AcAddSoundChunk,Menu_Add_SoundChunk);
            AddMenu(Crash.UI.Properties.Resources.NSFController_AcAddWavebankChunk,Menu_Add_WavebankChunk);
            AddMenu(Crash.UI.Properties.Resources.NSFController_AcAddSpeechChunk,Menu_Add_SpeechChunk);
            AddMenu(Crash.UI.Properties.Resources.NSFController_AcImportChunk,Menu_Import_Chunk);
            if (GameVersion == GameVersion.Crash2 || GameVersion == GameVersion.Crash3)
            {
                AddMenuSeparator();
                AddMenu(Crash.UI.Properties.Resources.NSFController_AcFixDetonator,Menu_Fix_Detonator);
                AddMenu(Crash.UI.Properties.Resources.NSFController_AcFixBoxCount,Menu_Fix_BoxCount);
                AddMenuSeparator();
            }
            if (GameVersion == GameVersion.Crash1 || GameVersion == GameVersion.Crash1BetaMAR08 || GameVersion == GameVersion.Crash1BetaMAY11)
                AddMenu(Crash.UI.Properties.Resources.NSFController_AcShowLevel,Menu_ShowLevelC1);
            else if (GameVersion == GameVersion.Crash2)
            {
                AddMenu(Crash.UI.Properties.Resources.NSFController_AcShowLevel,Menu_ShowLevelC2);
                AddMenu("Show Entire Level Zones",Menu_ShowLevelZonesC2);
            }
            else if (GameVersion == GameVersion.Crash3)
            {
                AddMenu(Crash.UI.Properties.Resources.NSFController_AcShowLevel,Menu_ShowLevelC3);
                AddMenu("Show Entire Level Zones",Menu_ShowLevelZonesC3);
            }
            AddMenuSeparator();
            AddMenu("Export all scenery as .OBJ",Menu_ExportScenery_OBJ);
            AddMenu("Export all scenery as .PLY",Menu_ExportScenery_PLY);
            InvalidateNode();
            InvalidateNodeImage();
        }

        public override void InvalidateNode()
        {
            Node.Text = Crash.UI.Properties.Resources.NSFController_Text;
        }

        public override void InvalidateNodeImage()
        {
            Node.ImageKey = "nsf";
            Node.SelectedImageKey = "nsf";
        }

        public NSF NSF { get; }
        public GameVersion GameVersion { get; }

        public ChunkController CreateChunkController(Chunk chunk)
        {
            if (chunk is NormalChunk)
            {
                return new NormalChunkController(this, (NormalChunk)chunk);
            }
            else if (chunk is TextureChunk)
            {
                return new TextureChunkController(this, (TextureChunk)chunk);
            }
            else if (chunk is OldSoundChunk)
            {
                return new OldSoundChunkController(this, (OldSoundChunk)chunk);
            }
            else if (chunk is SoundChunk)
            {
                return new SoundChunkController(this, (SoundChunk)chunk);
            }
            else if (chunk is WavebankChunk)
            {
                return new WavebankChunkController(this, (WavebankChunk)chunk);
            }
            else if (chunk is SpeechChunk)
            {
                return new SpeechChunkController(this, (SpeechChunk)chunk);
            }
            else if (chunk is UnprocessedChunk)
            {
                return new UnprocessedChunkController(this, (UnprocessedChunk)chunk);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void Menu_Add_NormalChunk()
        {
            NormalChunk chunk = new NormalChunk();
            NSF.Chunks.Add(chunk);
            NormalChunkController controller = new NormalChunkController(this,chunk);
            AddNode(controller);
        }

        private void Menu_Add_OldSoundChunk()
        {
            OldSoundChunk chunk = new OldSoundChunk();
            NSF.Chunks.Add(chunk);
            OldSoundChunkController controller = new OldSoundChunkController(this,chunk);
            AddNode(controller);
        }

        private void Menu_Add_SoundChunk()
        {
            SoundChunk chunk = new SoundChunk();
            NSF.Chunks.Add(chunk);
            SoundChunkController controller = new SoundChunkController(this,chunk);
            AddNode(controller);
        }

        private void Menu_Add_WavebankChunk()
        {
            WavebankChunk chunk = new WavebankChunk();
            NSF.Chunks.Add(chunk);
            WavebankChunkController controller = new WavebankChunkController(this,chunk);
            AddNode(controller);
        }

        private void Menu_Add_SpeechChunk()
        {
            SpeechChunk chunk = new SpeechChunk();
            NSF.Chunks.Add(chunk);
            SpeechChunkController controller = new SpeechChunkController(this,chunk);
            AddNode(controller);
        }

        private void Menu_Fix_Detonator()
        {
            List<Entity> nitros = new List<Entity>();
            List<Entity> detonators = new List<Entity>();
            foreach (Chunk chunk in NSF.Chunks)
            {
                if (chunk is EntryChunk)
                {
                    foreach (Entry entry in ((EntryChunk)chunk).Entries)
                    {
                        if (entry is NewZoneEntry)
                        {
                            foreach (Entity entity in ((NewZoneEntry)entry).Entities)
                            {
                                if (entity.Type == 34)
                                {
                                    if (entity.Subtype == 18 && entity.ID.HasValue)
                                    {
                                        nitros.Add(entity);
                                    }
                                    else if (entity.Subtype == 24)
                                    {
                                        detonators.Add(entity);
                                    }
                                }
                            }
                        }
                        if (entry is ZoneEntry)
                        {
                            foreach (Entity entity in ((ZoneEntry)entry).Entities)
                            {
                                if (entity.Type == 34)
                                {
                                    if (entity.Subtype == 18 && entity.ID.HasValue)
                                    {
                                        nitros.Add(entity);
                                    }
                                    else if (entity.Subtype == 24)
                                    {
                                        detonators.Add(entity);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (Entity detonator in detonators)
            {
                detonator.Victims.Clear();
                foreach (Entity nitro in nitros)
                {
                    detonator.Victims.Add(new EntityVictim((short)nitro.ID.Value));
                }
            }
        }

        private void Menu_Fix_BoxCount()
        {
            int boxcount = 0;
            List<Entity> willys = new List<Entity>();
            foreach (Chunk chunk in NSF.Chunks)
            {
                if (chunk is EntryChunk entrychunk)
                {
                    foreach (Entry entry in entrychunk.Entries)
                    {
                        if (entry is ZoneEntry zone2)
                        {
                            foreach (Entity entity in zone2.Entities)
                            {
                                if (entity.Type == 0 && entity.Subtype == 0)
                                {
                                    willys.Add(entity);
                                }
                                else if (entity.Type == 34)
                                {
                                    switch (entity.Subtype)
                                    {
                                        case 5: // iron
                                        case 7: // action
                                        case 15: // iron spring
                                        case 24: // nitro action
                                            break;
                                        default:
                                            boxcount++;
                                            break;
                                    }
                                }
                            }
                        }
                        if (entry is NewZoneEntry zone3)
                        {
                            foreach (Entity entity in zone3.Entities)
                            {
                                if (entity.Type == 0 && entity.Subtype == 0)
                                {
                                    willys.Add(entity);
                                }
                                else if (entity.Type == 34)
                                {
                                    switch (entity.Subtype)
                                    {
                                        case 5: // iron
                                        case 7: // action
                                        case 15: // iron spring
                                        case 24: // nitro action
                                        case 27: // iron continue
                                        case 28: // clock
                                            break;
                                        default:
                                            boxcount++;
                                            break;
                                    }
                                }
                                else if (entity.Type == 36)
                                {
                                    if (entity.Subtype == 1)
                                    {
                                        boxcount++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (Entity willy in willys)
            {
                if (willy.BoxCount.HasValue)
                {
                    willy.BoxCount = new EntitySetting(0,boxcount);
                }
            }
        }

        private void Menu_ShowLevelC1()
        {
            List<TextureChunk[]> sortedtexturechunks = new List<TextureChunk[]>();
            List<OldSceneryEntry> sceneryentries = new List<OldSceneryEntry>();
            foreach (Chunk chunk in NSF.Chunks)
            {
                if (chunk is EntryChunk entrychunk)
                {
                    foreach (Entry entry in entrychunk.Entries)
                    {
                        if (entry is OldSceneryEntry sceneryentry)
                        {
                            sceneryentries.Add(sceneryentry);
                            TextureChunk[] texturechunks = new TextureChunk[BitConv.FromInt32(sceneryentry.Info,0x18)];
                            for (int i = 0; i < texturechunks.Length; ++i)
                            {
                                texturechunks[i] = NSF.FindEID<TextureChunk>(BitConv.FromInt32(sceneryentry.Info,0x20+i*4));
                            }
                            sortedtexturechunks.Add(texturechunks);
                        }
                    }
                }
            }
            Form frm = new Form() { Text = "Loading...", Width = 480, Height = 360 };
            frm.Show();
            OldSceneryEntryViewer viewer = new OldSceneryEntryViewer(sceneryentries,sortedtexturechunks.ToArray()) { Dock = DockStyle.Fill };
            frm.Controls.Add(viewer);
            frm.Text = string.Empty;
        }
        
        private void Menu_ShowLevelC2()
        {
            List<TextureChunk[]> sortedtexturechunks = new List<TextureChunk[]>();
            List<SceneryEntry> sceneryentries = new List<SceneryEntry>();
            foreach (Chunk chunk in NSF.Chunks)
            {
                if (chunk is EntryChunk entrychunk)
                {
                    foreach (Entry entry in entrychunk.Entries)
                    {
                        if (entry is SceneryEntry sceneryentry)
                        {
                            sceneryentries.Add(sceneryentry);
                            TextureChunk[] texturechunks = new TextureChunk[BitConv.FromInt32(sceneryentry.Info,0x28)];
                            for (int i = 0; i < texturechunks.Length; ++i)
                            {
                                texturechunks[i] = NSF.FindEID<TextureChunk>(BitConv.FromInt32(sceneryentry.Info,0x2C+i*4));
                            }
                            sortedtexturechunks.Add(texturechunks);
                        }
                    }
                }
            }
            Form frm = new Form() { Text = "Loading...", Width = 480, Height = 360 };
            frm.Show();
            SceneryEntryViewer viewer = new SceneryEntryViewer(sceneryentries,sortedtexturechunks.ToArray()) { Dock = DockStyle.Fill };
            frm.Controls.Add(viewer);
            frm.Text = string.Empty;
        }

        private void Menu_ShowLevelC3()
        {
            List<TextureChunk[]> sortedtexturechunks = new List<TextureChunk[]>();
            List<NewSceneryEntry> sceneryentries = new List<NewSceneryEntry>();
            foreach (Chunk chunk in NSF.Chunks)
            {
                if (chunk is EntryChunk entrychunk)
                {
                    foreach (Entry entry in entrychunk.Entries)
                    {
                        if (entry is NewSceneryEntry newsceneryentry)
                        {
                            sceneryentries.Add(newsceneryentry);
                            TextureChunk[] texturechunks = new TextureChunk[BitConv.FromInt32(newsceneryentry.Info,0x28)];
                            for (int i = 0; i < texturechunks.Length; ++i)
                            {
                                texturechunks[i] = NSF.FindEID<TextureChunk>(BitConv.FromInt32(newsceneryentry.Info,0x2C+i*4));
                            }
                            sortedtexturechunks.Add(texturechunks);
                        }
                    }
                }
            }
            Form frm = new Form() { Text = "Loading...", Width = 480, Height = 360 };
            frm.Show();
            NewSceneryEntryViewer viewer = new NewSceneryEntryViewer(sceneryentries,sortedtexturechunks.ToArray()) { Dock = DockStyle.Fill };
            frm.Controls.Add(viewer);
            frm.Text = string.Empty;
        }
        
        private void Menu_ShowLevelZonesProto()
        {
            List<ProtoZoneEntry> entries = new List<ProtoZoneEntry>();
            List<ProtoSceneryEntry> sceneryEntries = new List<ProtoSceneryEntry>();
            List<TextureChunk[]> texturechunks = new List<TextureChunk[]>();

            foreach (Chunk chunk in NSF.Chunks)
            {
                if (chunk is EntryChunk)
                {
                    EntryChunk entryChunk = chunk as EntryChunk;

                    foreach (Entry entry in entryChunk.Entries)
                    {
                        if (entry is ProtoZoneEntry)
                        {
                            ProtoZoneEntry zoneentry = entry as ProtoZoneEntry;
                            int linkedsceneryentrycount = BitConv.FromInt32(zoneentry.Header, 0);
                            for (int i = 0; i < linkedsceneryentrycount; i++)
                            {
                                var sceneryEntry = NSF.FindEID<ProtoSceneryEntry>(BitConv.FromInt32(zoneentry.Header, 4 + i * 48));
                                TextureChunk[] _texturechunks = new TextureChunk[BitConv.FromInt32(sceneryEntry.Info, 0x18)];
                                for (int t = 0; t < _texturechunks.Length; ++t)
                                {
                                    _texturechunks[t] = NSF.FindEID<TextureChunk>(BitConv.FromInt32(sceneryEntry.Info, 0x20 + t * 4));
                                }
                                sceneryEntries.Add(sceneryEntry);
                                texturechunks.Add(_texturechunks);
                            }

                            entries.Add(entry as ProtoZoneEntry);
                        }
                    }
                }
            }

            if (entries.Count == 0)
            {
                MessageBox.Show("No zone entries found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                Form form = new Form { Text = "Loading...", Width = 480, Height = 360 };
                ProtoZoneEntryViewer viewer = new ProtoZoneEntryViewer(entries, sceneryEntries.ToArray(), texturechunks.ToArray()) { Dock = DockStyle.Fill };
                form.Controls.Add(viewer);
                form.Show();
                form.Text = string.Empty;
            }
        }

        private void Menu_ShowLevelZonesC1()
        {
            List<OldZoneEntry> entries = new List<OldZoneEntry>();
            List<OldSceneryEntry> sceneryEntries = new List<OldSceneryEntry>();
            List<TextureChunk[]> texturechunks = new List<TextureChunk[]>();

            foreach (Chunk chunk in NSF.Chunks)
            {
                if (chunk is EntryChunk)
                {
                    EntryChunk entryChunk = chunk as EntryChunk;

                    foreach (Entry entry in entryChunk.Entries)
                    {
                        if (entry is OldZoneEntry)
                        {
                            OldZoneEntry zoneentry = entry as OldZoneEntry;
                            int linkedsceneryentrycount = BitConv.FromInt32(zoneentry.Header, 0);
                            for (int i = 0; i < linkedsceneryentrycount; i++)
                            {
                                var sceneryEntry = NSF.FindEID<OldSceneryEntry>(BitConv.FromInt32(zoneentry.Header, 4 + i * 48));
                                TextureChunk[] _texturechunks = new TextureChunk[BitConv.FromInt32(sceneryEntry.Info, 0x18)];
                                for (int t = 0; t < _texturechunks.Length; ++t)
                                {
                                    _texturechunks[t] = NSF.FindEID<TextureChunk>(BitConv.FromInt32(sceneryEntry.Info, 0x20 + t * 4));
                                }
                                sceneryEntries.Add(sceneryEntry);
                                texturechunks.Add(_texturechunks);
                            }
                            entries.Add(entry as OldZoneEntry);
                        }
                    }
                }
            }

            if (entries.Count == 0)
            {
                MessageBox.Show("No zone entries found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                Form form = new Form { Text = "Loading...", Width = 480, Height = 360 };
                OldZoneEntryViewer viewer = new OldZoneEntryViewer(entries, sceneryEntries.ToArray(), texturechunks.ToArray()) { Dock = DockStyle.Fill };
                form.Controls.Add(viewer);
                form.Show();
                form.Text = string.Empty;
            }
        }

        private void Menu_ShowLevelZonesC2()
        {
            List<ZoneEntry> entries = new List<ZoneEntry>();
            List<SceneryEntry> sceneryEntries = new List<SceneryEntry>();

            foreach (Chunk chunk in NSF.Chunks)
            {
                if (chunk is EntryChunk)
                {
                    EntryChunk entryChunk = chunk as EntryChunk;

                    foreach (Entry entry in entryChunk.Entries)
                    {
                        if (entry is ZoneEntry)
                        {
                            ZoneEntry zoneentry = entry as ZoneEntry;
                            int linkedsceneryentrycount = BitConv.FromInt32(zoneentry.Header, 0);
                            for (int i = 0; i < linkedsceneryentrycount; i++)
                            {
                                sceneryEntries.Add(NSF.FindEID<SceneryEntry>(BitConv.FromInt32(zoneentry.Header, 4 + i * 48)));
                            }

                            entries.Add(entry as ZoneEntry);
                        }
                    }
                }
            }

            if (entries.Count == 0)
            {
                MessageBox.Show("No zone entries found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                // Since now texture rendering is now a thing we must hack this.
                List<TextureChunk[]> textureChunks = new List<TextureChunk[]>();
                foreach (var sceneryEntry in sceneryEntries)
                {
                    TextureChunk[] texturechunks = new TextureChunk[BitConv.FromInt32(sceneryEntry.Info, 0x28)];
                    for (int i = 0; i < texturechunks.Length; ++i)
                    {
                        texturechunks[i] = NSF.FindEID<TextureChunk>(BitConv.FromInt32(sceneryEntry.Info, 0x2C + i * 4));
                    }
                    textureChunks.Add(texturechunks);
                }
                Form form = new Form { Text = "Loading...", Width = 480, Height = 360 };
                ZoneEntryViewer viewer = new ZoneEntryViewer(entries, sceneryEntries.ToArray(), textureChunks.ToArray()) { Dock = DockStyle.Fill };
                form.Controls.Add(viewer);
                form.Show();
                form.Text = string.Empty;
            }
        }

        private void Menu_ShowLevelZonesC3()
        {
            List<NewZoneEntry> entries = new List<NewZoneEntry>();
            List<NewSceneryEntry> sceneryEntries = new List<NewSceneryEntry>();

            foreach (Chunk chunk in NSF.Chunks)
            {
                if (chunk is EntryChunk)
                {
                    EntryChunk entryChunk = chunk as EntryChunk;

                    foreach (Entry entry in entryChunk.Entries)
                    {
                        if (entry is NewZoneEntry)
                        {
                            NewZoneEntry zoneentry = entry as NewZoneEntry;
                            int linkedsceneryentrycount = BitConv.FromInt32(zoneentry.Header, 0);
                            for (int i = 0; i < linkedsceneryentrycount; i++)
                            {
                                sceneryEntries.Add(NSF.FindEID<NewSceneryEntry>(BitConv.FromInt32(zoneentry.Header, 4 + i * 48)));
                            }

                            entries.Add(entry as NewZoneEntry);
                        }
                    }
                }
            }

            if (entries.Count == 0)
            {
                MessageBox.Show("No zone entries found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                // Since now texture rendering is now a thing we must hack this.
                List<TextureChunk[]> textureChunks = new List<TextureChunk[]>();
                foreach (var sceneryEntry in sceneryEntries)
                {
                    TextureChunk[] texturechunks = new TextureChunk[BitConv.FromInt32(sceneryEntry.Info, 0x28)];
                    for (int i = 0; i < texturechunks.Length; ++i)
                    {
                        texturechunks[i] = NSF.FindEID<TextureChunk>(BitConv.FromInt32(sceneryEntry.Info, 0x2C + i * 4));
                    }
                    textureChunks.Add(texturechunks);
                }
                Form form = new Form { Text = "Loading...", Width = 480, Height = 360 };
                NewZoneEntryViewer viewer = new NewZoneEntryViewer(entries, sceneryEntries.ToArray(), textureChunks.ToArray()) { Dock = DockStyle.Fill };
                form.Controls.Add(viewer);
                form.Show();
                form.Text = string.Empty;
            }
        }

        private void Menu_ExportScenery_OBJ()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "OBJ files|*.obj";

            DialogResult result = dialog.ShowDialog();

            if (result != DialogResult.OK)
            {
                return;
            }

            if (GameVersion == GameVersion.Crash1Beta1995 || GameVersion == GameVersion.Crash1BetaMAR08 || GameVersion == GameVersion.Crash1BetaMAY11)
            {
                List<ProtoSceneryEntry> entries = new List<ProtoSceneryEntry>();

                foreach (Chunk chunk in NSF.Chunks)
                {
                    if (chunk is EntryChunk)
                    {
                        EntryChunk entryChunk = chunk as EntryChunk;

                        foreach (Entry entry in entryChunk.Entries)
                        {
                            if (entry is ProtoSceneryEntry)
                            {
                                entries.Add(entry as ProtoSceneryEntry);
                            }
                        }
                    }
                }

                if (entries.Count == 0)
                {
                    MessageBox.Show("No scenery entries found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    List<byte[]> outputs = new List<byte[]>();
                    int vertexStart = 0;

                    foreach (ProtoSceneryEntry scenery in entries)
                    {
                        byte[] tmp = scenery.ToOBJ(vertexStart);
                        outputs.Add(tmp);
                        vertexStart += scenery.Vertices.Count;
                    }

                    FileStream fp = File.OpenWrite(dialog.FileName);

                    foreach (byte[] data in outputs)
                    {
                        fp.Write(data, 0, data.Length);
                    }

                    fp.Flush();
                    fp.Close();
                }
            }
            else if (GameVersion == GameVersion.Crash1)
            {
                List<OldSceneryEntry> entries = new List<OldSceneryEntry>();

                foreach (Chunk chunk in NSF.Chunks)
                {
                    if (chunk is EntryChunk)
                    {
                        EntryChunk entryChunk = chunk as EntryChunk;

                        foreach (Entry entry in entryChunk.Entries)
                        {
                            if (entry is OldSceneryEntry)
                            {
                                entries.Add(entry as OldSceneryEntry);
                            }
                        }
                    }
                }

                if (entries.Count == 0)
                {
                    MessageBox.Show("No scenery entries found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    List<byte[]> outputs = new List<byte[]>();
                    int vertexStart = 0;

                    foreach (OldSceneryEntry scenery in entries)
                    {
                        byte[] tmp = scenery.ToOBJ(vertexStart);
                        outputs.Add(tmp);
                        vertexStart += scenery.Vertices.Count;
                    }

                    FileStream fp = File.OpenWrite(dialog.FileName);

                    foreach (byte[] data in outputs)
                    {
                        fp.Write(data, 0, data.Length);
                    }

                    fp.Flush();
                    fp.Close();
                }
            }
            else if (GameVersion == GameVersion.Crash2)
            {
                List<SceneryEntry> entries = new List<SceneryEntry>();

                foreach (Chunk chunk in NSF.Chunks)
                {
                    if (chunk is EntryChunk)
                    {
                        EntryChunk entryChunk = chunk as EntryChunk;

                        foreach (Entry entry in entryChunk.Entries)
                        {
                            if (entry is SceneryEntry)
                            {
                                entries.Add(entry as SceneryEntry);
                            }
                        }
                    }
                }

                if (entries.Count == 0)
                {
                    MessageBox.Show("No scenery entries found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    List<byte[]> outputs = new List<byte[]>();
                    int vertexStart = 0;

                    foreach (SceneryEntry scenery in entries)
                    {
                        byte[] tmp = scenery.ToOBJ(vertexStart);
                        outputs.Add(tmp);
                        vertexStart += scenery.Vertices.Count;
                    }

                    FileStream fp = File.OpenWrite(dialog.FileName);

                    foreach (byte[] data in outputs)
                    {
                        fp.Write(data, 0, data.Length);
                    }

                    fp.Flush();
                    fp.Close();
                }
            }
            else if (GameVersion == GameVersion.Crash3)
            {
                List<NewSceneryEntry> entries = new List<NewSceneryEntry>();

                foreach (Chunk chunk in NSF.Chunks)
                {
                    if (chunk is EntryChunk)
                    {
                        EntryChunk entryChunk = chunk as EntryChunk;

                        foreach (Entry entry in entryChunk.Entries)
                        {
                            if (entry is NewSceneryEntry)
                            {
                                entries.Add(entry as NewSceneryEntry);
                            }
                        }
                    }
                }

                if (entries.Count == 0)
                {
                    MessageBox.Show("No scenery entries found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    List<byte[]> outputs = new List<byte[]>();
                    int vertexStart = 0;

                    foreach (NewSceneryEntry scenery in entries)
                    {
                        byte[] tmp = scenery.ToOBJ(vertexStart);
                        outputs.Add(tmp);
                        vertexStart += scenery.Vertices.Count;
                    }

                    FileStream fp = File.OpenWrite(dialog.FileName);

                    foreach (byte[] data in outputs)
                    {
                        fp.Write(data, 0, data.Length);
                    }

                    fp.Flush();
                    fp.Close();
                }
            }
        }

        private void Menu_ExportScenery_PLY()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "PLY files|*.ply";

            DialogResult result = dialog.ShowDialog();

            if (result != DialogResult.OK)
            {
                return;
            }

            if (GameVersion == GameVersion.Crash1Beta1995 || GameVersion == GameVersion.Crash1BetaMAR08 || GameVersion == GameVersion.Crash1BetaMAY11)
            {
                List<ProtoSceneryEntry> entries = new List<ProtoSceneryEntry>();

                foreach (Chunk chunk in NSF.Chunks)
                {
                    if (chunk is EntryChunk)
                    {
                        EntryChunk entryChunk = chunk as EntryChunk;

                        foreach (Entry entry in entryChunk.Entries)
                        {
                            if (entry is ProtoSceneryEntry)
                            {
                                entries.Add(entry as ProtoSceneryEntry);
                            }
                        }
                    }
                }

                if (entries.Count == 0)
                {
                    MessageBox.Show("No scenery entries found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    List<byte[]> outputs = new List<byte[]>();
                    int vertexStart = 0;

                    foreach (ProtoSceneryEntry scenery in entries)
                    {
                        byte[] tmp = scenery.ToPLY(vertexStart);
                        outputs.Add(tmp);
                        vertexStart += scenery.Vertices.Count;
                    }

                    FileStream fp = File.OpenWrite(dialog.FileName);

                    foreach (byte[] data in outputs)
                    {
                        fp.Write(data, 0, data.Length);
                    }

                    fp.Flush();
                    fp.Close();
                }
            }
            else if (GameVersion == GameVersion.Crash1)
            {
                List<OldSceneryEntry> entries = new List<OldSceneryEntry>();

                foreach (Chunk chunk in NSF.Chunks)
                {
                    if (chunk is EntryChunk)
                    {
                        EntryChunk entryChunk = chunk as EntryChunk;

                        foreach (Entry entry in entryChunk.Entries)
                        {
                            if (entry is OldSceneryEntry)
                            {
                                entries.Add(entry as OldSceneryEntry);
                            }
                        }
                    }
                }

                if (entries.Count == 0)
                {
                    MessageBox.Show("No scenery entries found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    List<byte[]> outputs = new List<byte[]>();
                    int vertexStart = 0;

                    foreach (OldSceneryEntry scenery in entries)
                    {
                        byte[] tmp = scenery.ToPLY(vertexStart);
                        outputs.Add(tmp);
                        vertexStart += scenery.Vertices.Count;
                    }

                    FileStream fp = File.OpenWrite(dialog.FileName);

                    foreach (byte[] data in outputs)
                    {
                        fp.Write(data, 0, data.Length);
                    }

                    fp.Flush();
                    fp.Close();
                }
            }
            else if (GameVersion == GameVersion.Crash2)
            {
                List<SceneryEntry> entries = new List<SceneryEntry>();

                foreach (Chunk chunk in NSF.Chunks)
                {
                    if (chunk is EntryChunk)
                    {
                        EntryChunk entryChunk = chunk as EntryChunk;

                        foreach (Entry entry in entryChunk.Entries)
                        {
                            if (entry is SceneryEntry)
                            {
                                entries.Add(entry as SceneryEntry);
                            }
                        }
                    }
                }

                if (entries.Count == 0)
                {
                    MessageBox.Show("No scenery entries found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    List<byte[]> outputs = new List<byte[]>();
                    int vertexStart = 0;

                    foreach (SceneryEntry scenery in entries)
                    {
                        byte[] tmp = scenery.ToPLY(vertexStart);
                        outputs.Add(tmp);
                        vertexStart += scenery.Vertices.Count;
                    }

                    FileStream fp = File.OpenWrite(dialog.FileName);

                    foreach (byte[] data in outputs)
                    {
                        fp.Write(data, 0, data.Length);
                    }

                    fp.Flush();
                    fp.Close();
                }
            }
            else if (GameVersion == GameVersion.Crash3)
            {
                List<NewSceneryEntry> entries = new List<NewSceneryEntry>();

                foreach (Chunk chunk in NSF.Chunks)
                {
                    if (chunk is EntryChunk)
                    {
                        EntryChunk entryChunk = chunk as EntryChunk;

                        foreach (Entry entry in entryChunk.Entries)
                        {
                            if (entry is NewSceneryEntry)
                            {
                                entries.Add(entry as NewSceneryEntry);
                            }
                        }
                    }
                }

                if (entries.Count == 0)
                {
                    MessageBox.Show("No scenery entries found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    List<byte[]> outputs = new List<byte[]>();
                    int vertexStart = 0;

                    foreach (NewSceneryEntry scenery in entries)
                    {
                        byte[] tmp = scenery.ToPLY(vertexStart);
                        outputs.Add(tmp);
                        vertexStart += scenery.Vertices.Count;
                    }

                    FileStream fp = File.OpenWrite(dialog.FileName);

                    foreach (byte[] data in outputs)
                    {
                        fp.Write(data, 0, data.Length);
                    }

                    fp.Flush();
                    fp.Close();
                }
            }
        }

        private void Menu_Import_Chunk()
        {
            byte[][] datas = FileUtil.OpenFiles(FileFilters.Any);
            if (datas == null)
                return;
            bool process = MessageBox.Show("Do you want to process the imported chunks?", "Import Chunk", MessageBoxButtons.YesNo) == DialogResult.Yes;
            foreach (var data in datas)
            {
                try
                {
                    UnprocessedChunk chunk = Chunk.Load(data);
                    if (process)
                    {
                        Chunk processedchunk = chunk.Process(NSF.Chunks.Count*2 + 1);
                        NSF.Chunks.Add(processedchunk);
                        AddNode(CreateChunkController(processedchunk));
                    }
                    else
                    {
                        NSF.Chunks.Add(chunk);
                        AddNode(new UnprocessedChunkController(this,chunk));
                    }
                }
                catch (LoadAbortedException)
                {
                }
            }
        }
    }
}
