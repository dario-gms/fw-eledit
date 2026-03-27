using System.Collections.Generic;
using System.IO;

namespace FWEledit
{
    public sealed class ItemDescriptionFileService
    {
        public string ResolveItemExtDescFilePath(string gameRootPath, string workspaceRootPath)
        {
            if (string.IsNullOrWhiteSpace(gameRootPath) || !Directory.Exists(gameRootPath))
            {
                return string.Empty;
            }

            List<string> candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(workspaceRootPath) && Directory.Exists(workspaceRootPath))
            {
                candidates.Add(Path.Combine(workspaceRootPath, "resources", "configs.pck.files", "item_ext_desc.txt"));
                candidates.Add(Path.Combine(workspaceRootPath, "resources", "configs.pck.files", "data", "item_ext_desc.txt"));
            }
            candidates.Add(Path.Combine(gameRootPath, "resources", "configs.pck.files", "item_ext_desc.txt"));
            candidates.Add(Path.Combine(gameRootPath, "resources", "configs.pck.files", "data", "item_ext_desc.txt"));

            for (int i = 0; i < candidates.Count; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    return candidates[i];
                }
            }

            try
            {
                string[] found = Directory.GetFiles(
                    Path.Combine(gameRootPath, "resources"),
                    "item_ext_desc.txt",
                    SearchOption.AllDirectories);
                if (found.Length > 0)
                {
                    return found[0];
                }
            }
            catch
            { }

            return string.Empty;
        }
    }
}
