using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class FieldCompare : Form
    {
        private void click_browseElement(object sender, EventArgs e)
		{
            fieldCompareCoordinatorService.HandleBrowseElement(
                fieldCompareUiService,
                dialogService,
                textBox_ElementPath,
                database,
                Environment.CurrentDirectory,
                this);
            CheckCompareButton(null, null);
		}


        private void click_browseLogDir(object sender, EventArgs e)
		{
            fieldCompareCoordinatorService.HandleBrowseLogDir(
                fieldCompareUiService,
                folderDialogService,
                textBox_LogDir);
		}


        private void click_LoadElement(object sender, EventArgs e)
		{
            fieldCompareCoordinatorService.HandleLoadElement(
                fieldCompareUiService,
                viewModel,
                textBox_ElementPath.Text,
                ref cpb2_prog,
                textBox_ElementPath,
                message => MessageBox.Show(message),
                cursor => Cursor = cursor);
            CheckCompareButton(null, null);
		}


        private void change_list(object sender, EventArgs e)
		{
            fieldCompareCoordinatorService.HandleListChanged(
                fieldCompareUiService,
                viewModel,
                comboBox_List.SelectedIndex,
                dataGridView_fields);
            CheckCompareButton(null, null);
		}


        private void click_compare(object sender, EventArgs e)
		{
            fieldCompareCoordinatorService.HandleCompare(
                fieldCompareUiService,
                viewModel,
                comboBox_List.SelectedIndex,
                textBox_LogDir.Text,
                dataGridView_fields,
                cpb2,
                () =>
                {
                    cpb2.Value++;
                    Application.DoEvents();
                },
                message => MessageBox.Show(message));
		}


        private void CheckCompareButton(object sender, EventArgs e)
		{
            button_compare.Enabled = fieldCompareCoordinatorService.CanCompare(
                fieldCompareUiService,
                viewModel,
                comboBox_List.SelectedIndex,
                dataGridView_fields,
                textBox_ElementPath);
		}


        private void click_close(object sender, EventArgs e)
		{
			this.Close();
		}
    }
}


