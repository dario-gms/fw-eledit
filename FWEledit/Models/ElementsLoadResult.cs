using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ElementsLoadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string GameFolderPath { get; set; }
        public string ElementsPath { get; set; }
        public eListCollection ListCollection { get; set; }
        public eListConversation ConversationList { get; set; }
        public string[][] Xrefs { get; set; }
        public bool HasXrefs { get; set; }
        public List<string> ExportRules { get; set; } = new List<string>();
        public NavigationSettingsSnapshot NavigationSnapshot { get; set; }
    }
}
