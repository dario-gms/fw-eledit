using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementsSaveContext
    {
        public eListCollection ListCollection { get; set; }
        public eListConversation ConversationList { get; set; }
        public string ElementsPath { get; set; } = string.Empty;
        public AssetManager AssetManager { get; set; }
        public Action<int> SetProgress { get; set; }
        public Action BeginProgress { get; set; }
        public Action EndProgress { get; set; }
        public Func<bool> ValidateUniqueIds { get; set; }
        public Func<bool> FlushPendingDescriptions { get; set; }
        public Action ClearDirtyTracking { get; set; }
        public Action<string> ShowInfoMessage { get; set; }
        public Action<string> ErrorMessageHandler { get; set; }
        public Action<string, Exception> LogError { get; set; }
        public Func<NavigationSnapshot> CaptureNavigationSnapshot { get; set; }
        public Action<NavigationSnapshot> RestoreNavigationSnapshot { get; set; }
        public Func<string> PromptSavePath { get; set; }
        public Action<string> SyncSummaryHandler { get; set; }
    }
}
