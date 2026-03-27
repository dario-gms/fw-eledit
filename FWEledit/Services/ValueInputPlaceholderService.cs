using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ValueInputPlaceholderService
    {
        public void HandleEnter(TextBox textBox, PlaceholderTextService placeholderService, string placeholder)
        {
            if (textBox == null || placeholderService == null)
            {
                return;
            }

            placeholderService.ClearIfPlaceholder(textBox, placeholder);
        }

        public void HandleLeave(TextBox textBox, PlaceholderTextService placeholderService, string placeholder)
        {
            if (textBox == null || placeholderService == null)
            {
                return;
            }

            placeholderService.RestoreIfEmpty(textBox, placeholder);
        }
    }
}
