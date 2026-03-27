using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FWEledit
{
    public class ModelPickerWindow : Form
    {
        private readonly List<ModelPickerEntry> allEntries;
        private readonly ComboBox packageFilter;
        private readonly ComboBox folderFilter;
        private readonly TextBox searchBox;
        private readonly CheckBox onlyUnusedCheck;
        private readonly Button exportCsvButton;
        private readonly DataGridView grid;
        private readonly Label statusLabel;
        private readonly System.Windows.Forms.Timer filterTimer;
        private bool isInitializing;
        public int SelectedPathId { get; private set; }

        public ModelPickerWindow(List<ModelPickerEntry> entries, int currentPathId)
        {
            allEntries = entries ?? new List<ModelPickerEntry>();
            SelectedPathId = currentPathId;
            isInitializing = true;

            Text = "Choice Model";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(900, 550);
            Size = new Size(1200, 760);
            KeyPreview = true;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            KeyDown += OnKeyDown;

            Panel top = new Panel();
            top.Dock = DockStyle.Top;
            top.Height = 40;
            top.Padding = new Padding(8, 8, 8, 4);

            packageFilter = new ComboBox();
            packageFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            packageFilter.Width = 120;
            packageFilter.Left = 8;
            packageFilter.Top = 8;
            packageFilter.SelectedIndexChanged += (s, e) =>
            {
                BuildFolderFilter();
                ScheduleFilterApply();
            };
            top.Controls.Add(packageFilter);

            folderFilter = new ComboBox();
            folderFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            folderFilter.Width = 110;
            folderFilter.Left = packageFilter.Right + 8;
            folderFilter.Top = 8;
            folderFilter.SelectedIndexChanged += (s, e) => ScheduleFilterApply();
            top.Controls.Add(folderFilter);

            searchBox = new TextBox();
            searchBox.Left = folderFilter.Right + 8;
            searchBox.Top = 8;
            searchBox.Width = 400;
            searchBox.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            searchBox.TextChanged += (s, e) => ScheduleFilterApply();
            top.Controls.Add(searchBox);

            onlyUnusedCheck = new CheckBox();
            onlyUnusedCheck.Text = "Keep Only Unused";
            onlyUnusedCheck.AutoSize = true;
            onlyUnusedCheck.Left = searchBox.Right + 14;
            onlyUnusedCheck.Top = 10;
            onlyUnusedCheck.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            onlyUnusedCheck.CheckedChanged += (s, e) => ScheduleFilterApply();
            top.Controls.Add(onlyUnusedCheck);

            exportCsvButton = new Button();
            exportCsvButton.Text = "Export CSV";
            exportCsvButton.AutoSize = true;
            exportCsvButton.Height = 24;
            exportCsvButton.Left = onlyUnusedCheck.Right + 12;
            exportCsvButton.Top = 8;
            exportCsvButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            exportCsvButton.Click += (s, e) => ExportCsv();
            top.Controls.Add(exportCsvButton);

            grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.RowHeadersVisible = false;
            grid.ReadOnly = true;
            grid.BackgroundColor = Color.FromArgb(24, 24, 24);
            grid.GridColor = Color.FromArgb(40, 40, 40);
            grid.DefaultCellStyle.BackColor = Color.FromArgb(24, 24, 24);
            grid.DefaultCellStyle.ForeColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(44, 118, 171);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.WhiteSmoke;
            grid.EnableHeadersVisualStyles = false;
            grid.RowTemplate.Height = 24;
            grid.DoubleClick += (s, e) => ConfirmSelection();
            grid.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    ConfirmSelection();
                }
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIndex", HeaderText = "#", Width = 60 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPath", HeaderText = "Path", Width = 430 });
            DataGridViewImageColumn iconCol = new DataGridViewImageColumn();
            iconCol.Name = "colIcon";
            iconCol.HeaderText = string.Empty;
            iconCol.Width = 26;
            iconCol.ImageLayout = DataGridViewImageCellLayout.Zoom;
            iconCol.DefaultCellStyle.NullValue = Properties.Resources.NoIcon;
            grid.Columns.Add(iconCol);
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Name", Width = 260 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", Width = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUses", HeaderText = "Uses", Width = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPathId", HeaderText = "PathID", Width = 90 });

            Panel bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 48;
            bottom.Padding = new Padding(8, 6, 8, 6);

            statusLabel = new Label();
            statusLabel.Dock = DockStyle.Left;
            statusLabel.AutoSize = false;
            statusLabel.Width = 320;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            bottom.Controls.Add(statusLabel);

            Button cancelButton = new Button();
            cancelButton.Text = "Cancel [ESC]";
            cancelButton.Dock = DockStyle.Right;
            cancelButton.Width = 140;
            cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            bottom.Controls.Add(cancelButton);

            Button okButton = new Button();
            okButton.Text = "OK [Enter]";
            okButton.Dock = DockStyle.Right;
            okButton.Width = 140;
            okButton.Click += (s, e) => ConfirmSelection();
            bottom.Controls.Add(okButton);

            Controls.Add(grid);
            Controls.Add(bottom);
            Controls.Add(top);

            filterTimer = new System.Windows.Forms.Timer();
            filterTimer.Interval = 200;
            filterTimer.Tick += (s, e) =>
            {
                filterTimer.Stop();
                ApplyFilter();
            };

            BuildPackageFilter();
            string preferredPackage = GetPackageForPathId(currentPathId);
            if (!string.IsNullOrWhiteSpace(preferredPackage))
            {
                for (int i = 0; i < packageFilter.Items.Count; i++)
                {
                    if (string.Equals(Convert.ToString(packageFilter.Items[i]), preferredPackage, StringComparison.OrdinalIgnoreCase))
                    {
                        packageFilter.SelectedIndex = i;
                        break;
                    }
                }
            }
            BuildFolderFilter(currentPathId);
            ApplyFilter();
            SelectCurrentPathId(currentPathId);
            isInitializing = false;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void BuildPackageFilter()
        {
            packageFilter.Items.Clear();
            for (int i = 0; i < ModelPickerCatalog.PackageOrder.Length; i++)
            {
                packageFilter.Items.Add(ModelPickerCatalog.PackageOrder[i]);
            }
            if (packageFilter.Items.Count > 0)
            {
                packageFilter.SelectedItem = "models";
                if (packageFilter.SelectedIndex < 0) { packageFilter.SelectedIndex = 0; }
            }
        }

        private void BuildFolderFilter(int currentPathId = 0)
        {
            string package = Convert.ToString(packageFilter.SelectedItem);
            folderFilter.Items.Clear();

            if (string.IsNullOrWhiteSpace(package))
            {
                return;
            }

            HashSet<string> folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < allEntries.Count; i++)
            {
                ModelPickerEntry e = allEntries[i];
                if (!string.Equals(e.Package, package, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string folder = ExtractTopFolder(e.RelativePath, package);
                if (string.IsNullOrWhiteSpace(folder))
                {
                    continue;
                }

                folders.Add(folder);
            }

            List<string> sorted = folders
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            folderFilter.Items.Add(string.Empty);
            for (int i = 0; i < sorted.Count; i++)
            {
                folderFilter.Items.Add(sorted[i]);
            }

            if (folderFilter.Items.Count == 0)
            {
                return;
            }

            folderFilter.SelectedIndex = 0;
        }

        private static string ExtractTopFolder(string relativePath, string package)
        {
            string value = (relativePath ?? string.Empty).Replace('/', '\\').Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(package))
            {
                string packagePrefix = package + "\\";
                if (value.StartsWith(packagePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    value = value.Substring(packagePrefix.Length);
                }
            }

            int slash = value.IndexOf('\\');
            if (slash <= 0)
            {
                return value;
            }

            return value.Substring(0, slash);
        }

        private void ScheduleFilterApply()
        {
            if (isInitializing)
            {
                return;
            }
            filterTimer.Stop();
            filterTimer.Start();
        }

        private void ApplyFilter()
        {
            List<ModelPickerEntry> filtered = GetFilteredEntries();
            grid.SuspendLayout();
            grid.Rows.Clear();
            for (int i = 0; i < filtered.Count; i++)
            {
                ModelPickerEntry e = filtered[i];
                string name = e.Uses > 0 ? e.ItemName : "[NOT USING]";
                string idText = e.Uses > 0 && e.ItemId > 0 ? e.ItemId.ToString() : "-";
                string usesText = e.Uses > 0 ? e.Uses.ToString() : "-";
                int rowIndex = grid.Rows.Add(
                    i.ToString(),
                    e.RelativePath ?? string.Empty,
                    e.Icon ?? Properties.Resources.blank,
                    name,
                    idText,
                    usesText,
                    e.PathId.ToString()
                );

                if (e.Uses <= 0)
                {
                    grid.Rows[rowIndex].Cells[3].Style.ForeColor = Color.LimeGreen;
                }
                else
                {
                    grid.Rows[rowIndex].Cells[5].Style.ForeColor = Color.Cyan;
                }
            }
            grid.ResumeLayout();

            statusLabel.Text = filtered.Count.ToString() + " entries";
        }

        private List<ModelPickerEntry> GetFilteredEntries()
        {
            string package = Convert.ToString(packageFilter.SelectedItem);
            if (string.IsNullOrWhiteSpace(package))
            {
                package = "models";
            }
            string folder = Convert.ToString(folderFilter.SelectedItem);
            string term = (searchBox.Text ?? string.Empty).Trim().ToLowerInvariant();
            bool keepOnlyUnused = onlyUnusedCheck.Checked;

            IEnumerable<ModelPickerEntry> query = allEntries;
            query = query.Where(x => string.Equals(x.Package, package, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(folder))
            {
                query = query.Where(x => string.Equals(ExtractTopFolder(x.RelativePath, package), folder, StringComparison.OrdinalIgnoreCase));
            }
            if (keepOnlyUnused)
            {
                query = query.Where(x => x.Uses <= 0);
            }
            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(x =>
                    x.Index.ToString().Contains(term)
                    || x.PathId.ToString().Contains(term)
                    || (!string.IsNullOrWhiteSpace(x.RelativePath) && x.RelativePath.ToLowerInvariant().Contains(term))
                    || (!string.IsNullOrWhiteSpace(x.ItemName) && x.ItemName.ToLowerInvariant().Contains(term))
                    || (x.ItemId > 0 && x.ItemId.ToString().Contains(term))
                );
            }

            return query.ToList();
        }

        private void ExportCsv()
        {
            try
            {
                List<ModelPickerEntry> filtered = GetFilteredEntries();
                if (filtered.Count == 0)
                {
                    MessageBox.Show("No entries to export.", "Choice Model", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string package = Convert.ToString(packageFilter.SelectedItem);
                if (string.IsNullOrWhiteSpace(package))
                {
                    package = "models";
                }

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "Export Choice Model CSV";
                    sfd.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                    sfd.FileName = "choice-model-" + package + ".csv";
                    if (sfd.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    using (StreamWriter sw = new StreamWriter(sfd.FileName, false, new UTF8Encoding(false)))
                    {
                        sw.WriteLine("package,path,pathId,id,name,uses");
                        for (int i = 0; i < filtered.Count; i++)
                        {
                            ModelPickerEntry e = filtered[i];
                            string name = e.Uses > 0 ? e.ItemName : "[NOT USING]";
                            string idText = e.Uses > 0 && e.ItemId > 0 ? e.ItemId.ToString() : "-";
                            string usesText = e.Uses > 0 ? e.Uses.ToString() : "-";
                            string pathIdText = e.PathId > 0 ? e.PathId.ToString() : "0";
                            sw.WriteLine(
                                CsvEscape(package) + "," +
                                CsvEscape(e.RelativePath ?? string.Empty) + "," +
                                CsvEscape(pathIdText) + "," +
                                CsvEscape(idText) + "," +
                                CsvEscape(name ?? string.Empty) + "," +
                                CsvEscape(usesText)
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("CSV export failed:\n" + ex.Message, "Choice Model", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string CsvEscape(string value)
        {
            string v = value ?? string.Empty;
            if (v.Contains("\""))
            {
                v = v.Replace("\"", "\"\"");
            }
            if (v.Contains(",") || v.Contains("\n") || v.Contains("\r"))
            {
                v = "\"" + v + "\"";
            }
            return v;
        }

        private string GetPackageForPathId(int pathId)
        {
            if (pathId <= 0)
            {
                return string.Empty;
            }
            for (int i = 0; i < allEntries.Count; i++)
            {
                if (allEntries[i].PathId == pathId)
                {
                    return allEntries[i].Package;
                }
            }
            return string.Empty;
        }

        private string GetFolderForPathId(int pathId)
        {
            if (pathId <= 0)
            {
                return string.Empty;
            }

            for (int i = 0; i < allEntries.Count; i++)
            {
                if (allEntries[i].PathId == pathId)
                {
                    return ExtractTopFolder(allEntries[i].RelativePath, allEntries[i].Package);
                }
            }
            return string.Empty;
        }

        private void SelectCurrentPathId(int pathId)
        {
            if (pathId <= 0)
            {
                return;
            }
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                int rowPathId;
                if (!int.TryParse(Convert.ToString(grid.Rows[i].Cells[6].Value), out rowPathId))
                {
                    continue;
                }
                if (rowPathId == pathId)
                {
                    grid.CurrentCell = grid.Rows[i].Cells[0];
                    grid.Rows[i].Selected = true;
                    if (i > 0)
                    {
                        grid.FirstDisplayedScrollingRowIndex = i;
                    }
                    return;
                }
            }
        }

        private void ConfirmSelection()
        {
            if (grid.CurrentRow == null)
            {
                return;
            }
            int pathId;
            if (!int.TryParse(Convert.ToString(grid.CurrentRow.Cells[6].Value), out pathId) || pathId <= 0)
            {
                return;
            }
            SelectedPathId = pathId;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
