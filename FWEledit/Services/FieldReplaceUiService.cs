using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class FieldReplaceUiService
    {
        public void OpenFieldReplace(
            ReplaceWindowCommandService replaceWindowCommandService,
            EditorWindowService editorWindowService,
            eListCollection listCollection,
            eListConversation conversationList,
            ref ColorProgressBar.ColorProgressBar progressBar,
            Action<string> showMessage)
        {
            if (replaceWindowCommandService == null)
            {
                return;
            }

            replaceWindowCommandService.OpenFieldReplaceWindow(
                editorWindowService,
                listCollection,
                conversationList,
                ref progressBar,
                showMessage);
        }

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

        public void PopulateFieldCombo(FieldReplaceViewModel viewModel, int listIndex, ComboBox fieldCombo)
        {
            if (fieldCombo == null)
            {
                return;
            }

            fieldCombo.Items.Clear();
            fieldCombo.SelectedIndex = -1;
            if (viewModel == null || viewModel.Source == null || listIndex < 0)
            {
                return;
            }

            for (int i = 0; i < viewModel.Source.Lists[listIndex].elementFields.Length; i++)
            {
                fieldCombo.Items.Add("[" + i + "] - " + viewModel.Source.Lists[listIndex].elementFields[i]);
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
            FieldReplaceViewModel viewModel,
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

        public void ExecuteReplace(
            FieldReplaceViewModel viewModel,
            int listIndex,
            int fieldIndex,
            string logDirectory,
            ColorProgressBar.ColorProgressBar progressBar,
            Action progressTick,
            Action<string> showMessage)
        {
            if (viewModel == null || viewModel.Other == null || listIndex < 0 || fieldIndex < 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(logDirectory))
            {
                return;
            }

            FieldReplaceRequest request = new FieldReplaceRequest
            {
                ListIndex = listIndex,
                FieldIndex = fieldIndex,
                LogDirectory = logDirectory
            };

            if (viewModel.Source != null && listIndex == viewModel.Source.ConversationListIndex && viewModel.SourceConversation != null)
            {
                if (progressBar != null)
                {
                    progressBar.Maximum = viewModel.SourceConversation.talk_proc_count;
                }
            }
            else if (viewModel.Source != null && viewModel.Other != null)
            {
                int lo = listIndex;
                if (viewModel.Source.Version >= 191 && viewModel.Other.Version < 191 && listIndex >= viewModel.Other.ConversationListIndex)
                {
                    lo++;
                }
                if (lo < viewModel.Other.Lists.Length && progressBar != null)
                {
                    progressBar.Maximum = viewModel.Other.Lists[lo].elementValues.Length;
                }
            }

            FieldReplaceResult result = viewModel.Execute(request, progressTick ?? (() => { }));
            if (progressBar != null)
            {
                progressBar.Value = 0;
            }

            if (result != null && !result.Success)
            {
                if (showMessage != null)
                {
                    showMessage(result.Error ?? "Replace failed.");
                }
                return;
            }

            if (showMessage != null && result != null)
            {
                showMessage(result.ReplacedCount + " Values Replaced");
            }
        }

        public bool CanReplace(FieldReplaceViewModel viewModel, int listIndex, int fieldIndex, TextBox elementPath)
        {
            return viewModel != null
                && viewModel.Other != null
                && listIndex > -1
                && fieldIndex > -1
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
