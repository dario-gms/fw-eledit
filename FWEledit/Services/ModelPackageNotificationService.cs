namespace FWEledit
{
    public sealed class ModelPackageNotificationService
    {
        public bool ShouldNotify(ModelPickerService modelPickerService, string package)
        {
            if (modelPickerService == null)
            {
                return false;
            }
            return modelPickerService.ShouldNotifyMissingPackageExtraction(package);
        }

        public string BuildMissingPackageMessage(string package)
        {
            return "Unable to read package index:\n" + package + ".pck\n\n" +
                "FWEledit now reads PCK index tables directly (no extraction).\n" +
                "If the list stays empty, check that the PCK/PKX files are accessible and try reopening Choice Model.";
        }

        public string Title
        {
            get { return "Choice Model"; }
        }
    }
}
