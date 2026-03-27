using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class ConfigWindow : Form
    {
        private void click_load(object sender, EventArgs e)
		{
            configWindowCoordinatorService.HandleLoad(
                configFileLoadUiService,
                dialogService,
                viewModel,
                comboBox_lists,
                textBox_conversationListIndex,
                Application.StartupPath + "\\configs",
                this);
		}


        private void click_save(object sender, EventArgs e)
		{
            configWindowCoordinatorService.HandleSave(
                configFileSaveUiService,
                dialogService,
                viewModel,
                Data,
                Application.StartupPath + "\\configs",
                this);
		}


        private void change_list(object sender, EventArgs e)
		{
			configWindowCoordinatorService.HandleListChanged(
                configListDisplayUiService,
                Data,
                comboBox_lists.SelectedIndex,
                textBox_listName,
                textBox_offset,
                dataGridView_item);
		}


        private void change_row(object sender, DataGridViewCellEventArgs e)
		{
            configWindowCoordinatorService.HandleRowChanged(
                configRowUpdateUiService,
                Data,
                comboBox_lists.SelectedIndex,
                e,
                dataGridView_item,
                dialogService,
                this);
		}


        private void add_row(object sender, EventArgs e)
		{
            configWindowCoordinatorService.HandleAddRow(
                configRowInsertUiService,
                Data,
                comboBox_lists.SelectedIndex,
                dataGridView_item,
                () => change_list(null, null));
		}


        private void copy_row(object sender, EventArgs e)
		{
            configWindowCoordinatorService.HandleCopyRow(
                configRowCopyService,
                Data,
                comboBox_lists.SelectedIndex,
                dataGridView_item,
                gridCellSelectionService,
                (names, types) =>
                {
                    viewModel.CopiedFieldNames = names;
                    viewModel.CopiedFieldTypes = types;
                });
		}


        private void addCopied_row(object sender, EventArgs e)
		{
            configWindowCoordinatorService.HandlePasteRow(
                configRowPasteUiService,
                Data,
                comboBox_lists.SelectedIndex,
                dataGridView_item,
                viewModel.CopiedFieldNames,
                viewModel.CopiedFieldTypes,
                () => change_list(null, null));
		}


        private void delete_row(object sender, EventArgs e)
		{
            configWindowCoordinatorService.HandleDeleteRow(
                configRowDeleteUiService,
                Data,
                comboBox_lists.SelectedIndex,
                dataGridView_item,
                gridCellSelectionService,
                () => change_list(null, null));
		}


        private void click_addList(object sender, EventArgs e)
		{
            configWindowCoordinatorService.HandleAddList(
                configListMutationUiService,
                Data,
                comboBox_lists);
		}


        private void click_deleteList(object sender, EventArgs e)
		{
            configWindowCoordinatorService.HandleDeleteList(
                configListMutationUiService,
                Data,
                comboBox_lists,
                dataGridView_item);
		}


        private void click_SetValueSelectedNames(object sender, EventArgs e)
		{
            configWindowCoordinatorService.HandleSetSelectedNames(
                configFieldSetUiService,
                Data,
                dataGridView_item,
                textBox_SetName.Text);
		}


        private void click_SetValueSelectedTypes(object sender, EventArgs e)
		{
            configWindowCoordinatorService.HandleSetSelectedTypes(
                configFieldSetUiService,
                Data,
                dataGridView_item,
                textBox_SetType.Text);
		}


        private void change_conversationListIndex(object sender, EventArgs e)
		{
            configWindowCoordinatorService.HandleConversationIndexChanged(
                configListUpdateUiService,
                Data,
                textBox_conversationListIndex.Text);
		}


        private void change_listName(object sender, EventArgs e)
		{
            configWindowCoordinatorService.HandleListNameChanged(
                configListUpdateUiService,
                Data,
                comboBox_lists,
                textBox_listName.Text);
		}


        private void change_listOffset(object sender, EventArgs e)
		{
            configWindowCoordinatorService.HandleListOffsetChanged(
                configListUpdateUiService,
                Data,
                comboBox_lists.SelectedIndex,
                textBox_offset.Text);
		}


        private void click_scanSequel(object sender, EventArgs e)
		{
            configWindowCoordinatorService.HandleScanSequel(
                configSequelScannerService,
                Data,
                Application.StartupPath + "\\configs",
                this);
		}
    }
}

