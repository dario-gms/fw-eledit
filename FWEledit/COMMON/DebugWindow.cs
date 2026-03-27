using System.Windows.Forms;

namespace FWEledit
{
	public partial class DebugWindow : Form
	{
        private readonly DebugWindowCoordinatorService debugWindowCoordinatorService = new DebugWindowCoordinatorService();

		public DebugWindow(string Title, string Message)
		{
			InitializeComponent();
            debugWindowCoordinatorService.Initialize(this, message, Title, Message);
		}
	}
}
