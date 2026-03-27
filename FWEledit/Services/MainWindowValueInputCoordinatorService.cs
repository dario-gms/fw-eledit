using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowValueInputCoordinatorService
    {
        public void HandleEnter(
            ValueInputPlaceholderService placeholderService,
            TextBox valueBox,
            PlaceholderTextService placeholderTextService,
            string placeholderText)
        {
            if (placeholderService == null)
            {
                return;
            }

            placeholderService.HandleEnter(valueBox, placeholderTextService, placeholderText);
        }

        public void HandleLeave(
            ValueInputPlaceholderService placeholderService,
            TextBox valueBox,
            PlaceholderTextService placeholderTextService,
            string placeholderText)
        {
            if (placeholderService == null)
            {
                return;
            }

            placeholderService.HandleLeave(valueBox, placeholderTextService, placeholderText);
        }
    }
}
