namespace FWEledit
{
    public sealed class EditorWindowService
    {
        private readonly ISessionService sessionService;

        public EditorWindowService(ISessionService sessionService)
        {
            this.sessionService = sessionService ?? new SessionService();
        }

        public WindowOpenResult ShowReplaceWindow(eListCollection listCollection)
        {
            WindowOpenResult result = new WindowOpenResult { Success = false };
            if (listCollection == null)
            {
                result.ErrorMessage = "No File Loaded!";
                return result;
            }

            new ReplaceWindow(sessionService, listCollection).ShowDialog();
            result.Success = true;
            return result;
        }

        public WindowOpenResult ShowFieldReplaceWindow(
            eListCollection listCollection,
            eListConversation conversationList,
            ref ColorProgressBar.ColorProgressBar progressBar)
        {
            WindowOpenResult result = new WindowOpenResult { Success = false };
            if (listCollection == null)
            {
                result.ErrorMessage = "No File Loaded!";
                return result;
            }

            new FieldReplaceWindow(sessionService, listCollection, conversationList, ref progressBar).ShowDialog();
            result.Success = true;
            return result;
        }

        public void ShowConfigWindow()
        {
            new ConfigWindow(sessionService).Show();
        }

        public void ShowAboutWindow()
        {
            new About().Show();
        }

        public void ShowClassMaskWindow()
        {
            new ClassMaskWindow(sessionService).Show();
        }
    }
}
