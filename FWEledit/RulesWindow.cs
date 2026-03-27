using System;
using System.Windows.Forms;

namespace FWEledit
{
	public partial class RulesWindow : Form
	{
        private readonly RulesWindowViewModel viewModel;
        private readonly RulesThemeUiService rulesThemeUiService = new RulesThemeUiService();
        private readonly RulesWindowActionsService rulesWindowActionsService = new RulesWindowActionsService();
        private readonly ThemeComboBoxDrawService themeComboBoxDrawService = new ThemeComboBoxDrawService();
        private readonly ComboBoxThemeRendererService comboBoxThemeRendererService = new ComboBoxThemeRendererService();
        private readonly DialogService dialogService = new DialogService();
        private readonly RulesWindowCoordinatorService rulesWindowCoordinatorService = new RulesWindowCoordinatorService();
        private ColorProgressBar.ColorProgressBar cpb2_prog;
        private readonly ISessionService sessionService;
        private CacheSave database;
        //ProgressBar progressBar_progress;

	}
}

