using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowNpcExportUiService
    {
        public void ExportNpcNames(
            NpcExportUiService exportUiService,
            eListCollection listCollection,
            TextExportDialogService dialogService,
            NpcExportService exportService,
            string initialDirectory,
            Action<Cursor> setCursor,
            Action<string> showMessage)
        {
            if (exportUiService == null)
            {
                return;
            }

            exportUiService.ExportNpcNames(
                listCollection,
                dialogService,
                exportService,
                initialDirectory,
                setCursor,
                showMessage);
        }

        public void ExportNpcAi(
            NpcExportUiService exportUiService,
            eListCollection listCollection,
            TextExportDialogService dialogService,
            NpcAiExportService exportService,
            string initialDirectory,
            Action<Cursor> setCursor,
            Action<string> showMessage)
        {
            if (exportUiService == null)
            {
                return;
            }

            exportUiService.ExportNpcAi(
                listCollection,
                dialogService,
                exportService,
                initialDirectory,
                setCursor,
                showMessage);
        }
    }
}
