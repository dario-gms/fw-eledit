using System;

namespace FWEledit
{
    class MINE_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos)
        {
            if(pos == -1)
            {
                return "";
            }
            string line = "";
            try
            {
                for (int k = 0; k < sessionService.ListCollection.Lists[79].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[79].elementFields[k] == "level_required")
                    {
                        line += "\n" + Extensions.GetLocalization(sessionService, 7370) + " " + sessionService.ListCollection.GetValue(79, pos, k);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[79].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[79].elementFields[k] == "id_equipment_required")
                    {
                        string id_equipment_required = sessionService.ListCollection.GetValue(79, pos, k);
                        bool Suc = false;
                        if (id_equipment_required != "0")
                        {
                            for (int i = 0; i < sessionService.Database.task_items_list.Length; i += 2)
                            {
                                if (sessionService.ListCollection.Version >= Convert.ToInt32(sessionService.Database.task_items_list[i + 1]))
                                {
                                    int l = Convert.ToInt32(sessionService.Database.task_items_list[i]);
                                    for (int t = 0; t < sessionService.ListCollection.Lists[l].elementValues.Length; t++)
                                    {
                                        if (sessionService.ListCollection.GetValue(l, t, 0) == id_equipment_required)
                                        {
                                            for (int a = 0; a < sessionService.ListCollection.Lists[l].elementFields.Length; a++)
                                            {
                                                if (sessionService.ListCollection.Lists[l].elementFields[a] == "Name")
                                                {
                                                    line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7371), sessionService.ListCollection.GetValue(l, t, a), id_equipment_required);
                                                    break;
                                                }
                                            }
                                            Suc = true;
                                            break;
                                        }
                                    }
                                    if (Suc == true) break;
                                }
                            }
                            for (int i = 0; i < sessionService.ListCollection.Lists[79].elementFields.Length; i++)
                            {
                                if (sessionService.ListCollection.Lists[79].elementFields[i] == "eliminate_tool")
                                {

                                    line += "\n" + Extensions.GetLocalization(sessionService, 7372) + " ";
                                    if (sessionService.ListCollection.GetValue(79, pos, i) == "0") line += Extensions.GetLocalization(sessionService, 2310);
                                    else line += Extensions.GetLocalization(sessionService, 2311);
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[79].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[79].elementFields[k] == "time_min")
                    {
                        string time_min = sessionService.ListCollection.GetValue(79, pos, k);
                        string time_max = sessionService.ListCollection.GetValue(79, pos, k + 1);
                        string time = time_min;
                        if (time_min != time_max) time += "~" + time_max;
                        line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7373), time);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[79].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[79].elementFields[k] == "exp")
                    {
                        line += "\n" + Extensions.GetLocalization(sessionService, 7374) + " " + sessionService.ListCollection.GetValue(79, pos, k);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[79].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[79].elementFields[k] == "skillpoint")
                    {
                        line += "\n" + Extensions.GetLocalization(sessionService, 7375) + " " + sessionService.ListCollection.GetValue(79, pos, k);
                        break;
                    }
                }
                bool tmp = false;
                for (int t = 1; t < 17; t++)
                {
                    for (int a = 0; a < sessionService.ListCollection.Lists[79].elementFields.Length; a++)
                    {
                        if (sessionService.ListCollection.Lists[79].elementFields[a] == "materials_" + t + "_id")
                        {
                            string id = sessionService.ListCollection.GetValue(79, pos, a);
                            if (id != "0") tmp = true;
                            break;
                        }
                    }
                }
                line += "\n" + Extensions.GetLocalization(sessionService, 7376) + " ";
                if (tmp == false) line += Extensions.GetLocalization(sessionService, 2310);
                else line += Extensions.GetLocalization(sessionService, 2311);
                for (int k = 0; k < sessionService.ListCollection.Lists[79].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[79].elementFields[k] == "task_in")
                    {
                        string task_in = sessionService.ListCollection.GetValue(79, pos, k);
                        line += "\n" + Extensions.GetLocalization(sessionService, 7377) + " ";
                        if (task_in != "0") line += task_in;
                        else line += Extensions.GetLocalization(sessionService, 2310);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[79].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[79].elementFields[k] == "permenent")
                    {

                        string permenent = sessionService.ListCollection.GetValue(79, pos, k);
                        line += "\n" + Extensions.GetLocalization(sessionService, 7378) + " ";
                        if (permenent == "0") line += Extensions.GetLocalization(sessionService, 2310);
                        else line += Extensions.GetLocalization(sessionService, 2311);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[79].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[79].elementFields[k] == "max_gatherer")
                    {
                        line += "\n" + Extensions.GetLocalization(sessionService, 7379) + " " + sessionService.ListCollection.GetValue(79, pos, k);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[79].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[79].elementFields[k] == "gather_dist")
                    {
                        line += "\n" + Extensions.GetLocalization(sessionService, 7380) + " " + sessionService.ListCollection.GetValue(79, pos, k) + " ?.";
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

