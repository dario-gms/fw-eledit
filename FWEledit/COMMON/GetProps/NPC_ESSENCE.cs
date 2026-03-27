namespace FWEledit
{
    class NPC_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos)
        {
            string line = "";
            try
            {
                for (int k = 0; k < sessionService.ListCollection.Lists[57].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[57].elementFields[k] == "id_task_out_service")
                    {
                        string id_task_out_service = sessionService.ListCollection.GetValue(57, pos, k);
                        if (id_task_out_service != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7350) + " " + id_task_out_service;
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[57].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[57].elementFields[k] == "id_task_in_service")
                    {
                        string id_task_in_service = sessionService.ListCollection.GetValue(57, pos, k);
                        if (id_task_in_service != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7351) + " " + id_task_in_service;
                        }
                        break;
                    }
                }
            }
            catch
            {
                line = "";
            }
            return line;
        }
	}
}

