namespace FWEledit
{
    public sealed class DescriptionIdRemapService
    {
        public bool TryRemap(
            DescriptionViewModel descriptionViewModel,
            DescriptionLoadService loadService,
            DescriptionRuntimeService runtimeService,
            int oldId,
            int newId,
            System.Action<string[]> applyRuntime,
            System.Action markUnsaved)
        {
            if (descriptionViewModel == null || loadService == null || runtimeService == null)
            {
                return false;
            }
            if (oldId <= 0 || newId <= 0 || oldId == newId)
            {
                return false;
            }

            if (descriptionViewModel.RemapId(oldId, newId))
            {
                markUnsaved?.Invoke();
                loadService.SyncRuntime(descriptionViewModel, runtimeService, applyRuntime);
                return true;
            }

            return false;
        }
    }
}
