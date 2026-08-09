using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using FWEledit;

namespace FWPreviewProfileTool
{
    internal static class Program
    {
        private static readonly List<StepResult> Steps = new List<StepResult>();
        private static readonly List<ProfileEventResult> ProfileEvents = new List<ProfileEventResult>();

        [STAThread]
        private static int Main(string[] args)
        {
            Options options = Options.Parse(args);
            if (options.ShowHelp)
            {
                PrintHelp();
                return options.HasError ? 1 : 0;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                PckEntryReaderService.ProfileEvent = delegate(string operation, string target, TimeSpan elapsed, int count)
                {
                    ProfileEvents.Add(new ProfileEventResult(operation, target, elapsed, count));
                    if (elapsed.TotalMilliseconds >= 25)
                    {
                        Console.WriteLine("  " + operation + ": " + elapsed.TotalMilliseconds.ToString("0.0") + " ms | " + count.ToString() + " | " + target);
                    }
                };

                AssetManager.GameRootPath = options.GameRoot;
                AssetManager.WorkspaceRootPath = string.Empty;

                CacheSave database = null;
                AssetManager assetManager = null;
                ModelPickerService modelPickerService = null;
                ModelPreviewService previewService = null;

                Measure("create-services", delegate
                {
                    assetManager = new AssetManager();
                    modelPickerService = new ModelPickerService(
                        new ModelPickerCacheService(),
                        assetManager,
                        new IconResolutionService(),
                        new IdGenerationService(),
                        new ItemFieldClassifierService());
                    previewService = new ModelPreviewService();
                });

                Measure("load-path.data", delegate
                {
                    database = new CacheSave();
                    database.pathById = LoadPathById(options.GameRoot);
                });

                Console.WriteLine("Game root: " + options.GameRoot);
                Console.WriteLine("path.data entries: " + (database.pathById == null ? 0 : database.pathById.Count).ToString());

                string mappedPath = options.MappedPath;
                if (options.PathId > 0)
                {
                    Measure("resolve-path-id", delegate
                    {
                        int resolvedPathId;
                        if (!modelPickerService.TryResolveModelPathById(
                            database,
                            options.PathId,
                            options.FieldName,
                            options.ListName,
                            out resolvedPathId,
                            out mappedPath,
                            true))
                        {
                            throw new InvalidOperationException("PathID was not resolved: " + options.PathId.ToString());
                        }

                        Console.WriteLine("Resolved PathID: " + options.PathId.ToString() + " -> " + resolvedPathId.ToString());
                        Console.WriteLine("Mapped path: " + mappedPath);
                    });
                }

                if (string.IsNullOrWhiteSpace(mappedPath))
                {
                    throw new InvalidOperationException("Use --path-id or --mapped-path.");
                }

                if (options.Prewarm)
                {
                    Measure("prewarm-pck-indexes", delegate
                    {
                        previewService.PrewarmCommonPckIndexes();
                    });
                }

                ModelPreviewMeshData firstMesh = null;
                string firstError = string.Empty;
                Measure("build-preview-cold", delegate
                {
                    if (!previewService.TryBuildPreviewMeshDataFromMappedPath(assetManager, mappedPath, out firstMesh, out firstError))
                    {
                        throw new InvalidOperationException(firstError);
                    }
                });

                ModelPreviewMeshData secondMesh = null;
                string secondError = string.Empty;
                Measure("build-preview-warm-cache", delegate
                {
                    if (!previewService.TryBuildPreviewMeshDataFromMappedPath(assetManager, mappedPath, out secondMesh, out secondError))
                    {
                        throw new InvalidOperationException(secondError);
                    }
                });

                if (options.ShowWindow)
                {
                    Measure("show-preview-window", delegate
                    {
                        previewService.ShowPreviewWindow(secondMesh ?? firstMesh, true);
                        Application.DoEvents();
                        Thread.Sleep(options.WindowHoldMs);
                        Application.DoEvents();
                        previewService.ClosePreviewWindow();
                    });
                }

                PrintSummary(firstMesh ?? secondMesh);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.Message);
                PrintSummary(null);
                return 2;
            }
        }

