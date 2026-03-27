using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class FieldCompare : Form
	{
        private readonly FieldCompareViewModel viewModel;
        private readonly FieldCompareThemeUiService fieldCompareThemeUiService = new FieldCompareThemeUiService();
        private readonly FieldCompareUiService fieldCompareUiService = new FieldCompareUiService();
        private readonly ThemeComboBoxDrawService themeComboBoxDrawService = new ThemeComboBoxDrawService();
        private readonly ComboBoxThemeRendererService comboBoxThemeRendererService = new ComboBoxThemeRendererService();
        private readonly DialogService dialogService = new DialogService();
        private readonly GameFolderDialogService folderDialogService = new GameFolderDialogService();
        private readonly FieldCompareCoordinatorService fieldCompareCoordinatorService = new FieldCompareCoordinatorService();
        ColorProgressBar.ColorProgressBar cpb2_prog;
        private readonly ISessionService sessionService;
        private CacheSave database;

	}
}

