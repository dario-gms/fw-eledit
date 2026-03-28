using System;
using System.IO;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementsLoadUiService
    {
        public bool LoadGameFolder(
            string gameFolderPath,
            ElementsLoadWorkflowService workflowService,
            ColorProgressBar.ColorProgressBar progressBar,
            Action resetState,
            Action cancelIconWarmup,
            Action beginIconWarmup,
            Action applyTheme,
            Action loadDescriptions,
            Action persistNavigationState,
            Action<ElementsLoadResult> applyResult,
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
            ComboBox listComboBox,
            DataGridView itemGrid,
            DataGridView elementGrid,
            ToolStripMenuItem exportContainerMenu,
            ToolStripSeparator xrefSeparator,
            ToolStripMenuItem xrefMenuItem,
            EventHandler exportClickHandler,
            Form owner,
            Action<Cursor> setCursor,
            Action<string> showMessage)
        {
            if (string.IsNullOrWhiteSpace(gameFolderPath) || !Directory.Exists(gameFolderPath))
            {
                if (showMessage != null)
                {
                    showMessage("Invalid game folder.");
                }
                return false;
            }

            if (workflowService == null)
            {
                return false;
            }

            try
            {
                if (setCursor != null)
                {
                    setCursor(Cursors.AppStarting);
                }

                if (resetState != null)
                {
                    resetState();
                }

                if (cancelIconWarmup != null)
                {
                    cancelIconWarmup();
                }

                ElementsLoadResult result = workflowService.LoadFromGameFolder(gameFolderPath, ref progressBar);
                if (result == null || !result.Success)
                {
                    if (showMessage != null)
                    {
                        showMessage(result != null && !string.IsNullOrWhiteSpace(result.ErrorMessage)
                            ? result.ErrorMessage
                            : "LOADING ERROR!");
                    }
                    if (result != null && result.IsVersionUnsupported && navigationStateService != null)
                    {
                        navigationStateService.ResetOnStartup(navigationStateService.GetLastRunVersion());
                    }
                    if (progressBar != null)
                    {
                        progressBar.Value = 0;
                    }
                    return false;
                }

                if (applyResult != null)
                {
                    applyResult(result);
                }

                if (listDisplayService != null)
                {
                    listDisplayService.ResetList0DisplayCache();
                    listDisplayService.ClearListDisplayCache();
                }

                if (assetManager != null)
                {
                    assetManager.SetGameRootFromElements(result.ElementsPath ?? string.Empty);
                    assetManager.load();
                    if (viewModel != null && viewModel.Session != null)
                    {
                        viewModel.Session.AssetManager = assetManager;
                    }
                }

                if (beginIconWarmup != null)
                {
                    try
                    {
                        beginIconWarmup();
                    }
                    catch
                    {
                    }
                }

                if (applyTheme != null)
                {
                    applyTheme();
                }

                if (exportRulesMenuService != null)
                {
                    exportRulesMenuService.UpdateMenu(
                        exportContainerMenu,
                        result.ListCollection != null && result.ListCollection.ConfigFile != null,
                        result.ExportRules,
                        exportClickHandler);
                }

                if (xrefMenuService != null)
                {
                    xrefMenuService.SetVisibility(xrefSeparator, xrefMenuItem, result.HasXrefs);
                }

                if (itemGrid != null)
                {
                    itemGrid.Rows.Clear();
                }

                if (listComboPopulationService != null)
                {
                    listComboPopulationService.PopulateLists(listComboBox, result.ListCollection, listDisplayService);
                }

                if (windowTitleService != null && result.ListCollection != null)
                {
                    windowTitleService.Apply(owner, result.ListCollection, result.ElementsPath);
                }

                if (loadDescriptions != null)
                {
                    loadDescriptions();
                }

                if (iconListAvailabilityService != null)
                {
                    iconListAvailabilityService.EnsureIconListAvailable(assetManager);
                }

                if (progressBar != null)
                {
                    progressBar.Value = 0;
                }

                if (navigationSelectionService != null)
                {
                    navigationSelectionService.RestoreSelection(
                        result.NavigationSnapshot,
                        listComboBox,
                        elementGrid,
                        viewModel,
                        persistNavigationState);
                }

                if (navigationStateService != null)
                {
                    navigationStateService.SaveGameFolder(gameFolderPath);
                }

                return true;
            }
            catch
            {
                if (showMessage != null)
                {
                    showMessage(
                        "LOADING ERROR!\n\nThis error usually occurs if incorrect configuration, structure, or encrypted elements.data file...\n" +
                        "If you are using elements.list.count trying to decrypt, its likely the last list item count is incorrect... \n" +
                        "Use details below to assist... \n\nRead Failed at this point :\n" +
                        eListCollection.SStat[0].ToString() + " - List #\n" +
                        eListCollection.SStat[1].ToString() + " - # Items This List\n" +
                        eListCollection.SStat[2].ToString() + " - Item ID");
                }
                return false;
            }
            finally
            {
                if (setCursor != null)
                {
                    setCursor(Cursors.Default);
                }
            }
        }
    }
}
