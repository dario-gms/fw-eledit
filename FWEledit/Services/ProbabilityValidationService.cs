using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ProbabilityValidationService
    {
        public List<string> BuildInvalidProbabilities(eListCollection listCollection)
        {
            List<string> results = new List<string>();
            if (listCollection == null)
            {
                return results;
            }

            if (listCollection.Lists.Length > 3)
            {
                for (int n = 0; n < listCollection.Lists[3].elementValues.Length; n++)
                {
                    double attribute = 0;
                    for (int f = 32; f < 35; f++)
                    {
                        attribute += Convert.ToDouble(listCollection.GetValue(3, n, f));
                    }
                    if (Math.Round(attribute, 6) != 1)
                    {
                        results.Add("Suspicious Socket Drop Probability (sum != 1.0): " + attribute + " (Weapon: " + listCollection.GetValue(3, n, 0) + ")");
                    }

                    attribute = 0;
                    for (int f = 35; f < 38; f++)
                    {
                        attribute += Convert.ToDouble(listCollection.GetValue(3, n, f));
                    }
                    if (Math.Round(attribute, 6) != 1)
                    {
                        results.Add("Suspicious Socket Craft Probability (sum != 1.0): " + attribute + " (Weapon: " + listCollection.GetValue(3, n, 0) + ")");
                    }

                    attribute = 0;
                    for (int f = 38; f < 42; f++)
                    {
                        attribute += Convert.ToDouble(listCollection.GetValue(3, n, f));
                    }
                    if (Math.Round(attribute, 6) != 1)
                    {
                        results.Add("Suspicious Addon Count Probability (sum != 1.0): " + attribute + " (Weapon: " + listCollection.GetValue(3, n, 0) + ")");
                    }

                    attribute = 0;
                    for (int f = 44; f < 107; f += 2)
                    {
                        attribute += Convert.ToDouble(listCollection.GetValue(3, n, f));
                    }
                    if (Math.Round(attribute, 6) != 1)
                    {
                        results.Add("Suspicious Drop Attriutes Probability (sum != 1.0): " + attribute + " (Weapon: " + listCollection.GetValue(3, n, 0) + ")");
                    }

                    attribute = 0;
                    for (int f = 108; f < 171; f += 2)
                    {
                        attribute += Convert.ToDouble(listCollection.GetValue(3, n, f));
                    }
                    if (Math.Round(attribute, 6) != 1)
                    {
                        results.Add("Suspicious Craft Attributes Probability (sum != 1.0): " + attribute + " (Weapon: " + listCollection.GetValue(3, n, 0) + ")");
                    }

                    attribute = 0;
                    for (int f = 172; f < 203; f += 2)
                    {
                        attribute += Convert.ToDouble(listCollection.GetValue(3, n, f));
                    }
                    if (Math.Round(attribute, 6) != 1)
                    {
                        results.Add("Suspicious Unique Attributes Probability (sum != 1.0): " + attribute + " (Weapon: " + listCollection.GetValue(3, n, 0) + ")");
                    }
                }
            }

            if (listCollection.Lists.Length > 6)
            {
                for (int n = 0; n < listCollection.Lists[6].elementValues.Length; n++)
                {
                    double attribute = 0;
                    for (int f = 41; f < 46; f++)
                    {
                        attribute += Convert.ToDouble(listCollection.GetValue(6, n, f));
                    }
                    if (Math.Round(attribute, 6) != 1)
                    {
                        results.Add("Suspicious Socket Drop Probability (sum != 1.0): " + attribute + " (Armor: " + listCollection.GetValue(6, n, 0) + ")");
                    }

                    attribute = 0;
                    for (int f = 46; f < 51; f++)
                    {
                        attribute += Convert.ToDouble(listCollection.GetValue(6, n, f));
                    }
                    if (Math.Round(attribute, 6) != 1)
                    {
                        results.Add("Suspicious Socket Craft Probability (sum != 1.0): " + attribute + " (Armor: " + listCollection.GetValue(6, n, 0) + ")");
                    }

                    attribute = 0;
                    for (int f = 51; f < 55; f++)
                    {
                        attribute += Convert.ToDouble(listCollection.GetValue(6, n, f));
                    }
                    if (Math.Round(attribute, 6) != 1)
                    {
                        results.Add("Suspicious Addon Count Probability (sum != 1.0): " + attribute + " (Armor: " + listCollection.GetValue(6, n, 0) + ")");
                    }

                    attribute = 0;
                    for (int f = 56; f < 119; f += 2)
                    {
                        attribute += Convert.ToDouble(listCollection.GetValue(6, n, f));
                    }
                    if (Math.Round(attribute, 6) != 1)
                    {
                        results.Add("Suspicious Drop Attriutes Probability (sum != 1.0): " + attribute + " (Armor: " + listCollection.GetValue(6, n, 0) + ")");
                    }

                    attribute = 0;
                    for (int f = 120; f < 181; f += 2)
                    {
                        attribute += Convert.ToDouble(listCollection.GetValue(6, n, f));
                    }
                    if (Math.Round(attribute, 6) != 1)
                    {
                        results.Add("Suspicious Craft Attributes Probability (sum != 1.0): " + attribute + " (Armor: " + listCollection.GetValue(6, n, 0) + ")");
                    }
                }
            }

            if (listCollection.Lists.Length > 9)
            {
                for (int n = 0; n < listCollection.Lists[9].elementValues.Length; n++)
                {
                    double attribute = 0;
                    for (int f = 40; f < 44; f++)
                    {
                        attribute += Convert.ToDouble(listCollection.GetValue(9, n, f));
                    }
                    if (Math.Round(attribute, 6) != 1)
                    {
                        results.Add("Suspicious Addon Count Probability (sum != 1.0): " + attribute + " (Ornament: " + listCollection.GetValue(9, n, 0) + ")");
                    }

                    attribute = 0;
                    for (int f = 45; f < 108; f += 2)
                    {
                        attribute += Convert.ToDouble(listCollection.GetValue(9, n, f));
                    }
                    if (Math.Round(attribute, 6) != 1)
                    {
                        results.Add("Suspicious Drop Attriutes Probability (sum != 1.0): " + attribute + " (Ornament: " + listCollection.GetValue(9, n, 0) + ")");
                    }

                    attribute = 0;
                    for (int f = 109; f < 162; f += 2)
                    {
                        attribute += Convert.ToDouble(listCollection.GetValue(9, n, f));
                    }
                    if (Math.Round(attribute, 6) != 1)
                    {
                        results.Add("Suspicious Craft Attributes Probability (sum != 1.0): " + attribute + " (Ornament: " + listCollection.GetValue(9, n, 0) + ")");
                    }
                }
            }

            return results;
        }
    }
}
