using System;

namespace FWEledit
{
    public sealed class ValueChangeRequest
    {
        public eListCollection ListCollection { get; set; }
        public eListConversation ConversationList { get; set; }
        public CacheSave Database { get; set; }
        public int ListIndex { get; set; }
        public int FieldIndex { get; set; }
        public int CurrentElementIndex { get; set; }
        public int[] SelectedElementIndices { get; set; } = new int[0];
        public int[] SelectedGridIndices { get; set; } = new int[0];
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public bool IsModelField { get; set; }
        public bool IsIdEdit { get; set; }
        public Func<string, string, bool> IsValueCompatible { get; set; }
        public Func<int, int, int, string> ComposeListDisplayName { get; set; }
        public Action<int, int> RemapDescriptionId { get; set; }
        public Action ResetList0DisplayCache { get; set; }
        public Action<int, int> MarkRowDirty { get; set; }
        public Action<int, int, int> MarkFieldDirty { get; set; }
        public Action<int, int, int> MarkFieldInvalid { get; set; }
        public Action<int, int, int> ClearFieldInvalid { get; set; }
    }
}
