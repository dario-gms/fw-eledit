using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ValidationUiService
    {
        public void ValidateSkills(eListCollection listCollection, SkillValidationService validationService, Action<string> showMessage)
        {
            if (listCollection == null || validationService == null)
            {
                return;
            }

            List<string> issues = validationService.BuildInvalidSkills(listCollection, 846);
            if (issues.Count == 0)
            {
                if (showMessage != null)
                {
                    showMessage("OK, no invalid skills found!");
                }
                return;
            }

            new DebugWindow("Invalid Skills", string.Join("\r\n", issues));
        }

        public void ValidateProperties(eListCollection listCollection, PropertyValidationService validationService, Action<string> showMessage)
        {
            if (listCollection == null || validationService == null)
            {
                return;
            }

            List<string> issues = validationService.BuildInvalidProperties(listCollection, 1909);
            if (issues.Count == 0)
            {
                if (showMessage != null)
                {
                    showMessage("OK, no invalid properties found!");
                }
                return;
            }

            new DebugWindow("Invalid Properties", string.Join("\r\n", issues));
        }

        public void ValidateTomes(eListCollection listCollection, TomeValidationService validationService, Action<string> showMessage)
        {
            if (listCollection == null || validationService == null)
            {
                return;
            }

            List<string> issues = validationService.BuildInvalidTomeProperties(listCollection, 1909);
            if (issues.Count == 0)
            {
                if (showMessage != null)
                {
                    showMessage("OK, no invalid tome properties found!");
                }
                return;
            }

            new DebugWindow("Invalid Tome Properties", string.Join("\r\n", issues));
        }

        public void ValidateProbabilities(eListCollection listCollection, ProbabilityValidationService validationService, Action<string> showMessage)
        {
            if (listCollection == null || validationService == null)
            {
                return;
            }

            List<string> issues = validationService.BuildInvalidProbabilities(listCollection);
            if (issues.Count == 0)
            {
                if (showMessage != null)
                {
                    showMessage("OK, no invalid probabilities found!");
                }
                return;
            }

            new DebugWindow("Invalid Probabilities", string.Join("\r\n", issues));
        }
    }
}
