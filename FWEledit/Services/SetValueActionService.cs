namespace FWEledit
{
    public sealed class SetValueActionService
    {
        public void ShowDisabledMessage(System.Action<string> showMessage)
        {
            if (showMessage != null)
            {
                showMessage("Set Value is disabled in this mode. Edit directly in the Values grid.");
            }
        }
    }
}
