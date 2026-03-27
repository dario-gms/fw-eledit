using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class IToolType : Form
    {
        private readonly ISessionService sessionService;
        private CacheSave database;
        private readonly IToolTypeViewModel viewModel;
        private readonly ToolPreviewUiService toolPreviewUiService = new ToolPreviewUiService();
        private readonly IToolTypeCoordinatorService toolTypeCoordinatorService = new IToolTypeCoordinatorService();

        Timer fadeTimer;
        private Point mCurrentPoint;
    }
}

