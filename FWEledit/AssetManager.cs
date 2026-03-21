using sELedit.DDSReader;
using sELedit.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Security.Cryptography;

namespace sELedit
{
    public class AssetManager
    {
        public static string GameRootPath = string.Empty;
        public static string WorkspaceRootPath = string.Empty;
        // Keep UNKNOWN by default so DDSReader auto-detects compression from file header (DXT1/3/5...).
        public int DDSFORMAT = (int)DDSReader.Utils.PixelFormat.UNKNOWN;
        internal delegate void UpdateProgressDelegate(string value, int min, int max);
        private SortedList<int, int> item_color;
        private int rows;
        private Bitmap sourceBitmap;
        private CacheSave database = new CacheSave();
        private bool firstLoad = true;
        private int cols;
        private SortedList<int, string> imagesx;
        private SortedList<int, string> imagesById;
        private SortedList<string, Point> imageposition;
        private SortedList<int, string> item_desc;
        private List<string> arrTheme;
        private Dictionary<string, List<string>> resourceFileIndex = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private string indexedGameRoot = string.Empty;
        private bool triedExtractConfigsPck = false;
        private bool triedExtractSurfacesPck = false;
        private bool triedExtractModelsPck = false;
        private bool triedExtractLitModelsPck = false;
        private bool triedExtractMoxingPck = false;
        private HashSet<string> dirtyPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool pathDataDirty = false;

        public static object anydata;

        public void SetGameRootFromElements(string elementsFilePath)
        {
            try
            {
                string dataDir = Path.GetDirectoryName(elementsFilePath);
                if (string.IsNullOrWhiteSpace(dataDir))
                {
                    return;
                }
                string root = Path.GetDirectoryName(dataDir);
                if (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
                {
                    GameRootPath = root;
                    WorkspaceRootPath = string.Empty;
                    sourceBitmap = null;
                    database = new CacheSave();
                    firstLoad = true;
                    resourceFileIndex = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                    indexedGameRoot = string.Empty;
                    triedExtractConfigsPck = false;
                    triedExtractSurfacesPck = false;
                    triedExtractModelsPck = false;
                    triedExtractLitModelsPck = false;
                    triedExtractMoxingPck = false;
                    dirtyPackages.Clear();
                    pathDataDirty = false;
                }
            }
            catch
            { }
        }

        private static string BuildWorkspaceId(string gameRoot)
        {
            if (string.IsNullOrWhiteSpace(gameRoot))
            {
                return "default";
            }

            string normalized = gameRoot.Trim().ToLowerInvariant();
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(normalized));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private string GetWorkspaceRootForCurrentGame()
        {
            string baseWorkspace = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FWEledit",
                "workspace");
            return Path.Combine(baseWorkspace, BuildWorkspaceId(GameRootPath));
        }

        private string GetWorkspaceResourcesRoot()
        {
            if (string.IsNullOrWhiteSpace(WorkspaceRootPath))
            {
                return string.Empty;
            }
            return Path.Combine(WorkspaceRootPath, "resources");
        }

        private string GetWorkspaceDataRoot()
        {
            if (string.IsNullOrWhiteSpace(WorkspaceRootPath))
            {
                return string.Empty;
            }
            return Path.Combine(WorkspaceRootPath, "data");
        }

        private string GetWorkspacePathDataFile()
        {
            string dataRoot = GetWorkspaceDataRoot();
            if (string.IsNullOrWhiteSpace(dataRoot))
            {
                return string.Empty;
            }
            return Path.Combine(dataRoot, "path.data");
        }

        private bool EnsureWorkspacePckPrepared(string packageName, bool ensureExtracted)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(GameRootPath) || !Directory.Exists(GameRootPath))
                {
                    return false;
                }

                string gamePck = Path.Combine(GameRootPath, "resources", packageName + ".pck");
                string gamePkx = Path.Combine(GameRootPath, "resources", packageName + ".pkx");
                if (!File.Exists(gamePck))
                {
                    return false;
                }

                string workspaceResources = GetWorkspaceResourcesRoot();
                if (string.IsNullOrWhiteSpace(workspaceResources))
                {
                    return false;
                }
                Directory.CreateDirectory(workspaceResources);

                string workspacePck = Path.Combine(workspaceResources, packageName + ".pck");
                string workspacePkx = Path.Combine(workspaceResources, packageName + ".pkx");
                bool copyNeeded = !File.Exists(workspacePck);
                if (!copyNeeded)
                {
                    DateTime gameTime = File.GetLastWriteTimeUtc(gamePck);
                    DateTime wsTime = File.GetLastWriteTimeUtc(workspacePck);
                    copyNeeded = gameTime > wsTime;
                }
                if (copyNeeded)
                {
                    File.Copy(gamePck, workspacePck, true);
                }
                if (File.Exists(gamePkx))
                {
                    bool pkxCopyNeeded = !File.Exists(workspacePkx);
                    if (!pkxCopyNeeded)
                    {
                        DateTime gamePkxTime = File.GetLastWriteTimeUtc(gamePkx);
                        DateTime wsPkxTime = File.GetLastWriteTimeUtc(workspacePkx);
                        pkxCopyNeeded = gamePkxTime > wsPkxTime;
                    }
                    if (pkxCopyNeeded)
                    {
                        File.Copy(gamePkx, workspacePkx, true);
                    }
                }

                if (!ensureExtracted)
                {
                    return true;
                }

                string extractedDir = workspacePck + ".files";
                bool mustExtract = !Directory.Exists(extractedDir);
                if (!mustExtract)
                {
                    DateTime pckTime = File.GetLastWriteTimeUtc(workspacePck);
                    DateTime dirTime = Directory.GetLastWriteTimeUtc(extractedDir);
                    mustExtract = pckTime > dirTime;
                }

