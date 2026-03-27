namespace FWEledit
{
    public sealed class SaveProgressUiService
    {
        public void Begin(SaveProgressService progressService, ColorProgressBar.ColorProgressBar progressBar)
        {
            if (progressService == null)
            {
                return;
            }

            progressService.Begin(progressBar);
        }

        public void Set(SaveProgressService progressService, ColorProgressBar.ColorProgressBar progressBar, int value)
        {
            if (progressService == null)
            {
                return;
            }

            progressService.Set(progressBar, value);
        }

        public void End(SaveProgressService progressService, ColorProgressBar.ColorProgressBar progressBar)
        {
            if (progressService == null)
            {
                return;
            }

            progressService.End(progressBar);
        }
    }
}
