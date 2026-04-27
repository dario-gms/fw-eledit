using System;

namespace FWEledit
{
    public sealed class ModelPreviewService
    {
        private readonly PckEntryReaderService pckEntryReaderService = new PckEntryReaderService();
        private readonly EmbeddedModelPreviewLoaderService embeddedPreviewLoaderService;

        public ModelPreviewService()
        {
            embeddedPreviewLoaderService = new EmbeddedModelPreviewLoaderService(pckEntryReaderService);
        }

        public bool TryOpenPreview(
            AssetManager assetManager,
            CacheSave database,
            int pathId,
            string fieldName,
            ModelPickerService modelPickerService,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (assetManager == null || modelPickerService == null)
            {
                errorMessage = "Model preview unavailable.";
                return false;
            }
            if (pathId <= 0)
            {
                errorMessage = "Invalid model PathID.";
                return false;
            }

            int resolvedPathId;
            string mappedPath;
            if (!modelPickerService.TryResolveModelPathById(database, pathId, fieldName, out resolvedPathId, out mappedPath, true))
            {
                errorMessage = "Model PathID not found in path.data:\n" + pathId;
                return false;
            }

            try
            {
                ModelPreviewMeshData meshData;
                if (!embeddedPreviewLoaderService.TryLoadPreviewMesh(assetManager, mappedPath, out meshData, out errorMessage))
                {
                    if (string.IsNullOrWhiteSpace(errorMessage))
                    {
                        errorMessage = "Model preview unavailable for this file.";
                    }
                    return false;
                }

                ModelPreviewWindow window = new ModelPreviewWindow(meshData);
                window.Show();
                window.BringToFront();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "MODEL PREVIEW ERROR!\n" + ex.Message;
                return false;
            }
        }
    }
}
