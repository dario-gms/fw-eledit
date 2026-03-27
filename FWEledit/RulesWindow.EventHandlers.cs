using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class RulesWindow : Form
    {
        private void click_browse(object sender, EventArgs e)
		{
            bool isBase = sender == (object)button_browseBase;
            rulesWindowCoordinatorService.HandleBrowse(
                dialogService,
                rulesWindowActionsService,
                viewModel,
                Environment.CurrentDirectory,
                this,
                dataGridView_fields,
                dataGridView_values,
                comboBox_lists,
                Column1,
                Column2,
                Column7,
                Column8,
                isBase,
                ref cpb2_prog,
                textBox_baseFile,
                textBox_baseVersion,
                textBox_recentFile,
                textBox_recentVersion,
                cursor => Cursor = cursor);
		}


        private void change_list(object sender, EventArgs e)
		{
            rulesWindowCoordinatorService.HandleListChanged(
                rulesWindowActionsService,
                viewModel,
                comboBox_lists.SelectedIndex,
                dataGridView_fields,
                dataGridView_values,
                Column1,
                Column2,
                Column7,
                Column8,
                checkBox_removeList,
                radioButton_baseOffset,
                radioButton_recentOffset,
                textBox_baseOffset,
                textBox_recentOffset,
                cursor => Cursor = cursor);
		}


        private void check_removeList(object sender, EventArgs e)
		{
            rulesWindowCoordinatorService.HandleRemoveListChanged(
                rulesWindowActionsService,
                viewModel,
                comboBox_lists.SelectedIndex,
                checkBox_removeList.Checked);
		}


        private void check_offset(object sender, EventArgs e)
		{
            rulesWindowCoordinatorService.HandleOffsetChanged(
                rulesWindowActionsService,
                viewModel,
                comboBox_lists.SelectedIndex,
                radioButton_baseOffset.Checked);
		}


        private void change_field(object sender, DataGridViewCellEventArgs e)
		{
		}


        private void click_field(object sender, DataGridViewCellEventArgs e)
		{
            rulesWindowCoordinatorService.HandleFieldClick(
                rulesWindowActionsService,
                viewModel,
                comboBox_lists.SelectedIndex,
                dataGridView_fields,
                dataGridView_values,
                Column1,
                Column2,
                Column7,
                Column8,
                e,
                checkBox_removeList,
                radioButton_baseOffset,
                radioButton_recentOffset,
                textBox_baseOffset,
                textBox_recentOffset,
                cursor => Cursor = cursor);
		}


        private void click_viewRules(object sender, EventArgs e)
		{
            rulesWindowCoordinatorService.HandleShowRules(rulesWindowActionsService, viewModel);
		}


        private void click_importRules(object sender, EventArgs e)
		{
            rulesWindowCoordinatorService.HandleImportRules(
                dialogService,
                rulesWindowActionsService,
                viewModel,
                Environment.CurrentDirectory,
                this,
                () => change_list(null, null),
                cursor => Cursor = cursor);
		}


        private void click_exportRules(object sender, EventArgs e)
		{
            rulesWindowCoordinatorService.HandleExportRules(
                dialogService,
                rulesWindowActionsService,
                viewModel,
                Environment.CurrentDirectory,
                this,
                cursor => Cursor = cursor);
		}
    }
}

