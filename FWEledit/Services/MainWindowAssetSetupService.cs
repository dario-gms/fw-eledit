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
            // Keep startup lightweight. Full asset extraction/loading is deferred
            // to the game-folder load flow.
            assetManager.load(false);

            AddonTypeOptionService addonTypeOptionService = new AddonTypeOptionService(addonTypeHintService);
            ItemReferenceService itemReferenceService = new ItemReferenceService();
            ModelPickerService modelPickerService = new ModelPickerService(
                modelPickerCacheService,
                assetManager,
                iconResolutionService,
                idGenerationService,
                itemFieldClassifierService);
            ItemValueRowBuilderService itemValueRowBuilderService = new ItemValueRowBuilderService(
                addonTypeDisplayService,
                addonParamService,
                modelPickerService,
                iconResolutionService,
                itemReferenceService);
            ItemSelectionWorkflowService itemSelectionWorkflowService = new ItemSelectionWorkflowService(itemValueRowBuilderService);
            ValueCompatibilityService valueCompatibilityService = new ValueCompatibilityService(fieldValueValidationService);
            ValueChangeService valueChangeService = new ValueChangeService(
                addonParamService,
                modelPickerService,
                idGenerationService,
                iconResolutionService,
                itemReferenceService);

            return new MainWindowAssetSetupResult
            {
                AssetManager = assetManager,
                AddonTypeOptionService = addonTypeOptionService,
                ItemReferenceService = itemReferenceService,
                ModelPickerService = modelPickerService,
                ItemValueRowBuilderService = itemValueRowBuilderService,
                ItemSelectionWorkflowService = itemSelectionWorkflowService,
                ValueCompatibilityService = valueCompatibilityService,
                ValueChangeService = valueChangeService
            };
        }
    }
}
