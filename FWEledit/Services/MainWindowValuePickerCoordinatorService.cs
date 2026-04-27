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
            InlineIconButtonService inlineIconButtonService,
            Button inlineButton,
            DataGridView valuesGrid,
            TabControl rightTabs,
            TabPage valuesTab,
            ItemFieldClassifierService fieldClassifierService,
            ref int inlineRowIndex,
            bool suppressValuesUiRefresh)
        {
            if (mainWindowValueRowPickerUiService == null)
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
                fieldClassifierService,
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
            Action<int> openIconPickerForValueRow,
            Action<int> openAddonTypePickerForValueRow,
            Action<int> openItemQualityPickerForValueRow,
            Action<int> openModelPickerForValueRow,
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
                openIconPickerForValueRow,
                openAddonTypePickerForValueRow,
                openItemQualityPickerForValueRow,
                openModelPickerForValueRow,
                updatePickIconButtonState,
                showMessage);
        }
    }
}
