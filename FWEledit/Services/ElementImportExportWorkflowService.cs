using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ElementImportExportWorkflowService
    {
        private readonly ElementsImportExportService elementsImportExportService;

        public ElementImportExportWorkflowService(ElementsImportExportService elementsImportExportService)
        {
            this.elementsImportExportService = elementsImportExportService ?? throw new ArgumentNullException(nameof(elementsImportExportService));
        }

        public ElementExportResult ExportItems(eListCollection listCollection, int listIndex, int[] itemIndices, string outputFolder, Action<int> progress)
        {
            ElementExportResult result = new ElementExportResult();
            if (listCollection == null)
            {
                result.ErrorMessage = "EXPORT ERROR!\nNo file loaded.";
                return result;
            }
            if (listIndex == listCollection.ConversationListIndex)
            {
                result.IsConversationList = true;
                return result;
            }

            try
            {
                elementsImportExportService.ExportItems(listCollection, listIndex, itemIndices, outputFolder, progress);
                result.Success = true;
                return result;
            }
            catch
            {
                result.ErrorMessage = "EXPORT ERROR!\nExporting item to unicode text file failed!";
                return result;
            }
        }

        public ElementImportResult ImportItem(eListCollection listCollection, int listIndex, int targetIndex, string filePath)
        {
            ElementImportResult result = new ElementImportResult();
            if (listCollection == null)
            {
                result.ErrorMessage = "IMPORT ERROR!\nNo file loaded.";
                return result;
            }
            if (listIndex == listCollection.ConversationListIndex)
            {
                result.IsConversationList = true;
                return result;
            }

            try
            {
                elementsImportExportService.ImportItem(listCollection, listIndex, filePath, targetIndex);
                result.Success = true;
                return result;
            }
            catch
            {
                result.ErrorMessage = "IMPORT ERROR!\nCheck if the item version matches the elements.data version and is imported to the correct list!";
                return result;
            }
        }

        public ElementBatchAddResult AddItemsFromFiles(eListCollection listCollection, int listIndex, string[] fileNames, Action<int> progress)
        {
            ElementBatchAddResult result = new ElementBatchAddResult();
            if (listCollection == null)
            {
                result.ErrorMessage = "IMPORT ERROR!\nNo file loaded.";
                return result;
            }
            if (listIndex == listCollection.ConversationListIndex)
            {
                result.IsConversationList = true;
                return result;
            }
            if (fileNames == null || fileNames.Length == 0)
            {
                return result;
            }

            List<int> newIndices = new List<int>();
            try
            {
                for (int i = 0; i < fileNames.Length; i++)
                {
                    int templateIndex = listCollection.Lists[listIndex].elementValues.Length - 1;
                    int newIndex = elementsImportExportService.AddItemFromFile(listCollection, listIndex, fileNames[i], templateIndex);
                    newIndices.Add(newIndex);
                    if (progress != null)
                    {
                        progress(i + 1);
                    }
                }
            }
            catch
            {
                result.ErrorMessage = "IMPORT ERROR!\nCheck if the item version matches the elements.data version and is imported to the correct list!";
                return result;
            }

            result.Success = true;
            result.NewIndices = newIndices.ToArray();
            return result;
        }

        public ElementExportResult ExportNpcListPairs(eListCollection listCollection, string outputPath)
        {
            ElementExportResult result = new ElementExportResult();
            if (listCollection == null)
            {
                result.ErrorMessage = "SAVING ERROR!";
                return result;
            }

            try
            {
                elementsImportExportService.ExportNpcListPairs(listCollection, outputPath);
                result.Success = true;
                return result;
            }
            catch
            {
                result.ErrorMessage = "SAVING ERROR!";
                return result;
            }
        }
    }
}