        private static void Measure(string name, Action action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                action();
                stopwatch.Stop();
                Steps.Add(new StepResult(name, stopwatch.Elapsed, true));
                Console.WriteLine(name + ": " + stopwatch.ElapsedMilliseconds.ToString() + " ms");
            }
            catch
            {
                stopwatch.Stop();
                Steps.Add(new StepResult(name, stopwatch.Elapsed, false));
                Console.WriteLine(name + ": FAILED after " + stopwatch.ElapsedMilliseconds.ToString() + " ms");
                throw;
            }
        }

        private static SortedList<int, string> LoadPathById(string gameRoot)
        {
            SortedList<int, string> result = new SortedList<int, string>();
            string pathData = ResolvePathDataFile(gameRoot);
            if (string.IsNullOrWhiteSpace(pathData) || !File.Exists(pathData))
            {
                return result;
            }

            Encoding encoding = Encoding.GetEncoding("GBK");
            using (FileStream fs = File.OpenRead(pathData))
            using (BinaryReader br = new BinaryReader(fs, encoding))
            {
                if (br.BaseStream.Length < 8)
                {
                    return result;
                }

                string magic = Encoding.ASCII.GetString(br.ReadBytes(4));
                if (!string.Equals(magic, "DIMP", StringComparison.Ordinal))
                {
                    return result;
                }

                int count = br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    if (br.BaseStream.Position + 8 > br.BaseStream.Length)
                    {
                        break;
                    }

                    int id = br.ReadInt32();
                    int len = br.ReadInt32();
                    if (id < 0 || len < 0 || len > 8192 || br.BaseStream.Position + len > br.BaseStream.Length)
                    {
                        break;
                    }

                    string mappedPath = encoding.GetString(br.ReadBytes(len)).Replace('/', '\\');
                    if (!result.ContainsKey(id))
                    {
                        result.Add(id, mappedPath);
                    }
                }
            }

            return result;
        }

        private static string ResolvePathDataFile(string gameRoot)
        {
            if (string.IsNullOrWhiteSpace(gameRoot))
            {
                return string.Empty;
            }

            string direct = Path.Combine(gameRoot, "data", "path.data");
            if (File.Exists(direct))
            {
                return direct;
            }

            string legacy = Path.Combine(gameRoot, "fELedit", "resources", "data", "path.data");
            return File.Exists(legacy) ? legacy : string.Empty;
        }

        private static void PrintSummary(ModelPreviewMeshData mesh)
        {
            Console.WriteLine();
            Console.WriteLine("Summary");
            Console.WriteLine("-------");
            for (int i = 0; i < Steps.Count; i++)
            {
                StepResult step = Steps[i];
                Console.WriteLine((step.Success ? "OK   " : "FAIL ") + step.Name.PadRight(24) + step.Elapsed.TotalMilliseconds.ToString("0.0") + " ms");
            }

            if (mesh != null)
            {
                Console.WriteLine();
                Console.WriteLine("Mesh");
                Console.WriteLine("----");
                Console.WriteLine("Source: " + (mesh.SourceMappedPath ?? string.Empty));
                Console.WriteLine("Vertices: " + mesh.VertexCount.ToString());
                Console.WriteLine("Triangles: " + mesh.TriangleCount.ToString());
                Console.WriteLine("Textures: " + (mesh.Textures == null ? 0 : mesh.Textures.Length).ToString());
            }

            if (ProfileEvents.Count > 0)
            {
                ProfileEvents.Sort(delegate(ProfileEventResult left, ProfileEventResult right)
                {
                    return right.Elapsed.CompareTo(left.Elapsed);
                });

                Console.WriteLine();
                Console.WriteLine("Slowest PCK Events");
                Console.WriteLine("------------------");
                int limit = Math.Min(12, ProfileEvents.Count);
                for (int i = 0; i < limit; i++)
                {
                    ProfileEventResult item = ProfileEvents[i];
                    Console.WriteLine(item.Elapsed.TotalMilliseconds.ToString("0.0").PadLeft(8)
                        + " ms  "
                        + item.Operation.PadRight(18)
                        + item.Count.ToString().PadLeft(7)
                        + "  "
                        + item.Target);
                }
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("FWPreviewProfileTool");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  FWPreviewProfileTool --game-root <client> --path-id <id> [--field <field>] [--list <list>] [--show-window]");
            Console.WriteLine("  FWPreviewProfileTool --game-root <client> --mapped-path <models\\...> [--show-window] [--prewarm]");
        }

        private sealed class StepResult
        {
            public StepResult(string name, TimeSpan elapsed, bool success)
            {
                Name = name;
                Elapsed = elapsed;
                Success = success;
            }

            public string Name { get; private set; }
            public TimeSpan Elapsed { get; private set; }
            public bool Success { get; private set; }
        }

        private sealed class ProfileEventResult
        {
            public ProfileEventResult(string operation, string target, TimeSpan elapsed, int count)
            {
                Operation = operation ?? string.Empty;
                Target = target ?? string.Empty;
                Elapsed = elapsed;
                Count = count;
            }

            public string Operation { get; private set; }
            public string Target { get; private set; }
            public TimeSpan Elapsed { get; private set; }
            public int Count { get; private set; }
        }

        private sealed class Options
        {
            public string GameRoot { get; private set; }
            public int PathId { get; private set; }
            public string MappedPath { get; private set; }
            public string FieldName { get; private set; }
            public string ListName { get; private set; }
            public bool ShowWindow { get; private set; }
            public bool Prewarm { get; private set; }
            public int WindowHoldMs { get; private set; }
            public bool ShowHelp { get; private set; }
            public bool HasError { get; private set; }

            public static Options Parse(string[] args)
            {
                Options options = new Options
                {
                    FieldName = "file_model",
                    ListName = "EQUIPMENT_ESSENCE",
                    WindowHoldMs = 450
                };

                for (int i = 0; args != null && i < args.Length; i++)
                {
                    string arg = args[i] ?? string.Empty;
                    if (arg == "--help" || arg == "-h" || arg == "/?")
                    {
                        options.ShowHelp = true;
                        return options;
                    }

                    if (arg == "--show-window")
                    {
                        options.ShowWindow = true;
                        continue;
                    }

                    if (arg == "--prewarm")
                    {
                        options.Prewarm = true;
                        continue;
                    }

                    string value = i + 1 < args.Length ? args[++i] : string.Empty;
                    if (arg == "--game-root")
                    {
                        options.GameRoot = value;
                    }
                    else if (arg == "--path-id")
                    {
                        int parsed;
                        int.TryParse(value, out parsed);
                        options.PathId = parsed;
                    }
                    else if (arg == "--mapped-path")
                    {
                        options.MappedPath = value;
                    }
                    else if (arg == "--field")
                    {
                        options.FieldName = value;
                    }
                    else if (arg == "--list")
                    {
                        options.ListName = value;
                    }
                    else if (arg == "--hold-ms")
                    {
                        int parsed;
                        if (int.TryParse(value, out parsed) && parsed >= 0)
                        {
                            options.WindowHoldMs = parsed;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(options.GameRoot) || !Directory.Exists(options.GameRoot))
                {
                    Console.Error.WriteLine("Missing or invalid --game-root.");
                    options.ShowHelp = true;
                    options.HasError = true;
                }

                return options;
            }
        }
    }
}
