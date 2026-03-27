using System;
using System.Windows.Forms;

namespace FWEledit
{

    public struct ScanInfo
	{
		public int ElementCount;
		public int FirstElementID;
		public int SecondElementID;
		public int EntrySizePrior;
		public int EntrySizeEstimated;
	}

	public partial class ConfigWindow : Form
	{
        private readonly ISessionService sessionService;
        private CacheSave database;
        private readonly ConfigWindowViewModel viewModel;
        private readonly IDialogService dialogService;
        private readonly ConfigThemeUiService configThemeUiService = new ConfigThemeUiService();
        private readonly ConfigFileLoadUiService configFileLoadUiService = new ConfigFileLoadUiService();
        private readonly ConfigFileSaveUiService configFileSaveUiService = new ConfigFileSaveUiService();
        private readonly ConfigListDisplayUiService configListDisplayUiService = new ConfigListDisplayUiService();
        private readonly ConfigListMutationUiService configListMutationUiService = new ConfigListMutationUiService();
        private readonly ConfigFieldSetUiService configFieldSetUiService = new ConfigFieldSetUiService();
        private readonly ConfigListUpdateUiService configListUpdateUiService = new ConfigListUpdateUiService();
        private readonly ThemeComboBoxDrawService themeComboBoxDrawService = new ThemeComboBoxDrawService();
        private readonly ComboBoxThemeRendererService comboBoxThemeRendererService = new ComboBoxThemeRendererService();
        private readonly ConfigRowUpdateUiService configRowUpdateUiService = new ConfigRowUpdateUiService();
        private readonly ConfigRowInsertUiService configRowInsertUiService = new ConfigRowInsertUiService();
        private readonly ConfigRowCopyService configRowCopyService = new ConfigRowCopyService();
        private readonly ConfigRowPasteUiService configRowPasteUiService = new ConfigRowPasteUiService();
        private readonly ConfigRowDeleteUiService configRowDeleteUiService = new ConfigRowDeleteUiService();
        private readonly ConfigSequelScannerService configSequelScannerService = new ConfigSequelScannerService();
        private readonly GridCellSelectionService gridCellSelectionService = new GridCellSelectionService();
        private readonly ConfigWindowSetupCoordinatorService configWindowSetupCoordinatorService = new ConfigWindowSetupCoordinatorService();
        private readonly ConfigWindowCoordinatorService configWindowCoordinatorService = new ConfigWindowCoordinatorService();

        private ConfigData Data
        {
            get { return viewModel.Data; }
        }

	}
}

