using System;
using System.Collections.Generic;
using System.Text;

namespace FWEledit
{
    public sealed class UniqueIdValidationService
    {
        public bool Validate(eListCollection listCollection, Func<int, int> getIdFieldIndex, ElementsValidationService validationService, Action<string> showMessage)
        {
            if (listCollection == null || validationService == null)
            {
                return false;
            }

            List<string> issues = validationService.ValidateUniqueIds(listCollection, getIdFieldIndex);
            if (issues.Count == 0)
            {
                return true;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Save canceled: ID validation failed.");
            sb.AppendLine();
            for (int i = 0; i < issues.Count; i++)
            {
                sb.AppendLine("- " + issues[i]);
            }
            if (issues.Count >= 30)
            {
                sb.AppendLine();
                sb.AppendLine("More issues may exist. Fix IDs and try again.");
            }

            if (showMessage != null)
            {
                showMessage(sb.ToString());
            }
            return false;
        }
    }
}
