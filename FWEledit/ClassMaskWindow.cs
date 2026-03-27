using System;
using System.Windows.Forms;

namespace FWEledit
{
	public partial class ClassMaskWindow : Form
	{
        bool lockCheckBox;
        private readonly ClassMaskViewModel viewModel;
        private readonly ClassMaskThemeUiService classMaskThemeUiService = new ClassMaskThemeUiService();
        private readonly ClassMaskCoordinatorService classMaskCoordinatorService = new ClassMaskCoordinatorService();
        private readonly ISessionService sessionService;
        private CacheSave database;

	}
}

