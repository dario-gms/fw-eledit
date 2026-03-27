using System;

namespace FWEledit
{
    public sealed class RulesWindowService
    {
        public void ShowRulesWindow(ISessionService sessionService, ref ColorProgressBar.ColorProgressBar progressBar)
        {
            RulesWindow window = new RulesWindow(sessionService, ref progressBar);
            window.Show();
        }
    }
}
