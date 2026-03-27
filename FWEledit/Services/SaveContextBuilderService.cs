using System;

namespace FWEledit
{
    public sealed class SaveContextBuilderService
    {
        public ElementsSaveContext Build(
            eListCollection listCollection,
            eListConversation conversationList,
            string elementsPathOverride,
            AssetManager assetManager,
            Action beginProgress,
            Action<int> setProgress,
            Action endProgress,
            Func<bool> validateUniqueIds,
            Func<bool> flushPendingDescriptions,
            Action clearDirtyTracking,
            Action<string> showInfoMessage,
            Action<string> showErrorMessage,
            Action<string, Exception> logError,
            Func<NavigationSnapshot> captureNavigationSnapshot,
            Action<NavigationSnapshot> restoreNavigationSnapshot,
            Func<string> promptSavePath,
            Action<string> syncSummaryHandler)
        {
            return new ElementsSaveContext
            {
                ListCollection = listCollection,
                ConversationList = conversationList,
                ElementsPath = elementsPathOverride,
                AssetManager = assetManager,
                BeginProgress = beginProgress,
                SetProgress = setProgress,
                EndProgress = endProgress,
                ValidateUniqueIds = validateUniqueIds,
                FlushPendingDescriptions = flushPendingDescriptions,
                ClearDirtyTracking = clearDirtyTracking,
                ShowInfoMessage = showInfoMessage,
                ErrorMessageHandler = showErrorMessage,
                LogError = logError,
                CaptureNavigationSnapshot = captureNavigationSnapshot,
                RestoreNavigationSnapshot = restoreNavigationSnapshot,
                PromptSavePath = promptSavePath,
                SyncSummaryHandler = syncSummaryHandler
            };
        }
    }
}
