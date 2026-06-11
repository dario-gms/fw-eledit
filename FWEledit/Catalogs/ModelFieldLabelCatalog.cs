using System;
using System.Collections.Generic;

namespace FWEledit
{
    public static class ModelFieldLabelCatalog
    {
        private static readonly Dictionary<string, string> AircraftVehicleRaceLabels =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "file_models_1", "file_models_human" },
                { "file_models_2", "file_models_elf" },
                { "file_models_3", "file_models_dwarf" },
                { "file_models_4", "file_models_stoneman" },
                { "file_models_5", "file_models_kindred" },
                { "file_models_6", "file_models_lycan" }
            };

        public static string GetDisplayFieldName(
            eListCollection listCollection,
            string listName,
            int listIndex,
            int fieldIndex,
            string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return string.Empty;
            }

            string normalizedFieldName = fieldName.Trim();
            if (NpcSellServiceCatalog.IsNpcSellServiceList(listCollection, listIndex))
            {
                return NpcSellServiceCatalog.GetDisplayFieldName(listCollection, listIndex, fieldIndex, normalizedFieldName);
            }

            if ((listIndex == 21 || listIndex == 77)
                && AircraftVehicleRaceLabels.TryGetValue(normalizedFieldName, out string displayFieldName))
            {
                return displayFieldName;
            }

            return normalizedFieldName;
        }
    }
}
