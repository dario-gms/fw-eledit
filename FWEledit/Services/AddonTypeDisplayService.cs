using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class AddonTypeDisplayService
    {
        public string GetAddonTypeDisplayForUi(
            ISessionService sessionService,
            eListCollection listCollection,
            int listIndex,
            int entryIndex,
            string rawType,
            Func<Dictionary<int, string>> loadHints)
        {
            if (string.IsNullOrWhiteSpace(rawType))
            {
                return rawType ?? string.Empty;
            }

            int typeId;
            if (!int.TryParse(rawType, out typeId))
            {
                return rawType;
            }

            Dictionary<int, string> hintedTypes = loadHints != null ? loadHints() : new Dictionary<int, string>();
            string hint;
            if (hintedTypes.TryGetValue(typeId, out hint) && !string.IsNullOrWhiteSpace(hint))
            {
                return hint.Trim();
            }

            string fallback = EQUIPMENT_ADDON.GetAddonTypeDisplay(rawType);
            if (!string.Equals(fallback, rawType, StringComparison.OrdinalIgnoreCase))
            {
                return fallback;
            }

            if (listCollection != null && listIndex == 0 && entryIndex >= 0)
            {
                try
                {
                    string id = listCollection.GetValue(listIndex, entryIndex, 0);
                    string decoded = EQUIPMENT_ADDON.GetAddon(sessionService, id);
                    if (!string.IsNullOrWhiteSpace(decoded))
                    {
                        return decoded.Replace("\r", " ").Replace("\n", " / ").Trim();
                    }
                }
                catch
                { }
            }

            return rawType;
        }
    }
}
