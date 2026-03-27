using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private void cellMouseMove_ToolTip(object sender, DataGridViewCellMouseEventArgs e)
		{
            mainWindowTooltipCoordinatorService.HandleCellMouseMove(
                itemTooltipUiService,
                sessionService.ListCollection,
                comboBox_lists.SelectedIndex,
                dataGridView_elems.CurrentCell != null ? dataGridView_elems.CurrentCell.RowIndex : -1,
                dataGridView_item,
                dataGridView_elems,
                e,
                sender as Control,
                toolTip,
                itemTooltipService,
                rowIndex => valueRowIndexService.GetFieldIndexForValueRow(dataGridView_item, rowIndex),
                ref viewModel.MouseMoveCheck);
        }


        private void click_xrefItem(object sender, EventArgs ea)
		{
            int listIndex = comboBox_lists.SelectedIndex;
            int rowIndex = dataGridView_elems.CurrentCell != null ? dataGridView_elems.CurrentCell.RowIndex : -1;
            mainWindowTooltipCoordinatorService.ShowXrefForSelection(
                mainWindowXrefUiService,
                xrefItemUiService,
                sessionService.ListCollection,
                sessionService.Xrefs,
                listIndex,
                rowIndex,
                xrefLookupService,
                xrefLookupUiService,
                message => MessageBox.Show(message));
		}
    }
}




