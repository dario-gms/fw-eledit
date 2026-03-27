using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ItemSelectionRequest
    {
        public ISessionService Session { get; set; }
        public eListCollection ListCollection { get; set; }
        public eListConversation ConversationList { get; set; }
        public CacheSave Database { get; set; }
        public int ListIndex { get; set; }
        public int ElementIndex { get; set; }
        public Func<int, string, bool> ShouldIncludeField { get; set; }
        public Func<int, int, int, string> GetDisplayEntryName { get; set; }
        public Func<Dictionary<int, string>> LoadAddonTypeHints { get; set; }
        public Func<string, bool> IsModelFieldName { get; set; }
        public Func<int, int, int, bool> IsFieldInvalid { get; set; }
        public Func<int, int, int, bool> IsFieldDirty { get; set; }
    }
}
