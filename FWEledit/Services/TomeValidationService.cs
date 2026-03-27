using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class TomeValidationService
    {
        public List<string> BuildInvalidTomeProperties(eListCollection listCollection, int maxPropertyId)
        {
            List<string> results = new List<string>();
            if (listCollection == null || listCollection.Lists.Length <= 112)
            {
                return results;
            }

            for (int n = 0; n < listCollection.Lists[112].elementValues.Length; n++)
            {
                for (int f = 4; f < 14; f++)
                {
                    string attribute = listCollection.GetValue(112, n, f);
                    if (Convert.ToInt32(attribute) > maxPropertyId)
                    {
                        results.Add("Invalid Property: " + attribute + " (Tome: " + listCollection.GetValue(112, n, 0) + ")");
                    }
                }
            }

            return results;
        }
    }
}
