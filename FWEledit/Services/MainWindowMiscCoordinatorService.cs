using System;
using System.Drawing;

namespace FWEledit
{
    public sealed class MainWindowMiscCoordinatorService
    {
        public Bitmap BuildDdsIcon(
            DdsIconService ddsIconService,
            Bitmap rawImg,
            string rawTxt,
            string icoName)
        {
            if (ddsIconService == null)
            {
                return null;
            }

            return ddsIconService.BuildIcon(rawImg, rawTxt, icoName);
        }

        public void ExportListCounts(
            TextExportDialogService textExportDialogService,
            ListCountExportService listCountExportService,
            ISessionService sessionService,
            string initialDirectory,
            string title,
            string defaultFileName)
        {
            if (textExportDialogService == null || listCountExportService == null || sessionService == null)
            {
                return;
            }

            string filePath = textExportDialogService.PromptForTextExportPath(
                initialDirectory,
                title,
                defaultFileName);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            listCountExportService.ExportListCounts(sessionService.ListCollection, filePath);
        }
    }
}
