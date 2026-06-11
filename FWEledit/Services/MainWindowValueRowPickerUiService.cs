using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowValueRowPickerUiService
    {
        public void OpenModelPickerForValueRow(
            ValueRowPickerUiService valueRowPickerUiService,
            eListCollection listCollection,
            CacheSave database,
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
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenModelPickerForValueRow(
                listCollection,
                database,
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
            ValueRowPickerUiService valueRowPickerUiService,
            ISessionService sessionService,
            eListCollection listCollection,
            DataGridView valuesGrid,
            DataGridView listGrid,
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
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenAddonTypePickerForValueRow(
                sessionService,
                listCollection,
                valuesGrid,
                listGrid,
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
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IList<QualityOption> qualityOptions,
            IWin32Window owner)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenItemQualityPickerForValueRow(
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                qualityOptions,
                owner);
        }

        public void OpenGenderTypePickerForValueRow(
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenGenderTypePickerForValueRow(
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenPetFoodTypePickerForValueRow(
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenPetFoodTypePickerForValueRow(
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenPetHeroPickerForValueRow(
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenPetHeroPickerForValueRow(
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenImmuneTypePickerForValueRow(
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenImmuneTypePickerForValueRow(
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenBindFlagPickerForValueRow(
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenBindFlagPickerForValueRow(
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenNpcSellMoneyTypePickerForValueRow(
            ValueRowPickerUiService valueRowPickerUiService,
            eListCollection listCollection,
            int listIndex,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenNpcSellMoneyTypePickerForValueRow(
                listCollection,
                listIndex,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenReputationPickerForValueRow(
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenReputationPickerForValueRow(
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenSoulToolRewardTypePickerForValueRow(
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenSoulToolRewardTypePickerForValueRow(
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenProcTypePickerForValueRow(
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenProcTypePickerForValueRow(
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenProfessionMaskPickerForValueRow(
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenProfessionMaskPickerForValueRow(
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenCombinedServicesPickerForValueRow(
            ValueRowPickerUiService valueRowPickerUiService,
            eListCollection listCollection,
            int listIndex,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenCombinedServicesPickerForValueRow(
                listCollection,
                listIndex,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenRaceMaskPickerForValueRow(
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenRaceMaskPickerForValueRow(
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenModelProfessionPickerForValueRow(
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenModelProfessionPickerForValueRow(
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenModelRacePickerForValueRow(
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenModelRacePickerForValueRow(
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner);
        }

        public void OpenSkillPickerForValueRow(
            ValueRowPickerUiService valueRowPickerUiService,
            CacheSave database,
            DataGridView valuesGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            IWin32Window owner,
            Action<string> showMessage)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenSkillPickerForValueRow(
                database,
                valuesGrid,
                rowIndex,
                fieldClassifierService,
                owner,
                showMessage);
        }

        public void OpenItemReferencePickerForValueRow(
            ValueRowPickerUiService valueRowPickerUiService,
            eListCollection listCollection,
            CacheSave database,
            DataGridView valuesGrid,
            int listIndex,
            int currentElementIndex,
            int rowIndex,
            ItemReferenceService itemReferenceService,
            IconResolutionService iconResolutionService,
            IWin32Window owner,
            Action<string> showMessage)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenItemReferencePickerForValueRow(
                listCollection,
                database,
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
            ValueRowPickerUiService valueRowPickerUiService,
            eListCollection listCollection,
            CacheSave database,
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
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenIconPickerForValueRow(
                listCollection,
                database,
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
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenModelPreviewForValueRow(
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
            ValueRowPickerUiService valueRowPickerUiService,
            InlineIconButtonService inlineIconButtonService,
            Button inlineButton,
            DataGridView valuesGrid,
            TabControl rightTabs,
            TabPage valuesTab,
            eListCollection listCollection,
            int listIndex,
            ItemFieldClassifierService fieldClassifierService,
            ItemReferenceService itemReferenceService,
            ref int inlineRowIndex,
            bool suppressValuesUiRefresh)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.UpdateInlineIconButton(
                inlineIconButtonService,
                inlineButton,
                valuesGrid,
                rightTabs,
                valuesTab,
                listCollection,
                listIndex,
                fieldClassifierService,
                itemReferenceService,
                ref inlineRowIndex,
                suppressValuesUiRefresh);
        }

        public void HandleInlinePickIconClick(
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            int inlineRowIndex,
            Action<int> openIconPicker)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.HandleInlinePickIconClick(
                valuesGrid,
                inlineRowIndex,
                openIconPicker);
        }

        public void HandleCellDoubleClick(
            ValueRowPickerUiService valueRowPickerUiService,
            DataGridView valuesGrid,
            eListCollection listCollection,
            int listIndex,
            DataGridViewCellEventArgs args,
            ItemFieldClassifierService fieldClassifierService,
            ItemReferenceService itemReferenceService,
            Action<int> openIconPicker,
            Action<int> openAddonPicker,
            Action<int> openQualityPicker,
            Action<int> openGenderTypePicker,
            Action<int> openPetFoodTypePicker,
            Action<int> openPetHeroPicker,
            Action<int> openImmuneTypePicker,
            Action<int> openBindFlagPicker,
            Action<int> openNpcSellMoneyTypePicker,
            Action<int> openReputationPicker,
            Action<int> openSoulToolRewardTypePicker,
            Action<int> openProcTypePicker,
            Action<int> openProfessionMaskPicker,
            Action<int> openRaceMaskPicker,
            Action<int> openModelProfessionPicker,
            Action<int> openModelRacePicker,
            Action<int> openCombinedServicesPicker,
            Action<int> openSkillPicker,
            Action<int> openModelPicker,
            Action<int> openItemReferencePicker,
            Action updateInlineButton,
            Action<string> showMessage)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.HandleCellDoubleClick(
                valuesGrid,
                listCollection,
                listIndex,
                args,
                fieldClassifierService,
                itemReferenceService,
                openIconPicker,
                openAddonPicker,
                openQualityPicker,
                openGenderTypePicker,
                openPetFoodTypePicker,
                openPetHeroPicker,
                openImmuneTypePicker,
                openBindFlagPicker,
                openNpcSellMoneyTypePicker,
                openReputationPicker,
                openSoulToolRewardTypePicker,
                openProcTypePicker,
                openProfessionMaskPicker,
                openRaceMaskPicker,
                openModelProfessionPicker,
                openModelRacePicker,
                openCombinedServicesPicker,
                openSkillPicker,
                openModelPicker,
                openItemReferencePicker,
                updateInlineButton,
                showMessage);
        }
    }
}
