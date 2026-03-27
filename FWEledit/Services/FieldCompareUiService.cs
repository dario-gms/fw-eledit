using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class FieldCompareUiService
    {
        public void PopulateListCombo(eListCollection source, ComboBox listCombo)
        {
            if (listCombo == null)
            {
                return;
            }

            listCombo.Items.Clear();
            if (source == null || source.Lists == null)
            {
                return;
            }

            for (int l = 0; l < source.Lists.Length; l++)
            {
                listCombo.Items.Add("[" + l + "]: " + source.Lists[l].listName + " (" + source.Lists[l].elementValues.Length + ")");
            }
        }

        public void UpdateFieldGrid(FieldCompareViewModel viewModel, int listIndex, DataGridView grid)
        {
            if (grid == null)
            {
                return;
            }

            grid.Rows.Clear();
            if (viewModel == null || viewModel.Source == null || listIndex < 0)
            {
                return;
            }

            for (int i = 0; i < viewModel.Source.Lists[listIndex].elementFields.Length; i++)
            {
                grid.Rows.Add(new string[] { viewModel.Source.Lists[listIndex].elementFields[i], "False" });
                grid.Rows[grid.Rows.Count - 1].HeaderCell.Value = i.ToString();
            }
        }

        public bool BrowseElementFile(IDialogService dialogService, TextBox elementPath, CacheSave database, string initialDirectory, IWin32Window owner)
        {
            if (dialogService == null || elementPath == null)
            {
                return false;
            }

            string filePath = dialogService.ShowOpenFile(
                "Elements File",
                "Elements File (*.data)|*.data|All Files (*.*)|*.*",
                initialDirectory,
                owner);
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            elementPath.Text = filePath;
            ApplyPathTheme(elementPath, database);
            return true;
        }

        public bool BrowseLogDirectory(GameFolderDialogService folderDialogService, TextBox logDir)
        {
            if (folderDialogService == null || logDir == null)
            {
                return false;
            }

            string path = folderDialogService.PromptForGameFolder("Select Log Directory", logDir.Text);
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return false;
            }

            logDir.Text = path;
            return true;
        }

        public bool LoadOtherCollection(
            FieldCompareViewModel viewModel,
            string elementPath,
            ref ColorProgressBar.ColorProgressBar progressBar,
            TextBox elementPathBox,
            Action<string> showMessage)
        {
            if (viewModel == null || string.IsNullOrWhiteSpace(elementPath))
            {
                return false;
            }

            bool loaded = false;
            try
            {
                viewModel.Other = new eListCollection(elementPath, ref progressBar);
                viewModel.OtherConversation = new eListConversation((byte[])viewModel.Other.Lists[viewModel.Other.ConversationListIndex].elementValues[0][0]);
                if (elementPathBox != null)
                {
                    elementPathBox.BackColor = Color.LightGreen;
                }
                loaded = true;
            }
            catch
            {
                if (elementPathBox != null)
                {
                    elementPathBox.BackColor = Color.Salmon;
                }
                if (showMessage != null)
                {
                    showMessage("LOADING ERROR!\nThis error mostly occurs of configuration and elements.data mismatch");
                }
            }
            finally
            {
                if (progressBar != null)
                {
                    progressBar.Value = 0;
                }
            }

            return loaded;
        }

        public void ExecuteCompare(
            FieldCompareViewModel viewModel,
            int listIndex,
            string logDirectory,
            DataGridView grid,
            ColorProgressBar.ColorProgressBar progressBar,
            Action progressTick,
            Action<string> showMessage)
        {
            if (viewModel == null || viewModel.Other == null || grid == null || listIndex < 0)
            {
                return;
            }

            FieldCompareRequest request = new FieldCompareRequest
            {
                ListIndex = listIndex,
                LogDirectory = logDirectory
            };

            for (int i = 0; i < grid.Rows.Count; i++)
            {
                if (grid.Rows[i].Cells[1].Value != null && grid.Rows[i].Cells[1].Value.ToString() == "True")
                {
                    request.ExcludedFieldIndices.Add(i);
                }
            }

            if (progressBar != null && viewModel.Source != null)
            {
                progressBar.Maximum = viewModel.Source.Lists[listIndex].elementValues.Length + 1;
            }

            FieldCompareResult result = viewModel.Execute(request, progressTick ?? (() => { }));
            if (result != null && !result.Success)
            {
                if (showMessage != null)
                {
                    showMessage(result.Error ?? "Compare failed.");
                }
            }
            if (progressBar != null)
            {
                progressBar.Value = 0;
            }
        }

        public bool CanCompare(FieldCompareViewModel viewModel, int listIndex, DataGridView grid, TextBox elementPath)
        {
            return viewModel != null
                && viewModel.Other != null
                && listIndex > -1
                && grid != null
                && grid.RowCount > 0
                && elementPath != null
                && !string.IsNullOrWhiteSpace(elementPath.Text);
        }

        private void ApplyPathTheme(TextBox textBox, CacheSave database)
        {
            if (textBox == null)
            {
                return;
            }

            if (database != null && database.arrTheme != null)
            {
                textBox.BackColor = Color.FromName(database.arrTheme[6]);
            }
            else
            {
                textBox.BackColor = SystemColors.Window;
            }
        }
    }
}
