using System;

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

        public string[] LoadFromFile(string filePath)
        {
            StatusText = store.LoadFromFile(filePath);
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
