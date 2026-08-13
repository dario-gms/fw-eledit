using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class DescriptionViewModel : ViewModelBase
    {
        private readonly ItemDescriptionStore store;
        private int currentItemId;
        private string statusText = string.Empty;

        public DescriptionViewModel(ItemDescriptionStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public int CurrentItemId
        {
            get { return currentItemId; }
            private set
            {
                SetProperty(ref currentItemId, value);
            }
        }

        public string StatusText
        {
            get { return statusText; }
            private set
            {
                SetProperty(ref statusText, value ?? string.Empty);
            }
        }

        public bool HasPendingChanges
        {
            get { return store.HasPendingChanges; }
        }

        public bool HasPendingChangeForItem(int itemId)
        {
            return store.HasPendingChangeForItem(itemId);
        }

        public string[] LoadFromFile(string filePath)
        {
            StatusText = store.LoadFromFile(filePath);
            return store.BuildRuntimeArray();
        }

        public string[] LoadFromBytes(byte[] payload, string filePath)
        {
            StatusText = store.LoadFromBytes(payload, filePath);
            return store.BuildRuntimeArray();
        }

        public string GetEditorTextForItem(int itemId, Func<int, string> fallback)
        {
            CurrentItemId = itemId;
            string raw;
            if (!store.TryGetRaw(itemId, out raw))
            {
                raw = fallback != null ? fallback(itemId) : string.Empty;
            }
            return ItemDescriptionCodec.DecodeForEditor(raw);
        }

        public bool StageEditorText(string editorText, out string statusText)
        {
            statusText = StatusText;
            if (CurrentItemId <= 0)
            {
                return false;
            }

            string encoded = ItemDescriptionCodec.EncodeForStorage(editorText ?? string.Empty);
            bool changed = store.Stage(CurrentItemId, encoded);
            if (changed)
            {
                StatusText = "Description staged for item " + CurrentItemId + " (save with File > Save)";
                statusText = StatusText;
            }
            return changed;
        }

        public bool StageEditorTextForItems(IEnumerable<int> itemIds, string editorText, out string statusText)
        {
            statusText = StatusText;
            if (itemIds == null)
            {
                return StageEditorText(editorText, out statusText);
            }

            string encoded = ItemDescriptionCodec.EncodeForStorage(editorText ?? string.Empty);
            HashSet<int> stagedIds = new HashSet<int>();
            int firstStagedId = 0;
            int changedCount = 0;
            foreach (int itemId in itemIds)
            {
                if (itemId <= 0 || !stagedIds.Add(itemId))
                {
                    continue;
                }
                if (firstStagedId == 0)
                {
                    firstStagedId = itemId;
                }

                if (store.Stage(itemId, encoded))
                {
                    changedCount++;
                }
            }

            if (stagedIds.Count == 0)
            {
                return StageEditorText(editorText, out statusText);
            }

            if (changedCount > 0)
            {
                StatusText = stagedIds.Count == 1
                    ? "Description staged for item " + firstStagedId + " (save with File > Save)"
                    : "Description staged for " + stagedIds.Count + " items (save with File > Save)";
                statusText = StatusText;
                return true;
            }

            return false;
        }

        public bool RemoveItems(IEnumerable<int> itemIds, out string statusText)
        {
            statusText = StatusText;
            if (itemIds == null)
            {
                return false;
            }

            int removedCount = 0;
            foreach (int itemId in itemIds)
            {
                if (store.Remove(itemId))
                {
                    removedCount++;
                }
            }

            if (removedCount <= 0)
            {
                return false;
            }

            StatusText = removedCount == 1
                ? "Description removed for deleted item (save with File > Save)"
                : "Descriptions removed for " + removedCount + " deleted items (save with File > Save)";
            statusText = StatusText;
            return true;
        }

        public bool CopyItems(IList<int> sourceItemIds, IList<int> targetItemIds, out string statusText)
        {
            statusText = StatusText;
            if (sourceItemIds == null || targetItemIds == null)
            {
                return false;
            }

            int pairCount = Math.Min(sourceItemIds.Count, targetItemIds.Count);
            int copiedCount = 0;
            for (int i = 0; i < pairCount; i++)
            {
                if (store.Copy(sourceItemIds[i], targetItemIds[i]))
                {
                    copiedCount++;
                }
            }

            if (copiedCount <= 0)
            {
                return false;
            }

            StatusText = copiedCount == 1
                ? "Description cloned for new item (save with File > Save)"
                : "Descriptions cloned for " + copiedCount + " new items (save with File > Save)";
            statusText = StatusText;
            return true;
        }

        public bool FlushToDisk(AssetManager asm, out string statusText, out string errorMessage)
        {
            bool ok = store.FlushToDisk(asm, out statusText, out errorMessage);
            if (!string.IsNullOrWhiteSpace(statusText))
            {
                StatusText = statusText;
            }
            return ok;
        }

        public bool RemapId(int oldId, int newId)
        {
            return store.RemapId(oldId, newId);
        }

        public string[] BuildRuntimeArray()
        {
            return store.BuildRuntimeArray();
        }

        public void ResetPendingChanges()
        {
            store.ResetPendingChanges();
        }

    }
}
