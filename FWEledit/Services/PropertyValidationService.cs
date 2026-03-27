using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class PropertyValidationService
    {
        public List<string> BuildInvalidProperties(eListCollection listCollection, int maxPropertyId)
        {
            List<string> results = new List<string>();
            if (listCollection == null)
            {
                return results;
            }

            AppendRange(results, listCollection, 3, 43, 202, 2, maxPropertyId, "Weapon");
            AppendRange(results, listCollection, 6, 55, 180, 2, maxPropertyId, "Armor");
            AppendRange(results, listCollection, 9, 44, 161, 2, maxPropertyId, "Ornament");
            AppendRange(results, listCollection, 35, 11, 13, 1, maxPropertyId, "Soulgem");
            AppendRange(results, listCollection, 90, 15, 20, 1, maxPropertyId, "Complect Bonus");

            return results;
        }

        private static void AppendRange(
            List<string> results,
            eListCollection listCollection,
            int listIndex,
            int startField,
            int endFieldExclusive,
            int step,
            int maxPropertyId,
            string label)
        {
            if (listCollection.Lists.Length <= listIndex)
            {
                return;
            }

            for (int n = 0; n < listCollection.Lists[listIndex].elementValues.Length; n++)
            {
                for (int f = startField; f < endFieldExclusive; f += step)
                {
                    string attribute = listCollection.GetValue(listIndex, n, f);
                    if (Convert.ToInt32(attribute) > maxPropertyId)
                    {
                        results.Add("Invalid Property: " + attribute + " (" + label + ": " + listCollection.GetValue(listIndex, n, 0) + ")");
                    }
                }
            }
        }
    }
}