                if (mustExtract)
                {
                    try
                    {
                        if (Directory.Exists(extractedDir))
                        {
                            Directory.Delete(extractedDir, true);
                        }
                    }
                    catch
                    { }
                    if (!RunPckExtraction(workspacePck))
                    {
                        return false;
                    }
                    if (!Directory.Exists(extractedDir))
                    {
                        return false;
                    }
                    if (!Directory.Exists(extractedDir))
                    {
                        // Some spck builds extract next to the game pck path or spck working directory.
                        string altGame = Path.Combine(Path.GetDirectoryName(gamePck), Path.GetFileName(workspacePck) + ".files");
                        if (Directory.Exists(altGame))
                        {
                            try
                            {
                                if (Directory.Exists(extractedDir))
                                {
                                    Directory.Delete(extractedDir, true);
                                }
                            }
                            catch
                            { }
                            try
                            {
                                Directory.Move(altGame, extractedDir);
                            }
                            catch
                            { }
                        }
                        else
                        {
                            string spckExe = FindSpckExecutable();
                            if (!string.IsNullOrWhiteSpace(spckExe))
                            {
                                string altSpck = Path.Combine(Path.GetDirectoryName(spckExe), Path.GetFileName(workspacePck) + ".files");
                                if (Directory.Exists(altSpck))
                                {
                                    try
                                    {
                                        if (Directory.Exists(extractedDir))
                                        {
                                            Directory.Delete(extractedDir, true);
                                        }
                                    }
                                    catch
                                    { }
                                    try
                                    {
                                        Directory.Move(altSpck, extractedDir);
                                    }
                                    catch
                                    { }
                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void EnsureWorkspacePathDataPrepared()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(GameRootPath) || !Directory.Exists(GameRootPath))
                {
                    return;
                }

                string gamePathData = Path.Combine(GameRootPath, "data", "path.data");
                if (!File.Exists(gamePathData))
                {
                    return;
                }

                string workspaceData = GetWorkspaceDataRoot();
                if (string.IsNullOrWhiteSpace(workspaceData))
                {
                    return;
                }
                Directory.CreateDirectory(workspaceData);

                string workspacePathData = Path.Combine(workspaceData, "path.data");
                bool copyNeeded = !File.Exists(workspacePathData);
                if (!copyNeeded)
                {
                    copyNeeded = File.GetLastWriteTimeUtc(gamePathData) > File.GetLastWriteTimeUtc(workspacePathData);
                }
                if (copyNeeded)
                {
                    File.Copy(gamePathData, workspacePathData, true);
                }
            }
            catch
            { }
        }

        public bool EnsureWorkspaceReady()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(GameRootPath) || !Directory.Exists(GameRootPath))
                {
                    return false;
                }

                WorkspaceRootPath = GetWorkspaceRootForCurrentGame();
                Directory.CreateDirectory(WorkspaceRootPath);
                Directory.CreateDirectory(GetWorkspaceResourcesRoot());
                Directory.CreateDirectory(GetWorkspaceDataRoot());
                EnsureWorkspacePathDataPrepared();
                EnsureWorkspacePckPrepared("configs", true);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void MarkWorkspaceFileChanged(string absoluteFilePath)
        {
            if (string.IsNullOrWhiteSpace(absoluteFilePath))
            {
                return;
            }

            try
            {
                string full = Path.GetFullPath(absoluteFilePath);
                string workspaceResources = GetWorkspaceResourcesRoot();
                if (!string.IsNullOrWhiteSpace(workspaceResources))
                {
                    string configsRoot = Path.Combine(workspaceResources, "configs.pck.files") + "\\";
                    string surfacesRoot = Path.Combine(workspaceResources, "surfaces.pck.files") + "\\";

                    if (full.StartsWith(configsRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        dirtyPackages.Add("configs");
                        return;
                    }
                    if (full.StartsWith(surfacesRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        dirtyPackages.Add("surfaces");
                        return;
                    }
                }

                string workspacePathData = GetWorkspacePathDataFile();
                if (!string.IsNullOrWhiteSpace(workspacePathData) &&
                    string.Equals(full, Path.GetFullPath(workspacePathData), StringComparison.OrdinalIgnoreCase))
                {
                    pathDataDirty = true;
                }
            }
            catch
            { }
        }

        private bool RunPckCompress(string extractedDirectory, int compressionLevel)
        {
            try
            {
                string spck = FindSpckExecutable();
                if (string.IsNullOrWhiteSpace(spck) || !Directory.Exists(extractedDirectory))
                {
                    return false;
                }

                if (compressionLevel < 0) { compressionLevel = 0; }
                if (compressionLevel > 9) { compressionLevel = 9; }

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = spck;
                psi.Arguments = "-fw -c \"" + extractedDirectory + "\" " + compressionLevel;
                psi.WorkingDirectory = Path.GetDirectoryName(spck);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                using (Process p = Process.Start(psi))
                {
                    if (p == null)
                    {
                        return false;
                    }
                    p.WaitForExit(180000);
                    return p.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void CreateTimestampedZipBackup(string sourceFile, string backupDirectory, string prefix)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourceFile) || !File.Exists(sourceFile))
                {
                    return;
                }
                if (string.IsNullOrWhiteSpace(backupDirectory))
                {
                    return;
                }

                Directory.CreateDirectory(backupDirectory);
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string safePrefix = string.IsNullOrWhiteSpace(prefix)
                    ? Path.GetFileNameWithoutExtension(sourceFile)
                    : prefix.Trim();
                if (string.IsNullOrWhiteSpace(safePrefix))
                {
                    safePrefix = "backup";
                }
                string zipPath = Path.Combine(backupDirectory, safePrefix + "_" + stamp + ".zip");
                int counter = 1;
                while (File.Exists(zipPath))
                {
                    zipPath = Path.Combine(backupDirectory, safePrefix + "_" + stamp + "_" + counter.ToString("D2") + ".zip");
                    counter++;
                }

                bool created = false;
                try
                {
                    System.Reflection.Assembly.Load("System.IO.Compression");
                }
                catch
                {
                }
                try
                {
                    System.Reflection.Assembly.Load("System.IO.Compression.FileSystem");
                }
                catch
                {
                }

                Type zipFileType = Type.GetType("System.IO.Compression.ZipFile, System.IO.Compression.FileSystem");
                Type zipFileExtensionsType = Type.GetType("System.IO.Compression.ZipFileExtensions, System.IO.Compression.FileSystem");
                Type zipArchiveModeType = Type.GetType("System.IO.Compression.ZipArchiveMode, System.IO.Compression");
                Type zipArchiveType = Type.GetType("System.IO.Compression.ZipArchive, System.IO.Compression");
                if (zipFileType != null && zipArchiveModeType != null && zipArchiveType != null && zipFileExtensionsType != null)
                {
                    object createMode = Enum.Parse(zipArchiveModeType, "Create");
                    System.Reflection.MethodInfo openMethod = zipFileType.GetMethod("Open", new Type[] { typeof(string), zipArchiveModeType });
                    if (openMethod != null)
                    {
                        object archive = null;
                        try
                        {
                            archive = openMethod.Invoke(null, new object[] { zipPath, createMode });
                            if (archive != null)
                            {
                                Type compressionLevelType = Type.GetType("System.IO.Compression.CompressionLevel, System.IO.Compression");
                                System.Reflection.MethodInfo createEntryMethod = null;
                                if (compressionLevelType != null)
                                {
                                    createEntryMethod = zipFileExtensionsType.GetMethod(
                                        "CreateEntryFromFile",
                                        new Type[] { zipArchiveType, typeof(string), typeof(string), compressionLevelType });
                                }
                                if (createEntryMethod == null)
                                {
                                    createEntryMethod = zipFileExtensionsType.GetMethod(
                                        "CreateEntryFromFile",
                                        new Type[] { zipArchiveType, typeof(string), typeof(string) });
                                }
                                if (createEntryMethod != null)
                                {
                                    if (createEntryMethod.GetParameters().Length == 3 && compressionLevelType != null)
                                    {
                                        object level = Enum.Parse(compressionLevelType, "Fastest");
                                        createEntryMethod.Invoke(null, new object[] { archive, sourceFile, Path.GetFileName(sourceFile), level });
                                    }
                                    else
                                    {
                                        createEntryMethod.Invoke(null, new object[] { archive, sourceFile, Path.GetFileName(sourceFile) });
                                    }
                                    created = File.Exists(zipPath) && new FileInfo(zipPath).Length > 0;
                                }
                            }
                        }
                        finally
                        {
                            IDisposable disposable = archive as IDisposable;
                            if (disposable != null)
                            {
                                disposable.Dispose();
                            }
                        }
                    }
                }

                if (!created)
                {
                    try
                    {
                        if (File.Exists(zipPath) && new FileInfo(zipPath).Length == 0)
                        {
                            File.Delete(zipPath);
                        }
                    }
                    catch
                    {
                    }

                    string sevenZip = Resolve7ZipExecutable();
                    if (!string.IsNullOrWhiteSpace(sevenZip))
                    {
                        TryCreateZipWith7Zip(sevenZip, zipPath, sourceFile);
                    }
                }
            }
            catch
            {
            }
        }

        private static string Resolve7ZipExecutable()
        {
            List<string> candidates = new List<string>();
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            if (!string.IsNullOrWhiteSpace(GameRootPath))
            {
                candidates.Add(Path.Combine(GameRootPath, "fELedit", "Elements Editor Pago", "7za.exe"));
                candidates.Add(Path.Combine(GameRootPath, "Elements Editor Pago", "7za.exe"));
                candidates.Add(Path.Combine(GameRootPath, "tools", "7za.exe"));
                candidates.Add(Path.Combine(GameRootPath, "fELedit", "tools", "7za.exe"));
            }
            if (!string.IsNullOrWhiteSpace(WorkspaceRootPath))
            {
                candidates.Add(Path.Combine(WorkspaceRootPath, "fELedit", "Elements Editor Pago", "7za.exe"));
                candidates.Add(Path.Combine(WorkspaceRootPath, "Elements Editor Pago", "7za.exe"));
                candidates.Add(Path.Combine(WorkspaceRootPath, "tools", "7za.exe"));
                candidates.Add(Path.Combine(WorkspaceRootPath, "fELedit", "tools", "7za.exe"));
            }

            candidates.Add(Path.Combine(baseDir, "7za.exe"));
            candidates.Add(Path.Combine(baseDir, "tools", "7za.exe"));

            for (int i = 0; i < candidates.Count; i++)
            {
                string path = candidates[i];
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    return path;
                }
            }

            return string.Empty;
        }

        private static bool TryCreateZipWith7Zip(string sevenZipExe, string zipPath, string sourceFile)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sevenZipExe) || string.IsNullOrWhiteSpace(zipPath) || string.IsNullOrWhiteSpace(sourceFile))
                {
                    return false;
                }
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = sevenZipExe;
                psi.Arguments = "a -tzip \"" + zipPath + "\" \"" + sourceFile + "\" -mx=1";
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.WorkingDirectory = Path.GetDirectoryName(sevenZipExe);

                using (Process p = Process.Start(psi))
                {
                    if (p == null)
                    {
                        return false;
                    }
                    p.WaitForExit(30000);
                }

                return File.Exists(zipPath) && new FileInfo(zipPath).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        public bool ApplyWorkspaceChangesToGame(out string summary)
        {
            List<string> actions = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(GameRootPath) || !Directory.Exists(GameRootPath))
                {
                    summary = "Game folder not set.";
                    return false;
                }

                EnsureWorkspaceReady();
                string workspaceResources = GetWorkspaceResourcesRoot();
                string gameResources = Path.Combine(GameRootPath, "resources");

                foreach (string package in new List<string>(dirtyPackages))
                {
                    string extracted = Path.Combine(workspaceResources, package + ".pck.files");
                    string workspacePck = Path.Combine(workspaceResources, package + ".pck");
                    string gamePck = Path.Combine(gameResources, package + ".pck");

                    if (!Directory.Exists(extracted))
                    {
                        continue;
                    }

                    if (!RunPckCompress(extracted, 1))
                    {
                        summary = "Failed to repack " + package + ".pck";
                        return false;
                    }
                    if (!File.Exists(workspacePck))
                    {
                        summary = "Repack finished but output not found for " + package + ".pck";
                        return false;
                    }

                    if (File.Exists(gamePck))
                    {
                        if (string.Equals(package, "configs", StringComparison.OrdinalIgnoreCase))
                        {
                            string backupDir = Path.Combine(GameRootPath, "backup_configs");
                            CreateTimestampedZipBackup(gamePck, backupDir, "configs");
                        }
                        File.Copy(gamePck, gamePck + ".bak", true);
                    }
                    File.Copy(workspacePck, gamePck, true);
                    actions.Add(package + ".pck");
                    dirtyPackages.Remove(package);
                }

                if (pathDataDirty)
                {
                    string workspacePathData = GetWorkspacePathDataFile();
                    string gamePathData = Path.Combine(GameRootPath, "data", "path.data");
                    if (File.Exists(workspacePathData))
                    {
                        if (File.Exists(gamePathData))
                        {
                            string backupDir = Path.Combine(GameRootPath, "backup_path");
                            CreateTimestampedZipBackup(gamePathData, backupDir, "path");
                            File.Copy(gamePathData, gamePathData + ".bak", true);
                        }
                        File.Copy(workspacePathData, gamePathData, true);
                        actions.Add("path.data");
                    }
                    pathDataDirty = false;
                }

                if (actions.Count == 0)
                {
                    summary = "No PCK/path changes pending.";
                }
                else
                {
                    summary = "Updated: " + string.Join(", ", actions.ToArray());
                }
                return true;
            }
            catch (Exception ex)
            {
                summary = ex.Message;
                return false;
            }
        }
        public bool load()
        {
            if (string.IsNullOrWhiteSpace(GameRootPath) || !Directory.Exists(GameRootPath))
            {
                MainWindow.database = database;
                return false;
            }
            EnsureWorkspaceReady();

            if (sourceBitmap == null)
            {
                // Ensure surfaces are extracted so iconset files are available on first load.
                EnsureWorkspacePckPrepared("surfaces", true);
                imageposition = loadSurfaces();
                loadItem_color();
                firstLoad = true;
            }

            if (database.pathById == null || database.pathById.Count == 0)
            {
                database.pathById = LoadPathById();
            }

            if (firstLoad)
            {
                LoadTheme();
                Application.DoEvents();
                LoadLocalizationText();
                Application.DoEvents();
                //this.LoadInstanceList();
                //Application.DoEvents();
                LoadBuffList();
                Application.DoEvents();
                LoadItemExtDescList();
                Application.DoEvents();
                LoadSkillList();
                Application.DoEvents();
                LoadAddonList();
                Application.DoEvents();
                firstLoad = false;
            }
            MainWindow.database = database;

            return true;
        }

        private string ResolvePathDataFile()
        {
            string workspacePathData = GetWorkspacePathDataFile();
            if (!string.IsNullOrWhiteSpace(workspacePathData) && File.Exists(workspacePathData))
            {
                return workspacePathData;
            }

            if (!string.IsNullOrWhiteSpace(GameRootPath))
            {
                string gamePathData = Path.Combine(GameRootPath, "data", "path.data");
                if (File.Exists(gamePathData))
                {
                    return gamePathData;
                }

                string felEditPathData = Path.Combine(GameRootPath, "fELedit", "resources", "data", "path.data");
                if (File.Exists(felEditPathData))
                {
                    return felEditPathData;
                }
            }

            return string.Empty;
        }

        private SortedList<int, string> LoadPathById()
        {
            SortedList<int, string> result = new SortedList<int, string>();
            string pathDataFile = ResolvePathDataFile();
            if (string.IsNullOrWhiteSpace(pathDataFile) || !File.Exists(pathDataFile))
            {
                return result;
            }

            try
            {
                Encoding enc = Encoding.GetEncoding("GBK");
                using (FileStream fs = File.OpenRead(pathDataFile))
                using (BinaryReader br = new BinaryReader(fs, enc))
                {
                    if (br.BaseStream.Length < 12)
                    {
                        return result;
                    }

                    string magic = Encoding.ASCII.GetString(br.ReadBytes(4));
                    if (!string.Equals(magic, "DIMP", StringComparison.Ordinal))
                    {
                        return result;
                    }

                    int count = br.ReadInt32();
                    br.ReadInt32(); // version/reserved

                    for (int i = 0; i < count; i++)
                    {
                        if (br.BaseStream.Position + 8 > br.BaseStream.Length)
                        {
                            break;
                        }

                        int len = br.ReadInt32();
                        if (len < 0 || len > 8192 || br.BaseStream.Position + len + 4 > br.BaseStream.Length)
                        {
                            break;
                        }

                        byte[] pathBytes = br.ReadBytes(len);
                        int id = br.ReadInt32();
                        if (id < 0)
                        {
                            continue;
                        }

                        string mappedPath = enc.GetString(pathBytes).Replace('/', '\\');
                        if (!result.ContainsKey(id))
                        {
                            result.Add(id, mappedPath);
                        }
                    }
                }
            }
            catch
            { }

            return result;
        }

        private string NormalizeIconKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }
            string name = value.Trim().Replace('/', '\\');
            name = Path.GetFileName(name);
            return name.ToLowerInvariant();
        }

        private int? TryExtractLeadingNumber(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }
            int i = 0;
            while (i < fileName.Length && char.IsDigit(fileName[i]))
            {
                i++;
            }
            if (i == 0)
            {
                return null;
            }
            int result;
            if (int.TryParse(fileName.Substring(0, i), out result))
            {
                return result;
            }
            return null;
        }

        private string ResolveResourceFile(string relativePath)
        {
            string normalizedRelative = (relativePath ?? string.Empty).Replace('/', '\\').TrimStart('\\');
            TryEnsureRequiredPckExtracted(normalizedRelative);

            List<string> roots = new List<string>();
            if (!string.IsNullOrWhiteSpace(WorkspaceRootPath) && Directory.Exists(WorkspaceRootPath))
            {
                roots.Add(Path.Combine(WorkspaceRootPath, "resources", "surfaces.pck.files"));
                roots.Add(Path.Combine(WorkspaceRootPath, "resources", "configs.pck.files"));
                roots.Add(Path.Combine(WorkspaceRootPath, "resources", "models.pck.files"));
                roots.Add(Path.Combine(WorkspaceRootPath, "resources", "litmodels.pck.files"));
                roots.Add(Path.Combine(WorkspaceRootPath, "resources", "moxing.pck.files"));
            }
            if (!string.IsNullOrWhiteSpace(GameRootPath) && Directory.Exists(GameRootPath))
            {
                roots.Add(Path.Combine(GameRootPath, "resources", "surfaces.pck.files"));
                roots.Add(Path.Combine(GameRootPath, "resources", "configs.pck.files"));
                roots.Add(Path.Combine(GameRootPath, "resources", "models.pck.files"));
                roots.Add(Path.Combine(GameRootPath, "resources", "litmodels.pck.files"));
                roots.Add(Path.Combine(GameRootPath, "resources", "moxing.pck.files"));
            }

            foreach (string root in roots)
            {
                try
                {
                    string candidate = Path.Combine(root, relativePath);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                catch
                { }
            }

            string indexedCandidate = ResolveFromIndexedResources(relativePath);
            if (!string.IsNullOrEmpty(indexedCandidate))
            {
                return indexedCandidate;
            }

            return string.Empty;
        }

        private void EnsureResourceIndex()
        {
            if (string.IsNullOrWhiteSpace(GameRootPath) || !Directory.Exists(GameRootPath))
            {
                return;
            }
            if (string.Equals(indexedGameRoot, GameRootPath, StringComparison.OrdinalIgnoreCase) && resourceFileIndex.Count > 0)
            {
                return;
            }

            resourceFileIndex = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            List<string> scanRoots = new List<string>
            {
                Path.Combine(WorkspaceRootPath ?? string.Empty, "resources", "surfaces.pck.files"),
                Path.Combine(WorkspaceRootPath ?? string.Empty, "resources", "configs.pck.files"),
                Path.Combine(WorkspaceRootPath ?? string.Empty, "resources", "models.pck.files"),
                Path.Combine(WorkspaceRootPath ?? string.Empty, "resources", "litmodels.pck.files"),
                Path.Combine(WorkspaceRootPath ?? string.Empty, "resources", "moxing.pck.files"),
                Path.Combine(GameRootPath, "resources", "surfaces.pck.files"),
                Path.Combine(GameRootPath, "resources", "configs.pck.files"),
                Path.Combine(GameRootPath, "resources", "models.pck.files"),
                Path.Combine(GameRootPath, "resources", "litmodels.pck.files"),
                Path.Combine(GameRootPath, "resources", "moxing.pck.files")
            };

            foreach (string root in scanRoots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }
                try
                {
                    string[] files = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories);
                    for (int i = 0; i < files.Length; i++)
                    {
                        string name = Path.GetFileName(files[i]);
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            continue;
                        }
                        List<string> list;
                        if (!resourceFileIndex.TryGetValue(name, out list))
                        {
                            list = new List<string>();
                            resourceFileIndex[name] = list;
                        }
                        list.Add(files[i]);
                    }
                }
                catch
                { }
            }

            indexedGameRoot = GameRootPath;
        }

        private string ResolveFromIndexedResources(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return string.Empty;
            }

            EnsureResourceIndex();
            string fileName = Path.GetFileName(relativePath);
            if (string.IsNullOrWhiteSpace(fileName) || resourceFileIndex.Count == 0)
            {
                return string.Empty;
            }

            List<string> candidates;
            if (!resourceFileIndex.TryGetValue(fileName, out candidates) || candidates.Count == 0)
            {
                return string.Empty;
            }

            string normalizedRelative = relativePath.Replace('/', '\\').TrimStart('\\');
            string best = string.Empty;
            int bestScore = int.MinValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                string p = candidates[i];
                int score = 0;
                string normalizedPath = p.Replace('/', '\\');
                if (normalizedPath.EndsWith(normalizedRelative, StringComparison.OrdinalIgnoreCase))
                {
                    score += 60;
                }
                if (normalizedPath.IndexOf("\\resources\\surfaces.pck.files\\", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 40;
                }
                if (normalizedPath.IndexOf("\\resources\\configs.pck.files\\", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 35;
                }
                if (normalizedPath.IndexOf("\\resources\\", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 25;
                }
                if (normalizedRelative.StartsWith("data\\", StringComparison.OrdinalIgnoreCase) &&
                    normalizedPath.IndexOf("\\data\\", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 15;
                }
                if (normalizedRelative.StartsWith("surfaces\\", StringComparison.OrdinalIgnoreCase) &&
                    normalizedPath.IndexOf("\\surfaces\\", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 15;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }

            return best;
        }

        private string ResolveResourceFileAny(params string[] relativePaths)
        {
            for (int i = 0; i < relativePaths.Length; i++)
            {
                string candidate = ResolveResourceFile(relativePaths[i]);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            return ResolveResourceFile(relativePaths[0]);
        }

        private bool IsConfigRelativePath(string normalizedRelative)
        {
            if (string.IsNullOrWhiteSpace(normalizedRelative))
            {
                return false;
            }
            if (normalizedRelative.StartsWith("data\\", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            string file = Path.GetFileName(normalizedRelative);
            if (string.IsNullOrWhiteSpace(file))
            {
                return false;
            }
            string[] configFiles = new string[]
            {
                "item_color.txt",
                "item_ext_desc.txt",
                "skillstr.txt",
                "addon_table.txt",
                "language_en.txt",
                "buff_str.txt",
                "theme.txt"
            };
            for (int i = 0; i < configFiles.Length; i++)
            {
                if (string.Equals(file, configFiles[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsSurfaceRelativePath(string normalizedRelative)
        {
            if (string.IsNullOrWhiteSpace(normalizedRelative))
            {
                return false;
            }
            return normalizedRelative.StartsWith("surfaces\\", StringComparison.OrdinalIgnoreCase)
                || normalizedRelative.StartsWith("iconset\\", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsModelRelativePath(string normalizedRelative)
        {
            if (string.IsNullOrWhiteSpace(normalizedRelative))
            {
                return false;
            }

            return normalizedRelative.StartsWith("models\\", StringComparison.OrdinalIgnoreCase)
                || normalizedRelative.StartsWith("model\\", StringComparison.OrdinalIgnoreCase)
                || normalizedRelative.StartsWith("moxing\\", StringComparison.OrdinalIgnoreCase)
                || normalizedRelative.StartsWith("litmodels\\", StringComparison.OrdinalIgnoreCase);
        }

        private string FindSpckExecutable()
        {
            List<string> roots = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddRoot(string root)
            {
                if (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root) && seen.Add(root))
                {
                    roots.Add(root);
                }
            }

            AddRoot(GameRootPath);
            AddRoot(Application.StartupPath);
            AddRoot(Path.GetDirectoryName(Application.StartupPath));
            AddRoot(Path.GetDirectoryName(Path.GetDirectoryName(Application.StartupPath)));
            AddRoot(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Application.StartupPath))));
            AddRoot(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Application.StartupPath)))));
            AddRoot(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Application.StartupPath))))));

            foreach (string root in roots)
            {
                string[] candidates = new string[]
                {
                    Path.Combine(root, "fELedit", "tools", "spck", "spck", "bin", "spck.exe"),
                    Path.Combine(root, "tools", "spck", "spck", "bin", "spck.exe"),
                    Path.Combine(root, "fELedit", "tools", "spck.exe"),
                    Path.Combine(root, "tools", "spck.exe"),
                    Path.Combine(root, "spck.exe")
                };
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (File.Exists(candidates[i]))
                    {
                        return candidates[i];
                    }
                }
            }
            return string.Empty;
        }

        private bool RunPckExtraction(string pckFilePath)
        {
            try
            {
                string spck = FindSpckExecutable();
                if (string.IsNullOrWhiteSpace(spck) || !File.Exists(pckFilePath))
                {
                    return false;
                }

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = spck;
                psi.Arguments = "-fw -x \"" + pckFilePath + "\"";
                psi.WorkingDirectory = Path.GetDirectoryName(spck);
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                using (Process p = Process.Start(psi))
                {
                    if (p == null)
                    {
                        return false;
                    }
                    int timeoutMs = 120000;
                    string fileName = Path.GetFileName(pckFilePath) ?? string.Empty;
                    if (string.Equals(fileName, "models.pck", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(fileName, "surfaces.pck", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(fileName, "gfx.pck", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(fileName, "grasses.pck", StringComparison.OrdinalIgnoreCase))
                    {
                        timeoutMs = 900000;
                    }
                    bool exited = p.WaitForExit(timeoutMs);
                    if (!exited)
                    {
                        TryWriteSpckLog(pckFilePath, spck, -1, string.Empty, "Timed out.");
                        return false;
                    }
                    string stdout = string.Empty;
                    string stderr = string.Empty;
                    try { stdout = p.StandardOutput.ReadToEnd(); } catch { }
                    try { stderr = p.StandardError.ReadToEnd(); } catch { }
                    if (p.ExitCode != 0)
                    {
                        TryWriteSpckLog(pckFilePath, spck, p.ExitCode, stdout, stderr);
                        return false;
                    }
                    if (!string.IsNullOrWhiteSpace(stderr))
                    {
                        TryWriteSpckLog(pckFilePath, spck, p.ExitCode, stdout, stderr);
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void TryWriteSpckLog(string pckFilePath, string spckPath, int exitCode, string stdout, string stderr)
        {
            try
            {
                string root = !string.IsNullOrWhiteSpace(WorkspaceRootPath)
                    ? WorkspaceRootPath
                    : Path.GetTempPath();
                string logDir = Path.Combine(root, "spck_logs");
                Directory.CreateDirectory(logDir);
                string name = Path.GetFileName(pckFilePath) ?? "pck";
                string logPath = Path.Combine(logDir, name + "_extract.log");
                using (StreamWriter sw = new StreamWriter(logPath, false))
                {
                    sw.WriteLine("pck: " + pckFilePath);
                    sw.WriteLine("spck: " + spckPath);
                    sw.WriteLine("exit: " + exitCode);
                    sw.WriteLine("---- stdout ----");
                    sw.WriteLine(stdout ?? string.Empty);
                    sw.WriteLine("---- stderr ----");
                    sw.WriteLine(stderr ?? string.Empty);
                }
            }
            catch
            { }
        }

        public string GetSpckExecutablePath()
        {
            try
            {
                return FindSpckExecutable();
            }
            catch
            {
                return string.Empty;
            }
        }

        public bool TryEnumeratePckIndexEntries(string packageName, out List<string> entries)
        {
            entries = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(packageName) || string.IsNullOrWhiteSpace(GameRootPath))
                {
                    return false;
                }

                string resourcesRoot = Path.Combine(GameRootPath, "resources");
                string pckPath = Path.Combine(resourcesRoot, packageName.Trim() + ".pck");
                if (!File.Exists(pckPath))
                {
                    return false;
                }
                string pkxPath = Path.Combine(resourcesRoot, packageName.Trim() + ".pkx");
                if (!File.Exists(pkxPath))
                {
                    pkxPath = string.Empty;
                }

                return PckIndexReader.TryEnumerateEntries(packageName, pckPath, pkxPath, entries);
            }
            catch
            {
                return false;
            }
        }

        private static class PckIndexReader
        {
            private const uint Key1 = 566434367;
            private const uint Key2 = 408690725;
            private const int FooterSize = 272;
            private const int PathBytes = 260;
            private const int MinEntrySize = PathBytes + 12;
            private const int MaxEntrySize = 1024 * 1024;

            public static bool TryEnumerateEntries(string packageName, string pckPath, string pkxPath, List<string> entries)
            {
                if (entries == null)
                {
                    return false;
                }

                int decodeFail = 0;
                int entrySizeInvalid = 0;
                int rawTooShort = 0;
                int readFail = 0;

                try
                {
                    using (PckConcatStream stream = new PckConcatStream(pckPath, pkxPath))
                    using (BinaryReader br = new BinaryReader(stream))
                    {
                        long length = stream.Length;
                        if (length < FooterSize + 8)
                        {
                            return false;
                        }

                        uint entryCount;
                        long tableOffset;
                        if (!TryReadFooter(stream, br, length, out tableOffset, out entryCount))
                        {
                            return false;
                        }

                        stream.Seek(tableOffset, SeekOrigin.Begin);
                        Encoding enc = Encoding.GetEncoding("GBK");
                        for (uint i = 0; i < entryCount; i++)
                        {
                            int entrySize;
                            if (!TryReadEntrySize(br, length, out entrySize))
                            {
                                entrySizeInvalid++;
                                break;
                            }

                            byte[] entryData = br.ReadBytes(entrySize);
                            if (entryData.Length != entrySize)
                            {
                                readFail++;
                                break;
                            }

                            byte[] raw = entrySize == FooterSize ? entryData : InflateEntry(entryData);
                            if (raw == null || raw.Length < MinEntrySize)
                            {
                                if (raw == null)
                                {
                                    decodeFail++;
                                }
                                else
                                {
                                    rawTooShort++;
                                }
                                continue;
                            }

                            string path = DecodeEntryPath(enc, raw);
                            if (string.IsNullOrWhiteSpace(path))
                            {
                                continue;
                            }

                            entries.Add(path);
                        }
                    }
                }
                catch
                {
                    return false;
                }

                if (decodeFail > 0 || entrySizeInvalid > 0 || rawTooShort > 0 || readFail > 0)
                {
                    LogPckIndexDiagnostic(packageName, pckPath, pkxPath, entries.Count,
                        decodeFail, entrySizeInvalid, rawTooShort, readFail);
                }

                return entries.Count > 0;
            }

            private static bool TryReadFooter(Stream stream, BinaryReader br, long length, out long tableOffset, out uint entryCount)
            {
                tableOffset = 0;
                entryCount = 0;
                if (stream == null || br == null)
                {
                    return false;
                }

                if (length < FooterSize + 8)
                {
                    return false;
                }

                stream.Seek(length - FooterSize, SeekOrigin.Begin);
                uint rawOffset;
                if (!TryReadUInt32(br, out rawOffset))
                {
                    return false;
                }
                tableOffset = rawOffset ^ Key1;
                if (tableOffset < 0 || tableOffset >= length)
                {
                    return false;
                }

                stream.Seek(length - 8, SeekOrigin.Begin);
                if (!TryReadUInt32(br, out entryCount))
                {
                    return false;
                }
                if (entryCount == 0 || entryCount > 1000000)
                {
                    return false;
                }

                return true;
            }

            private static bool TryReadEntrySize(BinaryReader br, long length, out int entrySize)
            {
                entrySize = 0;
                if (br == null || br.BaseStream == null || br.BaseStream.Position + 8 > length)
                {
                    return false;
                }

                uint sizeX1;
                uint sizeX2;
                if (!TryReadUInt32(br, out sizeX1) || !TryReadUInt32(br, out sizeX2))
                {
                    return false;
                }

                uint sizeA = sizeX1 ^ Key1;
                uint sizeB = sizeX2 ^ Key2;
                uint resolvedSize = sizeA == sizeB ? sizeA : 0;
                if (resolvedSize == 0)
                {
                    if (sizeB > 0 && sizeB <= MaxEntrySize)
                    {
                        resolvedSize = sizeB;
                    }
                    else if (sizeA > 0 && sizeA <= MaxEntrySize)
                    {
                        resolvedSize = sizeA;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (resolvedSize == 0 || resolvedSize > MaxEntrySize)
                {
                    return false;
                }

                entrySize = (int)resolvedSize;
                return true;
            }

            private static string DecodeEntryPath(Encoding enc, byte[] raw)
            {
                if (raw == null || raw.Length < PathBytes)
                {
                    return string.Empty;
                }
                int len = 0;
                while (len < PathBytes && raw[len] != 0)
                {
                    len++;
                }
                if (len <= 0)
                {
                    return string.Empty;
                }

                string path = enc.GetString(raw, 0, len).Replace('/', '\\');
                if (string.IsNullOrWhiteSpace(path))
                {
                    return string.Empty;
                }
                return path.Trim();
            }

            private static byte[] InflateEntry(byte[] data)
            {
                if (data == null || data.Length == 0)
                {
                    return null;
                }

                byte[] zlib = TryInflateZlib(data);
                if (zlib != null)
                {
                    return zlib;
                }

                bool hasZlibHeader = data.Length >= 2 && IsZlibHeader(data[0], data[1]);
                if (hasZlibHeader)
                {
                    int index = 2;
                    if ((data[1] & 0x20) != 0)
                    {
                        index += 4;
                    }
                    int len = data.Length - index - 4;
                    if (len > 0)
                    {
                        byte[] deflated = TryInflateDeflate(data, index, len);
                        if (deflated != null)
                        {
                            return deflated;
                        }
                    }
                }
                else
                {
                    byte[] deflated = TryInflateDeflate(data, 0, data.Length);
                    if (deflated != null)
                    {
                        return deflated;
                    }
                }

                return null;
            }

            private static bool IsZlibHeader(byte cmf, byte flg)
            {
                if ((cmf & 0x0F) != 8)
                {
                    return false;
                }
                int header = (cmf << 8) | flg;
                return (header % 31) == 0;
            }

            private static byte[] TryInflateZlib(byte[] data)
            {
                try
                {
                    using (MemoryStream input = new MemoryStream(data))
                    using (Ionic.Zlib.ZlibStream zlib = new Ionic.Zlib.ZlibStream(input, Ionic.Zlib.CompressionMode.Decompress))
                    using (MemoryStream output = new MemoryStream())
                    {
                        zlib.CopyTo(output);
                        return output.ToArray();
                    }
                }
                catch
                {
                    return null;
                }
            }

            private static byte[] TryInflateDeflate(byte[] data, int index, int len)
            {
                try
                {
                    using (MemoryStream input = new MemoryStream(data, index, len))
                    using (Ionic.Zlib.DeflateStream deflate = new Ionic.Zlib.DeflateStream(input, Ionic.Zlib.CompressionMode.Decompress))
                    using (MemoryStream output = new MemoryStream())
                    {
                        deflate.CopyTo(output);
                        return output.ToArray();
                    }
                }
                catch
                {
                    return null;
                }
            }

            private static bool TryReadUInt32(BinaryReader br, out uint value)
            {
                value = 0;
                if (br == null)
                {
                    return false;
                }
                byte[] data = br.ReadBytes(4);
                if (data == null || data.Length < 4)
                {
                    return false;
                }
                value = BitConverter.ToUInt32(data, 0);
                return true;
            }

            private static void LogPckIndexDiagnostic(string packageName, string pckPath, string pkxPath,
                int decodedCount, int decodeFail, int entrySizeInvalid, int rawTooShort, int readFail)
            {
                try
                {
                    string logsDir = Path.Combine(Application.StartupPath, "logs");
                    Directory.CreateDirectory(logsDir);
                    string logFile = Path.Combine(logsDir, "fweledit-errors.log");
                    using (StreamWriter sw = new StreamWriter(logFile, true, Encoding.UTF8))
                    {
                        sw.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] PCK_INDEX_DIAG");
                        sw.WriteLine("  package: " + packageName);
                        sw.WriteLine("  pck: " + pckPath);
                        if (!string.IsNullOrWhiteSpace(pkxPath))
                        {
                            sw.WriteLine("  pkx: " + pkxPath);
                        }
                        sw.WriteLine("  decoded: " + decodedCount);
                        sw.WriteLine("  decodeFail: " + decodeFail);
                        sw.WriteLine("  entrySizeInvalid: " + entrySizeInvalid);
                        sw.WriteLine("  rawTooShort: " + rawTooShort);
                        sw.WriteLine("  readFail: " + readFail);
                        sw.WriteLine();
                    }
                }
                catch
                { }
            }

            private sealed class PckConcatStream : Stream
            {
                private readonly FileStream pck;
                private readonly FileStream pkx;
                private readonly long pckLength;
                private readonly long pkxLength;
                private long position;

                public PckConcatStream(string pckPath, string pkxPath)
                {
                    pck = new FileStream(pckPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    pckLength = pck.Length;
                    if (!string.IsNullOrWhiteSpace(pkxPath) && File.Exists(pkxPath))
                    {
                        pkx = new FileStream(pkxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        pkxLength = pkx.Length;
                    }
                }

                public override bool CanRead => true;
                public override bool CanSeek => true;
                public override bool CanWrite => false;
                public override long Length => pckLength + pkxLength;

                public override long Position
                {
                    get { return position; }
                    set { Seek(value, SeekOrigin.Begin); }
                }

                public override void Flush() { }

                public override int Read(byte[] buffer, int offset, int count)
                {
                    if (buffer == null || count <= 0)
                    {
                        return 0;
                    }

                    long length = Length;
                    if (position >= length)
                    {
                        return 0;
                    }

                    int totalRead = 0;
                    if (position < pckLength)
                    {
                        pck.Position = position;
                        int toRead = (int)Math.Min(count, pckLength - position);
                        int read = pck.Read(buffer, offset, toRead);
                        totalRead += read;
                        position += read;
                        offset += read;
                        count -= read;
                    }

                    if (count > 0 && pkx != null && position < length)
                    {
                        pkx.Position = position - pckLength;
                        int read = pkx.Read(buffer, offset, count);
                        totalRead += read;
                        position += read;
                    }

                    return totalRead;
                }

                public override long Seek(long offset, SeekOrigin origin)
                {
                    long newPos = position;
                    switch (origin)
                    {
                        case SeekOrigin.Begin:
                            newPos = offset;
                            break;
                        case SeekOrigin.Current:
                            newPos = position + offset;
                            break;
                        case SeekOrigin.End:
                            newPos = Length + offset;
                            break;
                    }
                    if (newPos < 0)
                    {
                        newPos = 0;
                    }
                    if (newPos > Length)
                    {
                        newPos = Length;
                    }
                    position = newPos;
                    return position;
                }

                public override void SetLength(long value)
                {
                    throw new NotSupportedException();
                }

                public override void Write(byte[] buffer, int offset, int count)
                {
                    throw new NotSupportedException();
                }

                protected override void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        pck.Dispose();
                        if (pkx != null)
                        {
                            pkx.Dispose();
                        }
                    }
                    base.Dispose(disposing);
                }
            }
        }

        private void TryEnsureRequiredPckExtracted(string normalizedRelative)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(GameRootPath) || !Directory.Exists(GameRootPath))
                {
                    return;
                }
                EnsureWorkspaceReady();
                string resourcesRoot = GetWorkspaceResourcesRoot();
                if (string.IsNullOrWhiteSpace(resourcesRoot) || !Directory.Exists(resourcesRoot))
                {
                    return;
                }

                if (!triedExtractConfigsPck && IsConfigRelativePath(normalizedRelative))
                {
                    triedExtractConfigsPck = EnsureWorkspacePckPrepared("configs", true);
                }

                if (!triedExtractSurfacesPck && IsSurfaceRelativePath(normalizedRelative))
                {
                    triedExtractSurfacesPck = EnsureWorkspacePckPrepared("surfaces", true);
                }

                if (IsModelRelativePath(normalizedRelative))
                {
                    if (!triedExtractModelsPck)
                    {
                        triedExtractModelsPck = EnsureWorkspacePckPrepared("models", true);
                    }
                    if (!triedExtractLitModelsPck)
                    {
                        triedExtractLitModelsPck = EnsureWorkspacePckPrepared("litmodels", true);
                    }
                    if (!triedExtractMoxingPck)
                    {
                        triedExtractMoxingPck = EnsureWorkspacePckPrepared("moxing", true);
                    }
                }
            }
            catch
            { }
        }

        public bool EnsurePackageExtracted(string packageName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(packageName))
                {
                    return false;
                }
                EnsureWorkspaceReady();
                return EnsureWorkspacePckPrepared(packageName.Trim(), true);
            }
            catch
            {
                return false;
            }
        }

        public string GetExtractedPackageRoot(string packageName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(packageName))
                {
                    return string.Empty;
                }

                string pkgRootName = packageName.Trim() + ".pck.files";

                if (!string.IsNullOrWhiteSpace(WorkspaceRootPath))
                {
                    string wr = Path.Combine(WorkspaceRootPath, "resources", pkgRootName);
                    if (Directory.Exists(wr))
                    {
                        return wr;
                    }
                }

                if (!string.IsNullOrWhiteSpace(GameRootPath))
                {
                    string gr = Path.Combine(GameRootPath, "resources", pkgRootName);
                    if (Directory.Exists(gr))
                    {
                        return gr;
                    }
                }
            }
            catch
            { }

            return string.Empty;
        }

        public string ResolveResourcePath(string relativePath)
        {
            try
            {
                string resolved = ResolveResourceFile(relativePath);
                if (!string.IsNullOrWhiteSpace(resolved) && File.Exists(resolved))
                {
                    return resolved;
                }
            }
            catch
            { }

            return string.Empty;
        }

        private bool TryResolveIconsetFiles(out string sourceFilename, out string iconListFilename)
        {
            bool TryFindIconsetPair(string iconsetRoot, out string imgPath, out string txtPath)
            {
                imgPath = string.Empty;
                txtPath = string.Empty;
                if (string.IsNullOrWhiteSpace(iconsetRoot) || !Directory.Exists(iconsetRoot))
                {
                    return false;
                }

                string[] imageExtensions = new string[] { ".dds", ".png" };
                string[] preferredVariants = new string[] { "iconlist_ivtr0", "iconlist_ivtrm" };

                for (int v = 0; v < preferredVariants.Length; v++)
                {
                    string txt = Path.Combine(iconsetRoot, preferredVariants[v] + ".txt");
                    if (!File.Exists(txt))
                    {
                        continue;
                    }
                    for (int e = 0; e < imageExtensions.Length; e++)
                    {
                        string img = Path.Combine(iconsetRoot, preferredVariants[v] + imageExtensions[e]);
                        if (File.Exists(img))
                        {
                            imgPath = img;
                            txtPath = txt;
                            return true;
                        }
                    }
                }

                // Fallback for clients that use another iconlist base name.
                string[] txtFiles = Directory.GetFiles(iconsetRoot, "iconlist*.txt");
                for (int t = 0; t < txtFiles.Length; t++)
                {
                    string baseName = Path.GetFileNameWithoutExtension(txtFiles[t]);
                    for (int e = 0; e < imageExtensions.Length; e++)
                    {
                        string img = Path.Combine(iconsetRoot, baseName + imageExtensions[e]);
                        if (File.Exists(img))
                        {
                            imgPath = img;
                            txtPath = txtFiles[t];
                            return true;
                        }
                    }
                }

                return false;
            }

            // FW production path (same basis used by paid editor).
            if (!string.IsNullOrWhiteSpace(WorkspaceRootPath))
            {
                string iconsetRoot = Path.Combine(WorkspaceRootPath, "resources", "surfaces.pck.files", "iconset");
                if (TryFindIconsetPair(iconsetRoot, out sourceFilename, out iconListFilename))
                {
                    return true;
                }
            }

            if (!string.IsNullOrWhiteSpace(GameRootPath))
            {
                string iconsetRoot = Path.Combine(GameRootPath, "resources", "surfaces.pck.files", "iconset");
                if (TryFindIconsetPair(iconsetRoot, out sourceFilename, out iconListFilename))
                {
                    return true;
                }
            }

            string[] variants = new string[] { "iconlist_ivtr0", "iconlist_ivtrm" };
            string[] imageExtensions2 = new string[] { ".dds", ".png" };

            foreach (string variant in variants)
            {
                foreach (string imageExt in imageExtensions2)
                {
                    string imageCandidate = ResolveResourceFile(Path.Combine("surfaces", "iconset", variant + imageExt));
                    string txtCandidate = ResolveResourceFile(Path.Combine("surfaces", "iconset", variant + ".txt"));
                    if (File.Exists(imageCandidate) && File.Exists(txtCandidate))
                    {
                        sourceFilename = imageCandidate;
                        iconListFilename = txtCandidate;
                        return true;
                    }

                    imageCandidate = ResolveResourceFile(Path.Combine("iconset", variant + imageExt));
                    txtCandidate = ResolveResourceFile(Path.Combine("iconset", variant + ".txt"));
                    if (File.Exists(imageCandidate) && File.Exists(txtCandidate))
                    {
                        sourceFilename = imageCandidate;
                        iconListFilename = txtCandidate;
                        return true;
                    }
                }
            }

            sourceFilename = ResolveResourceFile(Path.Combine("surfaces", "iconset", "iconlist_ivtr0.dds"));
            iconListFilename = ResolveResourceFile(Path.Combine("surfaces", "iconset", "iconlist_ivtr0.txt"));
            return File.Exists(sourceFilename) && File.Exists(iconListFilename);
        }

        public bool TryGetIconsetPair(out string sourceFilename, out string iconListFilename)
        {
            sourceFilename = string.Empty;
            iconListFilename = string.Empty;
            try
            {
                return TryResolveIconsetFiles(out sourceFilename, out iconListFilename);
            }
            catch
            {
                return false;
            }
        }

        private Bitmap TryLoadDdsWithPfim(string ddsPath)
        {
            try
            {
                string[] pfimCandidates = new string[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pfim.dll"),
                    Path.Combine(GameRootPath ?? string.Empty, "fELedit", "Elements Editor Pago", "Pfim.dll"),
                    Path.Combine(GameRootPath ?? string.Empty, "fELedit", "Elements Editor Pago", "sTASKedit by Rey35", "Pfim.dll")
                };

                string pfimPath = string.Empty;
                for (int i = 0; i < pfimCandidates.Length; i++)
                {
                    if (File.Exists(pfimCandidates[i]))
                    {
                        pfimPath = pfimCandidates[i];
                        break;
                    }
                }
                if (string.IsNullOrWhiteSpace(pfimPath) || !File.Exists(ddsPath))
                {
                    return null;
                }

                Assembly pfimAsm = Assembly.LoadFrom(pfimPath);
                Type pfimageType = pfimAsm.GetType("Pfim.Pfimage");
                Type iimageType = pfimAsm.GetType("Pfim.IImage");
                if (pfimageType == null || iimageType == null)
                {
                    return null;
                }

                MethodInfo fromFile = pfimageType.GetMethod("FromFile", new Type[] { typeof(string) });
                if (fromFile == null)
                {
                    return null;
                }

                object image = fromFile.Invoke(null, new object[] { ddsPath });
                if (image == null)
                {
                    return null;
                }

                try
                {
                    PropertyInfo compressedProp = iimageType.GetProperty("Compressed");
                    MethodInfo decompressMethod = iimageType.GetMethod("Decompress");
                    if (compressedProp != null && decompressMethod != null)
                    {
                        bool compressed = (bool)compressedProp.GetValue(image, null);
                        if (compressed)
                        {
                            decompressMethod.Invoke(image, null);
                        }
                    }

                    int width = (int)iimageType.GetProperty("Width").GetValue(image, null);
                    int height = (int)iimageType.GetProperty("Height").GetValue(image, null);
                    int stride = (int)iimageType.GetProperty("Stride").GetValue(image, null);
                    int bpp = (int)iimageType.GetProperty("BitsPerPixel").GetValue(image, null);
                    byte[] data = (byte[])iimageType.GetProperty("Data").GetValue(image, null);
                    if (data == null || width <= 0 || height <= 0 || stride <= 0)
                    {
                        return null;
                    }

                    PixelFormat pf = bpp == 24 ? PixelFormat.Format24bppRgb : PixelFormat.Format32bppArgb;
                    Bitmap bmp = new Bitmap(width, height, pf);
                    BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, pf);
                    try
                    {
                        int rowBytes = Math.Min(Math.Abs(bmpData.Stride), stride);
                        for (int y = 0; y < height; y++)
                        {
                            IntPtr dst = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                            Marshal.Copy(data, y * stride, dst, rowBytes);
                        }
                    }
                    finally
                    {
                        bmp.UnlockBits(bmpData);
                    }

                    return bmp;
                }
                finally
                {
                    MethodInfo dispose = iimageType.GetMethod("Dispose");
                    if (dispose != null)
                    {
                        dispose.Invoke(image, null);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public void LoadTheme()
        {
            try
            {
                string line;
                arrTheme = new List<string>();
                string theme_list = ResolveResourceFileAny("theme.txt", Path.Combine("configs.pck.files", "theme.txt"));
                Encoding enc = Encoding.GetEncoding("GBK");
                int lines = File.ReadAllLines(theme_list).Length;
                StreamReader file = new StreamReader(theme_list, enc);
                Application.DoEvents();
                int count = 0;

                while ((line = file.ReadLine()) != null)
                {
                    if (line != null && line.Length > 0 && !line.StartsWith("#") && !line.StartsWith("/"))
                    {
                        arrTheme.Add(line);
                    }
                    count++;
                }
                file.Close();
                database.arrTheme = arrTheme;
            }
            catch
            {
                database.arrTheme = null;
            }
        }

        static public Bitmap getSkillIcon(int skillid)
        {
            Bitmap img = Properties.Resources.ResourceManager.GetObject("_" + skillid) as Bitmap;
            return img != null ? img : new Bitmap(new Bitmap(Resources.blank));
        }

        public void loadItem_color()
        {
            string line;
            item_color = new SortedList<int, int>();
            string iconlist_ivtrm = ResolveResourceFileAny(
                Path.Combine("data", "item_color.txt"),
                Path.Combine("configs", "item_color.txt"),
                "item_color.txt",
                Path.Combine("configs.pck.files", "item_color.txt"));

            string extension = Path.GetExtension(iconlist_ivtrm);
            if (extension == ".txt")
            {
                Encoding enc = Encoding.GetEncoding("GBK");
                int lines = File.ReadAllLines(iconlist_ivtrm).Length;
                StreamReader file = new StreamReader(iconlist_ivtrm, enc);
                int count = 0;
                while ((line = file.ReadLine()) != null)
                {
                    Application.DoEvents();
                    string[] data = line.Split(null);
                    try
                    {
                        string v1 = data[0].ToString();
                        string v2 = data[1].ToString();
                        if (v1.Length > 0 && v2.Length > 0)
                        {
                            item_color.Add(int.Parse(v1), int.Parse(v2));
                        }
                        else
                        {
                            if (v1.Length > 0)
                            {
                                item_color.Add(int.Parse(v1), 0);
                            }
                            if (v2.Length > 0)
                            {
                                item_color.Add(0, int.Parse(v2));
                            }
                        }
                    }
                    catch (Exception) { }
                    count++;
                }
                file.Close();
            }
            database.item_color = item_color;

            //loaditem_desc();
        }

        //public void loaditem_desc()
        //{
        //    string line;
        //    item_desc = new SortedList<int, string>();
        //    string iconlist_ivtrm = Path.GetDirectoryName(Application.ExecutablePath) + "\\resources\\data\\item_ext_desc.txt";
        //    Encoding enc = Encoding.GetEncoding("GBK");
        //    int lines = File.ReadAllLines(iconlist_ivtrm).Length;
        //    StreamReader file = new StreamReader(iconlist_ivtrm, enc);
        //    Application.DoEvents();
        //    int count = 0;

        //    while ((line = file.ReadLine()) != null)
        //    {
        //        if (line != null && line.Length > 0 && !line.StartsWith("#") && !line.StartsWith("/"))
        //        {
        //            string[] data = line.Split('"');
        //            try
        //            {
        //                Application.DoEvents();
        //                item_desc.Add(int.Parse(data[0]), data[1].ToString().Replace('"', ' '));
        //            }
        //            catch (Exception) { }
        //        }
        //        count++;
        //    }
        //    file.Close();
        //    database.item_desc = item_desc;
        //}

        public void LoadItemExtDescList()
        {
            if (database.item_ext_desc != null)
            {
                MainWindow.item_ext_desc = database.item_ext_desc;
                return;
            }
            try
            {
                string path = ResolveResourceFileAny(
                    Path.Combine("data", "item_ext_desc.txt"),
                    "item_ext_desc.txt",
                    Path.Combine("configs.pck.files", "item_ext_desc.txt"));
                string extension = Path.GetExtension(path);
                if (File.Exists(path))
                {
                    try
                    {
                        StreamReader sr = new StreamReader(path, Encoding.Unicode);
                        MainWindow.item_ext_desc = sr.ReadToEnd().Split(new char[] { '\"' });
                        string[] temp = MainWindow.item_ext_desc[0].Split(new char[] { '\n' });
                        MainWindow.item_ext_desc[0] = temp[temp.Length - 1];
                        sr.Dispose();
                        sr.Close();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("ERROR LOADING ITEM DESCRIPTION LIST\n" + e.Message);
                    }
                }
                else
                {
                    MainWindow.item_ext_desc = new string[0];
                }
            }
            catch (Exception ex)
            {
                MainWindow.item_ext_desc = new string[0];
            }
            GC.Collect();
            database.item_ext_desc = MainWindow.item_ext_desc;
        }

        private SortedList<string, Point> loadSurfaces()
        {
            string sourceFilename;
            string iconlist_ivtrm;
            if (!TryResolveIconsetFiles(out sourceFilename, out iconlist_ivtrm))
            {
                database.sourceBitmap = null;
                return new SortedList<string, Point>();
            }

            string extension = Path.GetExtension(sourceFilename);
            if (extension == ".dds")
            {
                try
                {
                    Bitmap bmp = TryLoadDdsWithPfim(sourceFilename);
                    DDSReader.Utils.PixelFormat st = (DDSReader.Utils.PixelFormat)DDSFORMAT;
                    if (bmp == null)
                    {
                        bmp = (st == DDSReader.Utils.PixelFormat.UNKNOWN)
                            ? DDS.LoadImage(sourceFilename, true)
                            : DDS.LoadImage(sourceFilename, true, st);
                    }
                    if (bmp == null)
                    {
                        // FW icon atlases are commonly DXT3.
                        bmp = DDS.LoadImage(sourceFilename, true, DDSReader.Utils.PixelFormat.DXT3);
                    }
                    if (bmp == null)
                    {
                        bmp = DDS.LoadImage(sourceFilename, true, DDSReader.Utils.PixelFormat.DXT5);
                    }
                    if (bmp == null)
                    {
                        bmp = DDS.LoadImage(sourceFilename, true, DDSReader.Utils.PixelFormat.DXT1);
                    }
                    if (bmp != null)
                    {
                        sourceBitmap = bmp;
                    }
                }
                catch
                {
                    sourceBitmap = null;
                }
            }
            else
            {
                try
                {
                    sourceBitmap = (Bitmap)Image.FromFile(sourceFilename);
                }
                catch
                {
                    sourceBitmap = null;
                }
            }
            if (sourceBitmap == null && extension == ".dds")
            {
                string pngCandidate = Path.ChangeExtension(sourceFilename, ".png");
                if (File.Exists(pngCandidate))
                {
                    try
                    {
                        sourceBitmap = (Bitmap)Image.FromFile(pngCandidate);
                    }
                    catch
                    {
                        sourceBitmap = null;
                    }
                }
            }
            database.sourceBitmap = sourceBitmap;
            SortedList<string, Bitmap> results = new SortedList<string, Bitmap>();
            List<Bitmap> zxczxc = new List<Bitmap>();
            List<string> fileNames = new List<string>();

            imagesx = new SortedList<int, string>();
            imagesById = new SortedList<int, string>();
            int w = 0;
            int h = 0;

            int counter = 0;
            string line;
            Encoding enc = Encoding.GetEncoding("GBK");
            StreamReader file = null;
            string extension2 = Path.GetExtension(iconlist_ivtrm);
            file = new StreamReader(iconlist_ivtrm, enc);

            while ((line = file.ReadLine()) != null)
            {
                switch (counter)
                {
                    case 0:
                        w = int.Parse(line);
                        database.iconWidth = w;
                        break;
                    case 1:
                        h = int.Parse(line);
                        database.iconHeight = h;
                        break;
                    case 2:
                        rows = int.Parse(line);
                        database.rows = rows;
                        break;
                    case 3:
                        cols = int.Parse(line);
                        database.cols = cols;
                        break;
                    default:
                        fileNames.Add(line);
                        break;
                }
                counter++;
            }
            file.Close();
            imageposition = new SortedList<string, Point>();
            int x, y = 0;
            for (int a = 0; a < fileNames.Count; a++)
            {
                Application.DoEvents();
                y = a / cols;
                x = a - y * cols;
                x = x * w;
                y = y * h;
                try
                {
                    string key = NormalizeIconKey(fileNames[a]);
                    imagesx.Add(a, key);
                    if (!string.IsNullOrEmpty(key) && !imageposition.ContainsKey(key))
                    {
                        imageposition.Add(key, new Point(x, y));
                    }
                    int? leadingId = TryExtractLeadingNumber(key);
                    if (leadingId.HasValue && !imagesById.ContainsKey(leadingId.Value))
                    {
                        imagesById.Add(leadingId.Value, key);
                    }
                }
                catch (Exception) { }

            }
            database.imagesx = imagesx;
            database.imagesById = imagesById;
            database.imageposition = imageposition;
            return imageposition;
        }

        public void LoadSkillList()
        {
            if (database.skillstr != null)
            {
                MainWindow.skillstr = database.skillstr;
                return;
            }
            String path = ResolveResourceFileAny(
                Path.Combine("data", "skillstr.txt"),
                "skillstr.txt",
                Path.Combine("configs.pck.files", "skillstr.txt"));
            if (File.Exists(path))
            {
                try
                {
                    StreamReader sr = new StreamReader(path, Encoding.Unicode);
                    MainWindow.skillstr = sr.ReadToEnd().Split(new char[] { '\"' });
                    string[] temp = MainWindow.skillstr[0].Split(new char[] { '\n' });
                    MainWindow.skillstr[0] = temp[temp.Length - 1];
                    sr.Close();
                }
                catch (Exception e)
                {
                    MessageBox.Show("ERROR LOADING SKILL LIST\n" + e.Message);
                }
            }
            else
            {
                MainWindow.skillstr = new string[0];
            }
            database.skillstr = MainWindow.skillstr;
        }

        private void LoadAddonList()
        {
            if (database.addonslist != null)
            {
                MainWindow.addonslist = database.addonslist;
                return;
            }
            String path = ResolveResourceFileAny(
                Path.Combine("data", "addon_table.txt"),
                "addon_table.txt",
                Path.Combine("configs.pck.files", "addon_table.txt"));
            MainWindow.addonslist = new SortedList();
            if (File.Exists(path))
            {
                try
                {
                    StreamReader sr = new StreamReader(path, Encoding.Unicode);

                    char[] seperator = new char[] { '\t' };
                    string line;
                    string[] split;
                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine();
                        if (line.Contains("\t") && line != "" && !line.StartsWith("/") && !line.StartsWith("#"))
                        {
                            split = line.Split(seperator);
                            MainWindow.addonslist.Add(split[0], split[1]);
                        }
                    }

                    sr.Close();
                }
                catch (Exception e)
                {
                    MessageBox.Show("ERROR LOADING ADDON LIST\n" + e.Message);
                }
            }
            else
            {
                // FW fallback: map addon id -> addon type from item_ext_prop.txt
                string itemExtPropPath = ResolveResourceFileAny(
                    Path.Combine("data", "item_ext_prop.txt"),
                    "item_ext_prop.txt",
                    Path.Combine("configs.pck.files", "item_ext_prop.txt"));
                if (File.Exists(itemExtPropPath))
                {
                    try
                    {
                        using (StreamReader sr = new StreamReader(itemExtPropPath, Encoding.GetEncoding("GBK")))
                        {
                            int currentType = -1;
                            bool inBlock = false;
                            while (!sr.EndOfStream)
                            {
                                string line = sr.ReadLine();
                                if (string.IsNullOrWhiteSpace(line))
                                {
                                    continue;
                                }
                                string trimmed = line.Trim();
                                if (trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith("*") || trimmed.StartsWith("*/"))
                                {
                                    continue;
                                }

                                if (trimmed.StartsWith("type:", StringComparison.OrdinalIgnoreCase))
                                {
                                    string rawType = trimmed.Substring(5).Trim();
                                    int parsedType;
                                    if (int.TryParse(rawType, out parsedType))
                                    {
                                        currentType = parsedType;
                                        inBlock = false;
                                    }
                                    continue;
                                }

                                if (trimmed.StartsWith("{"))
                                {
                                    inBlock = true;
                                    continue;
                                }
                                if (trimmed.StartsWith("}"))
                                {
                                    inBlock = false;
                                    continue;
                                }

                                if (!inBlock || currentType < 0)
                                {
                                    continue;
                                }

                                string[] ids = trimmed.Split(new char[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 0; i < ids.Length; i++)
                                {
                                    int id;
                                    if (!int.TryParse(ids[i], out id))
                                    {
                                        continue;
                                    }
                                    string idKey = id.ToString();
                                    string typeValue = currentType.ToString();
                                    if (MainWindow.addonslist.ContainsKey(idKey))
                                    {
                                        MainWindow.addonslist[idKey] = typeValue;
                                    }
                                    else
                                    {
                                        MainWindow.addonslist.Add(idKey, typeValue);
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        MainWindow.addonslist = new SortedList();
                    }
                }
            }
            database.addonslist = MainWindow.addonslist;
        }

        public void LoadLocalizationText()
        {
            MainWindow.LocalizationText = new SortedList();
            string path = ResolveResourceFileAny(
                Path.Combine("data", "language_en.txt"),
                "language_en.txt",
                Path.Combine("configs.pck.files", "language_en.txt"));
            if (!File.Exists(path))
            {
                string editorOverride = Path.Combine(Application.StartupPath ?? string.Empty, "resources", "data", "language_en.txt");
                if (File.Exists(editorOverride))
                {
                    path = editorOverride;
                }
            }
            if (!File.Exists(path))
            {
                string paidToolPath = FindPaidToolLanguageFile();
                if (!string.IsNullOrWhiteSpace(paidToolPath) && File.Exists(paidToolPath))
                {
                    path = paidToolPath;
                    TryMirrorPaidLocalizationToLocal(paidToolPath);
                }
            }

            if (File.Exists(path))
            {
                if (!TryLoadLocalizationFile(path))
                {
                    MessageBox.Show("ERROR LOADING LOCALIZATION\nFailed to parse language file.");
                }
            }
            else
            {
                // FW builds may not ship language_en.txt; fallback to skillstr key/value pairs.
                string skillPath = ResolveResourceFileAny(
                    Path.Combine("data", "skillstr.txt"),
                    "skillstr.txt",
                    Path.Combine("configs.pck.files", "skillstr.txt"));
                if (File.Exists(skillPath))
                {
                    try
                    {
                        using (StreamReader sr = new StreamReader(skillPath, Encoding.Unicode))
                        {
                            while (!sr.EndOfStream)
                            {
                                string line = sr.ReadLine();
                                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("/") || line.StartsWith("#"))
                                {
                                    continue;
                                }
                                int firstQuote = line.IndexOf('"');
                                int lastQuote = line.LastIndexOf('"');
                                if (firstQuote <= 0 || lastQuote <= firstQuote)
                                {
                                    continue;
                                }
                                string key = line.Substring(0, firstQuote).Trim();
                                string value = line.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
                                if (key.Length == 0 || MainWindow.LocalizationText.ContainsKey(key))
                                {
                                    continue;
                                }
                                MainWindow.LocalizationText.Add(key, value);
                            }
                        }
                    }
                    catch
                    {
                        MainWindow.LocalizationText = new SortedList();
                    }
                }
            }

            MergeEditorLocalizationOverrides();
            database.LocalizationText = MainWindow.LocalizationText;
        }

        private bool TryLoadLocalizationFile(string path)
        {
            try
            {
                using (StreamReader sr = new StreamReader(path, Encoding.UTF8, true))
                {
                    char[] seperator = new char[] { '"' };
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("/") || line.StartsWith("#"))
                        {
                            continue;
                        }
                        string[] split = line.Split(seperator);
                        if (split.Length < 2)
                        {
                            continue;
                        }
                        string key = split[0].Trim();
                        string value = split[1];
                        if (key.Length == 0)
                        {
                            continue;
                        }
                        if (MainWindow.LocalizationText.ContainsKey(key))
                        {
                            MainWindow.LocalizationText[key] = value;
                        }
                        else
                        {
                            MainWindow.LocalizationText.Add(key, value);
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string FindPaidToolLanguageFile()
        {
            try
            {
                string baseDir = Application.StartupPath ?? string.Empty;
                if (string.IsNullOrWhiteSpace(baseDir))
                {
                    return string.Empty;
                }

                string paidRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Elements Editor Pago"));
                if (Directory.Exists(paidRoot))
                {
                    string direct = Path.Combine(paidRoot, "sTASKedit by Rey35", "localization", "language_en.txt");
                    if (File.Exists(direct))
                    {
                        return direct;
                    }
                    string found = Directory.GetFiles(paidRoot, "language_en.txt", SearchOption.AllDirectories).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(found))
                    {
                        return found;
                    }
                }
            }
            catch
            { }

            return string.Empty;
        }

        private void TryMirrorPaidLocalizationToLocal(string paidToolPath)
        {
            try
            {
                string appPath = Application.StartupPath ?? string.Empty;
                if (string.IsNullOrWhiteSpace(appPath))
                {
                    return;
                }
                string localPath = Path.Combine(appPath, "resources", "data", "language_en.txt");
                if (File.Exists(localPath))
                {
                    return;
                }
                string localDir = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrWhiteSpace(localDir) && !Directory.Exists(localDir))
                {
                    Directory.CreateDirectory(localDir);
                }
                File.Copy(paidToolPath, localPath, false);
            }
            catch
            {
                // Non-fatal: localization will still be loaded directly from paid tool path.
            }
        }

        private void MergeEditorLocalizationOverrides()
        {
            try
            {
                string localOverride = Path.Combine(Application.StartupPath, "resources", "data", "language_en.txt");
                if (!File.Exists(localOverride))
                {
                    return;
                }

                using (StreamReader sr = new StreamReader(localOverride, Encoding.Unicode))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("/") || line.StartsWith("#"))
                        {
                            continue;
                        }
                        int firstQuote = line.IndexOf('"');
                        int lastQuote = line.LastIndexOf('"');
                        if (firstQuote <= 0 || lastQuote <= firstQuote)
                        {
                            continue;
                        }
                        string key = line.Substring(0, firstQuote).Trim();
                        string value = line.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
                        if (key.Length == 0)
                        {
                            continue;
                        }

                        if (MainWindow.LocalizationText.ContainsKey(key))
                        {
                            MainWindow.LocalizationText[key] = value;
                        }
                        else
                        {
                            MainWindow.LocalizationText.Add(key, value);
                        }
                    }
                }
            }
            catch
            { }
        }

        //public void LoadInstanceList()
        //{
        //    if (database.InstanceList != null)
        //    {
        //        MainWindow.InstanceList = database.InstanceList;
        //        return;
        //    }

        //    database.defaultMapsTemplate = new SortedList<int, Map>();
        //    MainWindow.InstanceList = new SortedList();
        //    String path = Path.GetDirectoryName(Application.ExecutablePath) + "\\configs\\instance_en.txt";
        //    if (File.Exists(path))
        //    {
        //        try
        //        {
        //            StreamReader sr = new StreamReader(path, Encoding.Unicode);

        //            char[] seperator = new char[] { '\t' };
        //            string line;
        //            string[] split;
        //            while (!sr.EndOfStream)
        //            {
        //                line = sr.ReadLine();
        //                if (line.Contains("\t") && line != "" && !line.StartsWith("/") && !line.StartsWith("#"))
        //                {
        //                    split = line.Split(seperator);
        //                    if (split.Length > 2)
        //                    {
        //                        MainWindow.InstanceList.Add(split[0], " [" + split[1] + "] [" + split[2] + "] " + split[3] + "");
        //                        Map map = new Map();
        //                        map.name = split[3];
        //                        map.realName = split[2];
        //                        database.defaultMapsTemplate.Add(Int32.Parse(split[0]), map);
        //                    }
        //                    else
        //                    {
        //                        MainWindow.InstanceList.Add(split[0], split[1]);
        //                    }
        //                }
        //            }

        //            sr.Close();
        //        }
        //        catch (Exception e)
        //        {
        //            MessageBox.Show("ERROR LOADING INSTANCE LIST\n" + e.Message);
        //        }
        //    }
        //    else
        //    {
        //        MessageBox.Show("NOT FOUND localization:" + path + "!");
        //    }
        //    database.InstanceList = MainWindow.InstanceList;
        //}

        public void LoadBuffList()
        {
            if (database.buff_str != null)
            {
                MainWindow.buff_str = database.buff_str;
                return;
            }
            string path = ResolveResourceFileAny(
                Path.Combine("data", "buff_str.txt"),
                "buff_str.txt",
                Path.Combine("configs.pck.files", "buff_str.txt"));
            if (File.Exists(path))
            {
                try
                {
                    StreamReader sr = new StreamReader(path, Encoding.Unicode);
                    MainWindow.buff_str = sr.ReadToEnd().Split(new char[] { '\"' });
                    string[] temp = MainWindow.buff_str[0].Split(new char[] { '\n' });
                    MainWindow.buff_str[0] = temp[temp.Length - 1];

                    sr.Close();
                }
                catch (Exception e)
                {
                    MessageBox.Show("ERROR LOADING BUFF LIST\n" + e.Message);
                }
            }
            else
            {
                MainWindow.buff_str = new string[0];
            }
            database.buff_str = MainWindow.buff_str;
        }
    }
}
