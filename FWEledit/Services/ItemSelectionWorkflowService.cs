using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ItemSelectionWorkflowService
    {
        private readonly ItemValueRowBuilderService itemValueRowBuilderService;

        public ItemSelectionWorkflowService(ItemValueRowBuilderService itemValueRowBuilderService)
        {
            this.itemValueRowBuilderService = itemValueRowBuilderService;
        }

        public ItemSelectionResult BuildSelection(ItemSelectionRequest request)
        {
            ItemSelectionResult result = new ItemSelectionResult
            {
                ListIndex = request != null ? request.ListIndex : -1,
                ElementIndex = request != null ? request.ElementIndex : -1,
                Rows = new List<ValueRowDisplay>()
            };

            if (request == null || request.ListCollection == null || itemValueRowBuilderService == null)
            {
                result.ErrorMessage = "Invalid item selection request.";
                return result;
            }

            if (request.ListIndex < 0 || request.ElementIndex < 0)
            {
                result.ErrorMessage = "Invalid item selection.";
                return result;
            }

            try
            {
                result.Rows = itemValueRowBuilderService.BuildRows(
                    request.Session,
                    request.ListCollection,
                    request.ConversationList,
                    request.Database,
                    request.ListIndex,
                    request.ElementIndex,
                    request.ShouldIncludeField,
                    request.GetDisplayEntryName,
                    request.LoadAddonTypeHints,
                    request.IsModelFieldName,
                    request.IsFieldInvalid,
                    request.IsFieldDirty);
                result.Success = true;
            }
            catch
            {
                result.ErrorMessage = "Failed to build item rows.";
            }

            return result;
        }
    }
}
