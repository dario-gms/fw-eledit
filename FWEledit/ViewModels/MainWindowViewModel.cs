using System;

namespace FWEledit
{
    public sealed class MainWindowViewModel : ViewModelBase
    {
        private readonly SearchSuggestionService searchSuggestionService;
        private readonly ISessionService sessionService;

        public MainWindowViewModel(
            ISessionService sessionService,
            ItemDescriptionStore descriptionStore,
            SearchSuggestionService searchSuggestionService)
        {
            if (sessionService == null)
            {
                throw new ArgumentNullException(nameof(sessionService));
            }
            if (descriptionStore == null)
            {
                throw new ArgumentNullException(nameof(descriptionStore));
            }
            if (searchSuggestionService == null)
            {
                throw new ArgumentNullException(nameof(searchSuggestionService));
            }
            this.sessionService = sessionService;
            this.searchSuggestionService = searchSuggestionService;
            DescriptionViewModel = new DescriptionViewModel(descriptionStore);
        }

        public DescriptionViewModel DescriptionViewModel { get; private set; }

        private string elementsPath = string.Empty;
        private int selectedListIndex = -1;
        private int selectedItemId;
        private bool enableSelectionList = true;
        private bool enableSelectionItem = true;
        public IToolType CustomTooltype;
        public System.Drawing.Point MouseMoveCheck;

        public string ElementsPath
        {
            get { return elementsPath; }
            set { SetProperty(ref elementsPath, value ?? string.Empty); }
        }

        public int SelectedListIndex
        {
            get { return selectedListIndex; }
            set { SetProperty(ref selectedListIndex, value); }
        }

        public int SelectedItemId
        {
            get { return selectedItemId; }
            set { SetProperty(ref selectedItemId, value); }
        }

        public bool EnableSelectionList
        {
            get { return enableSelectionList; }
            set { SetProperty(ref enableSelectionList, value); }
        }

        public bool EnableSelectionItem
        {
            get { return enableSelectionItem; }
            set { SetProperty(ref enableSelectionItem, value); }
        }
        public ISessionService Session
        {
            get { return sessionService; }
        }

        public bool HasUnsavedChanges;
        public bool StartupSessionRestoreDone;
        public bool SuppressClosePrompt { get; set; }
        public bool IsRestoringSessionState { get; set; }
        public bool IsUpdatingDescriptionUi { get; set; }
        public bool SuppressValuesUiRefresh { get; set; }
        public bool HasPendingNavigationStateWrite { get; set; }

        public int SearchSuggestionMax
        {
            get { return 200; }
        }

        public int SearchSuggestionMaxHeight
        {
            get { return 180; }
        }

        public void MarkNavigationStateDirty()
        {
            HasPendingNavigationStateWrite = true;
        }

        public bool ShouldFlushNavigationState()
        {
            return HasPendingNavigationStateWrite;
        }

        public void MarkNavigationStateFlushed()
        {
            HasPendingNavigationStateWrite = false;
        }

        
        public void UpdateSelectionState(int listIndex, int itemId)
        {
            SelectedListIndex = listIndex;
            SelectedItemId = itemId;
        }

        public void PersistNavigationState(NavigationStateService navigationStateService, System.Action restartTimer)
        {
            if (navigationStateService == null)
            {
                return;
            }

            int? itemId = SelectedItemId > 0 ? (int?)SelectedItemId : null;
            navigationStateService.PersistSelection(IsRestoringSessionState, SelectedListIndex, itemId);
            MarkNavigationStateDirty();
            if (restartTimer != null)
            {
                restartTimer();
            }
        }

        public void FlushNavigationStateToDisk(NavigationStateService navigationStateService)
        {
            if (!ShouldFlushNavigationState())
            {
                return;
            }
            try
            {
                if (navigationStateService != null)
                {
                    navigationStateService.Flush();
                }
                MarkNavigationStateFlushed();
            }
            catch
            {
            }
        }

        public bool SaveCurrentSession(ElementsSessionService elementsSessionService, ElementsSaveContext context)
        {
            if (elementsSessionService == null)
            {
                return false;
            }

            bool ok = elementsSessionService.SaveCurrentSession(context);
            ElementsPath = context.ElementsPath ?? ElementsPath;
            return ok;
        }

public bool IsSearchPlaceholder(string text)
        {
            return string.Equals(text, "ID or NAME", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "VALUE", StringComparison.OrdinalIgnoreCase);
        }

        public System.Collections.Generic.List<SearchSuggestion> BuildSearchSuggestions(
            eListCollection listCollection,
            string query,
            bool matchCase,
            Func<int, int> getIdFieldIndex,
            Func<int, int> getNameFieldIndex)
        {
            return searchSuggestionService.BuildSuggestions(
                listCollection,
                query,
                matchCase,
                SearchSuggestionMax,
                getIdFieldIndex,
                getNameFieldIndex);
        }
    }
}
