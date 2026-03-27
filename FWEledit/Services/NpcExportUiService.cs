using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class NpcExportUiService
    {
        public void ExportNpcNames(
            eListCollection listCollection,
            TextExportDialogService dialogService,
            NpcExportService exportService,
            string initialDirectory,
            Action<Cursor> setCursor,
            Action<string> showMessage)
        {
            Export(
                listCollection,
                dialogService,
                initialDirectory,
                setCursor,
                showMessage,
                exportService != null ? new Func<eListCollection, string, FileExportResult>(exportService.ExportNpcNames) : null);
        }

        public void ExportNpcAi(
            eListCollection listCollection,
            TextExportDialogService dialogService,
            NpcAiExportService exportService,
            string initialDirectory,
            Action<Cursor> setCursor,
            Action<string> showMessage)
        {
            Export(
                listCollection,
                dialogService,
                initialDirectory,
                setCursor,
                showMessage,
                exportService != null ? new Func<eListCollection, string, FileExportResult>(exportService.ExportNpcAi) : null);
        }

        private static void Export(
            eListCollection listCollection,
            TextExportDialogService dialogService,
            string initialDirectory,
            Action<Cursor> setCursor,
            Action<string> showMessage,
            Func<eListCollection, string, FileExportResult> exportAction)
        {
            if (listCollection == null || dialogService == null || exportAction == null)
            {
                return;
            }

            string filePath = dialogService.PromptForTextExportPath(initialDirectory, null, null);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            if (setCursor != null)
            {
                setCursor(Cursors.AppStarting);
            }

            FileExportResult result = exportAction(listCollection, filePath);

            if (setCursor != null)
            {
                setCursor(Cursors.Default);
            }

            if (!result.Success && showMessage != null)
            {
                showMessage(result.ErrorMessage ?? "SAVING ERROR!");
            }
        }
    }
}
