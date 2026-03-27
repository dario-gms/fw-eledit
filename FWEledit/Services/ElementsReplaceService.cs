using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ElementsReplaceService
    {
        public ReplacementSummary ReplaceSkills(eListCollection listCollection, IDictionary<string, string> replacements)
        {
            ReplacementSummary summary = new ReplacementSummary();
            if (listCollection == null || replacements == null || replacements.Count == 0)
            {
                return summary;
            }
            if (listCollection.Lists.Length <= 38)
            {
                return summary;
            }

            // Monster skills (list 38 fields 119, 121, 123, 125, 127, 129)
            for (int n = 0; n < listCollection.Lists[38].elementValues.Length; n++)
            {
                for (int f = 119; f < 130; f += 2)
                {
                    string skill = listCollection.GetValue(38, n, f);
                    string replacement;
                    if (replacements.TryGetValue(skill, out replacement))
                    {
                        listCollection.SetValue(38, n, f, replacement);
                        summary.ReplacementCount++;
                    }
                }
            }

            return summary;
        }

        public ReplacementSummary ReplaceProperties(eListCollection listCollection, IDictionary<string, string> replacements)
        {
            ReplacementSummary summary = new ReplacementSummary();
            if (listCollection == null || replacements == null || replacements.Count == 0)
            {
                return summary;
            }

            ReplacePropertiesInList(listCollection, replacements, 3, 43, 202, 2, ref summary);
            ReplacePropertiesInList(listCollection, replacements, 6, 55, 180, 2, ref summary);
            ReplacePropertiesInList(listCollection, replacements, 9, 44, 161, 2, ref summary);
            ReplacePropertiesInList(listCollection, replacements, 35, 11, 13, 1, ref summary, true);
            ReplacePropertiesInList(listCollection, replacements, 90, 15, 20, 1, ref summary);

            return summary;
        }

        public ReplacementSummary ReplaceTomeProperties(eListCollection listCollection, IDictionary<string, string[]> replacements)
        {
            ReplacementSummary summary = new ReplacementSummary();
            if (listCollection == null || replacements == null || replacements.Count == 0)
            {
                return summary;
            }
            if (listCollection.Lists.Length <= 112)
            {
                return summary;
            }

            List<string> attributesReplaced = new List<string>();

            for (int n = 0; n < listCollection.Lists[112].elementValues.Length; n++)
            {
                attributesReplaced.Clear();

                for (int f = 4; f < 14; f++)
                {
                    string attribute = listCollection.GetValue(112, n, f);
                    if (attribute == "0")
                    {
                        continue;
                    }

                    string[] mapped;
                    if (replacements.TryGetValue(attribute, out mapped))
                    {
                        for (int a = 0; a < mapped.Length; a++)
                        {
                            if (!string.IsNullOrWhiteSpace(mapped[a]))
                            {
                                attributesReplaced.Add(mapped[a]);
                            }
                        }
                    }
                    else
                    {
                        attributesReplaced.Add(attribute);
                    }
                }

                if (attributesReplaced.Count > 10)
                {
                    summary.Warnings.Add("Tome Attribute Overflow: " + n + "\nAttributes Truncated");
                }

                for (int f = 4; f < 14; f++)
                {
                    if (f - 4 < attributesReplaced.Count)
                    {
                        string attribute = attributesReplaced[f - 4];
                        listCollection.SetValue(112, n, f, attribute);
                        summary.ReplacementCount++;
                    }
                    else
                    {
                        listCollection.SetValue(112, n, f, "0");
                    }
                }
            }

            return summary;
        }

        private static void ReplacePropertiesInList(
            eListCollection listCollection,
            IDictionary<string, string> replacements,
            int listIndex,
            int startField,
            int endFieldExclusive,
            int step,
            ref ReplacementSummary summary,
            bool updateSoulgemDescriptions = false)
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
                    string replacement;
                    if (replacements.TryGetValue(attribute, out replacement))
                    {
                        listCollection.SetValue(listIndex, n, f, replacement);
                        summary.ReplacementCount++;

                        if (updateSoulgemDescriptions)
                        {
                            ApplySoulgemDescriptionUpdates(listCollection, listIndex, n, f, replacement);
                        }
                    }
                }
            }
        }

        private static void ApplySoulgemDescriptionUpdates(eListCollection listCollection, int listIndex, int elementIndex, int fieldIndex, string replacement)
        {
            if (listCollection == null)
            {
                return;
            }
            if (string.Equals(replacement, "1515", StringComparison.OrdinalIgnoreCase))
            {
                listCollection.SetValue(listIndex, elementIndex, fieldIndex + 2, "Vit. +20");
            }
            if (string.Equals(replacement, "1517", StringComparison.OrdinalIgnoreCase))
            {
                listCollection.SetValue(listIndex, elementIndex, fieldIndex + 2, "Critical +2%");
            }
            if (string.Equals(replacement, "1518", StringComparison.OrdinalIgnoreCase))
            {
                listCollection.SetValue(listIndex, elementIndex, fieldIndex + 2, "Channel -6%");
            }
        }
    }
}
