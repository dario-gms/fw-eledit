using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ModelPickerPackageCacheEntry
    {
        public DateTime PckTimestampUtc { get; set; }
        public List<string> Files { get; set; }
    }
}
