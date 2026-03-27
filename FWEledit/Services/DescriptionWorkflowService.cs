using System;

namespace FWEledit
{
    public sealed class DescriptionWorkflowService
    {
        public DescriptionSelectionResult BuildSelection(
            eListCollection listCollection,
            int listIndex,
            int rowIndex,
            object idCellValue,
            Func<int, string> fallback,
            DescriptionViewModel descriptionViewModel)
        {
            DescriptionSelectionResult result = new DescriptionSelectionResult();
            if (descriptionViewModel == null || listCollection == null || listIndex < 0)
            {
                return result;
            }

            if (listIndex == listCollection.ConversationListIndex)
            {
                descriptionViewModel.GetEditorTextForItem(0, null);
                result.IsConversation = true;
                return result;
            }

            if (rowIndex < 0 || rowIndex >= listCollection.Lists[listIndex].elementValues.Length)
            {
                return result;
            }

            int id;
            if (!int.TryParse(Convert.ToString(idCellValue), out id))
            {
                int.TryParse(listCollection.GetValue(listIndex, rowIndex, 0), out id);
            }

            result.ItemId = id;
            result.EditorText = descriptionViewModel.GetEditorTextForItem(id, fallback);
            result.HasDescription = true;
            return result;
        }

        public DescriptionChangeResult StageEditorText(DescriptionViewModel descriptionViewModel, string editorText)
        {
            DescriptionChangeResult result = new DescriptionChangeResult();
            if (descriptionViewModel == null)
            {
                return result;
            }

            string statusText;
            result.Changed = descriptionViewModel.StageEditorText(editorText ?? string.Empty, out statusText);
            result.StatusText = statusText ?? string.Empty;
            return result;
        }

        public DescriptionFlushResult FlushPendingDescriptions(DescriptionViewModel descriptionViewModel, AssetManager assetManager)
        {
            DescriptionFlushResult result = new DescriptionFlushResult();
            if (descriptionViewModel == null)
            {
                result.Success = true;
                return result;
            }

            if (!descriptionViewModel.HasPendingChanges)
            {
                result.Success = true;
                result.HadPendingChanges = false;
                return result;
            }

            string statusText;
            string errorMessage;
            if (!descriptionViewModel.FlushToDisk(assetManager, out statusText, out errorMessage))
            {
                result.Success = false;
                result.ErrorMessage = errorMessage ?? string.Empty;
                return result;
            }

            result.Success = true;
            result.HadPendingChanges = true;
            result.StatusText = statusText ?? string.Empty;
            return result;
        }
    }
}
