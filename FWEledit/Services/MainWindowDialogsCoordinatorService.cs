using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowDialogsCoordinatorService
    {
        public void HandleExportRules(
            ExportRulesCommandService exportRulesCommandService,
            RulesExportUiService rulesExportUiService,
            ElementsRulesExportWorkflowService workflowService,
            ISessionService sessionService,
            ToolStripMenuItem sender)
        {
            if (exportRulesCommandService == null || sessionService == null)
            {
                return;
            }

            exportRulesCommandService.ExportRules(
                rulesExportUiService,
                workflowService,
                sessionService.ListCollection,
                sender);
        }

        public void HandleLogicReplace(
            ReplaceWindowUiService replaceWindowUiService,
            ReplaceWindowCommandService replaceWindowCommandService,
            EditorWindowService editorWindowService,
            ISessionService sessionService,
            DataGridView elementsGrid,
            Action refreshList,
            Action<string> showMessage)
        {
            if (replaceWindowUiService == null || sessionService == null)
            {
                return;
            }

            replaceWindowUiService.OpenReplaceWithSelectionRestore(
                replaceWindowCommandService,
                editorWindowService,
                sessionService.ListCollection,
                elementsGrid,
                refreshList,
                showMessage);
        }

        public void HandleFieldReplace(
            FieldReplaceUiService fieldReplaceUiService,
            ReplaceWindowCommandService replaceWindowCommandService,
            EditorWindowService editorWindowService,
            ISessionService sessionService,
            ref ColorProgressBar.ColorProgressBar progressBar,
            Action<string> showMessage)
        {
            if (fieldReplaceUiService == null || sessionService == null)
            {
                return;
            }

            fieldReplaceUiService.OpenFieldReplace(
                replaceWindowCommandService,
                editorWindowService,
                sessionService.ListCollection,
                sessionService.ConversationList,
                ref progressBar,
                showMessage);
        }

        public void HandleInfo(
            MainWindowDialogActionsUiService dialogActionsUiService,
            InfoDialogUiService infoDialogUiService,
            ISessionService sessionService,
            InfoMessageService infoMessageService,
            Action<string> showMessage)
        {
            if (dialogActionsUiService == null || sessionService == null)
            {
                return;
            }

            dialogActionsUiService.ShowInfo(
                infoDialogUiService,
                sessionService.ListCollection,
                infoMessageService,
                showMessage);
        }

        public void HandleElementsInfo(
            MainWindowDialogActionsUiService dialogActionsUiService,
            InfoDialogUiService infoDialogUiService,
            ElementsInfoDialogService elementsInfoDialogService,
            ElementsFileInfoService elementsFileInfoService,
            DialogService dialogService,
            IWin32Window owner)
        {
            if (dialogActionsUiService == null)
            {
                return;
            }

            dialogActionsUiService.ShowElementsInfo(
                infoDialogUiService,
                elementsInfoDialogService,
                elementsFileInfoService,
                dialogService,
                owner);
        }

        public void HandleConfig(
            MainWindowDialogActionsUiService dialogActionsUiService,
            ConfigWindowUiService configWindowUiService,
            ConfigWindowCommandService configWindowCommandService,
            EditorWindowService editorWindowService)
        {
            if (dialogActionsUiService == null)
            {
                return;
            }

            dialogActionsUiService.OpenConfig(
                configWindowUiService,
                configWindowCommandService,
                editorWindowService);
        }

        public void HandleNpcExport(
            MainWindowNpcExportUiService npcExportUiServiceFacade,
            NpcExportUiService npcExportUiService,
            ISessionService sessionService,
            TextExportDialogService textExportDialogService,
            NpcExportService npcExportService,
            string baseDirectory,
            Action<Cursor> setCursor,
            Action<string> showMessage)
        {
            if (npcExportUiServiceFacade == null || sessionService == null)
            {
                return;
            }

            npcExportUiServiceFacade.ExportNpcNames(
                npcExportUiService,
                sessionService.ListCollection,
                textExportDialogService,
                npcExportService,
                baseDirectory,
                setCursor,
                showMessage);
        }

        public void HandleJoin(
            MainWindowDialogActionsUiService dialogActionsUiService,
            CursorScopeService cursorScopeService,
            Action<Cursor> setCursor,
            ElementsJoinUiService joinUiService,
            ElementsJoinWorkflowService joinWorkflowService,
            ISessionService sessionService,
            ComboBox listCombo,
            ref ColorProgressBar.ColorProgressBar progressBar,
            Action<int> refreshList,
            Action<eListConversation> updateConversationList,
            Action<string> showMessage,
            Action<string> showWarning)
        {
            if (dialogActionsUiService == null || sessionService == null)
            {
                return;
            }

            dialogActionsUiService.ExecuteJoin(
                cursorScopeService,
                setCursor,
                joinUiService,
                joinWorkflowService,
                sessionService,
                sessionService.ListCollection,
                listCombo,
                ref progressBar,
                refreshList,
                updateConversationList,
                showMessage,
                showWarning);
        }

        public void HandleNpcAiExport(
            MainWindowNpcExportUiService npcExportUiServiceFacade,
            NpcExportUiService npcExportUiService,
            ISessionService sessionService,
            TextExportDialogService textExportDialogService,
            NpcAiExportService npcAiExportService,
            string baseDirectory,
            Action<Cursor> setCursor,
            Action<string> showMessage)
        {
            if (npcExportUiServiceFacade == null || sessionService == null)
            {
                return;
            }

            npcExportUiServiceFacade.ExportNpcAi(
                npcExportUiService,
                sessionService.ListCollection,
                textExportDialogService,
                npcAiExportService,
                baseDirectory,
                setCursor,
                showMessage);
        }

        public void HandleValidation(
            MainWindowValidationUiService validationUiServiceFacade,
            ValidationUiService validationUiService,
            ISessionService sessionService,
            Action<string> showMessage,
            Action<ValidationUiService, eListCollection> invokeValidation)
        {
            if (validationUiServiceFacade == null || sessionService == null || invokeValidation == null)
            {
                return;
            }

            invokeValidation(validationUiService, sessionService.ListCollection);
        }

        public void HandleSkillValidation(
            MainWindowValidationUiService validationUiServiceFacade,
            ValidationUiService validationUiService,
            ISessionService sessionService,
            SkillValidationService skillValidationService,
            Action<string> showMessage)
        {
            if (validationUiServiceFacade == null || sessionService == null)
            {
                return;
            }

            validationUiServiceFacade.ValidateSkills(
                validationUiService,
                sessionService.ListCollection,
                skillValidationService,
                showMessage);
        }

        public void HandlePropertyValidation(
            MainWindowValidationUiService validationUiServiceFacade,
            ValidationUiService validationUiService,
            ISessionService sessionService,
            PropertyValidationService propertyValidationService,
            Action<string> showMessage)
        {
            if (validationUiServiceFacade == null || sessionService == null)
            {
                return;
            }

            validationUiServiceFacade.ValidateProperties(
                validationUiService,
                sessionService.ListCollection,
                propertyValidationService,
                showMessage);
        }

        public void HandleTomeValidation(
            MainWindowValidationUiService validationUiServiceFacade,
            ValidationUiService validationUiService,
            ISessionService sessionService,
            TomeValidationService tomeValidationService,
            Action<string> showMessage)
        {
            if (validationUiServiceFacade == null || sessionService == null)
            {
                return;
            }

            validationUiServiceFacade.ValidateTomes(
                validationUiService,
                sessionService.ListCollection,
                tomeValidationService,
                showMessage);
        }

        public void HandleProbabilityValidation(
            MainWindowValidationUiService validationUiServiceFacade,
            ValidationUiService validationUiService,
            ISessionService sessionService,
            ProbabilityValidationService probabilityValidationService,
            Action<string> showMessage)
        {
            if (validationUiServiceFacade == null || sessionService == null)
            {
                return;
            }

            validationUiServiceFacade.ValidateProbabilities(
                validationUiService,
                sessionService.ListCollection,
                probabilityValidationService,
                showMessage);
        }

        public void HandleQuestOverflow(
            MainWindowValidationUiService validationUiServiceFacade,
            QuestOverflowUiService questOverflowUiService,
            ISessionService sessionService,
            QuestOverflowService questOverflowService)
        {
            if (validationUiServiceFacade == null || sessionService == null)
            {
                return;
            }

            validationUiServiceFacade.ShowQuestOverflow(
                questOverflowUiService,
                sessionService.ListCollection,
                questOverflowService);
        }

        public void HandleSkillReplace(
            MainWindowReplaceMappingUiService replaceMappingUiServiceFacade,
            ReplaceMappingUiService replaceMappingUiService,
            ISessionService sessionService,
            string mappingFolder,
            ReplaceMappingFileService replaceMappingFileService,
            ElementsReplaceService elementsReplaceService)
        {
            if (replaceMappingUiServiceFacade == null || sessionService == null)
            {
                return;
            }

            replaceMappingUiServiceFacade.ReplaceSkills(
                replaceMappingUiService,
                sessionService.ListCollection,
                mappingFolder,
                replaceMappingFileService,
                elementsReplaceService);
        }

        public void HandlePropertyReplace(
            MainWindowReplaceMappingUiService replaceMappingUiServiceFacade,
            ReplaceMappingUiService replaceMappingUiService,
            ISessionService sessionService,
            string mappingFolder,
            ReplaceMappingFileService replaceMappingFileService,
            ElementsReplaceService elementsReplaceService)
        {
            if (replaceMappingUiServiceFacade == null || sessionService == null)
            {
                return;
            }

            replaceMappingUiServiceFacade.ReplaceProperties(
                replaceMappingUiService,
                sessionService.ListCollection,
                mappingFolder,
                replaceMappingFileService,
                elementsReplaceService);
        }

        public void HandleTomeReplace(
            MainWindowReplaceMappingUiService replaceMappingUiServiceFacade,
            ReplaceMappingUiService replaceMappingUiService,
            ISessionService sessionService,
            string mappingFolder,
            ReplaceMappingFileService replaceMappingFileService,
            ElementsReplaceService elementsReplaceService,
            Action<string> showMessage)
        {
            if (replaceMappingUiServiceFacade == null || sessionService == null)
            {
                return;
            }

            replaceMappingUiServiceFacade.ReplaceTomes(
                replaceMappingUiService,
                sessionService.ListCollection,
                mappingFolder,
                replaceMappingFileService,
                elementsReplaceService,
                showMessage);
        }

        public void HandleClassMask(
            MainWindowDialogActionsUiService dialogActionsUiService,
            ClassMaskUiService classMaskUiService,
            ClassMaskCommandService classMaskCommandService,
            EditorWindowService editorWindowService)
        {
            if (dialogActionsUiService == null)
            {
                return;
            }

            dialogActionsUiService.ShowClassMask(
                classMaskUiService,
                classMaskCommandService,
                editorWindowService);
        }

        public void HandleRules(
            MainWindowDialogActionsUiService dialogActionsUiService,
            RulesWindowUiService rulesWindowUiService,
            RulesWindowService rulesWindowService,
            ISessionService sessionService,
            ref ColorProgressBar.ColorProgressBar progressBar)
        {
            if (dialogActionsUiService == null || sessionService == null)
            {
                return;
            }

            dialogActionsUiService.ShowRules(
                rulesWindowUiService,
                rulesWindowService,
                sessionService,
                ref progressBar);
        }

        public void HandleFieldCompare(
            MainWindowDialogActionsUiService dialogActionsUiService,
            FieldCompareCommandService fieldCompareCommandService,
            FieldCompareWorkflowService fieldCompareWorkflowService,
            ISessionService sessionService,
            ref ColorProgressBar.ColorProgressBar progressBar,
            Action<string> showMessage)
        {
            if (dialogActionsUiService == null || sessionService == null)
            {
                return;
            }

            dialogActionsUiService.OpenFieldCompare(
                fieldCompareCommandService,
                fieldCompareWorkflowService,
                sessionService,
                sessionService.ListCollection,
                sessionService.ConversationList,
                ref progressBar,
                showMessage);
        }

        public void HandleAbout(
            MainWindowDialogActionsUiService dialogActionsUiService,
            AboutWindowCommandService aboutWindowCommandService,
            EditorWindowService editorWindowService)
        {
            if (dialogActionsUiService == null)
            {
                return;
            }

            dialogActionsUiService.ShowAbout(
                aboutWindowCommandService,
                editorWindowService);
        }
    }
}
