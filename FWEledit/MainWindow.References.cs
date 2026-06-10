using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private const string ReferenceCacheSchemaVersion = "references-v3";

        private sealed class VisibleReferenceTarget
        {
            public int RowIndex { get; set; }
            public int ElementIndex { get; set; }
            public int Id { get; set; }
        }

        private sealed class ReferenceGridRowData
        {
            public int SourceListIndex { get; set; }
            public int SourceElementIndex { get; set; }
            public string ListLabel { get; set; }
            public string SourceId { get; set; }
            public Image Icon { get; set; }
            public string Name { get; set; }
            public string FieldLabel { get; set; }
            public string RawValue { get; set; }
            public int Quality { get; set; }
        }

        private sealed class PersistedReferenceIndexCache
        {
            public string Signature { get; set; }
            public Dictionary<string, List<ReferenceUsage>> ReferencesByTarget { get; set; }
        }

        private sealed class PersistedItemReferenceOptionsCache
        {
            public string Signature { get; set; }
            public Dictionary<int, List<ItemReferenceOption>> OptionsByList { get; set; }
        }

        private int GetReferenceCountForElement(int listIndex, int elementIndex)
        {
            int id;
            if (!TryGetElementId(listIndex, elementIndex, out id))
            {
                return 0;
            }

            return referenceIndexService.GetReferenceCount(
                sessionService.ListCollection,
                itemReferenceService,
                listIndex,
                id);
        }

        private void ResetReferenceCaches()
        {
            referenceIndexService.Clear();
            referenceIndexReady = false;
            referenceIndexBuildTask = null;
            itemReferenceService.ClearCache();
            itemReferenceOptionsReady = false;
            itemReferenceOptionsWarmupTask = null;
            lock (referenceRowsCacheSync)
            {
                referenceRowsCache.Clear();
            }
        }

        private void InvalidateItemReferenceOptionCaches()
        {
            itemReferenceService.ClearCache();
            itemReferenceOptionsReady = false;
            itemReferenceOptionsWarmupTask = null;
            lock (referenceRowsCacheSync)
            {
                referenceRowsCache.Clear();
            }
        }

        private void ConfigureReferenceCachePersistence(string elementsPath)
        {
            persistedReferenceCachePath = string.Empty;
            persistedReferenceCacheSignature = string.Empty;

            if (string.IsNullOrWhiteSpace(elementsPath) || !File.Exists(elementsPath))
            {
                return;
            }

            FileInfo fileInfo = new FileInfo(elementsPath);
            string normalizedPath = fileInfo.FullName.Trim().ToLowerInvariant();
            string signature = normalizedPath
                + "|"
                + fileInfo.Length.ToString()
                + "|"
                + fileInfo.LastWriteTimeUtc.Ticks.ToString()
                + "|"
                + ReferenceCacheSchemaVersion;

            string hash;
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(normalizedPath);
                byte[] digest = md5.ComputeHash(bytes);
                hash = BitConverter.ToString(digest).Replace("-", string.Empty).ToLowerInvariant();
            }

            string cacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FWEledit",
                "ReferenceCache");

            persistedReferenceCacheSignature = signature;
            persistedReferenceCachePath = Path.Combine(cacheDirectory, hash + ".json.gz");
            persistedItemReferenceOptionsCachePath = Path.Combine(cacheDirectory, hash + ".itemrefs.json.gz");
        }

        private bool TryLoadPersistedReferenceCache()
        {
            if (string.IsNullOrWhiteSpace(persistedReferenceCachePath)
                || string.IsNullOrWhiteSpace(persistedReferenceCacheSignature)
                || sessionService == null
                || sessionService.ListCollection == null
                || itemReferenceService == null
                || (viewModel != null && viewModel.HasUnsavedChanges)
                || !File.Exists(persistedReferenceCachePath))
            {
                return false;
            }

            try
            {
                string json;
                using (FileStream stream = File.OpenRead(persistedReferenceCachePath))
                using (GZipStream gzip = new GZipStream(stream, CompressionMode.Decompress))
                using (StreamReader reader = new StreamReader(gzip))
                {
                    json = reader.ReadToEnd();
                }

                PersistedReferenceIndexCache cache = JsonConvert.DeserializeObject<PersistedReferenceIndexCache>(json);
                if (cache == null
                    || !string.Equals(cache.Signature, persistedReferenceCacheSignature, StringComparison.OrdinalIgnoreCase)
                    || cache.ReferencesByTarget == null)
                {
                    return false;
                }

                referenceIndexService.ImportCache(
                    sessionService.ListCollection,
                    itemReferenceService,
                    cache.ReferencesByTarget);

                referenceIndexReady = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void PersistReferenceCacheToDisk()
        {
            if (string.IsNullOrWhiteSpace(persistedReferenceCachePath)
                || string.IsNullOrWhiteSpace(persistedReferenceCacheSignature)
                || sessionService == null
                || sessionService.ListCollection == null
                || itemReferenceService == null
                || viewModel == null
                || viewModel.HasUnsavedChanges)
            {
                return;
            }

            Dictionary<string, List<ReferenceUsage>> exported = referenceIndexService.ExportCache(
                sessionService.ListCollection,
                itemReferenceService);

            PersistedReferenceIndexCache payload = new PersistedReferenceIndexCache
            {
                Signature = persistedReferenceCacheSignature,
                ReferencesByTarget = exported
            };

            string cachePath = persistedReferenceCachePath;
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    string directory = Path.GetDirectoryName(cachePath);
                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    string json = JsonConvert.SerializeObject(payload);
                    using (FileStream stream = File.Create(cachePath))
                    using (GZipStream gzip = new GZipStream(stream, CompressionMode.Compress))
                    using (StreamWriter writer = new StreamWriter(gzip))
                    {
                        writer.Write(json);
                    }
                }
                catch
                {
                }
            });
        }

        private bool EnsureItemReferenceOptionsCacheAvailable()
        {
            if (itemReferenceOptionsReady)
            {
                return true;
            }

            if (sessionService == null
                || sessionService.ListCollection == null
                || sessionService.Database == null
                || itemReferenceService == null
                || iconResolutionService == null)
            {
                return false;
            }

            if (TryLoadPersistedItemReferenceOptionsCache())
            {
                return true;
            }

            if (itemReferenceOptionsWarmupTask != null && !itemReferenceOptionsWarmupTask.IsCompleted)
            {
                return false;
            }

            eListCollection listCollection = sessionService.ListCollection;
            CacheSave database = sessionService.Database;
            itemReferenceOptionsWarmupTask = System.Threading.Tasks.Task.Run(() =>
            {
                itemReferenceService.BuildSearchableOptions(listCollection, database, iconResolutionService);
            }).ContinueWith(task =>
            {
                if (IsDisposed || task.IsFaulted || task.IsCanceled)
                {
                    return;
                }

                itemReferenceOptionsReady = true;
                PersistItemReferenceOptionsCacheToDisk();
            });

            return false;
        }

        private bool TryLoadPersistedItemReferenceOptionsCache()
        {
            if (string.IsNullOrWhiteSpace(persistedItemReferenceOptionsCachePath)
                || string.IsNullOrWhiteSpace(persistedReferenceCacheSignature)
                || sessionService == null
                || sessionService.ListCollection == null
                || sessionService.Database == null
                || itemReferenceService == null
                || iconResolutionService == null
                || (viewModel != null && viewModel.HasUnsavedChanges)
                || !File.Exists(persistedItemReferenceOptionsCachePath))
            {
                return false;
            }

            try
            {
                string json;
                using (FileStream stream = File.OpenRead(persistedItemReferenceOptionsCachePath))
                using (GZipStream gzip = new GZipStream(stream, CompressionMode.Decompress))
                using (StreamReader reader = new StreamReader(gzip))
                {
                    json = reader.ReadToEnd();
                }

                PersistedItemReferenceOptionsCache cache = JsonConvert.DeserializeObject<PersistedItemReferenceOptionsCache>(json);
                if (cache == null
                    || !string.Equals(cache.Signature, persistedReferenceCacheSignature, StringComparison.OrdinalIgnoreCase)
                    || cache.OptionsByList == null)
                {
                    return false;
                }

                itemReferenceService.ImportOptionsCache(
                    sessionService.ListCollection,
                    sessionService.Database,
                    iconResolutionService,
                    cache.OptionsByList);
                itemReferenceOptionsReady = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void PersistItemReferenceOptionsCacheToDisk()
        {
            if (string.IsNullOrWhiteSpace(persistedItemReferenceOptionsCachePath)
                || string.IsNullOrWhiteSpace(persistedReferenceCacheSignature)
                || sessionService == null
                || sessionService.ListCollection == null
                || sessionService.Database == null
                || itemReferenceService == null
                || iconResolutionService == null
                || viewModel == null
                || viewModel.HasUnsavedChanges)
            {
                return;
            }

            Dictionary<int, List<ItemReferenceOption>> exported = itemReferenceService.ExportOptionsCache(
                sessionService.ListCollection,
                sessionService.Database,
                iconResolutionService);

            PersistedItemReferenceOptionsCache payload = new PersistedItemReferenceOptionsCache
            {
                Signature = persistedReferenceCacheSignature,
                OptionsByList = exported
            };

            string cachePath = persistedItemReferenceOptionsCachePath;
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    string directory = Path.GetDirectoryName(cachePath);
                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    string json = JsonConvert.SerializeObject(payload);
                    using (FileStream stream = File.Create(cachePath))
                    using (GZipStream gzip = new GZipStream(stream, CompressionMode.Compress))
                    using (StreamWriter writer = new StreamWriter(gzip))
                    {
                        writer.Write(json);
                    }
                }
                catch
                {
                }
            });
        }

        private string BuildReferenceCacheKey(int listIndex, int id)
        {
            return listIndex.ToString() + "|" + id.ToString();
        }

        private bool EnsureReferenceIndexAvailable()
        {
            if (referenceIndexReady)
            {
                return true;
            }

            if (sessionService == null || sessionService.ListCollection == null || itemReferenceService == null)
            {
                return false;
            }

            if (TryLoadPersistedReferenceCache())
            {
                RefreshVisibleReferenceCounts();
                if (referencesViewerForm != null && !referencesViewerForm.IsDisposed && referencesViewerForm.Visible)
                {
                    LoadReferencesViewerForSelectionAsync();
                }
                return true;
            }

            if (referenceIndexBuildTask != null && !referenceIndexBuildTask.IsCompleted)
            {
                return false;
            }

            eListCollection listCollection = sessionService.ListCollection;
            ItemReferenceService referenceService = itemReferenceService;
            referenceIndexBuildTask = System.Threading.Tasks.Task.Run(() =>
            {
                referenceIndexService.EnsureBuilt(listCollection, referenceService);
            }).ContinueWith(task =>
            {
                if (IsDisposed || !IsHandleCreated || task.IsFaulted || task.IsCanceled)
                {
                    return;
                }

                BeginInvoke((Action)(() =>
                {
                    if (IsDisposed)
                    {
                        return;
                    }

                    referenceIndexReady = true;
                    RefreshVisibleReferenceCounts();
                    PersistReferenceCacheToDisk();
                    if (referencesViewerForm != null && !referencesViewerForm.IsDisposed && referencesViewerForm.Visible)
                    {
                        LoadReferencesViewerForSelectionAsync();
                    }
                }));
            });

            return false;
        }

        private void StartReferenceCacheWarmup()
        {
            EnsureReferenceIndexAvailable();
            EnsureItemReferenceOptionsCacheAvailable();
        }

        private void ShowReferencesViewerForCurrentSelection()
        {
            if (sessionService == null || sessionService.ListCollection == null)
            {
                return;
            }

            int listIndex = comboBox_lists != null ? comboBox_lists.SelectedIndex : -1;
            int elementIndex = ResolveCurrentElementIndex();
            int id;
            if (!TryGetElementId(listIndex, elementIndex, out id))
            {
                MessageBox.Show("Select a valid element first.");
                return;
            }

            EnsureReferencesViewer();
            UpdateReferencesViewerTitle(listIndex, elementIndex, id);
            referencesViewerForm.Show(this);
            referencesViewerForm.BringToFront();
            LoadReferencesViewerForSelectionAsync();
        }

        private void EnsureReferencesViewer()
        {
            if (referencesViewerForm != null && !referencesViewerForm.IsDisposed && referencesViewerGrid != null && !referencesViewerGrid.IsDisposed)
            {
                return;
            }

            referencesViewerForm = new Form();
            referencesViewerForm.Text = "References";
            referencesViewerForm.StartPosition = FormStartPosition.CenterParent;
            referencesViewerForm.MinimumSize = new Size(920, 420);
            referencesViewerForm.Size = new Size(1080, 620);
            referencesViewerForm.ShowInTaskbar = false;

            referencesViewerForm.FormClosing += (sender, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    referencesViewerForm.Hide();
                }
            };

            referencesViewerLabel = new Label();
            referencesViewerLabel.Dock = DockStyle.Top;
            referencesViewerLabel.Height = 34;
            referencesViewerLabel.Padding = new Padding(10, 0, 10, 0);
            referencesViewerLabel.TextAlign = ContentAlignment.MiddleLeft;
            referencesViewerLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            referencesViewerLabel.Text = "Referenced by";

            referencesViewerTabs = new TabControl();
            referencesViewerTabs.Dock = DockStyle.Fill;
            referencesViewerTabs.Padding = new Point(18, 6);
            referencesViewerTabs.Margin = new Padding(0);

            referencesViewerGrid = CreateReferencesViewerGrid();
            SetReferencesViewerTabsLoadingState();

            referencesViewerForm.Controls.Add(referencesViewerLabel);
            referencesViewerForm.Controls.Add(referencesViewerTabs);
            referencesViewerTabs.BringToFront();

            ApplyReferencesViewerTheme();
        }

        private void SetReferencesViewerTabsLoadingState()
        {
            if (referencesViewerTabs == null || referencesViewerTabs.IsDisposed)
            {
                return;
            }

            referencesViewerTabs.SuspendLayout();
            try
            {
                referencesViewerTabs.TabPages.Clear();
                referencesViewerGridsByKey.Clear();

                TabPage page = new TabPage("Loading");
                DataGridView grid = CreateReferencesViewerGrid();
                grid.Rows.Add(string.Empty, string.Empty, Properties.Resources.blank, "Loading references...", string.Empty, string.Empty);
                page.Controls.Add(grid);
                referencesViewerTabs.TabPages.Add(page);
                referencesViewerGridsByKey["loading"] = grid;
                referencesViewerGrid = grid;
            }
            finally
            {
                referencesViewerTabs.ResumeLayout();
            }
        }

        private DataGridView CreateReferencesViewerGrid()
        {
            DataGridView grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.Margin = new Padding(0);
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.RowTemplate.Height = 38;
            grid.ColumnHeadersHeight = 30;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.EnableHeadersVisualStyles = false;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "refList", HeaderText = "List", Width = 185, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "refId", HeaderText = "ID", Width = 82, ReadOnly = true });
            grid.Columns.Add(new DataGridViewImageColumn
            {
                Name = "refIcon",
                HeaderText = string.Empty,
                Width = 42,
                MinimumWidth = 42,
                ReadOnly = true,
                ImageLayout = DataGridViewImageCellLayout.Zoom
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "refName", HeaderText = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 180, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "refField", HeaderText = "Field", Width = 220, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "refValue", HeaderText = "Value", Width = 70, ReadOnly = true });
            grid.CellDoubleClick += referencesViewerGrid_CellDoubleClick;
            grid.KeyDown += referencesViewerGrid_KeyDown;
            return grid;
        }

        private void referencesViewerGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e == null || e.RowIndex < 0)
            {
                return;
            }

            NavigateToReferenceRow(GetReferencesViewerGridFromSender(sender), e.RowIndex);
        }

        private void referencesViewerGrid_KeyDown(object sender, KeyEventArgs e)
        {
            DataGridView sourceGrid = GetReferencesViewerGridFromSender(sender);
            if (e == null || e.KeyCode != Keys.Enter || sourceGrid == null || sourceGrid.CurrentCell == null)
            {
                return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
            NavigateToReferenceRow(sourceGrid, sourceGrid.CurrentCell.RowIndex);
        }

        private void NavigateToReferenceRow(DataGridView sourceGrid, int rowIndex)
        {
            if (sourceGrid == null
                || rowIndex < 0
                || rowIndex >= sourceGrid.Rows.Count)
            {
                return;
            }

            ReferenceGridRowData data = sourceGrid.Rows[rowIndex].Tag as ReferenceGridRowData;
            if (data == null)
            {
                return;
            }

            NavigateToElement(data.SourceListIndex, data.SourceElementIndex);
        }

        private DataGridView GetReferencesViewerGridFromSender(object sender)
        {
            DataGridView grid = sender as DataGridView;
            if (grid != null)
            {
                return grid;
            }

            if (referencesViewerTabs != null
                && !referencesViewerTabs.IsDisposed
                && referencesViewerTabs.SelectedTab != null
                && referencesViewerTabs.SelectedTab.Controls.Count > 0)
            {
                return referencesViewerTabs.SelectedTab.Controls[0] as DataGridView;
            }

            return referencesViewerGrid;
        }

        private void ApplyReferencesViewerTheme()
        {
            if (referencesViewerForm == null || referencesViewerForm.IsDisposed)
            {
                return;
            }

            Color surface = fwDarkMode ? Color.FromArgb(23, 26, 31) : Color.FromArgb(238, 241, 245);
            Color raised = fwDarkMode ? Color.FromArgb(31, 35, 42) : Color.FromArgb(250, 251, 253);
            Color text = fwDarkMode ? Color.FromArgb(229, 234, 242) : Color.FromArgb(29, 36, 45);
            Color secondary = fwDarkMode ? Color.FromArgb(156, 169, 187) : Color.FromArgb(83, 96, 112);

            referencesViewerForm.BackColor = surface;
            referencesViewerForm.ForeColor = text;

            if (referencesViewerLabel != null)
            {
                referencesViewerLabel.BackColor = raised;
                referencesViewerLabel.ForeColor = secondary;
            }

            if (referencesViewerTabs != null && !referencesViewerTabs.IsDisposed)
            {
                referencesViewerTabs.BackColor = surface;
                for (int i = 0; i < referencesViewerTabs.TabPages.Count; i++)
                {
                    TabPage page = referencesViewerTabs.TabPages[i];
                    if (page == null)
                    {
                        continue;
                    }

                    page.BackColor = surface;
                    page.ForeColor = text;
                    if (page.Controls.Count == 0)
                    {
                        continue;
                    }

                    DataGridView grid = page.Controls[0] as DataGridView;
                    if (grid == null || grid.IsDisposed)
                    {
                        continue;
                    }

                    grid.BackgroundColor = surface;
                    grid.GridColor = fwDarkMode ? Color.FromArgb(50, 58, 70) : Color.FromArgb(211, 218, 226);
                    grid.DefaultCellStyle.BackColor = fwDarkMode ? Color.FromArgb(18, 21, 26) : Color.White;
                    grid.DefaultCellStyle.ForeColor = text;
                    grid.DefaultCellStyle.SelectionBackColor = fwDarkMode ? Color.FromArgb(47, 76, 112) : Color.FromArgb(84, 137, 196);
                    grid.DefaultCellStyle.SelectionForeColor = text;
                    grid.AlternatingRowsDefaultCellStyle.BackColor = fwDarkMode ? Color.FromArgb(22, 26, 32) : Color.FromArgb(247, 249, 252);
                    grid.ColumnHeadersDefaultCellStyle.BackColor = fwDarkMode ? Color.FromArgb(38, 44, 53) : Color.FromArgb(225, 231, 238);
                    grid.ColumnHeadersDefaultCellStyle.ForeColor = text;
                    grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = grid.ColumnHeadersDefaultCellStyle.BackColor;
                    grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = text;
                    grid.RowTemplate.Height = 38;
                    grid.BorderStyle = BorderStyle.None;
                    ReapplyReferenceRowStyles(grid);
                    grid.Refresh();
                }
            }
        }

        private void UpdateReferencesViewerTitle(int listIndex, int elementIndex, int id)
        {
            if (referencesViewerForm == null || referencesViewerForm.IsDisposed)
            {
                return;
            }

            string itemName = string.Empty;
            if (sessionService != null
                && sessionService.ListCollection != null
                && listIndex >= 0
                && listIndex < sessionService.ListCollection.Lists.Length)
            {
                int nameFieldIndex = fieldIndexLookupService.GetNameFieldIndex(sessionService.ListCollection, listIndex);
                if (nameFieldIndex >= 0)
                {
                    itemName = sessionService.ListCollection.GetValue(listIndex, elementIndex, nameFieldIndex) ?? string.Empty;
                }
            }

            string titleSuffix = string.IsNullOrWhiteSpace(itemName) ? id.ToString() : itemName + " (" + id.ToString() + ")";
            referencesViewerForm.Text = "References - " + titleSuffix;
            if (referencesViewerLabel != null)
            {
                referencesViewerLabel.Text = "Referenced by " + titleSuffix;
            }
        }

        private void UpdateReferencesTabForSelection()
        {
            ScheduleReferencesTabRefresh();
        }

        private void ScheduleReferencesTabRefresh()
        {
            if (referencesViewerTabs == null || referencesViewerForm == null || referencesViewerForm.IsDisposed || !referencesViewerForm.Visible)
            {
                return;
            }

            if (referencesTabRefreshTimer == null)
            {
                referencesTabRefreshTimer = new System.Windows.Forms.Timer();
                referencesTabRefreshTimer.Interval = 160;
                referencesTabRefreshTimer.Tick += (s, e) =>
                {
                    referencesTabRefreshTimer.Stop();
                    LoadReferencesViewerForSelectionAsync();
                };
            }

            referencesTabRefreshTimer.Stop();
            referencesTabRefreshTimer.Start();
        }

        private void LoadReferencesViewerForSelectionAsync()
        {
            if (referencesViewerTabs == null || referencesViewerForm == null || referencesViewerForm.IsDisposed || !referencesViewerForm.Visible)
            {
                return;
            }

            int listIndex = comboBox_lists != null ? comboBox_lists.SelectedIndex : -1;
            int elementIndex = ResolveCurrentElementIndex();
            int id;
            if (!TryGetElementId(listIndex, elementIndex, out id))
            {
                referencesViewerTabs.TabPages.Clear();
                return;
            }

            int loadVersion = ++referencesTabLoadVersion;
            SetReferencesViewerTabsLoadingState();

            eListCollection listCollection = sessionService != null ? sessionService.ListCollection : null;
            CacheSave database = sessionService != null ? sessionService.Database : null;
            if (listCollection == null || database == null || itemReferenceService == null)
            {
                return;
            }

            if (!EnsureReferenceIndexAvailable())
            {
                return;
            }

            System.Threading.Tasks.Task.Run(() =>
            {
                return BuildReferenceRows(listCollection, database, listIndex, id, loadVersion);
            }).ContinueWith(task =>
            {
                if (IsDisposed
                    || !IsHandleCreated
                    || loadVersion != referencesTabLoadVersion
                    || referencesViewerTabs == null
                    || referencesViewerForm == null
                    || referencesViewerForm.IsDisposed
                    || !referencesViewerForm.Visible)
                {
                    return;
                }

                if (task.IsFaulted || task.Result == null)
                {
                    return;
                }

                referencesViewerTabs.SuspendLayout();
                try
                {
                    PopulateReferencesViewerTabs(task.Result);
                }
                finally
                {
                    referencesViewerTabs.ResumeLayout();
                }
            }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void PopulateReferencesViewerTabs(List<ReferenceGridRowData> rows)
        {
            if (referencesViewerTabs == null || referencesViewerTabs.IsDisposed)
            {
                return;
            }

            referencesViewerTabs.TabPages.Clear();
            referencesViewerGridsByKey.Clear();

            AddReferencesViewerTab("all", "All", rows ?? new List<ReferenceGridRowData>());

            if (rows != null)
            {
                Dictionary<int, List<ReferenceGridRowData>> grouped = new Dictionary<int, List<ReferenceGridRowData>>();
                for (int i = 0; i < rows.Count; i++)
                {
                    ReferenceGridRowData row = rows[i];
                    List<ReferenceGridRowData> bucket;
                    if (!grouped.TryGetValue(row.SourceListIndex, out bucket))
                    {
                        bucket = new List<ReferenceGridRowData>();
                        grouped[row.SourceListIndex] = bucket;
                    }

                    bucket.Add(row);
                }

                List<int> keys = new List<int>(grouped.Keys);
                keys.Sort();
                for (int i = 0; i < keys.Count; i++)
                {
                    int sourceListIndex = keys[i];
                    List<ReferenceGridRowData> bucket = grouped[sourceListIndex];
                    string label = bucket.Count > 0 ? bucket[0].ListLabel : ("[" + sourceListIndex.ToString() + "]");
                    AddReferencesViewerTab("list:" + sourceListIndex.ToString(), label, bucket);
                }
            }

            if (referencesViewerTabs.TabPages.Count > 0)
            {
                referencesViewerTabs.SelectedIndex = 0;
            }
            ApplyReferencesViewerTheme();
        }

        private void AddReferencesViewerTab(string key, string label, List<ReferenceGridRowData> rows)
        {
            if (referencesViewerTabs == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            TabPage page = new TabPage(BuildReferencesTabTitle(label, rows != null ? rows.Count : 0));
            DataGridView grid = CreateReferencesViewerGrid();

            if (rows != null && rows.Count > 0)
            {
                DataGridViewRow[] gridRows = new DataGridViewRow[rows.Count];
                for (int i = 0; i < rows.Count; i++)
                {
                    ReferenceGridRowData data = rows[i];
                    DataGridViewRow row = (DataGridViewRow)grid.RowTemplate.Clone();
                    row.CreateCells(grid, new object[]
                    {
                        data.ListLabel ?? string.Empty,
                        data.SourceId ?? string.Empty,
                        data.Icon,
                        data.Name ?? string.Empty,
                        data.FieldLabel ?? string.Empty,
                        data.RawValue ?? string.Empty
                    });
                    row.Tag = data;
                    ApplyReferenceRowStyle(row, data.Quality, grid);
                    gridRows[i] = row;
                }

                grid.Rows.AddRange(gridRows);
            }

            page.Controls.Add(grid);
            referencesViewerTabs.TabPages.Add(page);
            referencesViewerGridsByKey[key] = grid;
            if (referencesViewerGrid == null || key == "all")
            {
                referencesViewerGrid = grid;
            }
        }

        private void ReapplyReferenceRowStyles(DataGridView grid)
        {
            if (grid == null || grid.IsDisposed)
            {
                return;
            }

            for (int i = 0; i < grid.Rows.Count; i++)
            {
                DataGridViewRow row = grid.Rows[i];
                if (row == null)
                {
                    continue;
                }

                ReferenceGridRowData data = row.Tag as ReferenceGridRowData;
                ApplyReferenceRowStyle(row, data != null ? data.Quality : -1, grid);
            }
        }

        private static string BuildReferencesTabTitle(string label, int count)
        {
            string baseLabel = string.IsNullOrWhiteSpace(label) ? "References" : label;
            return baseLabel + " (" + count.ToString() + ")";
        }

        private void RefreshVisibleReferenceCounts()
        {
            if (dataGridView_elems == null || comboBox_lists == null || dataGridView_elems.Columns.Count < 4)
            {
                return;
            }

            int listIndex = comboBox_lists.SelectedIndex;
            if (listIndex < 0)
            {
                return;
            }

            List<VisibleReferenceTarget> targets = CollectVisibleReferenceTargets(listIndex);
            if (targets.Count == 0)
            {
                return;
            }

            int refreshVersion = ++referenceCountRefreshVersion;
            eListCollection listCollection = sessionService != null ? sessionService.ListCollection : null;
            if (listCollection == null || itemReferenceService == null)
            {
                return;
            }

            if (!EnsureReferenceIndexAvailable())
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i].RowIndex >= 0 && targets[i].RowIndex < dataGridView_elems.Rows.Count)
                    {
                        dataGridView_elems.Rows[targets[i].RowIndex].Cells[3].Value = string.Empty;
                    }
                }
                return;
            }

            System.Threading.Tasks.Task.Run(() =>
            {
                Dictionary<int, int> countsById = new Dictionary<int, int>();
                for (int i = 0; i < targets.Count; i++)
                {
                    VisibleReferenceTarget target = targets[i];
                    if (!countsById.ContainsKey(target.Id))
                    {
                        countsById[target.Id] = referenceIndexService.GetReferenceCount(
                            listCollection,
                            itemReferenceService,
                            listIndex,
                            target.Id);
                    }
                }

                if (IsDisposed || !IsHandleCreated)
                {
                    return;
                }

                BeginInvoke((Action)(() =>
                {
                    if (IsDisposed
                        || refreshVersion != referenceCountRefreshVersion
                        || comboBox_lists == null
                        || comboBox_lists.SelectedIndex != listIndex
                        || dataGridView_elems == null)
                    {
                        return;
                    }

                    for (int i = 0; i < targets.Count; i++)
                    {
                        VisibleReferenceTarget target = targets[i];
                        if (target.RowIndex < 0 || target.RowIndex >= dataGridView_elems.Rows.Count)
                        {
                            continue;
                        }

                        int count;
                        countsById.TryGetValue(target.Id, out count);
                        string nextValue = count > 0 ? count.ToString() : string.Empty;
                        object currentValue = dataGridView_elems.Rows[target.RowIndex].Cells[3].Value;
                        string currentText = currentValue != null ? Convert.ToString(currentValue) : string.Empty;
                        if (!string.Equals(currentText, nextValue, StringComparison.Ordinal))
                        {
                            dataGridView_elems.Rows[target.RowIndex].Cells[3].Value = nextValue;
                        }
                    }
                }));
            });
        }

        private void ScheduleVisibleReferenceCountRefresh()
        {
            if (referenceCountRefreshTimer == null)
            {
                RefreshVisibleReferenceCounts();
                return;
            }

            referenceCountRefreshTimer.Stop();
            referenceCountRefreshTimer.Start();
        }

        private void InvalidateReferenceIndexAndDisplays()
        {
            ResetReferenceCaches();
            if (listDisplayService != null)
            {
                listDisplayService.ClearListDisplayCache();
            }

            RefreshVisibleReferenceCounts();
            if (referencesViewerForm != null && !referencesViewerForm.IsDisposed && referencesViewerForm.Visible)
            {
                ScheduleReferencesTabRefresh();
            }
        }

        private bool ShouldInvalidateReferenceState(int listIndex, int elementIndex, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            if (string.Equals(fieldName, "id", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return itemReferenceService != null
                && sessionService != null
                && sessionService.ListCollection != null
                && itemReferenceService.IsReferenceField(sessionService.ListCollection, listIndex, elementIndex, fieldName);
        }

        private bool TryGetElementId(int listIndex, int elementIndex, out int id)
        {
            id = 0;
            if (sessionService == null
                || sessionService.ListCollection == null
                || listIndex < 0
                || listIndex >= sessionService.ListCollection.Lists.Length
                || listIndex == sessionService.ListCollection.ConversationListIndex
                || elementIndex < 0
                || elementIndex >= sessionService.ListCollection.Lists[listIndex].elementValues.Length
                || sessionService.ListCollection.Lists[listIndex].elementFields == null
                || sessionService.ListCollection.Lists[listIndex].elementFields.Length == 0)
            {
                return false;
            }

            return int.TryParse(sessionService.ListCollection.GetValue(listIndex, elementIndex, 0), out id) && id > 0;
        }

        private static string BuildReferenceListLabel(ReferenceUsage usage)
        {
            if (usage == null)
            {
                return string.Empty;
            }

            return "[" + usage.SourceListIndex.ToString() + "] " + (usage.SourceListName ?? string.Empty);
        }

        private void ApplyReferenceRowStyle(DataGridViewRow row, int quality, DataGridView targetGrid)
        {
            if (row == null || targetGrid == null || row.Cells.Count < 6)
            {
                return;
            }

            DataGridViewCell nameCell = row.Cells[3];
            Color? qualityColor = itemQualityColorService.GetQualityColor(quality);
            if (qualityColor.HasValue)
            {
                nameCell.Style.ForeColor = qualityColor.Value;
                nameCell.Style.SelectionForeColor = qualityColor.Value;
            }
            else
            {
                nameCell.Style.ForeColor = targetGrid.DefaultCellStyle.ForeColor;
                nameCell.Style.SelectionForeColor = targetGrid.DefaultCellStyle.SelectionForeColor;
            }
        }

        private bool IsReferencesTabActive()
        {
            return fwRightTabs != null
                && fwReferencesTab != null
                && fwRightTabs.TabPages.Contains(fwReferencesTab)
                && fwRightTabs.SelectedTab == fwReferencesTab;
        }

        private List<VisibleReferenceTarget> CollectVisibleReferenceTargets(int listIndex)
        {
            List<VisibleReferenceTarget> targets = new List<VisibleReferenceTarget>();
            if (dataGridView_elems == null)
            {
                return targets;
            }

            int firstRowIndex = dataGridView_elems.FirstDisplayedScrollingRowIndex;
            if (firstRowIndex < 0)
            {
                firstRowIndex = 0;
            }

            int visibleRowCount = dataGridView_elems.DisplayedRowCount(true);
            int lastRowExclusive = visibleRowCount > 0
                ? Math.Min(dataGridView_elems.Rows.Count, firstRowIndex + visibleRowCount + 2)
                : dataGridView_elems.Rows.Count;

            for (int rowIndex = firstRowIndex; rowIndex < lastRowExclusive; rowIndex++)
            {
                int elementIndex = elementIndexResolverService.ResolveElementIndexFromGridRow(
                    sessionService.ListCollection,
                    listIndex,
                    rowIndex,
                    dataGridView_elems);

                int id;
                if (!TryGetElementId(listIndex, elementIndex, out id))
                {
                    continue;
                }

                targets.Add(new VisibleReferenceTarget
                {
                    RowIndex = rowIndex,
                    ElementIndex = elementIndex,
                    Id = id
                });
            }

            int currentRowIndex = dataGridView_elems.CurrentCell != null ? dataGridView_elems.CurrentCell.RowIndex : -1;
            if (currentRowIndex >= 0 && currentRowIndex < dataGridView_elems.Rows.Count)
            {
                bool alreadyIncluded = false;
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i].RowIndex == currentRowIndex)
                    {
                        alreadyIncluded = true;
                        break;
                    }
                }

                if (!alreadyIncluded)
                {
                    int elementIndex = elementIndexResolverService.ResolveElementIndexFromGridRow(
                        sessionService.ListCollection,
                        listIndex,
                        currentRowIndex,
                        dataGridView_elems);

                    int id;
                    if (TryGetElementId(listIndex, elementIndex, out id))
                    {
                        targets.Add(new VisibleReferenceTarget
                        {
                            RowIndex = currentRowIndex,
                            ElementIndex = elementIndex,
                            Id = id
                        });
                    }
                }
            }

            return targets;
        }

        private Dictionary<int, int> ComputeReferenceCountsForIds(eListCollection listCollection, int targetListIndex, HashSet<int> targetIds)
        {
            Dictionary<int, int> counts = new Dictionary<int, int>();
            if (listCollection == null || targetIds == null || targetIds.Count == 0 || itemReferenceService == null)
            {
                return counts;
            }

            bool targetListIsItemBearing = itemReferenceService.IsItemBearingList(listCollection, targetListIndex);
            foreach (int id in targetIds)
            {
                counts[id] = 0;
            }

            for (int sourceListIndex = 0; sourceListIndex < listCollection.Lists.Length; sourceListIndex++)
            {
                if (sourceListIndex == listCollection.ConversationListIndex
                    || listCollection.Lists[sourceListIndex] == null
                    || listCollection.Lists[sourceListIndex].elementFields == null
                    || listCollection.Lists[sourceListIndex].elementValues == null)
                {
                    continue;
                }

                string[] fields = listCollection.Lists[sourceListIndex].elementFields;
                for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
                {
                    string fieldName = fields[fieldIndex] ?? string.Empty;
                    for (int sourceElementIndex = 0; sourceElementIndex < listCollection.Lists[sourceListIndex].elementValues.Length; sourceElementIndex++)
                    {
                        int resolvedTargetListIndex;
                        if (!itemReferenceService.TryGetTargetListIndex(listCollection, sourceListIndex, sourceElementIndex, fieldName, out resolvedTargetListIndex))
                        {
                            continue;
                        }

                        bool matchesCurrentList = resolvedTargetListIndex == targetListIndex
                            || (targetListIsItemBearing && itemReferenceService.IsItemListTargetIndex(resolvedTargetListIndex));
                        if (!matchesCurrentList)
                        {
                            continue;
                        }

                        int referencedId;
                        if (!int.TryParse(listCollection.GetValue(sourceListIndex, sourceElementIndex, fieldIndex), out referencedId))
                        {
                            continue;
                        }

                        int currentCount;
                        if (targetIds.Contains(referencedId) && counts.TryGetValue(referencedId, out currentCount))
                        {
                            counts[referencedId] = currentCount + 1;
                        }
                    }
                }
            }

            return counts;
        }

        private List<ReferenceGridRowData> BuildReferenceRows(
            eListCollection listCollection,
            CacheSave database,
            int listIndex,
            int id,
            int loadVersion)
        {
            string cacheKey = BuildReferenceCacheKey(listIndex, id);
            lock (referenceRowsCacheSync)
            {
                List<ReferenceGridRowData> cachedRows;
                if (referenceRowsCache.TryGetValue(cacheKey, out cachedRows))
                {
                    return cachedRows;
                }
            }

            List<ReferenceGridRowData> rows = new List<ReferenceGridRowData>();
            if (listCollection == null || database == null || itemReferenceService == null)
            {
                return rows;
            }

            List<ReferenceUsage> usages = referenceIndexService.GetReferences(
                listCollection,
                itemReferenceService,
                listIndex,
                id);

            CreaturePortraitIconService portraitService = new CreaturePortraitIconService();
            Dictionary<int, int> qualityIndexCache = new Dictionary<int, int>();
            for (int i = 0; i < usages.Count; i++)
            {
                if (loadVersion != referencesTabLoadVersion)
                {
                    return null;
                }

                ReferenceUsage usage = usages[i];
                rows.Add(new ReferenceGridRowData
                {
                    SourceListIndex = usage.SourceListIndex,
                    SourceElementIndex = usage.SourceElementIndex,
                    ListLabel = BuildReferenceListLabel(usage),
                    SourceId = usage.SourceItemId ?? string.Empty,
                    Icon = ResolveReferenceIcon(usage, listCollection, database, portraitService),
                    Name = usage.SourceItemName ?? string.Empty,
                    FieldLabel = usage.SourceFieldIndex.ToString() + " - " + (usage.SourceFieldName ?? string.Empty),
                    RawValue = usage.RawValue ?? string.Empty,
                    Quality = ResolveReferenceQuality(usage, listCollection, qualityIndexCache)
                });
            }

            lock (referenceRowsCacheSync)
            {
                referenceRowsCache[cacheKey] = rows;
            }

            return rows;
        }

        private Image ResolveReferenceIcon(
            ReferenceUsage usage,
            eListCollection listCollection,
            CacheSave database,
            CreaturePortraitIconService portraitService)
        {
            if (usage != null && database != null && listCollection != null)
            {
                Image inheritedIcon = TryResolveInheritedReferenceIcon(usage, listCollection, database, portraitService);
                if (inheritedIcon != null)
                {
                    return inheritedIcon;
                }

                int iconFieldIndex = GetIconFieldIndex(usage.SourceListIndex, listCollection);
                if (iconFieldIndex >= 0)
                {
                    string rawIcon = listCollection.GetValue(
                        usage.SourceListIndex,
                        usage.SourceElementIndex,
                        iconFieldIndex);

                    Bitmap portrait;
                    if (portraitService != null
                        && portraitService.TryResolvePortrait(
                            database,
                            listCollection,
                            usage.SourceListIndex,
                            rawIcon,
                            out portrait))
                    {
                        return portrait;
                    }

                    string resolvedIconKey = iconResolutionService != null
                        ? iconResolutionService.ResolveIconKeyForList(database, listCollection, usage.SourceListIndex, rawIcon)
                        : rawIcon;

                    if (!string.IsNullOrWhiteSpace(resolvedIconKey)
                        && database.sourceBitmap != null
                        && database.ContainsKey(resolvedIconKey))
                    {
                        return database.images(resolvedIconKey);
                    }

                    if (!string.IsNullOrWhiteSpace(rawIcon)
                        && database.sourceBitmap != null
                        && database.ContainsKey(rawIcon))
                    {
                        return database.images(rawIcon);
                    }
                }
            }

            return Properties.Resources.NoIcon;
        }

        private Image TryResolveInheritedReferenceIcon(
            ReferenceUsage usage,
            eListCollection listCollection,
            CacheSave database,
            CreaturePortraitIconService portraitService)
        {
            if (usage == null
                || listCollection == null
                || database == null
                || usage.SourceListIndex < 0
                || usage.SourceListIndex >= listCollection.Lists.Length
                || listCollection.Lists[usage.SourceListIndex] == null)
            {
                return null;
            }

            string normalizedListName = NormalizeReferenceListName(listCollection.Lists[usage.SourceListIndex].listName);
            if (string.Equals(normalizedListName, "ITEM_TRADE_ESSENCE", StringComparison.OrdinalIgnoreCase))
            {
                int tradeServiceId;
                if (int.TryParse(usage.SourceItemId, out tradeServiceId))
                {
                    Bitmap portrait;
                    NpcTradePortraitService portraitServiceHelper = new NpcTradePortraitService();
                    if (portraitServiceHelper.TryResolveTradePortrait(listCollection, database, tradeServiceId, out portrait))
                    {
                        return portrait;
                    }
                }
            }
            else if (string.Equals(normalizedListName, "DROPTABLE_ESSENCE", StringComparison.OrdinalIgnoreCase))
            {
                int dropTableId;
                if (int.TryParse(usage.SourceItemId, out dropTableId))
                {
                    Bitmap portrait;
                    MonsterDropPortraitService monsterPortraitService = new MonsterDropPortraitService();
                    if (monsterPortraitService.TryResolveDropPortrait(listCollection, database, dropTableId, out portrait))
                    {
                        return portrait;
                    }
                }

                ItemReferenceOption option;
                if (TryResolveDropTableInheritedReferenceOption(
                    listCollection,
                    usage.SourceListIndex,
                    usage.SourceElementIndex,
                    database,
                    out option)
                    && option != null)
                {
                    Image icon = TryLoadReferenceOptionIcon(option, database, portraitService);
                    if (icon != null)
                    {
                        return icon;
                    }
                }
            }
            else if (string.Equals(normalizedListName, "ITEM_TRADE_PAGE_CONFIG", StringComparison.OrdinalIgnoreCase))
            {
                ItemReferenceOption option;
                if (TryResolveFirstReferenceOptionByFieldPattern(
                    listCollection,
                    usage.SourceListIndex,
                    usage.SourceElementIndex,
                    database,
                    "goods_",
                    "_1_id_goods",
                    out option)
                    && option != null)
                {
                    Image icon = TryLoadReferenceOptionIcon(option, database, portraitService);
                    if (icon != null)
                    {
                        return icon;
                    }
                }
            }
            else if (string.Equals(normalizedListName, "GIFT_BAG_CONFIG", StringComparison.OrdinalIgnoreCase))
            {
                ItemReferenceOption option;
                if (TryResolveFirstReferenceOptionByFieldPattern(
                    listCollection,
                    usage.SourceListIndex,
                    usage.SourceElementIndex,
                    database,
                    "gift_",
                    "_1_id",
                    out option)
                    && option != null)
                {
                    Image icon = TryLoadReferenceOptionIcon(option, database, portraitService);
                    if (icon != null)
                    {
                        return icon;
                    }
                }
            }

            return null;
        }

        private Image TryLoadReferenceOptionIcon(ItemReferenceOption option, CacheSave database, CreaturePortraitIconService portraitService)
        {
            if (option == null || database == null || string.IsNullOrWhiteSpace(option.IconKey))
            {
                return null;
            }

            if (database.sourceBitmap != null && database.ContainsKey(option.IconKey))
            {
                return database.images(option.IconKey);
            }

            return portraitService != null
                ? portraitService.TryLoadPortraitThumbnail(option.IconKey, 32)
                : null;
        }

        private bool TryResolveFirstReferenceOptionByFieldPattern(
            eListCollection listCollection,
            int listIndex,
            int elementIndex,
            CacheSave database,
            string prefix,
            string marker,
            out ItemReferenceOption option)
        {
            option = null;
            if (itemReferenceService == null
                || listCollection == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementFields == null
                || elementIndex < 0
                || listCollection.Lists[listIndex].elementValues == null
                || elementIndex >= listCollection.Lists[listIndex].elementValues.Length)
            {
                return false;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            for (int i = 0; i < fields.Length; i++)
            {
                string fieldName = fields[i] ?? string.Empty;
                if (!fieldName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    || fieldName.IndexOf(marker, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                string rawValue = listCollection.GetValue(listIndex, elementIndex, i);
                if (itemReferenceService.TryResolveReferenceOption(
                    listCollection,
                    listIndex,
                    elementIndex,
                    fieldName,
                    rawValue,
                    database,
                    iconResolutionService,
                    out option)
                    && option != null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryResolveDropTableInheritedReferenceOption(
            eListCollection listCollection,
            int listIndex,
            int elementIndex,
            CacheSave database,
            out ItemReferenceOption option)
        {
            option = null;
            if (itemReferenceService == null
                || listCollection == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementFields == null
                || listCollection.Lists[listIndex].elementValues == null
                || elementIndex < 0
                || elementIndex >= listCollection.Lists[listIndex].elementValues.Length)
            {
                return false;
            }

            return TryResolveDropTableInheritedReferenceOptionInternal(
                listCollection,
                listIndex,
                elementIndex,
                database,
                out option,
                0);
        }

        private bool TryResolveDropTableInheritedReferenceOptionInternal(
            eListCollection listCollection,
            int listIndex,
            int elementIndex,
            CacheSave database,
            out ItemReferenceOption option,
            int depth)
        {
            option = null;
            if (depth > 6)
            {
                return false;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            int isCategoryFieldIndex = -1;
            List<int> dropFieldIndexes = new List<int>();
            for (int i = 0; i < fields.Length; i++)
            {
                string fieldName = fields[i] ?? string.Empty;
                if (string.Equals(fieldName, "is_category", StringComparison.OrdinalIgnoreCase))
                {
                    isCategoryFieldIndex = i;
                }
                else if (fieldName.StartsWith("drops_", StringComparison.OrdinalIgnoreCase)
                    && fieldName.EndsWith("_id_obj", StringComparison.OrdinalIgnoreCase))
                {
                    dropFieldIndexes.Add(i);
                }
            }

            int firstDropId = 0;
            int firstDropFieldIndex = -1;
            for (int i = 0; i < dropFieldIndexes.Count; i++)
            {
                int candidateId;
                if (int.TryParse(listCollection.GetValue(listIndex, elementIndex, dropFieldIndexes[i]), out candidateId) && candidateId > 0)
                {
                    firstDropId = candidateId;
                    firstDropFieldIndex = dropFieldIndexes[i];
                    break;
                }
            }

            if (firstDropId <= 0)
            {
                return false;
            }

            int isCategory = 0;
            if (isCategoryFieldIndex >= 0)
            {
                int.TryParse(listCollection.GetValue(listIndex, elementIndex, isCategoryFieldIndex), out isCategory);
            }

            if (isCategory == 1)
            {
                int childRowIndex = FindElementIndexById(listCollection, listIndex, firstDropId);
                if (childRowIndex < 0)
                {
                    return false;
                }

                return TryResolveDropTableInheritedReferenceOptionInternal(
                    listCollection,
                    listIndex,
                    childRowIndex,
                    database,
                    out option,
                    depth + 1);
            }

            string fieldNameForLookup = fields[firstDropFieldIndex] ?? string.Empty;
            string rawValue = listCollection.GetValue(listIndex, elementIndex, firstDropFieldIndex);
            return itemReferenceService.TryResolveReferenceOption(
                listCollection,
                listIndex,
                elementIndex,
                fieldNameForLookup,
                rawValue,
                database,
                iconResolutionService,
                out option);
        }

        private static int FindElementIndexById(eListCollection listCollection, int listIndex, int id)
        {
            if (listCollection == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementValues == null
                || id <= 0)
            {
                return -1;
            }

            for (int i = 0; i < listCollection.Lists[listIndex].elementValues.Length; i++)
            {
                int candidateId;
                if (int.TryParse(listCollection.GetValue(listIndex, i, 0), out candidateId) && candidateId == id)
                {
                    return i;
                }
            }

            return -1;
        }

        private int ResolveReferenceQuality(
            ReferenceUsage usage,
            eListCollection listCollection,
            Dictionary<int, int> qualityIndexCache)
        {
            if (usage == null
                || listCollection == null
                || usage.SourceListIndex < 0
                || usage.SourceListIndex >= listCollection.Lists.Length)
            {
                return -1;
            }

            int qualityIndex;
            if (!qualityIndexCache.TryGetValue(usage.SourceListIndex, out qualityIndex))
            {
                qualityIndex = fieldIndexLookupService.GetItemQualityFieldIndex(listCollection, usage.SourceListIndex);
                qualityIndexCache[usage.SourceListIndex] = qualityIndex;
            }

            int quality;
            if (qualityIndex >= 0
                && int.TryParse(listCollection.GetValue(usage.SourceListIndex, usage.SourceElementIndex, qualityIndex), out quality))
            {
                return quality;
            }

            return -1;
        }

        private int GetIconFieldIndex(int listIndex, eListCollection listCollection)
        {
            if (listCollection == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementFields == null)
            {
                return -1;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            for (int i = 0; i < fields.Length; i++)
            {
                if (string.Equals(fields[i], "file_icon", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fields[i], "file_icon1", StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string NormalizeReferenceListName(string listName)
        {
            if (string.IsNullOrWhiteSpace(listName))
            {
                return string.Empty;
            }

            string[] split = listName.Split(new string[] { " - " }, StringSplitOptions.None);
            return split.Length > 1 ? split[1].Trim() : listName.Trim();
        }
    }
}
