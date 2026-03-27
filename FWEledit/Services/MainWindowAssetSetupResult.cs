namespace FWEledit
{
    public sealed class MainWindowAssetSetupResult
    {
        public AssetManager AssetManager { get; set; }
        public AddonTypeOptionService AddonTypeOptionService { get; set; }
        public ModelPickerService ModelPickerService { get; set; }
        public ItemValueRowBuilderService ItemValueRowBuilderService { get; set; }
        public ItemSelectionWorkflowService ItemSelectionWorkflowService { get; set; }
        public ValueCompatibilityService ValueCompatibilityService { get; set; }
        public ValueChangeService ValueChangeService { get; set; }
    }
}
