using System.Collections.Generic;

namespace FWEledit
{
    public sealed class QuestOverflowReport
    {
        public List<string> ReceiveItems { get; } = new List<string>();
        public List<string> ActivateItems { get; } = new List<string>();
    }
}
