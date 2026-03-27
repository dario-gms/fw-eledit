namespace FWEledit
{
    public sealed class ClassMaskUiService
    {
        public void ShowClassMask(ClassMaskCommandService classMaskCommandService, EditorWindowService editorWindowService)
        {
            if (classMaskCommandService == null || editorWindowService == null)
            {
                return;
            }

            classMaskCommandService.ShowClassMask(editorWindowService);
        }
    }
}
