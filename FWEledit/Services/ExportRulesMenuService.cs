using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ExportRulesMenuService
    {
        public void UpdateMenu(ToolStripMenuItem exportMenu, bool hasConfigFile, List<string> exportRules, EventHandler exportHandler)
        {
            if (exportMenu == null)
            {
                return;
            }

            exportMenu.DropDownItems.Clear();
            if (!hasConfigFile)
            {
                return;
            }

            exportMenu.DropDownItems.Add(new ToolStripLabel("Select a valid Conversation Rules Set"));
            exportMenu.DropDownItems[0].Font = new Font("Tahoma", 8.25F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            exportMenu.DropDownItems.Add(new ToolStripSeparator());

            if (exportRules == null)
            {
                return;
            }

            for (int i = 0; i < exportRules.Count; i++)
            {
                exportMenu.DropDownItems.Add(exportRules[i], null, exportHandler);
            }
        }
    }
}
