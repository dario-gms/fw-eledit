namespace FWEledit
{
    public sealed class SaveConfirmationUiService
    {
        public void Show(SaveConfirmationService confirmationService, string details)
        {
            if (confirmationService == null)
            {
                return;
            }

            confirmationService.Show(details);
        }
    }
}
