using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Text;
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
            ListRowBuilderService listRowBuilderService,
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

            ClientCacheBuildProgressWindow loadProgressWindow = null;
            try
            {
                if (setCursor != null)
                {
                    setCursor(Cursors.AppStarting);
                }

                SaveProgressService.SetVisible(progressBar, true);
                SetProgress(progressBar, 2);
                try
                {
                    if (owner != null && !owner.IsDisposed)
                    {
                        loadProgressWindow = new ClientCacheBuildProgressWindow();
                        loadProgressWindow.Show(owner);
                        loadProgressWindow.UpdateProgress(
                            "Opening client",
                            "Reading elements.data and preparing the editor. Please wait.",
                            0,
                            true);
                        loadProgressWindow.Update();
                        Application.DoEvents();
                    }
                }
                catch
                {
                    loadProgressWindow = null;
                }

                if (resetState != null)
                {
                    resetState();
                }

                if (cancelIconWarmup != null)
                {
                    cancelIconWarmup();
                }

                Stopwatch totalStopwatch = Stopwatch.StartNew();
                Stopwatch stageStopwatch = Stopwatch.StartNew();
                StringBuilder startupProfile = new StringBuilder();
                startupProfile.AppendLine("Client: " + gameFolderPath);
                startupProfile.AppendLine("Started: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                string liveStartupProfilePath = InitializeLiveStartupProfile(gameFolderPath);

                SaveProgressService.BeginScope(progressBar, 4, 58);
                ElementsLoadResult result = null;
                try
                {
                    UpdateLoadProgressWindow(
                        loadProgressWindow,
                        "Reading elements.data",
                        "Loading list structures and values from the selected client.",
                        8);
                    WriteLiveStartupProfile(liveStartupProfilePath, "START Load elements.data");
                    result = workflowService.LoadFromGameFolder(gameFolderPath, ref progressBar);
                }
                finally
                {
                    SaveProgressService.EndScope(progressBar);
                }
                AppendStartupProfile(startupProfile, "Load elements.data", stageStopwatch);
                WriteLiveStartupProfile(liveStartupProfilePath, startupProfile.ToString().TrimEnd());

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
                    UpdateLoadProgressWindow(
                        loadProgressWindow,
                        "Applying loaded data",
                        "Preparing editor state for the loaded elements.data.",
                        60);
                    stageStopwatch.Restart();
                    WriteLiveStartupProfile(liveStartupProfilePath, "START Apply loaded data");
                    applyResult(result);
                    AppendStartupProfile(startupProfile, "Apply loaded data", stageStopwatch);
                    WriteLiveStartupProfile(liveStartupProfilePath, "END Apply loaded data: " + stageStopwatch.ElapsedMilliseconds.ToString() + " ms");
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
                    stageStopwatch.Restart();
                    WriteLiveStartupProfile(liveStartupProfilePath, "START Check client resource map");
                    bool needsClientMapBuild = !assetManager.HasValidClientResourceMap();
                    AppendStartupProfile(startupProfile, "Check client resource map", stageStopwatch);
                    WriteLiveStartupProfile(liveStartupProfilePath, "END Check client resource map: " + stageStopwatch.ElapsedMilliseconds.ToString() + " ms");
                    ClientCacheBuildProgressWindow cacheProgressWindow = null;
                    try
                    {
                        if (needsClientMapBuild)
                        {
                            UpdateLoadProgressWindow(
                                loadProgressWindow,
                                "Indexing resources",
                                "Reading package indexes, path maps, icons, text resources, and description data.",
                                72);
                        }
                        else
                        {
                            UpdateLoadProgressWindow(
                                loadProgressWindow,
                                "Checking resource map",
                                "Using the existing client resource map.",
                                72);
                        }

                        stageStopwatch.Restart();
                        WriteLiveStartupProfile(liveStartupProfilePath, "START Prepare workspace/map");
                        assetManager.load(false);
                        AppendStartupProfile(startupProfile, "Prepare workspace/map", stageStopwatch);
                        WriteLiveStartupProfile(liveStartupProfilePath, "END Prepare workspace/map: " + stageStopwatch.ElapsedMilliseconds.ToString() + " ms");
                        SetProgress(progressBar, 80);

                        UpdateLoadProgressWindow(
                            loadProgressWindow,
                            "Preparing visual assets",
                            "Preparing icons and package-backed resources for the editor.",
                            82);

                        stageStopwatch.Restart();
                        WriteLiveStartupProfile(liveStartupProfilePath, "START Prepare visual assets");
                        assetManager.EnsureVisualAssetsLoaded();
                        AppendStartupProfile(startupProfile, "Prepare visual assets", stageStopwatch);
                        WriteLiveStartupProfile(liveStartupProfilePath, "END Prepare visual assets: " + stageStopwatch.ElapsedMilliseconds.ToString() + " ms");
                        SetProgress(progressBar, 88);
                    }
                    finally
                    {
                        if (cacheProgressWindow != null)
                        {
                            cacheProgressWindow.AllowCloseAndClose();
                            cacheProgressWindow.Dispose();
                        }
                    }

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
                    UpdateLoadProgressWindow(
                        loadProgressWindow,
                        "Preparing list selector",
                        "Loading list names. Icons will continue warming up after the editor opens.",
                        90);
                    stageStopwatch.Restart();
                    WriteLiveStartupProfile(liveStartupProfilePath, "START Populate list selector");
                    CacheSave database = viewModel != null && viewModel.Session != null
                        ? viewModel.Session.Database
                        : null;
                    listComboPopulationService.PopulateLists(
                        listComboBox,
                        result.ListCollection,
                        database,
                        listDisplayService,
                        listRowBuilderService,
                        false);
                    AppendStartupProfile(startupProfile, "Populate list selector", stageStopwatch);
                    WriteLiveStartupProfile(liveStartupProfilePath, "END Populate list selector: " + stageStopwatch.ElapsedMilliseconds.ToString() + " ms");
                }

                SetProgress(progressBar, 97);

                if (windowTitleService != null && result.ListCollection != null)
                {
                    windowTitleService.Apply(owner, result.ListCollection, result.ElementsPath);
                }

                if (navigationSelectionService != null)
                {
                    UpdateLoadProgressWindow(
                        loadProgressWindow,
                        "Restoring selection",
                        "Opening the last list and item without blocking the remaining background warmup.",
                        96);
                    stageStopwatch.Restart();
                    WriteLiveStartupProfile(liveStartupProfilePath, "START Restore selection");
                    navigationSelectionService.RestoreSelection(
                        result.NavigationSnapshot,
                        listComboBox,
                        elementGrid,
                        viewModel,
                        persistNavigationState);
                    AppendStartupProfile(startupProfile, "Restore selection", stageStopwatch);
                    WriteLiveStartupProfile(liveStartupProfilePath, "END Restore selection: " + stageStopwatch.ElapsedMilliseconds.ToString() + " ms");
                }

                SetProgress(progressBar, 99);

                UpdateLoadProgressWindow(
                    loadProgressWindow,
                    string.IsNullOrWhiteSpace(result.WarningMessage) ? "Finishing" : "Compatibility mode",
                    string.IsNullOrWhiteSpace(result.WarningMessage)
                        ? "Finalizing the editor view."
                        : result.WarningMessage,
                    99);
                loadDescriptions?.Invoke();
                
                StartDeferredAssetWarmup(
                    assetManager,
                    beginIconWarmup,
                    applyTheme,
                    loadDescriptions,
                    listDisplayService,
                    owner,
                    itemGrid,
                    elementGrid);

                if (navigationStateService != null)
                {
                    navigationStateService.SaveGameFolder(gameFolderPath);
                }

                SetProgress(progressBar, 100);
                totalStopwatch.Stop();
                startupProfile.AppendLine("Total: " + totalStopwatch.Elapsed.TotalMilliseconds.ToString("0") + " ms");
                WriteStartupProfile(startupProfile);
                WriteLiveStartupProfile(liveStartupProfilePath, "END Total: " + totalStopwatch.ElapsedMilliseconds.ToString() + " ms");

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
                try
                {
                    if (loadProgressWindow != null && !loadProgressWindow.IsDisposed)
                    {
                        loadProgressWindow.AllowCloseAndClose();
                        loadProgressWindow.Dispose();
                    }
                }
                catch
                {
                }
                if (setCursor != null)
                {
                    setCursor(Cursors.Default);
                }
            }
        }

        private static void AppendStartupProfile(StringBuilder profile, string stage, Stopwatch stopwatch)
        {
            if (profile == null || stopwatch == null)
            {
                return;
            }

            stopwatch.Stop();
            profile.AppendLine(stage + ": " + stopwatch.Elapsed.TotalMilliseconds.ToString("0") + " ms");
        }

        private static void WriteStartupProfile(StringBuilder profile)
        {
            if (profile == null)
            {
                return;
            }

            try
            {
                string logsDir = Path.Combine(Application.StartupPath, "logs");
                Directory.CreateDirectory(logsDir);
                string logPath = Path.Combine(logsDir, "startup-profile.log");
                using (StreamWriter writer = new StreamWriter(logPath, true, Encoding.UTF8))
                {
                    writer.WriteLine(profile.ToString());
                }
            }
            catch
            {
            }
        }

        private static string InitializeLiveStartupProfile(string gameFolderPath)
        {
            try
            {
                string logsDir = Path.Combine(Application.StartupPath, "logs");
                Directory.CreateDirectory(logsDir);
                string logPath = Path.Combine(logsDir, "startup-profile-live.log");
                using (StreamWriter writer = new StreamWriter(logPath, false, Encoding.UTF8))
                {
                    writer.WriteLine("Client: " + gameFolderPath);
                    writer.WriteLine("Started: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                }

                return logPath;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void WriteLiveStartupProfile(string logPath, string message)
        {
            if (string.IsNullOrWhiteSpace(logPath) || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            try
            {
                using (StreamWriter writer = new StreamWriter(logPath, true, Encoding.UTF8))
                {
                    writer.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + message);
                }
            }
            catch
            {
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
                progressBar.Update();
                if (progressBar.Parent != null)
                {
                    progressBar.Parent.Update();
                }
            }
            catch
            {
            }
        }

        private static void UpdateLoadProgressWindow(ClientCacheBuildProgressWindow window, string stage, string detail, int percent)
        {
            if (window == null || window.IsDisposed)
            {
                return;
            }

            try
            {
                window.UpdateProgress(stage, detail, percent, true);
                window.Update();
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
            ListDisplayService listDisplayService,
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
                    bool visualsLoaded = false;
                    bool metadataLoaded = false;

                    try
                    {
                        metadataLoaded = assetManager.EnsureDeferredMetadataLoaded();
                    }
                    catch
                    {
                        metadataLoaded = false;
                    }

                    try
                    {
                        assetManager.PrewarmCommonPckIndexes();
                    }
                    catch
                    {
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
                            if (visualsLoaded || metadataLoaded)
                            {
                                loadDescriptions?.Invoke();
                            }
                            beginIconWarmup?.Invoke();
                            TryInvokeOwnerMethod(owner, "ScheduleVisibleReferenceCountRefresh");
                            TryInvokeOwnerMethod(owner, "StartListIconHydration");

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
