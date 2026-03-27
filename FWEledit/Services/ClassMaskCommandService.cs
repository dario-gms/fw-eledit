namespace FWEledit
{
    public sealed class ClassMaskCommandService
    {
        public void ShowClassMask(EditorWindowService editorWindowService)
        {
            if (editorWindowService == null)
            {
                return;
            }

            editorWindowService.ShowClassMaskWindow();
        }
    }
}
