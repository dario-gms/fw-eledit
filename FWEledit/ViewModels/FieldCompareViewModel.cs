using System;

namespace FWEledit
{
    public sealed class FieldCompareViewModel : ViewModelBase
    {
        private readonly FieldCompareService compareService;
        private eListCollection source;
        private eListCollection other;
        private eListConversation sourceConversation;
        private eListConversation otherConversation;

        public FieldCompareViewModel(FieldCompareService compareService)
        {
            this.compareService = compareService ?? throw new ArgumentNullException(nameof(compareService));
        }

        public eListCollection Source
        {
            get { return source; }
            set { SetProperty(ref source, value); }
        }

        public eListCollection Other
        {
            get { return other; }
            set { SetProperty(ref other, value); }
        }

        public eListConversation SourceConversation
        {
            get { return sourceConversation; }
            set { SetProperty(ref sourceConversation, value); }
        }

        public eListConversation OtherConversation
        {
            get { return otherConversation; }
            set { SetProperty(ref otherConversation, value); }
        }

        public FieldCompareResult Execute(FieldCompareRequest request, Action progressTick)
        {
            return compareService.Execute(Source, Other, request, progressTick);
        }
    }
}
