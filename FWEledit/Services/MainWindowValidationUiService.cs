using System;

namespace FWEledit
{
    public sealed class MainWindowValidationUiService
    {
        public void ValidateSkills(
            ValidationUiService validationUiService,
            eListCollection listCollection,
            SkillValidationService validationService,
            Action<string> showMessage)
        {
            if (validationUiService == null)
            {
                return;
            }

            validationUiService.ValidateSkills(listCollection, validationService, showMessage);
        }

        public void ValidateProperties(
            ValidationUiService validationUiService,
            eListCollection listCollection,
            PropertyValidationService validationService,
            Action<string> showMessage)
        {
            if (validationUiService == null)
            {
                return;
            }

            validationUiService.ValidateProperties(listCollection, validationService, showMessage);
        }

        public void ValidateTomes(
            ValidationUiService validationUiService,
            eListCollection listCollection,
            TomeValidationService validationService,
            Action<string> showMessage)
        {
            if (validationUiService == null)
            {
                return;
            }

            validationUiService.ValidateTomes(listCollection, validationService, showMessage);
        }

        public void ValidateProbabilities(
            ValidationUiService validationUiService,
            eListCollection listCollection,
            ProbabilityValidationService validationService,
            Action<string> showMessage)
        {
            if (validationUiService == null)
            {
                return;
            }

            validationUiService.ValidateProbabilities(listCollection, validationService, showMessage);
        }

        public void ShowQuestOverflow(
            QuestOverflowUiService overflowUiService,
            eListCollection listCollection,
            QuestOverflowService overflowService)
        {
            if (overflowUiService == null)
            {
                return;
            }

            overflowUiService.ShowReport(listCollection, overflowService);
        }
    }
}
