using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowElementsLoadUiService
    {
        public void LoadFromDialog(
            GameFolderLoadUiService loadUiService,
            GameFolderDialogService dialogService,
            NavigationStateService navigationStateService,
            Action<string> loadGameFolder,
            Action<string> showMessage)
        {
            if (loadUiService == null)
            {
                return;
            }

            loadUiService.LoadFromDialog(
                dialogService,
                navigationStateService,
                loadGameFolder,
                showMessage);
        }

        public void LoadLastFolder(
            GameFolderLoadUiService loadUiService,
            GameFolderDialogService dialogService,
            NavigationStateService navigationStateService,
            Action<string> loadGameFolder,
            Action<string> showMessage)
        {
            if (loadUiService == null)
            {
                return;
            }

            loadUiService.LoadLastFolder(
                dialogService,
                navigationStateService,
                loadGameFolder,
                showMessage);
        }

        public void LoadGameFolder(
            string gameFolderPath,
            ElementsLoadUiService loadUiService,
            ElementsLoadWorkflowService workflowService,
            ColorProgressBar.ColorProgressBar progressBar,
            Action resetDirtyTracking,
            Action cancelIconWarmup,
            Action beginIconWarmup,
            Action applyTheme,
            Action loadDescriptions,
            Action persistNavigationState,
            Action<ElementsLoadResult> applyLoadResult,
            ListDisplayService listDisplayService,
            ListComboPopulationService listComboPopulationService,
            ExportRulesMenuService exportRulesMenuService,
            XrefMenuService xrefMenuService,
            NavigationSelectionService navigationSelectionService,
            NavigationStateService navigationStateService,
            WindowTitleService windowTitleService,
            IconListAvailabilityService iconListAvailabilityService,
            AssetManager assetManager,
            MainWindowViewModel viewModel,
            ComboBox listCombo,
            DataGridView valuesGrid,
            DataGridView elementsGrid,
            ToolStripMenuItem exportContainerMenuItem,
            ToolStripSeparator xrefSeparator,
            ToolStripMenuItem xrefItemMenuItem,
            EventHandler exportHandler,
            Form owner,
            Action<Cursor> setCursor,
            Action<string> showMessage)
        {
            if (loadUiService == null)
            {
                return;
            }

            loadUiService.LoadGameFolder(
                gameFolderPath,
                workflowService,
                progressBar,
                resetDirtyTracking,
                cancelIconWarmup,
                beginIconWarmup,
                applyTheme,
                loadDescriptions,
                persistNavigationState,
                applyLoadResult,
                listDisplayService,
                listComboPopulationService,
                exportRulesMenuService,
                xrefMenuService,
                navigationSelectionService,
                navigationStateService,
                windowTitleService,
                iconListAvailabilityService,
                assetManager,
                viewModel,
                listCombo,
                valuesGrid,
                elementsGrid,
                exportContainerMenuItem,
                xrefSeparator,
                xrefItemMenuItem,
                exportHandler,
                owner,
                setCursor,
                showMessage);
        }
    }
}
