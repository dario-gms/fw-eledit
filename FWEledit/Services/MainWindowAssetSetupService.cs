namespace FWEledit
{
    public sealed class MainWindowAssetSetupService
    {
        public MainWindowAssetSetupResult Build(
            ISessionService sessionService,
            AddonTypeHintService addonTypeHintService,
            ModelPickerCacheService modelPickerCacheService,
            IconResolutionService iconResolutionService,
            IdGenerationService idGenerationService,
            ItemFieldClassifierService itemFieldClassifierService,
            AddonTypeDisplayService addonTypeDisplayService,
            AddonParamService addonParamService,
            FieldValueValidationService fieldValueValidationService)
        {
            AssetManager assetManager = new AssetManager(sessionService);
            assetManager.load();

            AddonTypeOptionService addonTypeOptionService = new AddonTypeOptionService(addonTypeHintService);
            ModelPickerService modelPickerService = new ModelPickerService(
                modelPickerCacheService,
                assetManager,
                iconResolutionService,
                idGenerationService,
                itemFieldClassifierService);
            ItemValueRowBuilderService itemValueRowBuilderService = new ItemValueRowBuilderService(
                addonTypeDisplayService,
                addonParamService,
                modelPickerService);
            ItemSelectionWorkflowService itemSelectionWorkflowService = new ItemSelectionWorkflowService(itemValueRowBuilderService);
            ValueCompatibilityService valueCompatibilityService = new ValueCompatibilityService(fieldValueValidationService);
            ValueChangeService valueChangeService = new ValueChangeService(
                addonParamService,
                modelPickerService,
                idGenerationService,
                iconResolutionService);

            return new MainWindowAssetSetupResult
            {
                AssetManager = assetManager,
                AddonTypeOptionService = addonTypeOptionService,
                ModelPickerService = modelPickerService,
                ItemValueRowBuilderService = itemValueRowBuilderService,
                ItemSelectionWorkflowService = itemSelectionWorkflowService,
                ValueCompatibilityService = valueCompatibilityService,
                ValueChangeService = valueChangeService
            };
        }
    }
}
