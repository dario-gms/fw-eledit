using System;

namespace FWEledit
{
    public sealed class ListSelectionRequest
    {
        public eListCollection ListCollection { get; set; }
        public eListConversation ConversationList { get; set; }
        public CacheSave Database { get; set; }
        public int ListIndex { get; set; }
        public string[][] Xrefs { get; set; }
        public ListDisplayService ListDisplayService { get; set; }
        public ListRowBuilderService ListRowBuilderService { get; set; }
        public Func<int, int, int, string> ComposeListDisplayName { get; set; }
    }
}
