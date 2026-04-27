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

        public bool TryBuildPreviewMeshData(
            AssetManager assetManager,
            CacheSave database,
            int pathId,
            string fieldName,
            string listName,
            ModelPickerService modelPickerService,
            out ModelPreviewMeshData meshData,
            out string errorMessage)
        {
            meshData = null;
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
            if (!modelPickerService.TryResolveModelPathById(database, pathId, fieldName, listName, out resolvedPathId, out mappedPath, true))
            {
                errorMessage = "Model PathID not found in path.data:\n" + pathId;
                return false;
            }

            try
            {
                if (!embeddedPreviewLoaderService.TryLoadPreviewMesh(assetManager, mappedPath, out meshData, out errorMessage))
                {
                    if (string.IsNullOrWhiteSpace(errorMessage))
                    {
                        errorMessage = "Model preview unavailable for this file.";
                    }
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "MODEL PREVIEW ERROR!\n" + ex.Message;
                meshData = null;
                return false;
            }
        }

        public void ShowPreviewWindow(ModelPreviewMeshData meshData)
        {
            if (meshData == null)
            {
                return;
            }

            ModelPreviewWindow window = new ModelPreviewWindow(meshData);
            window.Show();
            window.BringToFront();
        }

        public bool TryOpenPreview(
            AssetManager assetManager,
            CacheSave database,
            int pathId,
            string fieldName,
            string listName,
            ModelPickerService modelPickerService,
            out string errorMessage)
        {
            if (!TryBuildPreviewMeshData(
                assetManager,
                database,
                pathId,
                fieldName,
                listName,
                modelPickerService,
                out ModelPreviewMeshData meshData,
                out errorMessage))
            {
                return false;
            }

            ShowPreviewWindow(meshData);
            return true;
        }
    }
}
