using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
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

                SaveProgressService.SetVisible(progressBar, true);
                SetProgress(progressBar, 2);

                if (resetState != null)
                {
                    resetState();
                }

                if (cancelIconWarmup != null)
                {
                    cancelIconWarmup();
                }

                SaveProgressService.BeginScope(progressBar, 4, 58);
                ElementsLoadResult result = null;
                try
                {
                    result = workflowService.LoadFromGameFolder(gameFolderPath, ref progressBar);
                }
                finally
                {
                    SaveProgressService.EndScope(progressBar);
                }

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
                    SetProgress(progressBar, 0);
                    SaveProgressService.SetVisible(progressBar, false);
                    return false;
                }

                SetProgress(progressBar, 62);

                if (applyResult != null)
                {
                    applyResult(result);
                }

                SetProgress(progressBar, 66);

                if (listDisplayService != null)
                {
                    listDisplayService.ResetList0DisplayCache();
                    listDisplayService.ClearListDisplayCache();
                }

                SetProgress(progressBar, 70);

                if (assetManager != null)
                {
                    assetManager.SetGameRootFromElements(result.ElementsPath ?? string.Empty);
                    SetProgress(progressBar, 73);
                    assetManager.load(false);
                    SetProgress(progressBar, 84);
                    assetManager.EnsureVisualAssetsLoaded();
                    SetProgress(progressBar, 92);
                    if (viewModel != null && viewModel.Session != null)
                    {
                        viewModel.Session.AssetManager = assetManager;
                    }
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

                SetProgress(progressBar, 94);

                if (itemGrid != null)
                {
                    itemGrid.Rows.Clear();
                }

                if (listComboPopulationService != null)
                {
                    listComboPopulationService.PopulateLists(listComboBox, result.ListCollection, listDisplayService);
                }

                SetProgress(progressBar, 97);

                if (windowTitleService != null && result.ListCollection != null)
                {
                    windowTitleService.Apply(owner, result.ListCollection, result.ElementsPath);
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

                SetProgress(progressBar, 99);

                loadDescriptions?.Invoke();
                TryInvokeOwnerMethod(owner, "change_item", null, null);
                
                StartDeferredAssetWarmup(
                    assetManager,
                    beginIconWarmup,
                    applyTheme,
                    loadDescriptions,
                    owner,
                    itemGrid,
                    elementGrid);

                if (showMessage != null && !string.IsNullOrWhiteSpace(result.WarningMessage))
                {
                    showMessage(result.WarningMessage);
                }

                if (navigationStateService != null)
                {
                    navigationStateService.SaveGameFolder(gameFolderPath);
                }

                SetProgress(progressBar, 100);

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
                SetProgress(progressBar, 0);
                return false;
            }
            finally
            {
                SaveProgressService.EndScope(progressBar);
                SaveProgressService.SetVisible(progressBar, false);
                if (setCursor != null)
                {
                    setCursor(Cursors.Default);
                }
            }
        }

        private static void SetProgress(ColorProgressBar.ColorProgressBar progressBar, int value)
        {
            if (progressBar == null)
            {
                return;
            }

            try
            {
                progressBar.Minimum = 0;
                progressBar.Maximum = 100;
                progressBar.Value = Math.Max(0, Math.Min(100, value));
                progressBar.Refresh();
                Application.DoEvents();
            }
            catch
            {
            }
        }

        private void StartDeferredAssetWarmup(
            AssetManager assetManager,
            Action beginIconWarmup,
            Action applyTheme,
            Action loadDescriptions,
            Form owner,
            DataGridView itemGrid,
            DataGridView elementGrid)
        {
            if (assetManager == null)
            {
                return;
            }

            Action queueWarmup = () =>
            {
                Task.Run(() =>
                {
                    bool metadataLoaded = false;
                    try
                    {
                        metadataLoaded = assetManager.EnsureDeferredMetadataLoaded();
                    }
                    catch
                    {
                        metadataLoaded = false;
                    }

                    if (owner == null || owner.IsDisposed)
                    {
                        return;
                    }

                    try
                    {
                        owner.BeginInvoke((Action)(() =>
                        {
                            if (owner.IsDisposed)
                            {
                                return;
                            }

                            applyTheme?.Invoke();
                            if (metadataLoaded)
                            {
                                loadDescriptions?.Invoke();
                            }
                            beginIconWarmup?.Invoke();

                            TryClearOwnerHashSetField(owner, "hydratedElementRowIconKeys");
                            TryInvokeOwnerMethod(owner, "ScheduleVisibleElementIconHydration");
                            TryInvokeOwnerMethod(owner, "ScheduleVisibleReferenceCountRefresh");
                            TryInvokeOwnerMethod(owner, "change_item", null, null);

                            elementGrid?.Refresh();
                            itemGrid?.Refresh();
                        }));
                    }
                    catch
                    {
                    }
                });
            };

            try
            {
                if (owner != null && owner.IsHandleCreated && !owner.IsDisposed)
                {
                    owner.BeginInvoke(queueWarmup);
                }
                else
                {
                    queueWarmup();
                }
            }
            catch
            {
                queueWarmup();
            }
        }

        private static void TryInvokeOwnerMethod(Form owner, string methodName, object arg0 = null, object arg1 = null)
        {
            if (owner == null || string.IsNullOrWhiteSpace(methodName))
            {
                return;
            }

            try
            {
                MethodInfo method = owner.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (method == null)
                {
                    return;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 0)
                {
                    method.Invoke(owner, null);
                }
                else if (parameters.Length == 2)
                {
                    method.Invoke(owner, new object[] { arg0, arg1 });
                }
            }
            catch
            {
            }
        }

        private static void TryClearOwnerHashSetField(Form owner, string fieldName)
        {
            if (owner == null || string.IsNullOrWhiteSpace(fieldName))
            {
                return;
            }

            try
            {
                FieldInfo field = owner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null)
                {
                    return;
                }

                object value = field.GetValue(owner);
                if (value == null)
                {
                    return;
                }

                MethodInfo clearMethod = value.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
                clearMethod?.Invoke(value, null);
            }
            catch
            {
            }
        }
    }
}
