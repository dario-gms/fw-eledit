using System;

namespace FWEledit
{
    public sealed class RulesWindowViewModel : ViewModelBase
    {
        private readonly RulesService rulesService;
        private eListCollection baseCollection;
        private eListCollection recentCollection;
        private RuleConfig[] rules;

        public RulesWindowViewModel(RulesService rulesService)
        {
            this.rulesService = rulesService ?? throw new ArgumentNullException(nameof(rulesService));
        }

        public eListCollection BaseCollection
        {
            get { return baseCollection; }
            set { SetProperty(ref baseCollection, value); }
        }

        public eListCollection RecentCollection
        {
            get { return recentCollection; }
            set { SetProperty(ref recentCollection, value); }
        }

        public RuleConfig[] Rules
        {
            get { return rules; }
            set { SetProperty(ref rules, value); }
        }

        public eListCollection LoadCollection(string filePath, ref ColorProgressBar.ColorProgressBar progressBar)
        {
            return rulesService.LoadCollection(filePath, ref progressBar);
        }

        public void EnsureRulesInitialized()
        {
            if (RecentCollection != null && (Rules == null || Rules.Length != RecentCollection.Lists.Length))
            {
                Rules = rulesService.InitializeRules(RecentCollection);
            }
        }

        public void ResetRules()
        {
            if (RecentCollection != null)
            {
                Rules = rulesService.InitializeRules(RecentCollection);
            }
        }

        public int CountMismatches(int listIndexBase, int listIndexRecent, int baseFieldIndex, int recentFieldIndex)
        {
            return rulesService.CountMismatches(BaseCollection, RecentCollection, Rules, listIndexBase, listIndexRecent, baseFieldIndex, recentFieldIndex);
        }

        public string BuildRulesMessage()
        {
            return rulesService.BuildRulesMessage(BaseCollection, RecentCollection, Rules);
        }

        public void ImportRules(string filePath)
        {
            rulesService.ImportRules(filePath, BaseCollection, Rules);
        }

        public void ExportRules(string filePath)
        {
            rulesService.ExportRules(filePath, BaseCollection, RecentCollection, Rules);
        }
    }
}
