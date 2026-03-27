using System;

namespace FWEledit
{
    public sealed class FieldReplaceViewModel : ViewModelBase
    {
        private readonly FieldReplaceService replaceService;
        private eListCollection source;
        private eListCollection other;
        private eListConversation sourceConversation;
        private eListConversation otherConversation;

        public FieldReplaceViewModel(FieldReplaceService replaceService)
        {
            this.replaceService = replaceService ?? throw new ArgumentNullException(nameof(replaceService));
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

        public FieldReplaceResult Execute(FieldReplaceRequest request, Action progressTick)
        {
            return replaceService.Execute(Source, Other, SourceConversation, OtherConversation, request, progressTick);
        }
    }
}
