using System;

namespace FWEledit
{
    public sealed class ElementsRulesExportWorkflowService
    {
        private readonly ElementsImportExportService importExportService;

        public ElementsRulesExportWorkflowService(ElementsImportExportService importExportService)
        {
            if (importExportService == null)
            {
                throw new ArgumentNullException(nameof(importExportService));
            }
            this.importExportService = importExportService;
        }

        public FileExportResult ExportWithRules(eListCollection listCollection, string rulesLabel, string filePath)
        {
            FileExportResult result = new FileExportResult { Success = false };
            if (listCollection == null)
            {
                result.ErrorMessage = "No File Loaded!";
                return result;
            }
            if (string.IsNullOrWhiteSpace(filePath))
            {
                result.ErrorMessage = "Invalid output path.";
                return result;
            }
            if (string.IsNullOrWhiteSpace(rulesLabel))
            {
                result.ErrorMessage = "Invalid rules selection.";
                return result;
            }

            try
            {
                importExportService.ExportElementsWithRules(listCollection, rulesLabel, filePath);
                result.Success = true;
                return result;
            }
            catch
            {
                result.ErrorMessage = "EXPORTING ERROR!\nThis error mostly occurs if selected rules fileset is invalid";
                return result;
            }
        }
    }
}
