using System;

namespace FWEledit
{
    public sealed class SaveSessionUiService
    {
        public bool SaveSession(
            ElementsSessionService sessionService,
            Func<Action<string>, ElementsSaveContext> buildContext,
            Action<string> updateElementsPath,
            Action<string> updateStatus,
            Action<string> showConfirmation)
        {
            if (sessionService == null || buildContext == null)
            {
                return false;
            }

            string summary = string.Empty;
            ElementsSaveContext context = buildContext(message =>
            {
                summary = message ?? string.Empty;
                if (updateStatus != null)
                {
                    updateStatus(message ?? string.Empty);
                }
            });

            if (context == null)
            {
                return false;
            }

            if (sessionService.SaveCurrentSession(context))
            {
                if (!string.IsNullOrWhiteSpace(context.ElementsPath) && updateElementsPath != null)
                {
                    updateElementsPath(context.ElementsPath);
                }
                if (showConfirmation != null)
                {
                    showConfirmation(summary);
                }
                return true;
            }

            return false;
        }
    }
}
