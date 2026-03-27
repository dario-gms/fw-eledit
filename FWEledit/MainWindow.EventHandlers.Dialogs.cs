using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private void click_export(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandleExportRules(
                exportRulesCommandService,
                rulesExportUiService,
                elementsRulesExportWorkflowService,
                sessionService,
                sender as ToolStripMenuItem);
		}


        private void click_logicReplace(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandleLogicReplace(
                replaceWindowUiService,
                replaceWindowCommandService,
                editorWindowService,
                sessionService,
                dataGridView_elems,
                () => change_list(null, null),
                message => MessageBox.Show(message));
		}


        private void click_fieldReplace(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandleFieldReplace(
                fieldReplaceUiService,
                replaceWindowCommandService,
                editorWindowService,
                sessionService,
                ref cpb2,
                message => MessageBox.Show(message));
		}


        private void click_info(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandleInfo(
                mainWindowDialogActionsUiService,
                infoDialogUiService,
                sessionService,
                infoMessageService,
                message => MessageBox.Show(message));
		}


        private void click_version(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandleElementsInfo(
                mainWindowDialogActionsUiService,
                infoDialogUiService,
                elementsInfoDialogService,
                elementsFileInfoService,
                dialogService,
                this);
		}


        private void click_config(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandleConfig(
                mainWindowDialogActionsUiService,
                configWindowUiService,
                configWindowCommandService,
                editorWindowService);
		}


        private void click_npcExport(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandleNpcExport(
                mainWindowNpcExportUiService,
                npcExportUiService,
                sessionService,
                textExportDialogService,
                npcExportService,
                Environment.CurrentDirectory,
                cursor => Cursor = cursor,
                message => MessageBox.Show(message));
		}


        private void click_joinEL(object sender, EventArgs e)
        {
            mainWindowDialogsCoordinatorService.HandleJoin(
                mainWindowDialogActionsUiService,
                cursorScopeService,
                cursor => Cursor = cursor,
                elementsJoinUiService,
                elementsJoinWorkflowService,
                sessionService,
                comboBox_lists,
                ref cpb2,
                index => change_list(null, null),
                updated => sessionService.ConversationList = updated,
                message => MessageBox.Show(message),
                warning => MessageBox.Show(warning, " WARNING"));
        }


        private void click_npcAIexport(object sender, EventArgs e)
        {
            mainWindowDialogsCoordinatorService.HandleNpcAiExport(
                mainWindowNpcExportUiService,
                npcExportUiService,
                sessionService,
                textExportDialogService,
                npcAiExportService,
                Environment.CurrentDirectory,
                cursor => Cursor = cursor,
                message => MessageBox.Show(message));
        }


        private void click_skillValidate(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandleSkillValidation(
                mainWindowValidationUiService,
                validationUiService,
                sessionService,
                skillValidationService,
                message => MessageBox.Show(message));
		}


        private void click_propertyValidate(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandlePropertyValidation(
                mainWindowValidationUiService,
                validationUiService,
                sessionService,
                propertyValidationService,
                message => MessageBox.Show(message));
		}


        private void click_tomeValidate(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandleTomeValidation(
                mainWindowValidationUiService,
                validationUiService,
                sessionService,
                tomeValidationService,
                message => MessageBox.Show(message));
		}


        private void click_skillReplace(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandleSkillReplace(
                mainWindowReplaceMappingUiService,
                replaceMappingUiService,
                sessionService,
                Application.StartupPath + "\\replace",
                replaceMappingFileService,
                elementsReplaceService);
		}


        private void click_propertyReplace(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandlePropertyReplace(
                mainWindowReplaceMappingUiService,
                replaceMappingUiService,
                sessionService,
                Application.StartupPath + "\\replace",
                replaceMappingFileService,
                elementsReplaceService);
		}


        private void click_tomeReplace(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandleTomeReplace(
                mainWindowReplaceMappingUiService,
                replaceMappingUiService,
                sessionService,
                Application.StartupPath + "\\replace",
                replaceMappingFileService,
                elementsReplaceService,
                message => MessageBox.Show(message));
		}


        private void click_probabilityValidate(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandleProbabilityValidation(
                mainWindowValidationUiService,
                validationUiService,
                sessionService,
                probabilityValidationService,
                message => MessageBox.Show(message));
		}


        private void click_TaskOverflowCheck(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandleQuestOverflow(
                mainWindowValidationUiService,
                questOverflowUiService,
                sessionService,
                questOverflowService);
		}


        private void click_classMask(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandleClassMask(
                mainWindowDialogActionsUiService,
                classMaskUiService,
                classMaskCommandService,
                editorWindowService);
		}


        private void click_diffEL(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandleRules(
                mainWindowDialogActionsUiService,
                rulesWindowUiService,
                rulesWindowService,
                sessionService,
                ref cpb2);
		}


        private void click_fieldCompare(object sender, EventArgs e)
		{
            mainWindowDialogsCoordinatorService.HandleFieldCompare(
                mainWindowDialogActionsUiService,
                fieldCompareCommandService,
                fieldCompareWorkflowService,
                sessionService,
                ref cpb2,
                message => MessageBox.Show(message));
		}


        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            mainWindowDialogsCoordinatorService.HandleAbout(
                mainWindowDialogActionsUiService,
                aboutWindowCommandService,
                editorWindowService);
        }
    }
}




