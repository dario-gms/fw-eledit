namespace FWEledit
{
    class TITLE_CONFIG
    {
        public static string GetProps(ISessionService sessionService, int pos)
        {
            string line = "";
            try
            {
                for (int t = 0; t < sessionService.ListCollection.Lists[169].elementFields.Length; t++)
                {
                    if (sessionService.ListCollection.Lists[169].elementFields[t] == "Name")
                    {
                        line += sessionService.ListCollection.GetValue(169, pos, t);
                        break;
                    }
                }
                for (int t = 0; t < sessionService.ListCollection.Lists[169].elementFields.Length; t++)
                {
                    if (sessionService.ListCollection.Lists[169].elementFields[t] == "desc")
                    {
                        string desc = sessionService.ListCollection.GetValue(169, pos, t);
                        if (desc != "")
                        {
                            line += "\n" + desc;
                        }
                        break;
                    }
                }
                for (int t = 0; t < sessionService.ListCollection.Lists[169].elementFields.Length; t++)
                {
                    if (sessionService.ListCollection.Lists[169].elementFields[t] == "condition")
                    {
                        string condition = sessionService.ListCollection.GetValue(169, pos, t);
                        if (condition != "")
                        {
                            line += "\n" + condition;
                        }
                        break;
                    }
                }
                for (int t = 0; t < sessionService.ListCollection.Lists[169].elementFields.Length; t++)
                {
                    if (sessionService.ListCollection.Lists[169].elementFields[t] == "phy_damage")
                    {
                        string phy_damage = sessionService.ListCollection.GetValue(169, pos, t);
                        if (phy_damage != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7390) + " +" + phy_damage;
                        }
                        break;
                    }
                }
                for (int t = 0; t < sessionService.ListCollection.Lists[169].elementFields.Length; t++)
                {
                    if (sessionService.ListCollection.Lists[169].elementFields[t] == "magic_damage")
                    {
                        string magic_damage = sessionService.ListCollection.GetValue(169, pos, t);
                        if (magic_damage != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7391) + " +" + magic_damage;
                        }
                        break;
                    }
                }
                for (int t = 0; t < sessionService.ListCollection.Lists[169].elementFields.Length; t++)
                {
                    if (sessionService.ListCollection.Lists[169].elementFields[t] == "phy_defence")
                    {
                        string phy_defence = sessionService.ListCollection.GetValue(169, pos, t);
                        if (phy_defence != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7392) + " +" + phy_defence;
                        }
                        break;
                    }
                }
                for (int t = 0; t < sessionService.ListCollection.Lists[169].elementFields.Length; t++)
                {
                    if (sessionService.ListCollection.Lists[169].elementFields[t] == "magic_defence")
                    {
                        string magic_defence = sessionService.ListCollection.GetValue(169, pos, t);
                        if (magic_defence != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7393) + " +" + magic_defence;
                        }
                        break;
                    }
                }
                for (int t = 0; t < sessionService.ListCollection.Lists[169].elementFields.Length; t++)
                {
                    if (sessionService.ListCollection.Lists[169].elementFields[t] == "armor")
                    {
                        string armor = sessionService.ListCollection.GetValue(169, pos, t);
                        if (armor != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7394) + " +" + armor;
                        }
                        break;
                    }
                }
                for (int t = 0; t < sessionService.ListCollection.Lists[169].elementFields.Length; t++)
                {
                    if (sessionService.ListCollection.Lists[169].elementFields[t] == "attack")
                    {
                        string attack = sessionService.ListCollection.GetValue(169, pos, t);
                        if (attack != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7395) + " +" + attack;
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

