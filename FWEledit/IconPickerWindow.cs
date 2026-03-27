using System.Windows.Forms;

namespace FWEledit
{
    public sealed partial class IconPickerWindow : Form
    {
        private readonly CacheSave database;
        private readonly IconPickerViewModel viewModel;
        private readonly IconPickerWindowCoordinatorService iconPickerCoordinatorService = new IconPickerWindowCoordinatorService();

        private TextBox searchBox;
        private AtlasGrid grid;
        private Label statusLabel;
        private Button okButton;
        private Button cancelButton;

        public int SelectedPathId { get; private set; }
    }
}
