namespace FWEledit
{
    public sealed class RulesWindowUiService
    {
        public void ShowRules(RulesWindowService rulesWindowService, ISessionService sessionService, ref ColorProgressBar.ColorProgressBar progressBar)
        {
            if (rulesWindowService == null)
            {
                return;
            }

            rulesWindowService.ShowRulesWindow(sessionService, ref progressBar);
        }
    }
}
