using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class SkillValidationService
    {
        public List<string> BuildInvalidSkills(eListCollection listCollection, int maxSkillId)
        {
            List<string> results = new List<string>();
            if (listCollection == null || listCollection.Lists.Length <= 38)
            {
                return results;
            }

            for (int n = 0; n < listCollection.Lists[38].elementValues.Length; n++)
            {
                for (int f = 119; f < 130; f += 2)
                {
                    string skill = listCollection.GetValue(38, n, f);
                    if (Convert.ToInt32(skill) > maxSkillId)
                    {
                        results.Add("Invalid Skill: " + skill + " (Monster: " + listCollection.GetValue(38, n, 0) + ")");
                    }
                }
            }

            return results;
        }
    }
}
