using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private void ResetLightweightListRenderState(int listIndex, bool lightweight)
        {
            lightweightListRenderIndex = lightweight ? listIndex : -1;
            hydratedElementRowIconKeys.Clear();
            if (visibleIconHydrationTimer != null)
            {
                visibleIconHydrationTimer.Stop();
            }
        }

        private void ScheduleVisibleElementIconHydration()
        {
            if (visibleIconHydrationTimer == null)
            {
                HydrateVisibleElementRowIcons();
                return;
            }

            visibleIconHydrationTimer.Stop();
            visibleIconHydrationTimer.Start();
        }

        private void HydrateVisibleElementRowIcons()
        {
            if (lightweightListRenderIndex < 0
                || dataGridView_elems == null
                || comboBox_lists == null
                || sessionService == null
                || sessionService.ListCollection == null
                || sessionService.Database == null
                || sessionService.Database.sourceBitmap == null
                || comboBox_lists.SelectedIndex != lightweightListRenderIndex
                || dataGridView_elems.Rows.Count == 0
                || listRowBuilderService == null)
            {
                return;
            }

            int firstDisplayedRow = dataGridView_elems.FirstDisplayedScrollingRowIndex;
            if (firstDisplayedRow < 0)
            {
                firstDisplayedRow = 0;
            }

            int displayedRowCount = dataGridView_elems.DisplayedRowCount(true);
            if (displayedRowCount <= 0)
            {
                displayedRowCount = Math.Min(dataGridView_elems.Rows.Count, 32);
            }

            int lastDisplayedRow = Math.Min(dataGridView_elems.Rows.Count - 1, firstDisplayedRow + displayedRowCount - 1);
            bool hasUpdates = false;
            using (new ControlRedrawScope(dataGridView_elems))
            {
                for (int rowIndex = firstDisplayedRow; rowIndex <= lastDisplayedRow; rowIndex++)
                {
                    int elementIndex = elementIndexResolverService.ResolveElementIndexFromGridRow(
                        sessionService.ListCollection,
                        lightweightListRenderIndex,
                        rowIndex,
                        dataGridView_elems);
                    if (elementIndex < 0)
                    {
                        continue;
                    }

                    string hydrationKey = lightweightListRenderIndex.ToString() + "|" + elementIndex.ToString();
                    if (hydratedElementRowIconKeys.Contains(hydrationKey))
                    {
                        continue;
                    }

                    DataGridViewRow row = dataGridView_elems.Rows[rowIndex];
                    if (row == null || row.Cells.Count < 2)
                    {
                        continue;
                    }

                    Bitmap icon = listRowBuilderService.BuildRowIcon(
                        sessionService.ListCollection,
                        sessionService.Database,
                        lightweightListRenderIndex,
                        elementIndex);
                    row.Cells[1].Value = icon ?? Properties.Resources.NoIcon;
                    hydratedElementRowIconKeys.Add(hydrationKey);
                    hasUpdates = true;
                }
            }

            if (hasUpdates)
            {
                dataGridView_elems.Refresh();
            }
        }
    }
}
