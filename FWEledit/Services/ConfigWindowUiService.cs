namespace FWEledit
{
    public sealed class ConfigWindowUiService
    {
        public void OpenConfig(ConfigWindowCommandService configWindowCommandService, EditorWindowService editorWindowService)
        {
            if (configWindowCommandService == null || editorWindowService == null)
            {
                return;
            }

            configWindowCommandService.OpenConfig(editorWindowService);
        }
    }
}
