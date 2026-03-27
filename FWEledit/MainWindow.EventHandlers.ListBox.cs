using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private void listBox_items_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int listIndex = comboBox_lists.SelectedIndex;
            int rowIndex = dataGridView_elems.CurrentCell != null ? dataGridView_elems.CurrentCell.RowIndex : -1;
            mainWindowListBoxCoordinatorService.HandleCopySelectedElement(
                mainWindowXrefUiService,
                clipboardUiService,
                elementClipboardService,
                sessionService,
                listIndex,
                rowIndex,
                message => MessageBox.Show(message),
                text => Clipboard.SetDataObject(text, true));
        }


        private void listBox_items_KeyDown(object sender, KeyEventArgs e)
		{
			//if (ModifierKeys == Keys.Control && listBox_items.SelectedIndices.Count > 0 && comboBox_lists.SelectedIndex != viewModel.Session.ListCollection.ConversationListIndex)
			//{
			//	if (e.KeyCode == Keys.Up)
			//	{
			//		if (listBox_items.SelectedIndices[0] > 0)
			//		{
			//			viewModel.EnableSelectionItem = false;
			//			int[] SelectedIndices = new int[listBox_items.SelectedIndices.Count];
			//			for (int i = 0; i < listBox_items.SelectedIndices.Count; i++)
			//			{
			//				SelectedIndices[i] = listBox_items.SelectedIndices[i];
			//			}
			//			int pos = -1;
			//			for (int i = 0; i < viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementFields.Length; i++)
			//			{
			//				if (string.Equals(viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementFields[i], "Name", StringComparison.OrdinalIgnoreCase))
			//				{
			//					pos = i;
			//					break;
			//				}
			//			}
			//			for (int i = 0; i < listBox_items.SelectedIndices.Count; i++)
			//			{
			//				object[][] temp = new object[viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementValues.Length][];
			//				Array.Copy(viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementValues, 0, temp, 0, listBox_items.SelectedIndices[i] - 1);
			//				Array.Copy(viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementValues, listBox_items.SelectedIndices[i], temp, listBox_items.SelectedIndices[i] - 1, 1);
			//				Array.Copy(viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementValues, listBox_items.SelectedIndices[i] - 1, temp, listBox_items.SelectedIndices[i], 1);
			//				if (listBox_items.SelectedIndices[i] < listBox_items.Items.Count - 1)
			//					Array.Copy(viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementValues, listBox_items.SelectedIndices[i] + 1, temp, listBox_items.SelectedIndices[i] + 1, listBox_items.Items.Count - listBox_items.SelectedIndices[i] - 1);
			//				viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementValues = temp;
			//				int ei = SelectedIndices[i] - 1;
			//				int ei2 = SelectedIndices[i];
			//				if (string.Equals(viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementFields[0], "ID", StringComparison.OrdinalIgnoreCase))
			//				{
			//					listBox_items.Items[ei] = "[" + ei + "]: " + viewModel.Session.ListCollection.GetValue(comboBox_lists.SelectedIndex, ei, 0) + " - " + viewModel.Session.ListCollection.GetValue(comboBox_lists.SelectedIndex, ei, pos);
			//					listBox_items.Items[ei2] = "[" + ei2 + "]: " + viewModel.Session.ListCollection.GetValue(comboBox_lists.SelectedIndex, ei2, 0) + " - " + viewModel.Session.ListCollection.GetValue(comboBox_lists.SelectedIndex, ei2, pos);
			//				}
			//				else
			//				{
			//					listBox_items.Items[ei] = "[" + ei + "]: " + viewModel.Session.ListCollection.GetValue(comboBox_lists.SelectedIndex, ei, pos);
			//					listBox_items.Items[ei2] = "[" + ei2 + "]: " + viewModel.Session.ListCollection.GetValue(comboBox_lists.SelectedIndex, ei2, pos);
			//				}
			//			}
			//			listBox_items.SelectedIndex = -1;
			//			listBox_items.SelectionMode = SelectionMode.MultiSimple;
			//			for (int i = 0; i < SelectedIndices.Length; i++)
			//			{
			//				listBox_items.SelectedIndex = SelectedIndices[i] - 1;
			//			}
			//			listBox_items.SelectionMode = SelectionMode.MultiExtended;
			//			viewModel.EnableSelectionItem = true;
			//			change_item(null, null);
			//		}
			//	}
			//	else if (e.KeyCode == Keys.Down)
			//	{
			//		if (listBox_items.SelectedIndices[listBox_items.SelectedIndices.Count - 1] < listBox_items.Items.Count - 1)
			//		{
			//			viewModel.EnableSelectionItem = false;
			//			int[] SelectedIndices = new int[listBox_items.SelectedIndices.Count];
			//			for (int i = 0; i < listBox_items.SelectedIndices.Count; i++)
			//			{
			//				SelectedIndices[i] = listBox_items.SelectedIndices[i];
			//			}
			//			int pos = -1;
			//			for (int i = 0; i < viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementFields.Length; i++)
			//			{
			//				if (string.Equals(viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementFields[i], "Name", StringComparison.OrdinalIgnoreCase))
			//				{
			//					pos = i;
			//					break;
			//				}
			//			}
			//			for (int i = listBox_items.SelectedIndices.Count - 1; i > -1; i--)
			//			{
			//				object[][] temp = new object[viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementValues.Length][];
			//				Array.Copy(viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementValues, 0, temp, 0, listBox_items.SelectedIndices[i]);
			//				Array.Copy(viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementValues, listBox_items.SelectedIndices[i] + 1, temp, listBox_items.SelectedIndices[i], 1);
			//				Array.Copy(viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementValues, listBox_items.SelectedIndices[i], temp, listBox_items.SelectedIndices[i] + 1, 1);
			//				if (listBox_items.SelectedIndices[i] < listBox_items.Items.Count - 2)
			//					Array.Copy(viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementValues, listBox_items.SelectedIndices[i] + 2, temp, listBox_items.SelectedIndices[i] + 2, listBox_items.Items.Count - listBox_items.SelectedIndices[i] - 2);
			//				viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementValues = temp;
			//				int ei = SelectedIndices[i] + 1;
			//				int ei2 = SelectedIndices[i];
			//				if (string.Equals(viewModel.Session.ListCollection.Lists[comboBox_lists.SelectedIndex].elementFields[0], "ID", StringComparison.OrdinalIgnoreCase))
			//				{
			//					listBox_items.Items[ei] = "[" + ei + "]: " + viewModel.Session.ListCollection.GetValue(comboBox_lists.SelectedIndex, ei, 0) + " - " + viewModel.Session.ListCollection.GetValue(comboBox_lists.SelectedIndex, ei, pos);
			//					listBox_items.Items[ei2] = "[" + ei2 + "]: " + viewModel.Session.ListCollection.GetValue(comboBox_lists.SelectedIndex, ei2, 0) + " - " + viewModel.Session.ListCollection.GetValue(comboBox_lists.SelectedIndex, ei2, pos);
			//				}
			//				else
			//				{
			//					listBox_items.Items[ei] = "[" + ei + "]: " + viewModel.Session.ListCollection.GetValue(comboBox_lists.SelectedIndex, ei, pos);
			//					listBox_items.Items[ei2] = "[" + ei2 + "]: " + viewModel.Session.ListCollection.GetValue(comboBox_lists.SelectedIndex, ei2, pos);
			//				}
			//			}
			//			listBox_items.SelectedIndex = -1;
			//			listBox_items.SelectionMode = SelectionMode.MultiSimple;
			//			for (int i = 0; i < SelectedIndices.Length; i++)
			//			{
			//				listBox_items.SelectedIndex = SelectedIndices[i] + 1;
			//			}
			//			listBox_items.SelectionMode = SelectionMode.MultiExtended;
			//			viewModel.EnableSelectionItem = true;
			//			change_item(null, null);
			//		}
			//	}
			//}
		}


        private void listBox_items_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            mainWindowListBoxCoordinatorService.HandleListHover(
                listItemHoverTooltipService,
                sessionService,
                comboBox_lists.SelectedIndex,
                dataGridView_elems,
                e,
                listItemTooltipService,
                ref viewModel.CustomTooltype);
        }
    }
}




