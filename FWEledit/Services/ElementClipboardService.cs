using System;

namespace FWEledit
{
    public sealed class ElementClipboardService
    {
        public ClipboardCopyResult BuildClipboardText(eListCollection listCollection, int listIndex, int elementIndex)
        {
            ClipboardCopyResult result = new ClipboardCopyResult { Success = false };
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
            if (listIndex < 0 || elementIndex < 0)
            {
                result.ErrorMessage = "Invalid List";
                return result;
            }
            if (listIndex >= listCollection.Lists.Length)
            {
                result.ErrorMessage = "Invalid List";
                return result;
            }

            int nameIndex = GetNameFieldIndex(listCollection, listIndex);
            if (nameIndex < 0)
            {
                result.ErrorMessage = "Config Error: cannot find Name field";
                return result;
            }

            string id = listCollection.GetValue(listIndex, elementIndex, 0);
            string name = listCollection.GetValue(listIndex, elementIndex, nameIndex);
            result.ClipboardText = id + "\t" + name;
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
