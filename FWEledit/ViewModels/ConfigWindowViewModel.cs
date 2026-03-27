using System;

namespace FWEledit
{
    public sealed class ConfigWindowViewModel : ViewModelBase
    {
        private readonly ConfigFileService configService;
        private ConfigData data;

        public ConfigWindowViewModel(ConfigFileService configService)
        {
            this.configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        public ConfigData Data
        {
            get { return data; }
            private set { SetProperty(ref data, value); }
        }

        public string[] CopiedFieldNames;
        public string[] CopiedFieldTypes;

        public void LoadConfig(string filePath)
        {
            Data = configService.Load(filePath);
        }

        public void SaveConfig(string filePath)
        {
            if (Data == null)
            {
                throw new InvalidOperationException("No configuration loaded.");
            }
            configService.Save(filePath, Data);
        }
    }
}
