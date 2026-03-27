using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowLayoutResult
    {
        public SplitContainer MainSplit { get; set; }
        public TabControl RightTabs { get; set; }
        public TabPage ValuesTab { get; set; }
        public TabControl EquipmentTabs { get; set; }
        public TabPage EquipmentTabMain { get; set; }
        public TabPage EquipmentTabRefine { get; set; }
        public TabPage EquipmentTabModels { get; set; }
        public TabPage EquipmentTabOther { get; set; }
        public TabPage DescriptionTab { get; set; }
        public TextBox DescriptionEditor { get; set; }
        public RichTextBox DescriptionPreview { get; set; }
        public Button DescriptionSaveButton { get; set; }
        public Label DescriptionStatusLabel { get; set; }
        public Button InlinePickIconButton { get; set; }
        public ListBox SearchSuggestionList { get; set; }
    }
}
