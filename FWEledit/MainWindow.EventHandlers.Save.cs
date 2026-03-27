using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private void click_save(object sender, EventArgs e)
		{
            mainWindowSaveCoordinatorService.SaveSession(
                mainWindowSaveUiService,
                saveSessionUiService,
                elementsSessionService,
                saveContextBuilderService,
                sessionService,
                string.Empty,
                saveProgressUiService,
                saveProgressService,
                cpb2,
                ValidateUniqueIdsBeforeSave,
                FlushPendingDescriptionsToDisk,
                ClearDirtyTrackingAfterSave,
                message => MessageBox.Show(message),
                message => MessageBox.Show(message),
                LogError,
                CaptureNavigationSnapshot,
                RestoreNavigationSnapshot,
                () => savePathService.PromptElementsSavePath(Environment.CurrentDirectory, this),
                path => { viewModel.ElementsPath = path ?? viewModel.ElementsPath; },
                summary =>
                {
                    if (fwDescriptionStatusLabel != null)
                    {
                        fwDescriptionStatusLabel.Text = summary;
                    }
                },
                ShowSaveConfirmation);
		}


        private void click_save2(object sender, EventArgs e)
		{
            mainWindowSaveCoordinatorService.SaveSession(
                mainWindowSaveUiService,
                saveSessionUiService,
                elementsSessionService,
                saveContextBuilderService,
                sessionService,
                viewModel.ElementsPath,
                saveProgressUiService,
                saveProgressService,
                cpb2,
                ValidateUniqueIdsBeforeSave,
                FlushPendingDescriptionsToDisk,
                ClearDirtyTrackingAfterSave,
                message => MessageBox.Show(message),
                message => MessageBox.Show(message),
                LogError,
                CaptureNavigationSnapshot,
                RestoreNavigationSnapshot,
                () => savePathService.PromptElementsSavePath(Environment.CurrentDirectory, this),
                path => { viewModel.ElementsPath = path ?? viewModel.ElementsPath; },
                summary =>
                {
                    if (fwDescriptionStatusLabel != null)
                    {
                        fwDescriptionStatusLabel.Text = summary;
                    }
                },
                ShowSaveConfirmation);
		}
    }
}




