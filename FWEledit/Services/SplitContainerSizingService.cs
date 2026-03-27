using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class SplitContainerSizingService
    {
        public void EnsureSizing(
            SplitContainer split,
            int minLeft,
            int minRight,
            int desiredLeftMin,
            int desiredLeftMax,
            double desiredLeftRatio)
        {
            if (split == null || split.IsDisposed)
            {
                return;
            }

            int minTotal = minLeft + minRight + split.SplitterWidth;
            if (split.Width < minTotal)
            {
                return;
            }

            split.Panel1MinSize = minLeft;
            split.Panel2MinSize = minRight;

            int desiredLeft = Math.Max(desiredLeftMin, Math.Min(desiredLeftMax, (int)(split.Width * desiredLeftRatio)));
            int maxLeft = split.Width - split.Panel2MinSize - split.SplitterWidth;
            desiredLeft = Math.Max(split.Panel1MinSize, Math.Min(desiredLeft, maxLeft));

            if (split.SplitterDistance < split.Panel1MinSize || split.SplitterDistance > maxLeft)
            {
                split.SplitterDistance = desiredLeft;
            }
        }
    }
}
