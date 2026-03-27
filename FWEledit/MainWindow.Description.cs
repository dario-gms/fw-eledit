using System;
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


