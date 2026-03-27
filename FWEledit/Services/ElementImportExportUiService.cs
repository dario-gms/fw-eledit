using System;
using System.IO;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementImportExportUiService
    {
        public bool ValidateNotConversationList(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null)
            {
                return false;
            }

            if (listIndex == listCollection.ConversationListIndex)
            {
                MessageBox.Show("Operation not supported in List " + listCollection.ConversationListIndex.ToString());
                return false;
            }

            return true;
        }

        public void ExportSelectedItems(
            ElementImportExportWorkflowService workflow,
            eListCollection listCollection,
            int listIndex,
            int[] selectedIndices,
            ColorProgressBar.ColorProgressBar progressBar)
        {
            if (workflow == null || listCollection == null || selectedIndices == null || selectedIndices.Length == 0)
            {
                return;
            }

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK || !Directory.Exists(dialog.SelectedPath))
                {
                    return;
                }

                Cursor.Current = Cursors.AppStarting;
                if (progressBar != null)
                {
                    progressBar.Maximum = selectedIndices.Length;
                    progressBar.Value = 0;
                }

                ElementExportResult result = workflow.ExportItems(
                    listCollection,
                    listIndex,
                    selectedIndices,
                    dialog.SelectedPath,
                    value =>
                    {
                        if (progressBar != null)
                        {
                            progressBar.Value = value;
                        }
                    });

                Cursor.Current = Cursors.Default;
                if (!result.Success)
                {
                    if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    {
                        MessageBox.Show(result.ErrorMessage);
                    }
                    if (progressBar != null)
                    {
                        progressBar.Value = 0;
                    }
                    return;
                }

                MessageBox.Show("Export complete!");
                if (progressBar != null)
                {
                    progressBar.Value = 0;
                }
            }
        }

        public bool ImportSingleItem(
            ElementImportExportWorkflowService workflow,
            eListCollection listCollection,
            int listIndex,
            int elementIndex)
        {
            if (workflow == null || listCollection == null)
            {
                return false;
            }

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "All Files (*.*)|*.*";
                if (dialog.ShowDialog() != DialogResult.OK || !File.Exists(dialog.FileName))
                {
                    return false;
                }

                Cursor.Current = Cursors.AppStarting;
                ElementImportResult result = workflow.ImportItem(listCollection, listIndex, elementIndex, dialog.FileName);
                Cursor.Current = Cursors.Default;
                if (!result.Success)
                {
                    if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    {
                        MessageBox.Show(result.ErrorMessage);
                    }
                    return false;
                }
            }

            return true;
        }

        public ElementBatchAddResult ImportMultipleItems(
            ElementImportExportWorkflowService workflow,
            eListCollection listCollection,
            int listIndex,
            ColorProgressBar.ColorProgressBar progressBar)
        {
            if (workflow == null || listCollection == null)
            {
                return new ElementBatchAddResult();
            }

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "All Files (*.*)|*.*";
                dialog.Multiselect = true;
                if (dialog.ShowDialog() != DialogResult.OK || dialog.FileNames == null || dialog.FileNames.Length == 0 || !File.Exists(dialog.FileName))
                {
                    return new ElementBatchAddResult();
                }

                Cursor.Current = Cursors.AppStarting;
                if (progressBar != null)
                {
                    progressBar.Maximum = dialog.FileNames.Length;
                    progressBar.Value = 0;
                }

                ElementBatchAddResult result = workflow.AddItemsFromFiles(
                    listCollection,
                    listIndex,
                    dialog.FileNames,
                    value =>
                    {
                        if (progressBar != null)
                        {
                            progressBar.Value = value;
                        }
                    });

                Cursor.Current = Cursors.Default;
                if (!result.Success)
                {
                    if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    {
                        MessageBox.Show(result.ErrorMessage);
                    }
                    if (progressBar != null)
                    {
                        progressBar.Value = 0;
                    }
                    return result;
                }

                if (progressBar != null)
                {
                    progressBar.Value = 0;
                }

                return result;
            }
        }
    }
}
