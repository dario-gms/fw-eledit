using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ModelPathEditorWindow : Form
    {
        private readonly AssetManager assetManager;
        private readonly CacheSave database;
        private readonly int originalPathId;
        private readonly string canonicalMappedPath;
        private readonly TextBox pathIdBox;
        private readonly TextBox pathBox;
        private readonly Label statusLabel;

        public int SavedPathId { get; private set; }
        public string SavedMappedPath { get; private set; }

        public ModelPathEditorWindow(
            AssetManager assetManager,
            CacheSave database,
            int currentPathId,
            string mappedPath)
        {
            this.assetManager = assetManager;
            this.database = database;
            originalPathId = currentPathId;
            canonicalMappedPath = (mappedPath ?? string.Empty).Replace('/', '\\').Trim().TrimStart('\\');
            SavedPathId = currentPathId;
            SavedMappedPath = canonicalMappedPath;

            Text = "Path Editor";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(700, 205);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));

            Label idLabel = new Label();
            idLabel.Text = "PathID";
            idLabel.Left = 16;
            idLabel.Top = 20;
            idLabel.Width = 60;
            Controls.Add(idLabel);

            pathIdBox = new TextBox();
            pathIdBox.Left = 80;
            pathIdBox.Top = 16;
            pathIdBox.Width = 160;
            pathIdBox.Text = currentPathId > 0 ? currentPathId.ToString() : string.Empty;
            Controls.Add(pathIdBox);

            Button generateButton = new Button();
            generateButton.Text = "Generate Auto";
            generateButton.Left = 250;
            generateButton.Top = 14;
            generateButton.Width = 120;
            generateButton.Click += (s, e) => GenerateSuggestedPathId();
            Controls.Add(generateButton);

            Label pathLabel = new Label();
            pathLabel.Text = "Path";
            pathLabel.Left = 16;
            pathLabel.Top = 58;
            pathLabel.Width = 60;
            Controls.Add(pathLabel);

            pathBox = new TextBox();
            pathBox.Left = 80;
            pathBox.Top = 54;
            pathBox.Width = 590;
            pathBox.ReadOnly = true;
            pathBox.Text = canonicalMappedPath;
            Controls.Add(pathBox);

            statusLabel = new Label();
            statusLabel.Left = 16;
            statusLabel.Top = 98;
            statusLabel.Width = 654;
            statusLabel.Height = 42;
            statusLabel.ForeColor = Color.FromArgb(70, 70, 70);
            statusLabel.Text = currentPathId > 0
                ? "This model already has a PathID. Keep it, remap it, or clear the box/use 0 to remove it."
                : "This model has no PathID yet. Enter one manually, generate a new unused ID, or keep it blank.";
            Controls.Add(statusLabel);

            Button cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Left = 480;
            cancelButton.Top = 155;
            cancelButton.Width = 90;
            cancelButton.DialogResult = DialogResult.Cancel;
            Controls.Add(cancelButton);

            Button applyButton = new Button();
            applyButton.Text = "Apply";
            applyButton.Left = 580;
            applyButton.Top = 155;
            applyButton.Width = 90;
            applyButton.Click += (s, e) => ApplyChanges();
            Controls.Add(applyButton);

            AcceptButton = applyButton;
            CancelButton = cancelButton;
        }

        private void GenerateSuggestedPathId()
        {
            int suggested = GetSuggestedPathId();
            if (suggested <= 0)
            {
                MessageBox.Show(
                    this,
                    "Failed to generate a free PathID.",
                    "Path Editor",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            pathIdBox.Text = suggested.ToString();
            pathIdBox.SelectAll();
            pathIdBox.Focus();
        }

        private int GetSuggestedPathId()
        {
            int maxInDatabase = 0;
            if (database != null && database.pathById != null && database.pathById.Count > 0)
            {
                maxInDatabase = database.pathById.Keys[database.pathById.Count - 1];
            }

            int maxInPathData = assetManager != null ? assetManager.GetMaxPathId() : 0;
            int candidate = Math.Max(maxInDatabase, maxInPathData) + 1;
            if (candidate <= 0)
            {
                candidate = 1;
            }

            while (database != null && database.pathById != null && database.pathById.ContainsKey(candidate))
            {
                candidate++;
            }

            return candidate;
        }

        private void ApplyChanges()
        {
            if (assetManager == null)
            {
                MessageBox.Show(this, "Asset manager unavailable.", "Path Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(canonicalMappedPath))
            {
                MessageBox.Show(this, "Invalid model path.", "Path Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string rawPathId = (pathIdBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(rawPathId) || string.Equals(rawPathId, "0", StringComparison.Ordinal))
            {
                if (!assetManager.TryRemoveWorkspacePathDataEntry(originalPathId, canonicalMappedPath, out string removeError))
                {
                    MessageBox.Show(
                        this,
                        string.IsNullOrWhiteSpace(removeError) ? "Failed to remove PathID from path.data." : removeError,
                        "Path Editor",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                SavedPathId = 0;
                SavedMappedPath = canonicalMappedPath;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            if (!int.TryParse(rawPathId, out int requestedPathId) || requestedPathId <= 0)
            {
                MessageBox.Show(this, "Enter a valid PathID greater than zero, or use 0/blank to remove it.", "Path Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                pathIdBox.Focus();
                pathIdBox.SelectAll();
                return;
            }

            if (!assetManager.TryUpsertWorkspacePathDataEntry(
                    originalPathId,
                    canonicalMappedPath,
                    requestedPathId,
                    canonicalMappedPath,
                    out int savedPathId,
                    out string error))
            {
                MessageBox.Show(
                    this,
                    string.IsNullOrWhiteSpace(error) ? "Failed to save path.data." : error,
                    "Path Editor",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SavedPathId = savedPathId > 0 ? savedPathId : requestedPathId;
            SavedMappedPath = canonicalMappedPath;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
