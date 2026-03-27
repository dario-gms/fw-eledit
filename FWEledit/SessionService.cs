namespace FWEledit
{
    public sealed class SessionService : ISessionService
    {
        public eListCollection ListCollection { get; set; }
        public eListConversation ConversationList { get; set; }
        public string[][] Xrefs { get; set; }
        public AssetManager AssetManager { get; set; }
        public CacheSave Database { get; set; }
        public string[] BuffStr { get; set; }
        public string[] ItemExtDesc { get; set; }
        public string[] SkillStr { get; set; }
        public System.Collections.SortedList AddonsList { get; set; }
        public System.Collections.SortedList InstanceList { get; set; }
        public System.Collections.SortedList LocalizationText { get; set; }
    }
}
