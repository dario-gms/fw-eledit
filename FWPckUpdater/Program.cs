using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FWPckUpdater
{
    internal static class Program
    {
        private const int WinPckOk = 0;
        private const int DefaultPckVersionId = 0;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("pckdll_x64.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int do_CreatePckFile(string sourcePath, string destinationPckFile, int versionId, int level);

        [DllImport("pckdll_x64.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int do_AddFileToPckFile(string sourcePath, string destinationPckFile, string pathInPckToAdd, int level);

        [DllImport("pckdll_x64.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int pck_open(string pckFile);

        [DllImport("pckdll_x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pck_close();

        [DllImport("pckdll_x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void pck_StringArrayReset();

        [DllImport("pckdll_x64.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern void pck_StringArrayAppend(string path);

        [DllImport("pckdll_x64.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr pck_getFileEntryByPath(string pathInPck);

        [DllImport("pckdll_x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pck_DeleteEntry(IntPtr entry);

        [DllImport("pckdll_x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pck_DeleteEntrySubmit();

        [DllImport("pckdll_x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pck_setCompressLevel(int level);

        [DllImport("pckdll_x64.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int pck_UpdatePckFileSubmit(string pckFile, IntPtr entry);

        [DllImport("pckdll_x64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr pck_getLastErrorMsg();

        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            try
            {
                SetDllDirectory(AppDomain.CurrentDomain.BaseDirectory);

                if (args == null || args.Length < 3)
                {
                    Console.Error.WriteLine("Usage: FWPckUpdater.exe <update|rebuild> <staging-folder> <target-pck> [compression-level] [pck-version-id]");
                    Console.Error.WriteLine("       FWPckUpdater.exe verify <target-pck> <path-in-pck>");
                    return 2;
                }

                string mode = Normalize(args[0]);
                if (string.Equals(mode, "verify", StringComparison.OrdinalIgnoreCase))
                {
                    string verifyPck = Normalize(args[1]);
                    string verifyPath = Normalize(args[2]).Replace(Path.DirectorySeparatorChar, '\\').Replace(Path.AltDirectorySeparatorChar, '\\').Trim('\\');
                    if (!File.Exists(verifyPck))
                    {
                        Console.Error.WriteLine("Target package not found: " + verifyPck);
                        return 5;
                    }

                    bool exists = PackageEntryExists(verifyPck, verifyPath);
                    Console.WriteLine(exists ? "FOUND " + verifyPath : "MISSING " + verifyPath);
                    return exists ? 0 : 20;
                }

                string stagingFolder = Normalize(args[1]);
                string targetPck = Normalize(args[2]);
                int compressionLevel = ParseCompressionLevel(args.Length >= 4 ? args[3] : null);
                int pckVersionId = ParsePckVersionId(args.Length >= 5 ? args[4] : null);

                if (!Directory.Exists(stagingFolder))
                {
                    Console.Error.WriteLine("Staging folder not found: " + stagingFolder);
                    return 3;
                }

                string[] topLevelEntries = Directory.GetFileSystemEntries(stagingFolder, "*", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (topLevelEntries.Length == 0)
                {
                    Console.Error.WriteLine("Staging folder is empty: " + stagingFolder);
                    return 4;
                }

                string targetDirectory = Path.GetDirectoryName(targetPck) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(targetDirectory))
                {
                    Console.Error.WriteLine("Invalid target package path: " + targetPck);
                    return 5;
                }

                Directory.CreateDirectory(targetDirectory);

                if (string.Equals(mode, "rebuild", StringComparison.OrdinalIgnoreCase))
                {
                    string tempRebuildPck = targetPck + ".rebuild.tmp";
                    try
                    {
                        if (File.Exists(tempRebuildPck))
                        {
                            File.Delete(tempRebuildPck);
                        }
                    }
                    catch
                    {
                    }

                    Console.WriteLine("Rebuilding package from folder...");
                    int rebuildResult = RebuildPackageFromRoots(topLevelEntries, tempRebuildPck, compressionLevel, pckVersionId);
                    if (rebuildResult != WinPckOk)
                    {
                        Console.Error.WriteLine("WinPCK rebuild failed with code " + rebuildResult.ToString() + "." + BuildLastErrorSuffix());
                        return 12;
                    }

                    if (!File.Exists(tempRebuildPck))
                    {
                        Console.Error.WriteLine("WinPCK rebuild finished but output package was not found: " + tempRebuildPck);
                        return 13;
                    }

                    if (File.Exists(targetPck))
                    {
                        File.Delete(targetPck);
                    }

                    File.Move(tempRebuildPck, targetPck);
                    Console.WriteLine("Package rebuild completed.");
                    return 0;
                }

                if (!string.Equals(mode, "update", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine("Unsupported mode: " + mode);
                    return 14;
                }

                if (!File.Exists(targetPck))
                {
                    Console.WriteLine("Creating new package from staged root entries...");
                    int createResult = RebuildPackageFromRoots(topLevelEntries, targetPck, compressionLevel, pckVersionId);
                    if (createResult != WinPckOk)
                    {
                        Console.Error.WriteLine("WinPCK create failed with code " + createResult.ToString() + ".");
                        return 10;
                    }
                    Console.WriteLine("Package update completed.");
                    return 0;
                }

                string[] files = Directory.GetFiles(stagingFolder, "*", SearchOption.AllDirectories)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (files.Length == 0)
                {
                    Console.Error.WriteLine("Staging folder has no files: " + stagingFolder);
                    return 4;
                }

                HashSet<string> directoriesAddedAsFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string[] stagedDirectories = Directory.GetDirectories(stagingFolder, "*", SearchOption.AllDirectories)
                    .OrderBy(path => GetRelativePckPath(stagingFolder, path).Count(ch => ch == '\\'))
                    .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                for (int i = 0; i < stagedDirectories.Length; i++)
                {
                    string directory = stagedDirectories[i];
                    string directoryInPck = GetRelativePckPath(stagingFolder, directory);
                    if (string.IsNullOrWhiteSpace(directoryInPck)
                        || IsUnderAddedDirectory(directoryInPck, directoriesAddedAsFolders))
                    {
                        continue;
                    }

                    string parentInPck = Path.GetDirectoryName(directoryInPck) ?? string.Empty;
                    parentInPck = parentInPck.Replace(Path.DirectorySeparatorChar, '\\').Replace(Path.AltDirectorySeparatorChar, '\\');

                    if (PackageEntryExists(targetPck, directoryInPck))
                    {
                        Console.WriteLine("Merging existing folder: " + directoryInPck);
                        int mergeFolderResult = do_AddFileToPckFile(directory, targetPck, parentInPck, compressionLevel);
                        if (mergeFolderResult == WinPckOk)
                        {
                            directoriesAddedAsFolders.Add(directoryInPck);
                            continue;
                        }

                        Console.WriteLine("Existing folder merge failed with code " + mergeFolderResult.ToString() + " for " + directoryInPck + "; trying child entries." + BuildLastErrorSuffix());
                        continue;
                    }

                    Console.WriteLine("Adding new folder: " + directoryInPck);
                    int addFolderResult = do_AddFileToPckFile(directory, targetPck, parentInPck, compressionLevel);
                    if (addFolderResult != WinPckOk)
                    {
                        Console.WriteLine("Folder add failed with code " + addFolderResult.ToString() + " for " + directoryInPck + "; falling back to individual files." + BuildLastErrorSuffix());
                        continue;
                    }

                    directoriesAddedAsFolders.Add(directoryInPck);
                }

                bool usedRootSubmitFallback = false;
                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];
                    string pathInPck = GetRelativePckPath(stagingFolder, file);
                    if (IsUnderAddedDirectory(pathInPck, directoriesAddedAsFolders))
                    {
                        continue;
                    }

                    string directoryInPck = Path.GetDirectoryName(pathInPck);
                    if (directoryInPck == null)
                    {
                        directoryInPck = string.Empty;
                    }
                    directoryInPck = directoryInPck.Replace(Path.DirectorySeparatorChar, '\\').Replace(Path.AltDirectorySeparatorChar, '\\');
                    Console.WriteLine("Adding file " + (i + 1).ToString() + "/" + files.Length.ToString() + ": " + pathInPck);

                    if (PackageEntryExists(targetPck, pathInPck))
                    {
                        int existingUpdateResult = UpdateExistingPackageFile(file, pathInPck, targetPck, compressionLevel);
                        if (existingUpdateResult == WinPckOk)
                        {
                            continue;
                        }

                        Console.WriteLine("Existing file replace failed with code " + existingUpdateResult.ToString() + " for " + pathInPck + "; trying root submit fallback." + BuildLastErrorSuffix());
                        if (!usedRootSubmitFallback)
                        {
                            int fallbackResult = UpdatePackageFromRoots(topLevelEntries, targetPck, compressionLevel);
                            if (fallbackResult != WinPckOk)
                            {
                                Console.Error.WriteLine("WinPCK update failed with code " + existingUpdateResult.ToString() + " while replacing existing staged entry " + pathInPck + ". Root submit fallback failed with code " + fallbackResult.ToString() + "." + BuildLastErrorSuffix());
                                return 11;
                            }
                            usedRootSubmitFallback = true;
                        }

                        continue;
                    }

                    int addResult = do_AddFileToPckFile(file, targetPck, directoryInPck, compressionLevel);
                    if (addResult != WinPckOk)
                    {
                        Console.WriteLine("Direct add failed with code " + addResult.ToString() + " for " + pathInPck + "; trying root submit fallback." + BuildLastErrorSuffix());
                        int fallbackResult = UpdatePackageFromRoots(topLevelEntries, targetPck, compressionLevel);
                        if (fallbackResult != WinPckOk)
                        {
                            Console.Error.WriteLine("WinPCK direct add failed with code " + addResult.ToString() + " for file " + pathInPck + ". Root submit fallback failed with code " + fallbackResult.ToString() + "." + BuildLastErrorSuffix());
                            return 11;
                        }

                        usedRootSubmitFallback = true;
                        break;
                    }
                }

                Console.WriteLine("Package update completed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().Trim('"');
        }

        private static string GetRelativePath(string rootPath, string filePath)
        {
            string normalizedRoot = EnsureTrailingSeparator(Path.GetFullPath(rootPath));
            string normalizedFile = Path.GetFullPath(filePath);
            Uri rootUri = new Uri(normalizedRoot, UriKind.Absolute);
            Uri fileUri = new Uri(normalizedFile, UriKind.Absolute);
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(fileUri).ToString()).Replace('/', '\\');
        }

        private static string EnsureTrailingSeparator(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (value.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                || value.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                return value;
            }

            return value + Path.DirectorySeparatorChar;
        }

        private static string GetRelativePckPath(string root, string file)
        {
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullFile = Path.GetFullPath(file);
            string relative = fullFile.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)
                ? fullFile.Substring(fullRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                : Path.GetFileName(file);
            return relative.Replace(Path.DirectorySeparatorChar, '\\').Replace(Path.AltDirectorySeparatorChar, '\\');
        }

        private static string GetTopLevelName(string pathInPck)
        {
            string normalized = (pathInPck ?? string.Empty).Replace(Path.DirectorySeparatorChar, '\\').Replace(Path.AltDirectorySeparatorChar, '\\').TrimStart('\\');
            int slash = normalized.IndexOf('\\');
            return slash > 0 ? normalized.Substring(0, slash) : normalized;
        }

        private static bool IsUnderAddedDirectory(string pathInPck, HashSet<string> addedDirectories)
        {
            if (string.IsNullOrWhiteSpace(pathInPck) || addedDirectories == null || addedDirectories.Count == 0)
            {
                return false;
            }

            string normalized = pathInPck.Replace(Path.DirectorySeparatorChar, '\\').Replace(Path.AltDirectorySeparatorChar, '\\').Trim('\\');
            foreach (string directory in addedDirectories)
            {
                string normalizedDirectory = (directory ?? string.Empty)
                    .Replace(Path.DirectorySeparatorChar, '\\')
                    .Replace(Path.AltDirectorySeparatorChar, '\\')
                    .Trim('\\');
                if (string.IsNullOrWhiteSpace(normalizedDirectory))
                {
                    continue;
                }

                if (string.Equals(normalized, normalizedDirectory, StringComparison.OrdinalIgnoreCase)
                    || normalized.StartsWith(normalizedDirectory + "\\", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool PackageEntryExists(string targetPck, string pathInPck)
        {
            if (string.IsNullOrWhiteSpace(targetPck) || string.IsNullOrWhiteSpace(pathInPck))
            {
                return false;
            }

            int openResult = pck_open(targetPck);
            if (openResult != WinPckOk)
            {
                return false;
            }

            try
            {
                return pck_getFileEntryByPath(pathInPck) != IntPtr.Zero;
            }
            finally
            {
                pck_close();
            }
        }

        private static int ParseCompressionLevel(string value)
        {
            if (!int.TryParse(value, out int parsed))
            {
                parsed = 9;
            }

            if (parsed < 0)
            {
                parsed = 0;
            }
            if (parsed > 12)
            {
                parsed = 12;
            }

            return parsed;
        }

        private static int ParsePckVersionId(string value)
        {
            if (!int.TryParse(value, out int parsed))
            {
                return DefaultPckVersionId;
            }

            return parsed < 0 ? DefaultPckVersionId : parsed;
        }

        private static int RebuildPackageFromRoots(string[] topLevelEntries, string targetPck, int compressionLevel, int pckVersionId)
        {
            if (topLevelEntries == null || topLevelEntries.Length == 0 || string.IsNullOrWhiteSpace(targetPck))
            {
                return 1;
            }

            int createResult = do_CreatePckFile(topLevelEntries[0], targetPck, pckVersionId, compressionLevel);
            if (createResult != WinPckOk)
            {
                return createResult;
            }

            if (topLevelEntries.Length == 1)
            {
                return WinPckOk;
            }

            int openResult = pck_open(targetPck);
            if (openResult != WinPckOk)
            {
                return openResult;
            }

            try
            {
                pck_setCompressLevel(compressionLevel);

                for (int i = 1; i < topLevelEntries.Length; i++)
                {
                    string entry = topLevelEntries[i];
                    string topLevelName = Path.GetFileName(entry.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    IntPtr anchorEntry = string.IsNullOrWhiteSpace(topLevelName)
                        ? IntPtr.Zero
                        : pck_getFileEntryByPath(topLevelName);

                    pck_StringArrayReset();

                    bool hasAnchor = anchorEntry != IntPtr.Zero;
                    bool isDirectory = Directory.Exists(entry);
                    if (hasAnchor && isDirectory)
                    {
                        string[] children = Directory.GetFileSystemEntries(entry, "*", SearchOption.TopDirectoryOnly);
                        if (children.Length == 0)
                        {
                            continue;
                        }

                        for (int childIndex = 0; childIndex < children.Length; childIndex++)
                        {
                            pck_StringArrayAppend(children[childIndex]);
                        }
                    }
                    else
                    {
                        pck_StringArrayAppend(entry);
                    }

                    int submitResult = pck_UpdatePckFileSubmit(targetPck, anchorEntry);
                    if (submitResult != WinPckOk)
                    {
                        return submitResult;
                    }
                }
            }
            finally
            {
                pck_close();
            }

            return WinPckOk;
        }

        private static int UpdateExistingPackageFile(string sourceFile, string pathInPck, string targetPck, int compressionLevel)
        {
            if (string.IsNullOrWhiteSpace(sourceFile) || string.IsNullOrWhiteSpace(pathInPck) || string.IsNullOrWhiteSpace(targetPck))
            {
                return 1;
            }

            int openResult = pck_open(targetPck);
            if (openResult != WinPckOk)
            {
                return openResult;
            }

            try
            {
                IntPtr existingEntry = pck_getFileEntryByPath(pathInPck);
                if (existingEntry == IntPtr.Zero)
                {
                    return 2;
                }

                int deleteResult = pck_DeleteEntry(existingEntry);
                if (deleteResult != WinPckOk)
                {
                    return deleteResult;
                }

                int submitDeleteResult = pck_DeleteEntrySubmit();
                if (submitDeleteResult != WinPckOk)
                {
                    return submitDeleteResult;
                }
            }
            finally
            {
                pck_close();
            }

            string directoryInPck = Path.GetDirectoryName(pathInPck) ?? string.Empty;
            directoryInPck = directoryInPck.Replace(Path.DirectorySeparatorChar, '\\').Replace(Path.AltDirectorySeparatorChar, '\\');
            return do_AddFileToPckFile(sourceFile, targetPck, directoryInPck, compressionLevel);
        }

        private static int UpdatePackageFromRoots(string[] topLevelEntries, string targetPck, int compressionLevel)
        {
            if (topLevelEntries == null || topLevelEntries.Length == 0 || string.IsNullOrWhiteSpace(targetPck))
            {
                return 1;
            }

            int openResult = pck_open(targetPck);
            if (openResult != WinPckOk)
            {
                return openResult;
            }

            try
            {
                pck_setCompressLevel(compressionLevel);

                for (int i = 0; i < topLevelEntries.Length; i++)
                {
                    string entry = topLevelEntries[i];
                    string topLevelName = Path.GetFileName(entry.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    IntPtr anchorEntry = string.IsNullOrWhiteSpace(topLevelName)
                        ? IntPtr.Zero
                        : pck_getFileEntryByPath(topLevelName);

                    pck_StringArrayReset();

                    bool hasAnchor = anchorEntry != IntPtr.Zero;
                    bool isDirectory = Directory.Exists(entry);
                    if (hasAnchor && isDirectory)
                    {
                        string[] children = Directory.GetFileSystemEntries(entry, "*", SearchOption.TopDirectoryOnly);
                        if (children.Length == 0)
                        {
                            continue;
                        }

                        for (int childIndex = 0; childIndex < children.Length; childIndex++)
                        {
                            pck_StringArrayAppend(children[childIndex]);
                        }
                    }
                    else
                    {
                        pck_StringArrayAppend(entry);
                    }

                    int submitResult = pck_UpdatePckFileSubmit(targetPck, anchorEntry);
                    if (submitResult != WinPckOk)
                    {
                        return submitResult;
                    }
                }
            }
            finally
            {
                pck_close();
            }

            return WinPckOk;
        }

        private static string BuildLastErrorSuffix()
        {
            try
            {
                IntPtr errorPtr = pck_getLastErrorMsg();
                string errorText = errorPtr == IntPtr.Zero
                    ? string.Empty
                    : Marshal.PtrToStringAnsi(errorPtr);
                return string.IsNullOrWhiteSpace(errorText) ? string.Empty : " " + errorText.Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

    }
}
