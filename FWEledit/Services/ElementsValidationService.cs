using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ElementsValidationService
    {
        public List<string> ValidateUniqueIds(eListCollection listCollection, Func<int, int> getIdFieldIndex)
        {
            List<string> issues = new List<string>();
            if (listCollection == null)
            {
                return issues;
            }

            for (int l = 0; l < listCollection.Lists.Length; l++)
            {
                if (l == listCollection.ConversationListIndex)
                {
                    continue;
                }

                int idFieldIndex = getIdFieldIndex != null ? getIdFieldIndex(l) : -1;
                if (idFieldIndex < 0)
                {
                    continue;
                }

                Dictionary<int, int> firstRowById = new Dictionary<int, int>();
                for (int r = 0; r < listCollection.Lists[l].elementValues.Length; r++)
                {
                    int id;
                    string raw = listCollection.GetValue(l, r, idFieldIndex);
                    if (!int.TryParse(raw, out id))
                    {
                        issues.Add("List [" + l + "] has non-numeric ID at row " + (r + 1) + ": " + raw);
                        if (issues.Count >= 30)
                        {
                            break;
                        }
                        continue;
                    }

                    int firstRow;
                    if (firstRowById.TryGetValue(id, out firstRow))
                    {
                        issues.Add("List [" + l + "] duplicate ID " + id + " at rows " + (firstRow + 1) + " and " + (r + 1));
                        if (issues.Count >= 30)
                        {
                            break;
                        }
                    }
                    else
                    {
                        firstRowById[id] = r;
                    }
                }

                if (issues.Count >= 30)
                {
                    break;
                }
            }

            return issues;
        }

        public List<string> ValidateSkills(eListCollection listCollection)
        {
            List<string> issues = new List<string>();
            if (listCollection == null || listCollection.Lists.Length <= 38)
            {
                return issues;
            }

            // Monster skills (list 38 fields 119, 121, 123, 125, 127, 129)
            for (int n = 0; n < listCollection.Lists[38].elementValues.Length; n++)
            {
                for (int f = 119; f < 130; f += 2)
                {
                    string skill = listCollection.GetValue(38, n, f);
                    if (Convert.ToInt32(skill) > 846)
                    {
                        issues.Add("Invalid Skill: " + skill + " (Monster: " + listCollection.GetValue(38, n, 0) + ")");
                    }
                }
            }

            return issues;
        }

        public List<string> ValidateProperties(eListCollection listCollection)
        {
            List<string> issues = new List<string>();
            if (listCollection == null)
            {
                return issues;
            }

            ValidatePropertyRange(listCollection, 3, 43, 202, 2, "Weapon", issues);
            ValidatePropertyRange(listCollection, 6, 55, 180, 2, "Armor", issues);
            ValidatePropertyRange(listCollection, 9, 44, 161, 2, "Ornament", issues);
            ValidatePropertyRange(listCollection, 35, 11, 13, 1, "Soulgem", issues);
            ValidatePropertyRange(listCollection, 90, 15, 20, 1, "Complect Bonus", issues);

            return issues;
        }

        public List<string> ValidateTomeProperties(eListCollection listCollection)
        {
            List<string> issues = new List<string>();
            if (listCollection == null || listCollection.Lists.Length <= 112)
            {
                return issues;
            }

            for (int n = 0; n < listCollection.Lists[112].elementValues.Length; n++)
            {
                for (int f = 4; f < 14; f++)
                {
                    string attribute = listCollection.GetValue(112, n, f);
                    if (Convert.ToInt32(attribute) > 1909)
                    {
                        issues.Add("Invalid Property: " + attribute + " (Tome: " + listCollection.GetValue(112, n, 0) + ")");
                    }
                }
            }

            return issues;
        }

        public List<string> ValidateProbabilities(eListCollection listCollection)
        {
            List<string> issues = new List<string>();
            if (listCollection == null)
            {
                return issues;
            }

            ValidateWeaponProbabilities(listCollection, issues);
            ValidateArmorProbabilities(listCollection, issues);
            ValidateOrnamentProbabilities(listCollection, issues);

            return issues;
        }

        private static void ValidatePropertyRange(
            eListCollection listCollection,
            int listIndex,
            int startField,
            int endFieldExclusive,
            int step,
            string label,
            List<string> issues)
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
                    if (Convert.ToInt32(attribute) > 1909)
                    {
                        issues.Add("Invalid Property: " + attribute + " (" + label + ": " + listCollection.GetValue(listIndex, n, 0) + ")");
                    }
                }
            }
        }

        private static void ValidateWeaponProbabilities(eListCollection listCollection, List<string> issues)
        {
            if (listCollection.Lists.Length <= 3)
            {
                return;
            }

            for (int n = 0; n < listCollection.Lists[3].elementValues.Length; n++)
            {
                // weapon drop sockets count(fields 32-34, +=1)
                double attribute = 0;
                for (int f = 32; f < 35; f++)
                {
                    attribute += Convert.ToDouble(listCollection.GetValue(3, n, f));
                }
                if (Math.Round(attribute, 6) != 1)
                {
                    issues.Add("Suspicious Socket Drop Probability (sum != 1.0): " + attribute.ToString() + " (Weapon: " + listCollection.GetValue(3, n, 0) + ")");
                }

                // weapon craft sockets count(fields 35-37, +=1)
                attribute = 0;
                for (int f = 35; f < 38; f++)
                {
                    attribute += Convert.ToDouble(listCollection.GetValue(3, n, f));
                }
                if (Math.Round(attribute, 6) != 1)
                {
                    issues.Add("Suspicious Socket Craft Probability (sum != 1.0): " + attribute.ToString() + " (Weapon: " + listCollection.GetValue(3, n, 0) + ")");
                }

                // weapon addons count(fields 38-41, +=1)
                attribute = 0;
                for (int f = 38; f < 42; f++)
                {
                    attribute += Convert.ToDouble(listCollection.GetValue(3, n, f));
                }
                if (Math.Round(attribute, 6) != 1)
                {
                    issues.Add("Suspicious Addon Count Probability (sum != 1.0): " + attribute.ToString() + " (Weapon: " + listCollection.GetValue(3, n, 0) + ")");
                }

                // weapon drop (fields 44-106, +=2)
                attribute = 0;
                for (int f = 44; f < 107; f += 2)
                {
                    attribute += Convert.ToDouble(listCollection.GetValue(3, n, f));
                }
                if (Math.Round(attribute, 6) != 1)
                {
                    issues.Add("Suspicious Drop Attriutes Probability (sum != 1.0): " + attribute.ToString() + " (Weapon: " + listCollection.GetValue(3, n, 0) + ")");
                }

                // weapon craft (fields 108-170, +=2)
                attribute = 0;
                for (int f = 108; f < 171; f += 2)
                {
                    attribute += Convert.ToDouble(listCollection.GetValue(3, n, f));
                }
                if (Math.Round(attribute, 6) != 1)
                {
                    issues.Add("Suspicious Craft Attributes Probability (sum != 1.0): " + attribute.ToString() + " (Weapon: " + listCollection.GetValue(3, n, 0) + ")");
                }

                // weapons unique (fields 172-202, +=2)
                attribute = 0;
                for (int f = 172; f < 203; f += 2)
                {
                    attribute += Convert.ToDouble(listCollection.GetValue(3, n, f));
                }
                if (Math.Round(attribute, 6) != 1)
                {
                    issues.Add("Suspicious Unique Attributes Probability (sum != 1.0): " + attribute.ToString() + " (Weapon: " + listCollection.GetValue(3, n, 0) + ")");
                }
            }
        }

        private static void ValidateArmorProbabilities(eListCollection listCollection, List<string> issues)
        {
            if (listCollection.Lists.Length <= 6)
            {
                return;
            }

            for (int n = 0; n < listCollection.Lists[6].elementValues.Length; n++)
            {
                // armor drop sockets count(fields 41-45, +=1)
                double attribute = 0;
                for (int f = 41; f < 46; f++)
                {
                    attribute += Convert.ToDouble(listCollection.GetValue(6, n, f));
                }
                if (Math.Round(attribute, 6) != 1)
                {
                    issues.Add("Suspicious Socket Drop Probability (sum != 1.0): " + attribute.ToString() + " (Armor: " + listCollection.GetValue(6, n, 0) + ")");
                }

                // armor craft sockets count(fields 46-50, +=1)
                attribute = 0;
                for (int f = 46; f < 51; f++)
                {
                    attribute += Convert.ToDouble(listCollection.GetValue(6, n, f));
                }
                if (Math.Round(attribute, 6) != 1)
                {
                    issues.Add("Suspicious Socket Craft Probability (sum != 1.0): " + attribute.ToString() + " (Armor: " + listCollection.GetValue(6, n, 0) + ")");
                }

                // armor addons count(fields 51-54, +=1)
                attribute = 0;
                for (int f = 51; f < 55; f++)
                {
                    attribute += Convert.ToDouble(listCollection.GetValue(6, n, f));
                }
                if (Math.Round(attribute, 6) != 1)
                {
                    issues.Add("Suspicious Addon Count Probability (sum != 1.0): " + attribute.ToString() + " (Armor: " + listCollection.GetValue(6, n, 0) + ")");
                }

                // armor drop (fields 56-118, +=2)
                attribute = 0;
                for (int f = 56; f < 119; f += 2)
                {
                    attribute += Convert.ToDouble(listCollection.GetValue(6, n, f));
                }
                if (Math.Round(attribute, 6) != 1)
                {
                    issues.Add("Suspicious Drop Attriutes Probability (sum != 1.0): " + attribute.ToString() + " (Armor: " + listCollection.GetValue(6, n, 0) + ")");
                }

                // armor craft (fields 120-180, +=2)
                attribute = 0;
                for (int f = 120; f < 181; f += 2)
                {
                    attribute += Convert.ToDouble(listCollection.GetValue(6, n, f));
                }
                if (Math.Round(attribute, 6) != 1)
                {
                    issues.Add("Suspicious Craft Attributes Probability (sum != 1.0): " + attribute.ToString() + " (Armor: " + listCollection.GetValue(6, n, 0) + ")");
                }
            }
        }

        private static void ValidateOrnamentProbabilities(eListCollection listCollection, List<string> issues)
        {
            if (listCollection.Lists.Length <= 9)
            {
                return;
            }

            for (int n = 0; n < listCollection.Lists[9].elementValues.Length; n++)
            {
                // ornament addons count(fields 40-43, +=1)
                double attribute = 0;
                for (int f = 40; f < 44; f++)
                {
                    attribute += Convert.ToDouble(listCollection.GetValue(9, n, f));
                }
                if (Math.Round(attribute, 6) != 1)
                {
                    issues.Add("Suspicious Addon Count Probability (sum != 1.0): " + attribute.ToString() + " (Ornament: " + listCollection.GetValue(9, n, 0) + ")");
                }

                // ornament drop (fields 45-107, +=2)
                attribute = 0;
                for (int f = 45; f < 108; f += 2)
                {
                    attribute += Convert.ToDouble(listCollection.GetValue(9, n, f));
                }
                if (Math.Round(attribute, 6) != 1)
                {
                    issues.Add("Suspicious Drop Attriutes Probability (sum != 1.0): " + attribute.ToString() + " (Ornament: " + listCollection.GetValue(9, n, 0) + ")");
                }

                // ornament craft (fields 109-161, +=2)
                attribute = 0;
                for (int f = 109; f < 162; f += 2)
                {
                    attribute += Convert.ToDouble(listCollection.GetValue(9, n, f));
                }
                if (Math.Round(attribute, 6) != 1)
                {
                    issues.Add("Suspicious Craft Attributes Probability (sum != 1.0): " + attribute.ToString() + " (Ornament: " + listCollection.GetValue(9, n, 0) + ")");
                }
            }
        }
    }
}
