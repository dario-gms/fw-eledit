using System.Windows.Forms;

namespace FWEledit
{
    public sealed class IconListAvailabilityService
    {
        public void EnsureIconListAvailable(AssetManager assetManager)
        {
            if (assetManager == null)
            {
                return;
            }

            try
            {
                assetManager.EnsurePackageExtracted("surfaces");
                string iconImg;
                string iconTxt;
                if (!assetManager.TryGetIconsetPair(out iconImg, out iconTxt))
                {
                    MessageBox.Show(
                        "Icon list not found. Ensure surfaces.pck is present and extracted.\n" +
                        "Expected in workspace or game resources:\n" +
                        "surfaces.pck.files\\iconset\\iconlist_*.{dds,png}",
                        "Icons",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch
            {
            }
        }
    }
}
