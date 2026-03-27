using System;

namespace FWEledit
{
    public sealed class XrefLookupService
    {
        public XrefLookupResult FindReferences(eListCollection listCollection, string[][] xrefs, int listIndex, int elementIndex)
        {
            XrefLookupResult result = new XrefLookupResult();
            if (listCollection == null)
            {
                result.ErrorMessage = "No File Loaded!";
                return result;
            }
            if (listIndex == listCollection.ConversationListIndex)
            {
                result.IsConversationList = true;
                return result;
            }
            if (listIndex < 0 || elementIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                result.ErrorMessage = "Invalid List";
                return result;
            }

            if (xrefs == null || listIndex >= xrefs.Length || xrefs[listIndex] == null || xrefs[listIndex].Length < 2)
            {
                result.HasRules = false;
                result.ErrorMessage = "No cross-reference rules for this list.";
                return result;
            }

            result.HasRules = true;
            char[] chars = { '-' };
            string sourceId = listCollection.GetValue(listIndex, elementIndex, 0);

            for (int j = 1; j < xrefs[listIndex].Length; j++)
            {
                string[] x = xrefs[listIndex][j].Split(chars);
                if (x.Length < 2)
                {
                    continue;
                }

                int targetListIndex;
                if (!int.TryParse(x[0], out targetListIndex))
                {
                    continue;
                }
                if (targetListIndex < 0 || targetListIndex >= listCollection.Lists.Length)
                {
                    continue;
                }

                int nameFieldIndex = GetNameFieldIndex(listCollection, targetListIndex);
                if (nameFieldIndex < 0)
                {
                    nameFieldIndex = 0;
                }

                for (int m = 1; m < listCollection.Lists[targetListIndex].elementValues.Length; m++)
                {
                    for (int k = 1; k < x.Length; k++)
                    {
                        int fieldIndex;
                        if (!int.TryParse(x[k], out fieldIndex))
                        {
                            continue;
                        }
                        if (fieldIndex < 0 || fieldIndex >= listCollection.Lists[targetListIndex].elementFields.Length)
                        {
                            continue;
                        }

                        if (listCollection.GetValue(targetListIndex, m, fieldIndex) == sourceId)
                        {
                            result.Rows.Add(new XrefResultRow
                            {
                                ListIndex = targetListIndex,
                                ListName = listCollection.Lists[targetListIndex].listName,
                                ItemId = listCollection.GetValue(targetListIndex, m, 0),
                                ItemName = listCollection.GetValue(targetListIndex, m, nameFieldIndex),
                                FieldLabel = x[k] + " - " + listCollection.Lists[targetListIndex].elementFields[fieldIndex]
                            });
                        }
                    }
                }
            }

            result.HasResults = result.Rows.Count > 0;
            result.Success = true;
            return result;
        }

        private static int GetNameFieldIndex(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return -1;
            }
            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                if (string.Equals(listCollection.Lists[listIndex].elementFields[i], "Name", StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
