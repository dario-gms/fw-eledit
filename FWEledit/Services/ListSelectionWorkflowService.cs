using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ListSelectionWorkflowService
    {
        public ListSelectionResult BuildSelection(ListSelectionRequest request)
        {
            ListSelectionResult result = new ListSelectionResult
            {
                ListIndex = request != null ? request.ListIndex : -1,
                Rows = new List<object[]>()
            };

            if (request == null || request.ListCollection == null)
            {
                result.ErrorMessage = "Invalid list selection request.";
                return result;
            }
            if (request.ListDisplayService == null || request.ListRowBuilderService == null)
            {
                result.ErrorMessage = "List display services unavailable.";
                return result;
            }
            if (request.ListIndex < 0 || request.ListIndex >= request.ListCollection.Lists.Length)
            {
                result.ErrorMessage = "Invalid list selection.";
                return result;
            }

            if (request.ListIndex == 0 &&
                request.ListCollection.Lists.Length > 0 &&
                request.ListDisplayService.List0DisplayNameCount != request.ListCollection.Lists[0].elementValues.Length)
            {
                request.ListDisplayService.ResetList0DisplayCache();
                request.ListDisplayService.InvalidateListDisplayCache(0);
            }

            result.OffsetText = request.ListCollection.GetOffset(request.ListIndex);
            result.HasXref = request.Xrefs != null
                && request.ListIndex < request.Xrefs.Length
                && request.Xrefs[request.ListIndex] != null
                && request.Xrefs[request.ListIndex].Length > 1;

            List<object[]> rows;
            if (!request.ListDisplayService.TryGetListDisplayRows(request.ListIndex, out rows))
            {
                rows = request.ListRowBuilderService.BuildRows(
                    request.ListCollection,
                    request.ConversationList,
                    request.Database,
                    request.ListIndex,
                    request.ComposeListDisplayName);
                request.ListDisplayService.SetListDisplayRows(request.ListIndex, rows);
            }

            result.Rows = rows ?? new List<object[]>();
            result.Success = true;
            return result;
        }
    }
}
