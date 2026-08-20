using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private void click_deleteItem(object sender, EventArgs ea)
		{
            int listIndex = comboBox_lists.SelectedIndex;
            int[] deletedDescriptionIds = GetSelectedDescriptionItemIds();
            mainWindowActionsCoordinatorService.HandleDeleteSelected(
                mainWindowElementActionsUiService,
                elementDeleteCommandService,
                sessionService,
                listIndex,
                dataGridView_elems,
                gridSelectionService,
                elementListMutationService,
                elementDeletionUiService,
                comboBox_lists,
                viewModel,
                () => change_item(null, null));
            RemoveDescriptionsForDeletedItems(deletedDescriptionIds);
		}


        private void click_cloneItem(object sender, EventArgs ea)
		{
            int listIndex = comboBox_lists.SelectedIndex;
            int[] sourceDescriptionIds = GetSelectedDescriptionItemIds();
            mainWindowActionsCoordinatorService.HandleCloneSelected(
                mainWindowElementActionsUiService,
                elementCloneCommandService,
                sessionService,
                listIndex,
                dataGridView_elems,
                gridSelectionService,
                elementListMutationService,
                elementCloneUiService,
                comboBox_lists,
                viewModel,
                rowIndex => mainWindowDirtyTrackingService.MarkRowDirty(
                    dirtyStateTracker,
                    listDisplayService,
                    ref viewModel.HasUnsavedChanges,
                    listIndex,
                    rowIndex),
                () => change_list(null, null),
                () => change_item(null, null),
                index => listDisplayService.GetFriendlyListName(sessionService.ListCollection.Lists[index].listName));
            CopyDescriptionsForClonedItems(sourceDescriptionIds, GetSelectedDescriptionItemIds());

            InvalidateItemReferenceOptionCaches();
            if (!referenceIndexReady)
            {
                ScheduleVisibleReferenceCountRefresh();
                return;
            }

            int[] selectedGridRows = gridSelectionService != null
                ? gridSelectionService.GetSelectedIndices(dataGridView_elems)
                : new int[0];

            if (selectedGridRows.Length == 0)
            {
                ScheduleVisibleReferenceCountRefresh();
                return;
            }

            for (int i = 0; i < selectedGridRows.Length; i++)
            {
                int elementIndex = elementIndexResolverService.ResolveElementIndexFromGridRow(
                    sessionService.ListCollection,
                    listIndex,
                    selectedGridRows[i],
                    dataGridView_elems);

                if (elementIndex < 0)
                {
                    continue;
                }

                UpdateReferenceIndexForEditedElement(listIndex, elementIndex);
            }
		}


        private void click_exportItem(object sender, EventArgs ea)
		{
            int listIndex = comboBox_lists.SelectedIndex;
            mainWindowActionsCoordinatorService.HandleExportSelected(
                mainWindowElementActionsUiService,
                elementExportUiService,
                elementExportCommandService,
                sessionService,
                listIndex,
                dataGridView_elems,
                gridSelectionService,
                elementImportExportUiService,
                elementImportExportWorkflowService,
                cpb2);
		}


        private void click_importItem(object sender, EventArgs ea)
		{
            int listIndex = comboBox_lists.SelectedIndex;
            mainWindowActionsCoordinatorService.HandleImportSingle(
                mainWindowElementActionsUiService,
                elementImportUiService,
                elementImportCommandService,
                sessionService,
                listIndex,
                dataGridView_elems,
                elementImportExportUiService,
                elementImportExportWorkflowService,
                rowIndex => mainWindowDirtyTrackingService.MarkRowDirty(
                    dirtyStateTracker,
                    listDisplayService,
                    ref viewModel.HasUnsavedChanges,
                    listIndex,
                    rowIndex),
                () => change_list(null, null),
                rowIndex =>
                {
                    if (rowIndex >= 0 && rowIndex < dataGridView_elems.Rows.Count)
                    {
                        dataGridView_elems.Rows[rowIndex].Selected = true;
                    }
                },
                viewModel);
		}


        private void click_addItems(object sender, EventArgs ea)
		{
            int listIndex = comboBox_lists.SelectedIndex;
            mainWindowActionsCoordinatorService.HandleAddMultiple(
                mainWindowElementActionsUiService,
                elementImportUiService,
                elementBatchAddCommandService,
                sessionService,
                listIndex,
                elementImportExportUiService,
                elementImportExportWorkflowService,
                cpb2,
                comboBox_lists,
                dataGridView_elems,
                viewModel,
                rowIndex => mainWindowDirtyTrackingService.MarkRowDirty(
                    dirtyStateTracker,
                    listDisplayService,
                    ref viewModel.HasUnsavedChanges,
                    listIndex,
                    rowIndex),
                () => change_list(null, null),
                () => change_item(null, null),
                index => "[" + index + "]: " + sessionService.ListCollection.Lists[index].listName + " (" + sessionService.ListCollection.Lists[index].elementValues.Length + ")");
		}


        private void click_moveItemsToTop(object sender, EventArgs ea)
        {
            int listIndex = comboBox_lists.SelectedIndex;
            mainWindowActionsCoordinatorService.HandleMoveToTop(
                mainWindowElementActionsUiService,
                elementMoveCommandService,
                sessionService,
                listIndex,
                dataGridView_elems,
                gridSelectionService,
                elementListMutationService,
                elementMoveUiService,
                viewModel,
                () => change_list(null, null));
        }


        private void click_moveItemsToEnd(object sender, EventArgs ea)
        {
            int listIndex = comboBox_lists.SelectedIndex;
            mainWindowActionsCoordinatorService.HandleMoveToEnd(
                mainWindowElementActionsUiService,
                elementMoveCommandService,
                sessionService,
                listIndex,
                dataGridView_elems,
                gridSelectionService,
                elementListMutationService,
                elementMoveUiService,
                viewModel,
                () => change_list(null, null));
        }


        private void click_SetValue(object sender, EventArgs e)
		{
            ApplyRawValueEditorToCurrentCell();
		}


        private async void click_exportItemPackage(object sender, EventArgs ea)
        {
            if (sessionService == null
                || sessionService.ListCollection == null
                || sessionService.Database == null
                || sessionService.AssetManager == null
                || dataGridView_elems == null)
            {
                MessageBox.Show("Load elements.data before exporting an item package.");
                return;
            }

            int listIndex = comboBox_lists.SelectedIndex;
            if (!elementImportExportUiService.ValidateNotConversationList(sessionService.ListCollection, listIndex))
            {
                return;
            }
            if (!ItemTransferPackageService.IsEquipmentEssenceList(sessionService.ListCollection.Lists[listIndex]))
            {
                MessageBox.Show("Item package export currently supports only Equipment Essence.");
                return;
            }

            int[] selectedRows = gridSelectionService.GetSelectedIndices(dataGridView_elems);
            if ((selectedRows == null || selectedRows.Length == 0) && dataGridView_elems.CurrentCell != null)
            {
                selectedRows = new[] { dataGridView_elems.CurrentCell.RowIndex };
            }

            int[] elementIndices = (selectedRows ?? new int[0])
                .Select(rowIndex => elementIndexResolverService.ResolveElementIndexFromGridRow(
                    sessionService.ListCollection,
                    listIndex,
                    rowIndex,
                    dataGridView_elems))
                .Where(elementIndex => elementIndex >= 0)
                .Distinct()
                .ToArray();
            if (elementIndices.Length == 0)
            {
                MessageBox.Show("Select one or more items to export.");
                return;
            }

            if (elementIndices.Length == 1)
            {
                int elementIndex = elementIndices[0];
                string id = sessionService.ListCollection.GetValue(listIndex, elementIndex, 0);
                string name = ResolveItemNameForTransferPackage(listIndex, elementIndex);
                string safeName = BuildSafeItemTransferFileName(id + " - " + name);

                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "FWEledit item package (*.fweitem)|*.fweitem|All files (*.*)|*.*";
                    dialog.FileName = safeName + ".fweitem";
                    string lastExportFolder = GetLastItemPackageExportFolder();
                    if (!string.IsNullOrWhiteSpace(lastExportFolder))
                    {
                        dialog.InitialDirectory = lastExportFolder;
                    }

                    if (dialog.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    SaveLastItemPackageExportFolder(Path.GetDirectoryName(dialog.FileName));

                    using (ItemTransferProgressWindow progressWindow = ShowItemTransferProgressWindow(
                        "Export Equipment Package",
                        "Preparing export",
                        safeName))
                    {

                        ItemTransferExportResult result;
                        try
                        {
                            result = await Task.Run(() => ExportEquipmentPackageWithCli(
                                listIndex,
                                elementIndex,
                                dialog.FileName,
                                progressWindow.UpdateProgress,
                                progressWindow.Cancellation.Token));
                        }
                        finally
                        {
                            progressWindow.AllowCloseAndClose();
                        }

                        if (!result.Success)
                        {
                            MessageBox.Show(result.ErrorMessage ?? "Failed to export item package.");
                            return;
                        }

                        MessageBox.Show(BuildItemTransferExportMessage(result, "Item package exported."));
                    }
                }
                return;
            }

            string selectedOutputDirectory = gameFolderDialogService.PromptForGameFolder(
                "Choose a folder for the exported equipment packages.",
                GetLastItemPackageExportFolder(),
                this);
            if (string.IsNullOrWhiteSpace(selectedOutputDirectory))
            {
                return;
            }

            SaveLastItemPackageExportFolder(selectedOutputDirectory);

            using (ItemTransferProgressWindow progressWindow = ShowItemTransferProgressWindow(
                "Export Equipment Packages",
                "Preparing batch export",
                elementIndices.Length.ToString() + " equipment packages"))
            {

                ItemTransferExportResult result;
                try
                {
                    result = await Task.Run(() => ExportEquipmentPackagesWithCli(
                        listIndex,
                        elementIndices,
                        selectedOutputDirectory,
                        progressWindow.UpdateProgress,
                        progressWindow.Cancellation.Token));
                }
                finally
                {
                    progressWindow.AllowCloseAndClose();
                }

                if (!result.Success)
                {
                    MessageBox.Show(result.ErrorMessage ?? "Failed to export item packages.");
                    return;
                }

                MessageBox.Show(BuildItemTransferExportMessage(result, "Item packages exported: " + elementIndices.Length.ToString()));
            }
        }

        private ItemTransferProgressWindow ShowItemTransferProgressWindow(string title, string stage, string detail)
        {
            ItemTransferProgressWindow progressWindow = new ItemTransferProgressWindow(title);
            progressWindow.StartPosition = FormStartPosition.CenterParent;
            progressWindow.UpdateProgress(new ItemTransferProgressInfo
            {
                Stage = stage ?? string.Empty,
                Detail = detail ?? string.Empty,
                Current = 0,
                Total = 0,
                IsIndeterminate = true
            });
            progressWindow.Show(this);
            progressWindow.Activate();
            progressWindow.Refresh();
            Application.DoEvents();
            return progressWindow;
        }

        private string GetLastItemPackageExportFolder()
        {
            string path = Properties.Settings.Default.LastItemPackageExportFolder ?? string.Empty;
            return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path) ? path : string.Empty;
        }

        private void SaveLastItemPackageExportFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return;
            }

            Properties.Settings.Default.LastItemPackageExportFolder = folderPath;
            Properties.Settings.Default.Save();
        }

        private ItemTransferExportResult ExportEquipmentPackagesWithCli(
            int listIndex,
            int[] elementIndices,
            string outputDirectory,
            Action<ItemTransferProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            ItemTransferExportResult combined = new ItemTransferExportResult
            {
                Success = true,
                MissingAssets = new System.Collections.Generic.List<string>()
            };

            if (elementIndices == null || elementIndices.Length == 0)
            {
                combined.Success = false;
                combined.ErrorMessage = "No items selected.";
                return combined;
            }

            Directory.CreateDirectory(outputDirectory);
            for (int i = 0; i < elementIndices.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int elementIndex = elementIndices[i];
                string id = sessionService.ListCollection.GetValue(listIndex, elementIndex, 0);
                string name = ResolveItemNameForTransferPackage(listIndex, elementIndex);
                string safeName = BuildSafeItemTransferFileName(id + " - " + name);
                string outputFile = GetUniqueItemPackagePath(outputDirectory, safeName + ".fweitem");

                progress?.Invoke(new ItemTransferProgressInfo
                {
                    Stage = "Exporting package " + (i + 1).ToString() + "/" + elementIndices.Length.ToString(),
                    Detail = safeName,
                    Current = i + 1,
                    Total = elementIndices.Length,
                    IsIndeterminate = false
                });

                ItemTransferExportResult result = ExportEquipmentPackageWithCli(
                    listIndex,
                    elementIndex,
                    outputFile,
                    progress,
                    cancellationToken);
                if (!result.Success)
                {
                    combined.Success = false;
                    combined.ErrorMessage = result.ErrorMessage;
                    return combined;
                }

                combined.AssetCount += result.AssetCount;
                combined.MissingAssetCount += result.MissingAssetCount;
                if (result.MissingAssets != null)
                {
                    combined.MissingAssets.AddRange(result.MissingAssets);
                }
            }

            return combined;
        }

        private static string GetUniqueItemPackagePath(string directory, string fileName)
        {
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            string candidate = Path.Combine(directory, fileName);
            int suffix = 2;
            while (File.Exists(candidate))
            {
                candidate = Path.Combine(directory, baseName + " (" + suffix.ToString() + ")" + extension);
                suffix++;
            }

            return candidate;
        }

        private static string BuildItemTransferExportMessage(ItemTransferExportResult result, string header)
        {
            string message = header + "\nAssets: " + result.AssetCount.ToString();
            if (result.MissingAssetCount > 0)
            {
                message += "\nMissing assets: " + result.MissingAssetCount.ToString();
                if (result.MissingAssets != null && result.MissingAssets.Count > 0)
                {
                    int maxToShow = Math.Min(5, result.MissingAssets.Count);
                    for (int i = 0; i < maxToShow; i++)
                    {
                        message += "\n- " + result.MissingAssets[i];
                    }
                    if (result.MissingAssets.Count > maxToShow)
                    {
                        message += "\n- ...";
                    }
                }
            }

            return message;
        }

        private ItemTransferExportResult ExportEquipmentPackageWithCli(
            int listIndex,
            int elementIndex,
            string outputFile,
            Action<ItemTransferProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            ItemTransferExportResult result = new ItemTransferExportResult();
            string toolPath = FindEquipmentPackageToolPath();
            if (string.IsNullOrWhiteSpace(toolPath) || !File.Exists(toolPath))
            {
                result.ErrorMessage = "Equipment package tool was not found.";
                return result;
            }

            if (string.IsNullOrWhiteSpace(AssetManager.GameRootPath) || !Directory.Exists(AssetManager.GameRootPath))
            {
                result.ErrorMessage = "Game root path is not configured.";
                return result;
            }

            ItemTransferPackageManifest manifest = itemTransferPackageService.BuildEquipmentExportManifest(
                sessionService.ListCollection,
                sessionService.Database,
                listIndex,
                elementIndex);
            if (manifest == null)
            {
                result.ErrorMessage = "Failed to build equipment export manifest.";
                return result;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "FWEledit", "equipment-package");
            Directory.CreateDirectory(tempDir);
            string token = Guid.NewGuid().ToString("N");
            string requestFile = Path.Combine(tempDir, token + ".request.json");
            string resultFile = Path.Combine(tempDir, token + ".result.json");

            try
            {
                var request = new
                {
                    GameRootPath = AssetManager.GameRootPath,
                    WorkspaceRootPath = AssetManager.WorkspaceRootPath,
                    OutputFile = outputFile,
                    ResultFile = resultFile,
                    Manifest = manifest
                };

                File.WriteAllText(requestFile, JsonConvert.SerializeObject(request, Formatting.Indented), Encoding.UTF8);
                progress?.Invoke(new ItemTransferProgressInfo
                {
                    Stage = "Starting equipment package tool",
                    Detail = Path.GetFileName(toolPath),
                    Current = 0,
                    Total = 0,
                    IsIndeterminate = true
                });

                StringBuilder stderr = new StringBuilder();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = toolPath,
                    Arguments = "export-equipment --request " + QuoteArgument(requestFile),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    WorkingDirectory = Path.GetDirectoryName(toolPath)
                };

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            HandleEquipmentPackageToolOutput(e.Data, progress);
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            lock (stderr)
                            {
                                stderr.AppendLine(e.Data);
                            }
                        }
                    };

                    if (!process.Start())
                    {
                        result.ErrorMessage = "Failed to start equipment package tool.";
                        return result;
                    }

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    while (!process.WaitForExit(100))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            TryKillProcess(process);
                            result.ErrorMessage = "Export cancelled.";
                            return result;
                        }
                    }

                    process.WaitForExit();
                    if (File.Exists(resultFile))
                    {
                        result = JsonConvert.DeserializeObject<ItemTransferExportResult>(File.ReadAllText(resultFile, Encoding.UTF8))
                            ?? new ItemTransferExportResult();
                    }

                    if (process.ExitCode != 0)
                    {
                        if (string.IsNullOrWhiteSpace(result.ErrorMessage))
                        {
                            string toolError;
                            lock (stderr)
                            {
                                toolError = stderr.ToString().Trim();
                            }
                            result.ErrorMessage = string.IsNullOrWhiteSpace(toolError)
                                ? "Equipment package tool failed."
                                : toolError;
                        }
                        result.Success = false;
                    }

                    return result;
                }
            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "Export cancelled.";
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                return result;
            }
            finally
            {
                TryDeleteTempFile(requestFile);
                TryDeleteTempFile(resultFile);
            }
        }

        private static void HandleEquipmentPackageToolOutput(string line, Action<ItemTransferProgressInfo> progress)
        {
            if (progress == null || string.IsNullOrWhiteSpace(line) || !line.StartsWith("PROGRESS|", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string[] parts = line.Split(new char[] { '|' }, 6);
            if (parts.Length < 6)
            {
                return;
            }

            int current;
            int total;
            int.TryParse(parts[3], out current);
            int.TryParse(parts[4], out total);
            progress(new ItemTransferProgressInfo
            {
                Stage = parts[1] ?? string.Empty,
                Detail = parts[2] ?? string.Empty,
                Current = current,
                Total = total,
                IsIndeterminate = string.Equals(parts[5], "1", StringComparison.Ordinal)
            });
        }

        private static string FindEquipmentPackageToolPath()
        {
            string fileName = "FWEquipmentPackageTool.exe";
            string[] directCandidates =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "equipment-package", fileName),
                Path.Combine(Environment.CurrentDirectory, "tools", "equipment-package", fileName),
                Path.Combine(AssetManager.WorkspaceRootPath ?? string.Empty, "tools", "equipment-package", fileName)
            };

            for (int i = 0; i < directCandidates.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(directCandidates[i]) && File.Exists(directCandidates[i]))
                {
                    return directCandidates[i];
                }
            }

            DirectoryInfo directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 8 && directory != null; i++, directory = directory.Parent)
            {
                string candidate = Path.Combine(directory.FullName, "tools", "equipment-package", fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return string.Empty;
        }

        private static string QuoteArgument(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }

        private static void TryKillProcess(Process process)
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
            }
        }

        private static void TryDeleteTempFile(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }


        private async void click_importItemPackage(object sender, EventArgs ea)
        {
            if (sessionService == null
                || sessionService.ListCollection == null
                || sessionService.Database == null
                || sessionService.AssetManager == null)
            {
                MessageBox.Show("Load elements.data before importing an item package.");
                return;
            }

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "FWEledit item package (*.fweitem)|*.fweitem|All files (*.*)|*.*";
                dialog.Multiselect = true;
                if (dialog.ShowDialog(this) != DialogResult.OK || !File.Exists(dialog.FileName))
                {
                    return;
                }

                ItemTransferImportMode importMode = AskItemTransferImportMode();
                if (importMode != ItemTransferImportMode.FullStructure
                    && importMode != ItemTransferImportMode.ModelsOnly)
                {
                    return;
                }

                using (ItemTransferProgressWindow progressWindow = new ItemTransferProgressWindow("Import Equipment Package"))
                {
                    progressWindow.StartPosition = FormStartPosition.CenterParent;
                    progressWindow.Show(this);

                    ItemTransferImportResult result;
                    try
                    {
                        result = await Task.Run(() => itemTransferPackageService.ImportItemPackages(
                            sessionService.ListCollection,
                            sessionService.Database,
                            sessionService.AssetManager,
                            idGenerationService,
                            dialog.FileNames,
                            importMode,
                            progressWindow.UpdateProgress,
                            progressWindow.Cancellation.Token));
                    }
                    finally
                    {
                        progressWindow.AllowCloseAndClose();
                    }

                    if (!result.Success)
                    {
                        MessageBox.Show(result.ErrorMessage ?? "Failed to import item package.");
                        return;
                    }

                    if (result.Mode == ItemTransferImportMode.ModelsOnly)
                    {
                        viewModel.HasUnsavedChanges = true;
                        InvalidateItemReferenceOptionCaches();
                        ScheduleVisibleReferenceCountRefresh();
                        ShowModelsOnlyImportResult(result);
                        return;
                    }

                    if (result.ImportedItems != null && result.ImportedItems.Count > 0)
                    {
                        foreach (ItemTransferImportedItem importedItem in result.ImportedItems)
                        {
                            mainWindowDirtyTrackingService.MarkRowDirty(
                                dirtyStateTracker,
                                listDisplayService,
                                ref viewModel.HasUnsavedChanges,
                                importedItem.ListIndex,
                                importedItem.ItemIndex);
                        }
                    }
                    else
                    {
                        mainWindowDirtyTrackingService.MarkRowDirty(
                            dirtyStateTracker,
                            listDisplayService,
                            ref viewModel.HasUnsavedChanges,
                            result.TargetListIndex,
                            result.NewItemIndex);
                    }
                    viewModel.HasUnsavedChanges = true;

                    if (result.TargetListIndex >= 0 && result.TargetListIndex < comboBox_lists.Items.Count)
                    {
                        comboBox_lists.SelectedIndex = result.TargetListIndex;
                        ListComboPopulationService.SetItemTextPreservingIcon(
                            comboBox_lists,
                            result.TargetListIndex,
                            BuildItemTransferListLabel(result.TargetListIndex));
                    }

                    change_list(null, null);
                    if (result.NewItemIndex >= 0 && result.NewItemIndex < dataGridView_elems.Rows.Count)
                    {
                        dataGridView_elems.ClearSelection();
                        dataGridView_elems.Rows[result.NewItemIndex].Selected = true;
                        dataGridView_elems.CurrentCell = dataGridView_elems.Rows[result.NewItemIndex].Cells[0];
                        dataGridView_elems.FirstDisplayedScrollingRowIndex = result.NewItemIndex;
                    }
                    change_item(null, null);

                    InvalidateItemReferenceOptionCaches();
                    ScheduleVisibleReferenceCountRefresh();

                    string message = result.ImportedItemCount > 1
                        ? "Item packages imported: " + result.ImportedItemCount.ToString()
                        : "Item package imported.\nNew ID: " + result.NewId.ToString();
                    message += "\nAssets imported: " + result.ImportedAssetCount.ToString();
                    message += "\nAssets already present: " + result.ExistingAssetCount.ToString();
                    if (result.UpdatedPackageCount > 0)
                    {
                        message += "\nPCK packages updated: " + result.UpdatedPackageCount.ToString();
                    }
                    if (result.FallbackPackageCount > 0)
                    {
                        message += "\nMissing source packages redirected to models.pck: " + result.FallbackPackageCount.ToString();
                    }
                    if (result.RemappedPathIdCount > 0)
                    {
                        message += "\nPathIDs remapped: " + result.RemappedPathIdCount.ToString();
                    }
                    if (result.MissingAssetCount > 0)
                    {
                        message += "\nWarnings: " + result.MissingAssetCount.ToString();
                    }
                    MessageBox.Show(message);
                }
            }
        }

        private ItemTransferImportMode AskItemTransferImportMode()
        {
            using (ItemTransferImportModeWindow window = new ItemTransferImportModeWindow())
            {
                return window.ShowDialog(this) == DialogResult.OK
                    ? window.SelectedMode
                    : (ItemTransferImportMode)(-1);
            }
        }

        private void ShowModelsOnlyImportResult(ItemTransferImportResult result)
        {
            string importDetails = BuildModelsOnlyImportDetailsText(result);
            string message = "Assets imported: " + result.ImportedAssetCount.ToString();
            message += "\nAssets already present: " + result.ExistingAssetCount.ToString();
            if (result.UpdatedPackageCount > 0)
            {
                message += "\nPCK packages updated: " + result.UpdatedPackageCount.ToString();
            }
            if (result.FallbackPackageCount > 0)
            {
                message += "\nMissing source packages redirected to models.pck: " + result.FallbackPackageCount.ToString();
            }
            if (result.RemappedPathIdCount > 0)
            {
                message += "\nPathIDs remapped: " + result.RemappedPathIdCount.ToString();
            }
            if (result.MissingAssetCount > 0)
            {
                message += "\nWarnings: " + result.MissingAssetCount.ToString();
            }
            if (!string.IsNullOrWhiteSpace(importDetails))
            {
                using (ItemTransferModelsOnlyResultWindow window = new ItemTransferModelsOnlyResultWindow(message, importDetails))
                {
                    window.ShowDialog(this);
                }
            }
            else
            {
                MessageBox.Show(this, message + "\n\nNo model path IDs were found in this package.", "Import Models Only", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static string BuildModelsOnlyImportDetailsText(ItemTransferImportResult result)
        {
            if (result == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            string packageSummary = BuildPackageAssetSummaryText(result);
            if (!string.IsNullOrWhiteSpace(packageSummary))
            {
                builder.AppendLine("Package assets");
                builder.AppendLine(packageSummary);
            }

            string modelSummary = BuildImportedModelPathSummaryText(result);
            if (!string.IsNullOrWhiteSpace(modelSummary))
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }
                builder.AppendLine("Model path IDs");
                builder.AppendLine(modelSummary);
            }

            string dependencySummary = BuildDependencyAssetSummaryText(result);
            if (!string.IsNullOrWhiteSpace(dependencySummary))
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }
                builder.AppendLine("Dependency assets");
                builder.AppendLine(dependencySummary);
            }

            return builder.ToString().TrimEnd();
        }

        private static string BuildPackageAssetSummaryText(ItemTransferImportResult result)
        {
            if (result == null || result.AssetSummaries == null || result.AssetSummaries.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            foreach (ItemTransferPackageAssetSummary summary in result.AssetSummaries.OrderBy(s => s.Package))
            {
                if (summary == null || string.IsNullOrWhiteSpace(summary.Package))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(summary.Package);
                builder.Append(": imported ");
                builder.Append(summary.ImportedCount.ToString());
                builder.Append(", already present ");
                builder.Append(summary.ExistingCount.ToString());
            }

            return builder.ToString();
        }

        private static string BuildDependencyAssetSummaryText(ItemTransferImportResult result)
        {
            if (result == null || result.DependencyAssetPaths == null || result.DependencyAssetPaths.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            foreach (string path in result.DependencyAssetPaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }
                builder.Append(path);
            }

            return builder.ToString();
        }

        private static string BuildImportedModelPathSummaryText(ItemTransferImportResult result)
        {
            if (result == null || result.ImportedModelPaths == null || result.ImportedModelPaths.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < result.ImportedModelPaths.Count; i++)
            {
                ItemTransferImportedPath item = result.ImportedModelPaths[i];
                if (item == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(item.FieldName);
                builder.Append(": ");
                builder.Append(item.TargetPathId.ToString());
                if (item.OriginalPathId > 0 && item.OriginalPathId != item.TargetPathId)
                {
                    builder.Append(" (from ");
                    builder.Append(item.OriginalPathId.ToString());
                    builder.Append(")");
                }
                if (!string.IsNullOrWhiteSpace(item.MappedPath))
                {
                    builder.Append(" - ");
                    builder.Append(item.MappedPath);
                }
            }

            return builder.ToString();
        }

        private string ResolveItemNameForTransferPackage(int listIndex, int elementIndex)
        {
            try
            {
                if (sessionService == null
                    || sessionService.ListCollection == null
                    || listIndex < 0
                    || listIndex >= sessionService.ListCollection.Lists.Length)
                {
                    return "Item";
                }

                eList list = sessionService.ListCollection.Lists[listIndex];
                int nameIndex = -1;
                for (int i = 0; i < list.elementFields.Length; i++)
                {
                    if (string.Equals(list.elementFields[i], "name", StringComparison.OrdinalIgnoreCase))
                    {
                        nameIndex = i;
                        break;
                    }
                }

                string name = nameIndex >= 0
                    ? listDisplayService.GetDisplayEntryName(sessionService, sessionService.ListCollection, listIndex, elementIndex, nameIndex)
                    : string.Empty;
                return string.IsNullOrWhiteSpace(name) ? "Item" : name.Trim();
            }
            catch
            {
                return "Item";
            }
        }

        private string BuildItemTransferListLabel(int listIndex)
        {
            if (sessionService == null
                || sessionService.ListCollection == null
                || listIndex < 0
                || listIndex >= sessionService.ListCollection.Lists.Length)
            {
                return string.Empty;
            }

            eList list = sessionService.ListCollection.Lists[listIndex];
            return "[" + listIndex.ToString() + "] "
                + listDisplayService.GetFriendlyListName(list.listName)
                + " ("
                + list.elementValues.Length.ToString()
                + ")";
        }

        private static string BuildSafeItemTransferFileName(string value)
        {
            string safe = string.IsNullOrWhiteSpace(value) ? "item" : value.Trim();
            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++)
            {
                safe = safe.Replace(invalid[i], '_');
            }

            while (safe.Contains("  "))
            {
                safe = safe.Replace("  ", " ");
            }

            if (safe.Length > 120)
            {
                safe = safe.Substring(0, 120).Trim();
            }

            return string.IsNullOrWhiteSpace(safe) ? "item" : safe;
        }

    }
}




