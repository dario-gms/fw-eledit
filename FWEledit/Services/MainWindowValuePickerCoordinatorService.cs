using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowValuePickerCoordinatorService
    {
        public void OpenModelPickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            ISessionService sessionService,
            DataGridView valuesGrid,
            int listIndex,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            ModelPickerService modelPickerService,
            ModelPickerCacheService modelPickerCacheService,
            ModelPackageNotificationService packageNotificationService,
            DialogService dialogService,
            PathIdResolutionService pathIdResolutionService,
            AssetManager assetManager,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null || sessionService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenModelPickerForValueRow(
                valueRowPickerUiService,
                sessionService.ListCollection,
                sessionService.Database,
                valuesGrid,
                listIndex,
                rowIndex,
                fieldClassifierService,
                modelPickerService,
                modelPickerCacheService,
                packageNotificationService,
                dialogService,
                pathIdResolutionService,
                assetManager,
                owner);
        }

        public void OpenAddonTypePickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            ISessionService sessionService,
            DataGridView valuesGrid,
            DataGridView elementsGrid,
            int listIndex,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            AddonTypeOptionService addonTypeOptionService,
            string startupPath,
            string gameRootPath,
            Func<int, int> getFieldIndexForValueRow,
            Action<string> showMessage,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null || sessionService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenAddonTypePickerForValueRow(
                valueRowPickerUiService,
                sessionService,
                sessionService.ListCollection,
                valuesGrid,
                elementsGrid,
                listIndex,
                rowIndex,
                fieldClassifierService,
                addonTypeOptionService,
                startupPath,
                gameRootPath,
                getFieldIndexForValueRow,
                showMessage,
                owner);
        }

        public void OpenItemQualityPickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            System.Collections.Generic.IList<QualityOption> qualityOptions,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenItemQualityPickerForValueRow(
                valueRowPickerUiService,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                qualityOptions,
                owner);
        }

        public void OpenGenderTypePickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenGenderTypePickerForValueRow(
                valueRowPickerUiService,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenPetFoodTypePickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenPetFoodTypePickerForValueRow(
                valueRowPickerUiService,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenPetHeroPickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenPetHeroPickerForValueRow(
                valueRowPickerUiService,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenImmuneTypePickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenImmuneTypePickerForValueRow(
                valueRowPickerUiService,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenBindFlagPickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenBindFlagPickerForValueRow(
                valueRowPickerUiService,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenNpcSellMoneyTypePickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            eListCollection listCollection,
            int listIndex,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenNpcSellMoneyTypePickerForValueRow(
                valueRowPickerUiService,
                listCollection,
                listIndex,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenReputationPickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenReputationPickerForValueRow(
                valueRowPickerUiService,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenSoulToolRewardTypePickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenSoulToolRewardTypePickerForValueRow(
                valueRowPickerUiService,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenProcTypePickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenProcTypePickerForValueRow(
                valueRowPickerUiService,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenProfessionMaskPickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenProfessionMaskPickerForValueRow(
                valueRowPickerUiService,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenCombinedServicesPickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            ISessionService sessionService,
            DataGridView valuesGrid,
            int listIndex,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null || sessionService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenCombinedServicesPickerForValueRow(
                valueRowPickerUiService,
                sessionService.ListCollection,
                listIndex,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenRaceMaskPickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenRaceMaskPickerForValueRow(
                valueRowPickerUiService,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenModelProfessionPickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenModelProfessionPickerForValueRow(
                valueRowPickerUiService,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenModelRacePickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenModelRacePickerForValueRow(
                valueRowPickerUiService,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenSkillPickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            ISessionService sessionService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner,
            Action<string> showMessage)
        {
            if (mainWindowValueRowPickerUiService == null || sessionService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenSkillPickerForValueRow(
                valueRowPickerUiService,
                sessionService.Database,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner,
                showMessage);
        }

        public void OpenItemReferencePickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            ISessionService sessionService,
            DataGridView valuesGrid,
            int listIndex,
            int currentElementIndex,
            int rowIndex,
            ItemReferenceService itemReferenceService,
            IconResolutionService iconResolutionService,
            IWin32Window owner,
            Action<string> showMessage)
        {
            if (mainWindowValueRowPickerUiService == null || sessionService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenItemReferencePickerForValueRow(
                valueRowPickerUiService,
                sessionService.ListCollection,
                sessionService.Database,
                valuesGrid,
                listIndex,
                currentElementIndex,
                rowIndex,
                itemReferenceService,
                iconResolutionService,
                owner,
                showMessage);
        }

        public void OpenIconPickerForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            ISessionService sessionService,
            DataGridView valuesGrid,
            int listIndex,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            PathIdResolutionService pathIdResolutionService,
            ModelPickerService modelPickerService,
            ValueRowIndexService valueRowIndexService,
            IconUsageLookupService iconUsageLookupService,
            IWin32Window owner)
        {
            if (mainWindowValueRowPickerUiService == null || sessionService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenIconPickerForValueRow(
                valueRowPickerUiService,
                sessionService.ListCollection,
                sessionService.Database,
                valuesGrid,
                listIndex,
                rowIndex,
                fieldClassifierService,
                pathIdResolutionService,
                modelPickerService,
                valueRowIndexService,
                iconUsageLookupService,
                owner);
        }

        public void OpenModelPreviewForValueRow(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            AssetManager assetManager,
            CacheSave database,
            eListCollection listCollection,
            DataGridView valuesGrid,
            int listIndex,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            PathIdResolutionService pathIdResolutionService,
            ModelPickerService modelPickerService,
            ModelPreviewService modelPreviewService,
            Form owner,
            Action<string> showMessage)
        {
            if (mainWindowValueRowPickerUiService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.OpenModelPreviewForValueRow(
                valueRowPickerUiService,
                assetManager,
                database,
                listCollection,
                valuesGrid,
                listIndex,
                rowIndex,
                fieldClassifierService,
                pathIdResolutionService,
                modelPickerService,
                modelPreviewService,
                owner,
                showMessage);
        }

        public void UpdatePickIconButtonState(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            ISessionService sessionService,
            InlineIconButtonService inlineIconButtonService,
            Button inlineButton,
            DataGridView valuesGrid,
            TabControl rightTabs,
            TabPage valuesTab,
            int listIndex,
            ItemFieldClassifierService fieldClassifierService,
            ItemReferenceService itemReferenceService,
            ref int inlineRowIndex,
            bool suppressValuesUiRefresh)
        {
            if (mainWindowValueRowPickerUiService == null || sessionService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.UpdatePickIconButtonState(
                valueRowPickerUiService,
                inlineIconButtonService,
                inlineButton,
                valuesGrid,
                rightTabs,
                valuesTab,
                sessionService.ListCollection,
                listIndex,
                fieldClassifierService,
                itemReferenceService,
                ref inlineRowIndex,
                suppressValuesUiRefresh);
        }

        public void HandleInlinePickIconClick(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int inlineRowIndex,
            Action<int> openIconPicker)
        {
            if (mainWindowValueRowPickerUiService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.HandleInlinePickIconClick(
                valueRowPickerUiService,
                valuesGrid,
                inlineRowIndex,
                openIconPicker);
        }

        public void HandleCellDoubleClick(
            MainWindowValueRowPickerUiService mainWindowValueRowPickerUiService,
            ValueRowPickerUiService valueRowPickerUiService,
            ISessionService sessionService,
            DataGridView valuesGrid,
            int listIndex,
            DataGridViewCellEventArgs args,
            ItemFieldClassifierService fieldClassifierService,
            ItemReferenceService itemReferenceService,
            Action<int> openIconPickerForValueRow,
            Action<int> openAddonTypePickerForValueRow,
            Action<int> openItemQualityPickerForValueRow,
            Action<int> openGenderTypePickerForValueRow,
            Action<int> openPetFoodTypePickerForValueRow,
            Action<int> openPetHeroPickerForValueRow,
            Action<int> openImmuneTypePickerForValueRow,
            Action<int> openBindFlagPickerForValueRow,
            Action<int> openNpcSellMoneyTypePickerForValueRow,
            Action<int> openReputationPickerForValueRow,
            Action<int> openSoulToolRewardTypePickerForValueRow,
            Action<int> openProcTypePickerForValueRow,
            Action<int> openProfessionMaskPickerForValueRow,
            Action<int> openRaceMaskPickerForValueRow,
            Action<int> openModelProfessionPickerForValueRow,
            Action<int> openModelRacePickerForValueRow,
            Action<int> openCombinedServicesPickerForValueRow,
            Action<int> openSkillPickerForValueRow,
            Action<int> openModelPickerForValueRow,
            Action<int> openItemReferencePickerForValueRow,
            Action updatePickIconButtonState,
            Action<string> showMessage)
        {
            if (mainWindowValueRowPickerUiService == null || sessionService == null)
            {
                return;
            }

            mainWindowValueRowPickerUiService.HandleCellDoubleClick(
                valueRowPickerUiService,
                valuesGrid,
                sessionService.ListCollection,
                listIndex,
                args,
                fieldClassifierService,
                itemReferenceService,
                openIconPickerForValueRow,
                openAddonTypePickerForValueRow,
                openItemQualityPickerForValueRow,
                openGenderTypePickerForValueRow,
                openPetFoodTypePickerForValueRow,
                openPetHeroPickerForValueRow,
                openImmuneTypePickerForValueRow,
                openBindFlagPickerForValueRow,
                openNpcSellMoneyTypePickerForValueRow,
                openReputationPickerForValueRow,
                openSoulToolRewardTypePickerForValueRow,
                openProcTypePickerForValueRow,
                openProfessionMaskPickerForValueRow,
                openRaceMaskPickerForValueRow,
                openModelProfessionPickerForValueRow,
                openModelRacePickerForValueRow,
                openCombinedServicesPickerForValueRow,
                openSkillPickerForValueRow,
                openModelPickerForValueRow,
                openItemReferencePickerForValueRow,
                updatePickIconButtonState,
                showMessage);
        }
    }
}
