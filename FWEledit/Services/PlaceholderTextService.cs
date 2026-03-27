using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class PlaceholderTextService
    {
        public void ClearIfPlaceholder(TextBox textBox, string placeholder)
        {
            if (textBox == null)
            {
                return;
            }

            string text = textBox.Text ?? string.Empty;
            if (string.Equals(text.Trim(), placeholder ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                textBox.Clear();
            }
        }

        public void RestoreIfEmpty(TextBox textBox, string placeholder)
        {
            if (textBox == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = placeholder ?? string.Empty;
            }
        }
    }
}
