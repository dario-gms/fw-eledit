using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        public Bitmap ddsIcon(Bitmap rawImg, string rawTxt, string icoName)
        {
            return mainWindowMiscCoordinatorService.BuildDdsIcon(ddsIconService, rawImg, rawTxt, icoName);
        }


        private void createListWithCountsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainWindowMiscCoordinatorService.ExportListCounts(
                textExportDialogService,
                listCountExportService,
                sessionService,
                Environment.CurrentDirectory,
                "Save List Count File",
                "elements.list.count");
        }
    }
}




