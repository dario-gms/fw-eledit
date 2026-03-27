using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class IconPickerViewModel : ViewModelBase
    {
        private readonly IconPickerService iconPickerService;
        private IconPickerData data = new IconPickerData();
        private List<IconEntryModel> currentEntries = new List<IconEntryModel>();

        public IconPickerViewModel(IconPickerService iconPickerService)
        {
            this.iconPickerService = iconPickerService ?? throw new ArgumentNullException(nameof(iconPickerService));
        }

        public IconPickerData Data
        {
            get { return data; }
            private set { SetProperty(ref data, value); }
        }

        public List<IconEntryModel> AllEntries
        {
            get { return Data.Entries; }
        }

        public List<IconEntryModel> CurrentEntries
        {
            get { return currentEntries; }
            private set { SetProperty(ref currentEntries, value); }
        }

        public Dictionary<int, int> UsageByPathId
        {
            get { return Data.UsageByPathId; }
        }

        public Dictionary<string, int> UsageByIconKey
        {
            get { return Data.UsageByIconKey; }
        }

        public int PendingSelectPathId
        {
            get { return Data.PendingSelectPathId; }
        }

        public void Load(CacheSave database, Dictionary<int, int> usageByPathId, int pendingSelectPathId)
        {
            Data = iconPickerService.BuildData(database, usageByPathId, pendingSelectPathId);
            CurrentEntries = new List<IconEntryModel>(Data.Entries);
        }

        public void ApplyFilter(string search)
        {
            CurrentEntries = iconPickerService.FilterEntries(Data.Entries, search);
        }
    }
}
