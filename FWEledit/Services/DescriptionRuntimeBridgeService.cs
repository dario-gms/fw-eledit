namespace FWEledit
{
    public sealed class DescriptionRuntimeBridgeService
    {
        public void ApplyRuntime(ISessionService sessionService, string[] data, CacheSave database)
        {
            if (sessionService == null)
            {
                return;
            }

            sessionService.ItemExtDesc = data ?? new string[0];
            if (database != null)
            {
                database.item_ext_desc = sessionService.ItemExtDesc;
            }
        }
    }
}
