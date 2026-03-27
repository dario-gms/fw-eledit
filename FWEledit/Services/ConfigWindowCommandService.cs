namespace FWEledit
{
    public sealed class ConfigWindowCommandService
    {
        public void OpenConfig(EditorWindowService editorWindowService)
        {
            if (editorWindowService == null)
            {
                return;
            }

            editorWindowService.ShowConfigWindow();
        }
    }
}
