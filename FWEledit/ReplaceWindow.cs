using System;
using System.Windows.Forms;

namespace FWEledit
{
	public partial class ReplaceWindow : Form
	{
        private readonly ReplaceWindowViewModel viewModel;
        private readonly ReplaceWindowThemeUiService replaceWindowThemeUiService = new ReplaceWindowThemeUiService();
        private readonly ReplaceWindowUiService replaceWindowUiService = new ReplaceWindowUiService();
        private readonly ThemeComboBoxDrawService themeComboBoxDrawService = new ThemeComboBoxDrawService();
        private readonly ComboBoxThemeRendererService comboBoxThemeRendererService = new ComboBoxThemeRendererService();
        private readonly ReplaceWindowCoordinatorService replaceWindowCoordinatorService = new ReplaceWindowCoordinatorService();
        private readonly ISessionService sessionService;
        private CacheSave database = new CacheSave();

	}
}

