using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ReplaceWindowCoordinatorService
    {
        public void DrawComboBoxItem(
            ThemeComboBoxDrawService drawService,
            ComboBoxThemeRendererService rendererService,
            CacheSave database,
            object sender,
            DrawItemEventArgs e)
        {
            if (drawService == null || database == null || database.arrTheme == null)
            {
                return;
            }

            drawService.DrawItem(rendererService, sender, e, database.arrTheme);
        }

        public void HandleReplace(
            ReplaceWindowUiService replaceWindowUiService,
            ReplaceWindowViewModel viewModel,
            TextBox listBox,
            TextBox itemBox,
            TextBox fieldBox,
            TextBox oldValueBox,
            TextBox newValueBox,
            ComboBox operationCombo,
            NumericUpDown operandControl,
            RadioButton replaceRadio,
            RadioButton recalcRadio,
            Action<Cursor> setCursor,
            Action<string> showMessage,
            Action closeWindow)
        {
            if (replaceWindowUiService == null || viewModel == null)
            {
                return;
            }

            setCursor?.Invoke(Cursors.AppStarting);

            ReplaceParameters parameters = replaceWindowUiService.BuildParameters(
                listBox,
                itemBox,
                fieldBox,
                oldValueBox,
                newValueBox,
                operationCombo,
                operandControl,
                replaceRadio,
                recalcRadio);

            ReplaceResult result = replaceWindowUiService.ExecuteReplace(viewModel, parameters);
            if (!result.Success)
            {
                showMessage?.Invoke(result.Error ?? "Replace failed.");
                setCursor?.Invoke(Cursors.Default);
                return;
            }

            showMessage?.Invoke(result.ReplacementCount.ToString() + " fields were updated");
            setCursor?.Invoke(Cursors.Default);
            closeWindow?.Invoke();
        }

        public void HandleClose(Action closeWindow)
        {
            closeWindow?.Invoke();
        }
    }
}
