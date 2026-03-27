using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class IToolType : Form
    {
        public IToolType(InfoTool data)
            : this(new SessionService(), data)
        {
        }

        public IToolType(ISessionService sessionService, InfoTool data)
        {
            this.sessionService = sessionService ?? new SessionService();
            viewModel = new IToolTypeViewModel(new ToolPreviewService());
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.FromArgb(32, 32, 32);
            Opacity = 0;
            fadeTimer = new Timer { Interval = 15, Enabled = true };
            fadeTimer.Tick += new EventHandler(fadeTimer_Tick);
            mCurrentPoint = Cursor.Position;
            Left = mCurrentPoint.X + 20;
            Top = mCurrentPoint.Y;

            toolTypeCoordinatorService.InitializePreview(
                viewModel,
                this.sessionService,
                toolPreviewUiService,
                data,
                titleText,
                iconBox,
                richTextBox_PreviewText);

            toolTypeCoordinatorService.AdjustLayout(this, richTextBox_PreviewText);
            database = this.sessionService.Database;
            colorTheme();
        }


        private void colorTheme()
        {
            toolTypeCoordinatorService.ApplyTheme(
                toolPreviewUiService,
                database,
                this,
                richTextBox_PreviewText,
                titleText);
        }
    }
}


