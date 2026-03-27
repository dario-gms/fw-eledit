using System;

namespace FWEledit
{
    public sealed class ReplaceWindowViewModel : ViewModelBase
    {
        private readonly ReplaceService replaceService;
        private eListCollection listCollection;

        public ReplaceWindowViewModel(ReplaceService replaceService)
        {
            this.replaceService = replaceService ?? throw new ArgumentNullException(nameof(replaceService));
        }

        public eListCollection ListCollection
        {
            get { return listCollection; }
            set { SetProperty(ref listCollection, value); }
        }

        public ReplaceResult Execute(ReplaceParameters parameters)
        {
            return replaceService.Execute(ListCollection, parameters);
        }
    }
}
