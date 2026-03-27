namespace FWEledit
{
    public sealed class AboutWindowCommandService
    {
        public void ShowAbout(EditorWindowService editorWindowService)
        {
            if (editorWindowService == null)
            {
                return;
            }

            editorWindowService.ShowAboutWindow();
        }
    }
}
