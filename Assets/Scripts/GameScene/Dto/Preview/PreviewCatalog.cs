using System;
using UnityEngine.Scripting;

namespace Data.Preview
{
    [Serializable, Preserve]
    public sealed class PreviewCatalog
    {
        [Preserve] public PreviewCatalog() { }
        public string status;
        public string revision;
        public long? serverId;
        public PreviewEntry[] magics;
    }

    [Serializable, Preserve]
    public sealed class PreviewEntry
    {
        [Preserve] public PreviewEntry() { }
        public string name;
        public string hash;
        public int bytes;
        public bool available;
    }
}
