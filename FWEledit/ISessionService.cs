namespace FWEledit
{
    public interface ISessionService
    {
        eListCollection ListCollection { get; set; }
        eListConversation ConversationList { get; set; }
        string[][] Xrefs { get; set; }
        AssetManager AssetManager { get; set; }
        CacheSave Database { get; set; }
        string[] BuffStr { get; set; }
        string[] ItemExtDesc { get; set; }
        string[] SkillStr { get; set; }
        System.Collections.SortedList AddonsList { get; set; }
        System.Collections.SortedList InstanceList { get; set; }
        System.Collections.SortedList LocalizationText { get; set; }
    }
}
