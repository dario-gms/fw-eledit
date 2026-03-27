using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FWEledit
{
    public sealed class AddonTypeOptionService
    {
        private readonly AddonTypeHintService addonTypeHintService;

        public AddonTypeOptionService(AddonTypeHintService addonTypeHintService)
        {
            this.addonTypeHintService = addonTypeHintService ?? new AddonTypeHintService();
        }

        public List<AddonTypeOption> BuildOptions(
            ISessionService sessionService,
            eListCollection listCollection,
            int listIndex,
            string appPath,
            string gameRootPath)
        {
            List<AddonTypeOption> options = new List<AddonTypeOption>();
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return options;
            }

            bool isAddonList = listIndex == 0;
            int typeFieldIndex = -1;
            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                if (string.Equals(listCollection.Lists[listIndex].elementFields[i], "type", StringComparison.OrdinalIgnoreCase))
                {
                    typeFieldIndex = i;
                    break;
                }
            }
            if (typeFieldIndex < 0)
            {
                return options;
            }

            HashSet<int> usedTypes = new HashSet<int>();
            Dictionary<int, int> firstRowByType = new Dictionary<int, int>();
            for (int row = 0; row < listCollection.Lists[listIndex].elementValues.Length; row++)
            {
                int typeValue;
                if (!int.TryParse(listCollection.GetValue(listIndex, row, typeFieldIndex), out typeValue))
                {
                    continue;
                }
                if (typeValue < 0)
                {
                    continue;
                }
                usedTypes.Add(typeValue);
                if (!firstRowByType.ContainsKey(typeValue))
                {
                    firstRowByType[typeValue] = row;
                }
            }

            Dictionary<int, string> hintedTypes = addonTypeHintService.LoadHints(appPath, gameRootPath);
            HashSet<int> knownTypes = new HashSet<int>(usedTypes);
            if (isAddonList)
            {
                int maxType = 145;
                if (usedTypes.Count > 0)
                {
                    maxType = Math.Max(maxType, usedTypes.Max());
                }
                for (int i = 0; i <= maxType; i++)
                {
                    knownTypes.Add(i);
                }
            }
            else
            {
                foreach (int key in hintedTypes.Keys)
                {
                    knownTypes.Add(key);
                }
            }

            if (knownTypes.Count == 0)
            {
                return options;
            }

            List<int> sorted = knownTypes.OrderBy(x => x).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                int typeId = sorted[i];
                string display;
                if (isAddonList)
                {
                    display = EQUIPMENT_ADDON.GetAddonTypeDisplay(typeId.ToString());
                    if (string.Equals(display, typeId.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        int rowIndex;
                        if (firstRowByType.TryGetValue(typeId, out rowIndex))
                        {
                            try
                            {
                                string id = listCollection.GetValue(listIndex, rowIndex, 0);
                                string decoded = EQUIPMENT_ADDON.GetAddon(sessionService, id);
                                if (!string.IsNullOrWhiteSpace(decoded))
                                {
                                    display = decoded.Replace("\r", " ").Replace("\n", " / ").Trim();
                                }
                            }
                            catch
                            { }
                        }
                    }
                    if (string.Equals(display, typeId.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        string hint;
                        if (hintedTypes.TryGetValue(typeId, out hint))
                        {
                            display = hint;
                        }
                    }
                }
                else
                {
                    if (!hintedTypes.TryGetValue(typeId, out display))
                    {
                        display = typeId.ToString(CultureInfo.InvariantCulture);
                    }
                }

                options.Add(new AddonTypeOption
                {
                    TypeId = typeId,
                    Display = display
                });
            }

            return options;
        }
    }
}
