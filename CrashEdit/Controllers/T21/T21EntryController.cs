using Crash;

namespace CrashEdit
{
    public sealed class T21EntryController : MysteryMultiItemEntryController
    {
        public T21EntryController(EntryChunkController entrychunkcontroller,T21Entry t21entry) : base(entrychunkcontroller,t21entry)
        {
            T21Entry = t21entry;
            InvalidateNode();
            InvalidateNodeImage();
        }

        public override void InvalidateNode()
        {
            Node.Text = string.Format("Sprite ({0})",T21Entry.EName);
        }

        public override void InvalidateNodeImage()
        {
            Node.ImageKey = "image";
            Node.SelectedImageKey = "image";
        }

        public T21Entry T21Entry { get; }
    }
}
