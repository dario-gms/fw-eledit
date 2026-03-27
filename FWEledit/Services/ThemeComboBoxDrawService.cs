using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ThemeComboBoxDrawService
    {
        public void DrawItem(ComboBoxThemeRendererService rendererService, object sender, DrawItemEventArgs e, System.Collections.Generic.IList<string> theme)
        {
            if (rendererService == null || theme == null)
            {
                return;
            }

            rendererService.DrawItem(sender as ComboBox, e, theme);
        }
    }
}
