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
            DataGridView valuesGrid,
            int listIndex,
            int rowIndex,
            ItemFieldClassifierService fieldClassifierService,
            PathIdResolutionService pathIdResolutionService,
            ModelPickerService modelPickerService,
            ModelPreviewService modelPreviewService,
            Action<string> showMessage)
        {
            if (valueRowPickerUiService == null)
            {
                return;
            }

            valueRowPickerUiService.OpenModelPreviewForValueRow(
                assetManager,
                database,
                valuesGrid,
                listIndex,
                rowIndex,
                fieldClassifierService,
                pathIdResolutionService,
                modelPickerService,
                modelPreviewService,
                showMessage);
        }

        public void UpdatePickIconButtonState(
            ValueRowPickerUiService valueRowPickerUiService,
            InlineIconButtonService inlineIconButtonService,
            Button inlineButton,
            DataGridView valuesGrid,
            TabControl rightTabs,
            TabPage valuesTab,
            ItemFieldClassifierService fieldClassifierService,
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
                fieldClassifierService,
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
            Action<int> openIconPicker,
            Action<int> openAddonPicker,
            Action<int> openQualityPicker,
            Action<int> openModelPicker,
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
                openIconPicker,
                openAddonPicker,
                openQualityPicker,
                openModelPicker,
                updateInlineButton,
                showMessage);
        }
    }
}
