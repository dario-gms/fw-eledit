using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class FieldReplaceWindow : Form
    {
        private void click_browseElement(object sender, EventArgs e)
		{
            fieldReplaceCoordinatorService.HandleBrowseElement(
                fieldReplaceUiService,
                dialogService,
                textBox_ElementPath,
                database,
                Environment.CurrentDirectory,
                this);
            CheckReplaceButton(null, null);
		}


        private void click_browseLogDir(object sender, EventArgs e)
		{
            fieldReplaceCoordinatorService.HandleBrowseLogDir(
                fieldReplaceUiService,
                folderDialogService,
                textBox_LogDir);
		}


        private void click_LoadElement(object sender, EventArgs e)
		{
            fieldReplaceCoordinatorService.HandleLoadElement(
                fieldReplaceUiService,
                viewModel,
                textBox_ElementPath.Text,
                ref cpb2_prog,
                textBox_ElementPath,
                message => MessageBox.Show(message),
                cursor => Cursor = cursor);
            CheckReplaceButton(null, null);
		}


        private void change_list(object sender, EventArgs e)
		{
            fieldReplaceCoordinatorService.HandleListChanged(
                fieldReplaceUiService,
                viewModel,
                comboBox_List.SelectedIndex,
                comboBox_Field);
            CheckReplaceButton(null, null);
		}


        private void click_replace(object sender, EventArgs e)
		{
            fieldReplaceCoordinatorService.HandleReplace(
                fieldReplaceUiService,
                viewModel,
                comboBox_List.SelectedIndex,
                comboBox_Field.SelectedIndex,
                textBox_LogDir.Text,
                cpb2,
                () =>
                {
                    cpb2.Value++;
                    Application.DoEvents();
                },
                message => MessageBox.Show(message));
		}


        private void CheckReplaceButton(object sender, EventArgs e)
		{
            button_replace.Enabled = fieldReplaceCoordinatorService.CanReplace(
                fieldReplaceUiService,
                viewModel,
                comboBox_List.SelectedIndex,
                comboBox_Field.SelectedIndex,
                textBox_ElementPath);
		}


        private void click_close(object sender, EventArgs e)
		{
			this.Close();
		}
    }
}


