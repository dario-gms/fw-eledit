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
                    Console.Error.WriteLine("Usage: FWPckUpdater.exe <update|rebuild> <staging-folder> <target-pck> [compression-level]");
                    return 2;
                }

                string mode = Normalize(args[0]);
                string stagingFolder = Normalize(args[1]);
                string targetPck = Normalize(args[2]);
                int compressionLevel = ParseCompressionLevel(args.Length >= 4 ? args[3] : null);

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
                    int rebuildResult = RebuildPackageFromRoots(topLevelEntries, tempRebuildPck, compressionLevel);
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

                int startIndex = 0;
                if (!File.Exists(targetPck))
                {
                    Console.WriteLine("Creating new package from " + Path.GetFileName(topLevelEntries[0]) + "...");
                    int createResult = do_CreatePckFile(topLevelEntries[0], targetPck, 0, compressionLevel);
                    if (createResult != WinPckOk)
                    {
                        Console.Error.WriteLine("WinPCK create failed with code " + createResult.ToString() + ".");
                        return 10;
                    }

                    startIndex = 1;
                }

                if (startIndex >= topLevelEntries.Length)
                {
                    Console.WriteLine("Package update completed.");
                    return 0;
                }

                int openResult = pck_open(targetPck);
                if (openResult != WinPckOk)
                {
                    Console.Error.WriteLine("WinPCK open failed with code " + openResult.ToString() + "." + BuildLastErrorSuffix());
                    return 15;
                }

                try
                {
                    pck_setCompressLevel(compressionLevel);

                    int remainingCount = topLevelEntries.Length - startIndex;
                    for (int i = startIndex; i < topLevelEntries.Length; i++)
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
                                Console.WriteLine("Skipping empty root: " + topLevelName);
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

                        Console.WriteLine("Adding root " + ((i - startIndex) + 1).ToString() + "/" + remainingCount.ToString() + ": " + topLevelName);
                        int submitResult = pck_UpdatePckFileSubmit(targetPck, anchorEntry);
                        if (submitResult != WinPckOk)
                        {
                            Console.Error.WriteLine("WinPCK update failed with code " + submitResult.ToString() + " for root " + topLevelName + "." + BuildLastErrorSuffix());
                            return 11;
                        }
                    }
                }
                finally
                {
                    pck_close();
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

        private static int RebuildPackageFromRoots(string[] topLevelEntries, string targetPck, int compressionLevel)
        {
            if (topLevelEntries == null || topLevelEntries.Length == 0 || string.IsNullOrWhiteSpace(targetPck))
            {
                return 1;
            }

            int createResult = do_CreatePckFile(topLevelEntries[0], targetPck, 0, compressionLevel);
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
