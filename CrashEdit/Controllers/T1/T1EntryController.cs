using Crash;

namespace CrashEdit
{
    public sealed class T1EntryController : MysteryMultiItemEntryController
    {
        public T1EntryController(EntryChunkController entrychunkcontroller,T1Entry t1entry) : base(entrychunkcontroller,t1entry)
        {
            T1Entry = t1entry;
            InvalidateNode();
            InvalidateNodeImage();
        }

        public override void InvalidateNode()
        {
            Node.Text = string.Format("Animation ({0})",T1Entry.EName);
        }

        public override void InvalidateNodeImage()
        {
            Node.ImageKey = "limeb";
            Node.SelectedImageKey = "limeb";
        }

        public T1Entry T1Entry { get; }
    }
}
