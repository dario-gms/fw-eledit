using System;

namespace FWEledit
{
    public sealed class IToolTypeViewModel : ViewModelBase
    {
        private readonly ToolPreviewService previewService;
        private ToolPreviewData previewData = new ToolPreviewData();

        public IToolTypeViewModel(ToolPreviewService previewService)
        {
            this.previewService = previewService ?? throw new ArgumentNullException(nameof(previewService));
        }

        public ToolPreviewData PreviewData
        {
            get { return previewData; }
            private set { SetProperty(ref previewData, value); }
        }

        public ToolPreviewData Load(InfoTool data, CacheSave database)
        {
            PreviewData = previewService.BuildPreview(data, database);
            return PreviewData;
        }
    }
}
