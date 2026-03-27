using System;
using System.Windows.Forms;

namespace FWEledit
{
	public partial class FieldReplaceWindow : Form
	{
        private readonly FieldReplaceViewModel viewModel;
        private readonly FieldReplaceThemeUiService fieldReplaceThemeUiService = new FieldReplaceThemeUiService();
        private readonly FieldReplaceUiService fieldReplaceUiService = new FieldReplaceUiService();
        private readonly ThemeComboBoxDrawService themeComboBoxDrawService = new ThemeComboBoxDrawService();
        private readonly ComboBoxThemeRendererService comboBoxThemeRendererService = new ComboBoxThemeRendererService();
        private readonly DialogService dialogService = new DialogService();
        private readonly GameFolderDialogService folderDialogService = new GameFolderDialogService();
        private readonly FieldReplaceWindowCoordinatorService fieldReplaceCoordinatorService = new FieldReplaceWindowCoordinatorService();
        ColorProgressBar.ColorProgressBar cpb2_prog;
        private readonly ISessionService sessionService;
        private CacheSave database = new CacheSave();

	}
}

