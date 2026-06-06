using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private void ApplyItemDescriptionRuntime(string[] data)
        {
            mainWindowDescriptionCoordinatorService.ApplyItemDescriptionRuntime(
                descriptionRuntimeBridgeService,
                sessionService,
                data,
                sessionService.Database);
        }

        private void LoadItemDescriptionsFromConfigs()
        {
            mainWindowDescriptionCoordinatorService.LoadItemDescriptionsFromConfigs(
                mainWindowDescriptionUiService,
                descriptionLoadUiService,
                descriptionLoadService,
                viewModel,
                itemDescriptionFileService,
                descriptionRuntimeService,
                AssetManager.GameRootPath,
                AssetManager.WorkspaceRootPath,
                status =>
                {
                    if (fwDescriptionStatusLabel != null)
                    {
                        fwDescriptionStatusLabel.Text = status;
                    }
                },
                ApplyItemDescriptionRuntime);
        }

        private void UpdateDescriptionTabForSelection()
        {
            mainWindowDescriptionCoordinatorService.UpdateDescriptionTabForSelection(
                mainWindowDescriptionUiService,
                sessionService.ListCollection,
                comboBox_lists,
                dataGridView_elems,
                fwDescriptionEditor,
                viewModel,
                descriptionUiService,
                descriptionWorkflowService,
                id => Extensions.ItemDesc(sessionService, id),
                RenderDescriptionPreview);
        }

        private void fw_description_changed(object sender, EventArgs e)
        {
            mainWindowDescriptionCoordinatorService.HandleDescriptionChanged(
                mainWindowDescriptionUiService,
                descriptionUiService,
                viewModel,
                fwDescriptionEditor,
                fwDescriptionPreview,
                RenderDescriptionPreview,
                () => StageCurrentDescriptionChange(false));
        }

        private void StageCurrentDescriptionChange(bool updateStatus)
        {
            mainWindowDescriptionCoordinatorService.StageCurrentDescriptionChange(
                mainWindowDescriptionUiService,
                descriptionUiService,
                viewModel,
                descriptionWorkflowService,
                descriptionLoadService,
                descriptionRuntimeService,
                fwDescriptionEditor != null ? fwDescriptionEditor.Text : string.Empty,
                ApplyItemDescriptionRuntime,
                () => viewModel.HasUnsavedChanges = true,
                () => comboBox_lists.SelectedIndex,
                () => dataGridView_elems.CurrentCell != null ? dataGridView_elems.CurrentCell.RowIndex : -1,
                (listIndex, rowIndex) => mainWindowDirtyTrackingService.MarkRowDirty(
                    dirtyStateTracker,
                    listDisplayService,
                    ref viewModel.HasUnsavedChanges,
                    listIndex,
                    rowIndex),
                updateStatus,
                status =>
                {
                    if (fwDescriptionStatusLabel != null)
                    {
                        fwDescriptionStatusLabel.Text = status;
                    }
                });
        }

        private bool FlushPendingDescriptionsToDisk()
        {
            return mainWindowDescriptionCoordinatorService.FlushPendingDescriptionsToDisk(
                mainWindowDescriptionUiService,
                descriptionFlushUiService,
                descriptionWorkflowService,
                descriptionLoadService,
                descriptionRuntimeService,
                viewModel,
                sessionService.AssetManager,
                message => MessageBox.Show(message),
                status =>
                {
                    if (fwDescriptionStatusLabel != null)
                    {
                        fwDescriptionStatusLabel.Text = status;
                    }
                },
                ApplyItemDescriptionRuntime);
        }
        private void RemapDescriptionIdIfNeeded(int oldId, int newId)
        {
            mainWindowDescriptionCoordinatorService.RemapDescriptionIdIfNeeded(
                descriptionIdRemapService,
                viewModel.DescriptionViewModel,
                descriptionLoadService,
                descriptionRuntimeService,
                oldId,
                newId,
                ApplyItemDescriptionRuntime,
                () => viewModel.HasUnsavedChanges = true);
        }

        private void RenderDescriptionPreview(string rawText)
        {
            mainWindowDescriptionCoordinatorService.RenderDescriptionPreview(
                mainWindowDescriptionUiService,
                descriptionPreviewUiService,
                descriptionPreviewService,
                fwDescriptionPreview,
                rawText);
        }

        private void InitializeDescriptionFormattingActions()
        {
            if (fwDescriptionColorButton != null)
            {
                fwDescriptionColorButton.Click += click_description_color;
            }
            if (fwDescriptionLineBreakButton != null)
            {
                fwDescriptionLineBreakButton.Click += (s, e) => InsertDescriptionText(Environment.NewLine);
            }
            if (fwDescriptionNormalFontButton != null)
            {
                fwDescriptionNormalFontButton.Click += (s, e) => InsertDescriptionTag("^O053", false);
            }
            if (fwDescriptionSmallFontButton != null)
            {
                fwDescriptionSmallFontButton.Click += (s, e) => InsertDescriptionTag("^O005", "^O053");
            }
            if (fwDescriptionTitleFontButton != null)
            {
                fwDescriptionTitleFontButton.Click += (s, e) => InsertDescriptionTag("^O057", "^O053");
            }
        }

        private void click_description_color(object sender, EventArgs e)
        {
            using (ColorDialog dialog = new ColorDialog())
            {
                dialog.AllowFullOpen = true;
                dialog.FullOpen = true;
                dialog.Color = Color.White;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                InsertDescriptionTag("^" + ToFwColorTag(dialog.Color), true);
            }
        }

        private void InsertDescriptionTag(string tag, bool resetAfterSelection)
        {
            InsertDescriptionTag(tag, resetAfterSelection ? "^FFFFFF" : string.Empty);
        }

        private void InsertDescriptionTag(string tag, string resetTag)
        {
            if (fwDescriptionEditor == null || string.IsNullOrEmpty(tag))
            {
                return;
            }

            string selectedText = fwDescriptionEditor.SelectedText ?? string.Empty;
            if (selectedText.Length > 0)
            {
                string reset = resetTag ?? string.Empty;
                fwDescriptionEditor.SelectedText = tag + selectedText + reset;
            }
            else
            {
                fwDescriptionEditor.SelectedText = tag;
            }

            fwDescriptionEditor.Focus();
        }

        private void InsertDescriptionText(string text)
        {
            if (fwDescriptionEditor == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            fwDescriptionEditor.SelectedText = text;
            fwDescriptionEditor.Focus();
        }

        private static string ToFwColorTag(Color color)
        {
            return color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }

        private void click_save_description(object sender, EventArgs e)
        {
            mainWindowDescriptionCoordinatorService.TrySaveCurrentDescription(
                mainWindowDescriptionUiService,
                descriptionUiService,
                viewModel,
                () => StageCurrentDescriptionChange(true),
                message => MessageBox.Show(message));
        }
    }
}


