
using System;
using System.Globalization;

namespace FWEledit
{
    class FACEPILL_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                for (int k = 0; k < sessionService.ListCollection.Lists[89].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[89].elementFields[k] == "duration")
                    {
                        string duration = sessionService.ListCollection.GetValue(89, pos_item, k);
                        line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7047), duration);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[89].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[89].elementFields[k] == "character_combo_id")
                    {
                        line += Extensions.DecodingCharacterComboId(sessionService, sessionService.ListCollection.GetValue(89, pos_item, k));
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[89].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[89].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(89, pos_item, k);
                        if (price != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7024) + " " + Convert.ToInt32(price).ToString("N0", CultureInfo.CreateSpecificCulture("zh-CN"));
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

