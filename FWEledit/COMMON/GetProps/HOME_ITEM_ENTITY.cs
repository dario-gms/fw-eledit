
using System;
using System.Collections.Generic;

namespace FWEledit
{
    class HOME_ITEM_ENTITY
    {
        public static string GetProps(ISessionService sessionService, int pos)
        {
            string line = "";
            string name = "";
            string level = "";
            string flourish_min = "";
            string flourish_max = "";
            string unique_flourish_min = "";
            string unique_flourish_max = "";
            string clean = "";
            string put_area = "";
            string desc = "";
            try
            {
                for (int t = 0; t < sessionService.ListCollection.Lists[223].elementFields.Length; t++)
                {
                    if (sessionService.ListCollection.Lists[223].elementFields[t] == "Name")
                    {
                        name = sessionService.ListCollection.GetValue(223, pos, t);
                        break;
                    }
                }
                for (int t = 0; t < sessionService.ListCollection.Lists[223].elementFields.Length; t++)
                {
                    if (sessionService.ListCollection.Lists[223].elementFields[t] == "level")
                    {
                        level = sessionService.ListCollection.GetValue(223, pos, t);
                        break;
                    }
                }
                for (int t = 0; t < sessionService.ListCollection.Lists[223].elementFields.Length; t++)
                {
                    if (sessionService.ListCollection.Lists[223].elementFields[t] == "flourish_min")
                    {
                        flourish_min = sessionService.ListCollection.GetValue(223, pos, t);
                        flourish_max = sessionService.ListCollection.GetValue(223, pos, t + 1);
                    }
                }
                for (int t = 0; t < sessionService.ListCollection.Lists[223].elementFields.Length; t++)
                {
                    if (sessionService.ListCollection.Lists[223].elementFields[t] == "unique_flourish_min")
                    {
                        unique_flourish_min = sessionService.ListCollection.GetValue(223, pos, t);
                        unique_flourish_max = sessionService.ListCollection.GetValue(223, pos, t + 1);
                        break;
                    }
                }
                for (int t = 0; t < sessionService.ListCollection.Lists[223].elementFields.Length; t++)
                {
                    if (sessionService.ListCollection.Lists[223].elementFields[t] == "clean")
                    {
                        clean = sessionService.ListCollection.GetValue(223, pos, t);
                        break;
                    }
                }
                for (int t = 0; t < sessionService.ListCollection.Lists[223].elementFields.Length; t++)
                {
                    if (sessionService.ListCollection.Lists[223].elementFields[t] == "put_area1")
                    {
                        string put_area1 = sessionService.ListCollection.GetValue(223, pos, t);
                        string put_area2 = sessionService.ListCollection.GetValue(223, pos, t + 1);
                        //line += "\n" + Extensions.GetLocalization(sessionService, 7404);
                        if (put_area1 == "0") put_area += Extensions.GetLocalization(sessionService, 3110);
                        if (put_area1 == "1") put_area += Extensions.GetLocalization(sessionService, 3111);
                        uint putarea2;
                        bool result_put_area2 = uint.TryParse(put_area2, out putarea2);
                        List<uint> powers_put_area2 = new List<uint>(Extensions.GetPowers(putarea2));
                        for (int p = 0; p < powers_put_area2.Count; p++)
                        {
                            if (powers_put_area2[p] == 0) continue;

                            switch (p)
                            {
                                case 0:
                                    put_area +=  Extensions.GetLocalization(sessionService, 3120);//put_area2_1
                                    break;
                                case 1:
                                    put_area +=  Extensions.GetLocalization(sessionService, 3121);//put_area2_2
                                    break;
                                case 2:
                                    put_area +=  Extensions.GetLocalization(sessionService, 3122);//put_area2_4
                                    break;
                                case 3:
                                    put_area +=  Extensions.GetLocalization(sessionService, 3123);//put_area2_8
                                    break;
                            }
                        }
                        break;
                    }
                }
                for (int t = 0; t < sessionService.ListCollection.Lists[223].elementFields.Length; t++)
                {
                    if (sessionService.ListCollection.Lists[223].elementFields[t] == "desc")
                    {
                        desc = sessionService.ListCollection.GetValue(223, pos, t);
                        break;
                    }
                }
                line += String.Format(Extensions.GetLocalization(sessionService, 7400), name, level, flourish_min, flourish_max, unique_flourish_min, unique_flourish_max, clean, put_area, desc);
            }
            catch
            {
                line = "";
            }
            return line;
        }
	}
}

