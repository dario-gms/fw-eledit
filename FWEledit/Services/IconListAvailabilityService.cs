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
                string iconImg;
                string iconTxt;
                if (!assetManager.TryGetIconsetPair(out iconImg, out iconTxt))
                {
                    MessageBox.Show(
                        "Icon list not found. Ensure surfaces.pck is present in the client resources.",
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
