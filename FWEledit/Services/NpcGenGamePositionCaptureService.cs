using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Globalization;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace FWEledit
{
    public sealed class NpcGenGamePositionCaptureResult
    {
        public bool Success { get; set; }
        public PointF3 Position { get; set; }
        public string Error { get; set; }
        public string RecognizedText { get; set; }
    }

    public sealed class NpcGenGamePositionCaptureService
    {
        private static readonly Regex CoordinatePattern = new Regex(
            @"(?<![\d])(-?\d{1,6}(?:[\.,]\d{1,6})?)\s*[,;]\s*(-?\d{1,6}(?:[\.,]\d{1,6})?)\s*[,;]\s*(-?\d{1,6}(?:[\.,]\d{1,6})?)(?![\d])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex PartialCoordinatePattern = new Regex(
            @"(?<![\d])(-?\d{1,6}(?:[\.,]\d{1,6})?)\s*[,;].{1,24}?[,;]\s*(-?\d{1,6}(?:[\.,]\d{1,6})?)(?![\d])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex CoordinateNumberPattern = new Regex(
            @"(?<![A-Za-z\d])[-+]?\d{1,6}(?:\.\d{1,6})?(?![A-Za-z\d\.])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private const int SwRestore = 9;
        private const uint ProcessVmRead = 0x0010;
        private const uint ProcessQueryInformation = 0x0400;
        private const uint MemCommit = 0x1000;
        private const uint PageNoAccess = 0x01;
        private const uint PageGuard = 0x100;
        private readonly object cacheSync = new object();
        private int cachedProcessId;
        private List<long> cachedVectorAddresses = new List<long>();
        private RectangleF? captureRegionRelative;

        public bool ConfigureCaptureRegion(IntPtr editorWindow, out string error)
        {
            error = string.Empty;
            IntPtr gameWindow = FindGameWindow();
            if (gameWindow == IntPtr.Zero)
            {
                error = "Forsaken World is not open. Start the game and try Set again.";
                return false;
            }

            Rectangle clientBounds;
            if (!TryGetClientScreenBounds(gameWindow, out clientBounds) || clientBounds.Width < 320 || clientBounds.Height < 240)
            {
                error = "The Forsaken World window is minimized or its client area could not be read.";
                return false;
            }

            try
            {
                ShowWindow(gameWindow, SwRestore);
                SetForegroundWindow(gameWindow);
                using (GameCaptureRegionOverlay overlay = new GameCaptureRegionOverlay(clientBounds))
                {
                    if (overlay.ShowDialog() != System.Windows.Forms.DialogResult.OK || overlay.SelectedRegion.IsEmpty)
                    {
                        return false;
                    }

                    Rectangle selected = overlay.SelectedRegion;
                    captureRegionRelative = new RectangleF(
                        selected.X / (float)clientBounds.Width,
                        selected.Y / (float)clientBounds.Height,
                        selected.Width / (float)clientBounds.Width,
                        selected.Height / (float)clientBounds.Height);
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = "The capture area could not be set: " + ex.Message;
                return false;
            }
            finally
            {
                if (editorWindow != IntPtr.Zero)
                {
                    SetForegroundWindow(editorWindow);
                }
            }
        }

        public async Task<NpcGenGamePositionCaptureResult> CaptureAsync(IntPtr editorWindow)
        {
            IntPtr gameWindow = FindGameWindow();
            if (gameWindow == IntPtr.Zero)
            {
                return Failure("Forsaken World is not open. Start the game and try Capture again.");
            }

            if (!captureRegionRelative.HasValue)
            {
                return Failure("Click Set and draw a rectangle around the character coordinates before using Capture.");
            }

            Rectangle clientBounds;
            if (!TryGetClientScreenBounds(gameWindow, out clientBounds) || clientBounds.Width < 320 || clientBounds.Height < 240)
            {
                return Failure("The Forsaken World window is minimized or its client area could not be read.");
            }

            Bitmap screenshot = null;
            try
            {
                ShowWindow(gameWindow, SwRestore);
                SetForegroundWindow(gameWindow);
                await Task.Delay(350);

                if (!TryGetClientScreenBounds(gameWindow, out clientBounds))
                {
                    return Failure("The Forsaken World client area changed while it was being captured.");
                }

                screenshot = new Bitmap(clientBounds.Width, clientBounds.Height, PixelFormat.Format24bppRgb);
                using (Graphics graphics = Graphics.FromImage(screenshot))
                {
                    graphics.CopyFromScreen(clientBounds.Location, Point.Empty, clientBounds.Size, CopyPixelOperation.SourceCopy);
                }
            }
            catch (Exception ex)
            {
                return Failure("The game window could not be captured: " + ex.Message);
            }
            finally
            {
                if (editorWindow != IntPtr.Zero)
                {
                    SetForegroundWindow(editorWindow);
                }
            }

            using (screenshot)
            {
                try
                {
                    Rectangle scanBounds = GetConfiguredScanBounds(screenshot.Size);
                    uint gameProcessId;
                    GetWindowThreadProcessId(gameWindow, out gameProcessId);
                    return await ReadPositionAsync(screenshot, scanBounds, unchecked((int)gameProcessId));
                }
                catch (Exception ex)
                {
                    return Failure("Windows OCR could not read the game position: " + ex.Message);
                }
            }
        }

        internal async Task<NpcGenGamePositionCaptureResult> ReadPositionAsync(Bitmap screenshot, int processId = 0)
        {
            if (screenshot == null)
            {
                return Failure("No game image was available for position recognition.");
            }

            return await ReadPositionAsync(screenshot, BuildScanBounds(screenshot.Size), processId);
        }

        private async Task<NpcGenGamePositionCaptureResult> ReadPositionAsync(Bitmap screenshot, Rectangle scanBounds, int processId)
        {
            if (screenshot == null)
            {
                return Failure("No game image was available for position recognition.");
            }

            PointF3 cachedPosition;
            if (processId > 0 && TryReadCachedPosition(processId, out cachedPosition))
            {
                return new NpcGenGamePositionCaptureResult
                {
                    Success = true,
                    Position = cachedPosition,
                    RecognizedText = "Position read from the cached game coordinate vector."
                };
            }

            List<PositionCandidate> candidates = new List<PositionCandidate>();
            StringBuilder recognizedText = new StringBuilder();

            using (Bitmap original = BuildOcrBitmap(screenshot, scanBounds, false))
            {
                SaveCaptureDiagnostic(original, "npcgen-position-original.png");
                await CollectCandidatesAsync(original, scanBounds, screenshot.Size, candidates, recognizedText);
            }

            if (!candidates.Any(candidate => candidate.HasCompletePosition))
            {
                using (Bitmap threshold = BuildOcrBitmap(screenshot, scanBounds, true))
                {
                    SaveCaptureDiagnostic(threshold, "npcgen-position-threshold.png");
                    await CollectCandidatesAsync(threshold, scanBounds, screenshot.Size, candidates, recognizedText);
                }
            }

            SaveCaptureDiagnosticText(recognizedText.ToString());

            PositionCandidate best = candidates
                .Where(candidate => candidate.HasCompletePosition)
                .OrderBy(candidate => candidate.Score)
                .FirstOrDefault();
            if (best == null && processId > 0)
            {
                // Animated UI can cover the middle value. X/Z still identify the player's
                // repeated position vector, so resolve Y from read-only process memory.
                foreach (PositionCandidate partial in candidates
                    .Where(candidate => !candidate.HasCompletePosition)
                    .OrderBy(candidate => candidate.Score))
                {
                    float? resolvedY = await Task.Run(() =>
                    {
                        float value;
                        return TryResolveYFromProcess(processId, partial.X, partial.Z, out value)
                            ? (float?)value
                            : null;
                    });
                    if (resolvedY.HasValue)
                    {
                        partial.Y = resolvedY.Value;
                        partial.HasCompletePosition = true;
                        best = partial;
                        break;
                    }
                }
            }
            if (best == null)
            {
                return new NpcGenGamePositionCaptureResult
                {
                    Success = false,
                    Error = "The character position was not found. Check the selected rectangle and keep the coordinate line visible. The latest capture was saved in the CaptureDiagnostics folder.",
                    RecognizedText = recognizedText.ToString()
                };
            }

            return new NpcGenGamePositionCaptureResult
            {
                Success = true,
                Position = new PointF3(best.X, best.Y, best.Z),
                RecognizedText = recognizedText.ToString()
            };
        }

        private Rectangle GetConfiguredScanBounds(Size screenshotSize)
        {
            RectangleF relative = captureRegionRelative ?? new RectangleF(0F, 0F, 1F, 1F);
            Rectangle bounds = new Rectangle(
                (int)Math.Round(relative.X * screenshotSize.Width),
                (int)Math.Round(relative.Y * screenshotSize.Height),
                Math.Max(1, (int)Math.Round(relative.Width * screenshotSize.Width)),
                Math.Max(1, (int)Math.Round(relative.Height * screenshotSize.Height)));
            return Rectangle.Intersect(new Rectangle(Point.Empty, screenshotSize), bounds);
        }

        private static async Task CollectCandidatesAsync(
            Bitmap image,
            Rectangle scanBounds,
            Size sourceSize,
            List<PositionCandidate> candidates,
            StringBuilder recognizedText)
        {
            int initialCandidateCount = candidates.Count;
            StringBuilder passText = new StringBuilder();
            OcrEngine engine = OcrEngine.TryCreateFromLanguage(new Language("en-US"))
                ?? OcrEngine.TryCreateFromUserProfileLanguages();
            if (engine == null)
            {
                throw new InvalidOperationException("No Windows OCR language is installed.");
            }

            OcrResult result = await RecognizeAsync(engine, image);
            foreach (OcrLine line in result.Lines)
            {
                string text = NormalizeOcrText(line.Text);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                recognizedText.AppendLine(text);
                passText.AppendLine(text);
                float x = 0F;
                float y = 0F;
                float z = 0F;
                bool complete = false;
                Match match = CoordinatePattern.Match(text);
                if (match.Success)
                {
                    complete = TryParseCoordinate(match.Groups[1].Value, out x)
                        && TryParseCoordinate(match.Groups[2].Value, out y)
                        && TryParseCoordinate(match.Groups[3].Value, out z);
                }
                if (!complete)
                {
                    List<float> values = ExtractCoordinateNumbers(text);
                    if (values.Count >= 3)
                    {
                        x = values[values.Count - 3];
                        y = values[values.Count - 2];
                        z = values[values.Count - 1];
                        complete = true;
                    }
                }
                if (!complete)
                {
                    Match partialMatch = PartialCoordinatePattern.Match(text);
                    if (!partialMatch.Success
                        || !TryParseCoordinate(partialMatch.Groups[1].Value, out x)
                        || !TryParseCoordinate(partialMatch.Groups[2].Value, out z))
                    {
                        continue;
                    }
                }

                RectangleF lineBounds = GetLineBounds(line);
                float scaleX = scanBounds.Width / (float)image.Width;
                float scaleY = scanBounds.Height / (float)image.Height;
                float centerX = scanBounds.X + ((lineBounds.Left + (lineBounds.Width / 2F)) * scaleX);
                float centerY = scanBounds.Y + ((lineBounds.Top + (lineBounds.Height / 2F)) * scaleY);
                float normalizedX = centerX / Math.Max(1F, sourceSize.Width);
                float normalizedY = centerY / Math.Max(1F, sourceSize.Height);

                bool hasLetters = Regex.IsMatch(text, "[A-Za-z]");
                double score = (Math.Abs(normalizedX - 0.5F) * 0.8D)
                    + (Math.Abs(normalizedY - 0.48F) * 1.2D)
                    + (hasLetters ? 0.35D : 0D);

                candidates.Add(new PositionCandidate
                {
                    X = x,
                    Y = y,
                    Z = z,
                    HasCompletePosition = complete,
                    Score = score
                });
            }

            if (!candidates.Skip(initialCandidateCount).Any(candidate => candidate.HasCompletePosition))
            {
                List<float> passValues = ExtractCoordinateNumbers(passText.ToString());
                if (passValues.Count == 3 && passValues.All(IsPlausibleCoordinate))
                {
                    candidates.Add(new PositionCandidate
                    {
                        X = passValues[0],
                        Y = passValues[1],
                        Z = passValues[2],
                        HasCompletePosition = true,
                        Score = 0D
                    });
                }
            }

        }

        private static List<float> ExtractCoordinateNumbers(string text)
        {
            List<float> values = new List<float>();
            foreach (Match number in CoordinateNumberPattern.Matches(text ?? string.Empty))
            {
                float value;
                if (TryParseCoordinate(number.Value, out value))
                {
                    values.Add(value);
                }
            }
            return values;
        }

        private bool TryResolveYFromProcess(int processId, float targetX, float targetZ, out float resolvedY)
        {
            resolvedY = 0F;
            IntPtr process = OpenProcess(ProcessQueryInformation | ProcessVmRead, false, processId);
            if (process == IntPtr.Zero)
            {
                return false;
            }

            Dictionary<int, int> yCounts = new Dictionary<int, int>();
            Dictionary<int, List<long>> addressesByY = new Dictionary<int, List<long>>();
            byte[] buffer = new byte[(1024 * 1024) + 8];
            ulong address = 0;
            int informationSize = Marshal.SizeOf(typeof(MemoryBasicInformation));
            try
            {
                while (address < 0x00007FFFFFFF0000UL)
                {
                    MemoryBasicInformation information;
                    UIntPtr queryResult = VirtualQueryEx(
                        process,
                        new IntPtr((long)address),
                        out information,
                        new UIntPtr((uint)informationSize));
                    if (queryResult == UIntPtr.Zero)
                    {
                        break;
                    }

                    ulong baseAddress = (ulong)information.BaseAddress.ToInt64();
                    ulong regionSize = information.RegionSize.ToUInt64();
                    if (regionSize == 0)
                    {
                        break;
                    }

                    if (information.State == MemCommit
                        && (information.Protect & (PageGuard | PageNoAccess)) == 0)
                    {
                        ScanMemoryRegion(process, baseAddress, regionSize, buffer, targetX, targetZ, yCounts, addressesByY);
                    }

                    ulong nextAddress = baseAddress + regionSize;
                    if (nextAddress <= address)
                    {
                        break;
                    }
                    address = nextAddress;
                }
            }
            finally
            {
                CloseHandle(process);
            }

            if (yCounts.Count == 0)
            {
                return false;
            }

            KeyValuePair<int, int>[] ranked = yCounts
                .OrderByDescending(pair => pair.Value)
                .ToArray();
            int bestCount = ranked[0].Value;
            int secondCount = ranked.Length > 1 ? ranked[1].Value : 0;
            if (bestCount < 2 || (secondCount > 0 && bestCount < secondCount * 2))
            {
                return false;
            }

            int selectedY = ranked[0].Key;
            resolvedY = selectedY / 1000F;
            List<long> addresses;
            if (addressesByY.TryGetValue(selectedY, out addresses))
            {
                lock (cacheSync)
                {
                    cachedProcessId = processId;
                    cachedVectorAddresses = addresses.Distinct().ToList();
                }
            }
            return true;
        }

        private bool TryReadCachedPosition(int processId, out PointF3 position)
        {
            position = new PointF3();
            List<long> addresses;
            lock (cacheSync)
            {
                if (cachedProcessId != processId || cachedVectorAddresses.Count == 0)
                {
                    return false;
                }
                addresses = new List<long>(cachedVectorAddresses);
            }

            IntPtr process = OpenProcess(ProcessQueryInformation | ProcessVmRead, false, processId);
            if (process == IntPtr.Zero)
            {
                return false;
            }

            Dictionary<string, CachedPositionSample> samples = new Dictionary<string, CachedPositionSample>(StringComparer.Ordinal);
            byte[] bytes = new byte[12];
            try
            {
                foreach (long address in addresses)
                {
                    UIntPtr bytesRead;
                    if (!ReadProcessMemory(process, new IntPtr(address), bytes, new UIntPtr(12), out bytesRead)
                        || bytesRead.ToUInt64() != 12)
                    {
                        continue;
                    }

                    float x = BitConverter.ToSingle(bytes, 0);
                    float y = BitConverter.ToSingle(bytes, 4);
                    float z = BitConverter.ToSingle(bytes, 8);
                    if (!IsPlausibleCoordinate(x) || !IsPlausibleCoordinate(y) || !IsPlausibleCoordinate(z))
                    {
                        continue;
                    }

                    float roundedX = (float)Math.Round(x, 3);
                    float roundedY = (float)Math.Round(y, 3);
                    float roundedZ = (float)Math.Round(z, 3);
                    string key = roundedX.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
                        + "|" + roundedY.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
                        + "|" + roundedZ.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                    CachedPositionSample sample;
                    if (!samples.TryGetValue(key, out sample))
                    {
                        sample = new CachedPositionSample
                        {
                            Position = new PointF3(roundedX, roundedY, roundedZ)
                        };
                        samples[key] = sample;
                    }
                    sample.Count++;
                }
            }
            finally
            {
                CloseHandle(process);
            }

            CachedPositionSample[] ranked = samples.Values
                .OrderByDescending(sample => sample.Count)
                .ToArray();
            if (ranked.Length == 0
                || ranked[0].Count < 2
                || (ranked.Length > 1 && ranked[0].Count < ranked[1].Count * 2))
            {
                return false;
            }

            position = ranked[0].Position;
            return true;
        }

        private static bool IsPlausibleCoordinate(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) <= 100000F;
        }

        private static void ScanMemoryRegion(
            IntPtr process,
            ulong baseAddress,
            ulong regionSize,
            byte[] buffer,
            float targetX,
            float targetZ,
            Dictionary<int, int> yCounts,
            Dictionary<int, List<long>> addressesByY)
        {
            const int chunkSize = 1024 * 1024;
            for (ulong regionOffset = 0; regionOffset < regionSize; regionOffset += chunkSize)
            {
                int requested = (int)Math.Min((ulong)(buffer.Length - 8), regionSize - regionOffset);
                UIntPtr bytesRead;
                if (!ReadProcessMemory(
                    process,
                    new IntPtr((long)(baseAddress + regionOffset)),
                    buffer,
                    new UIntPtr((uint)requested),
                    out bytesRead))
                {
                    continue;
                }

                int length = (int)bytesRead.ToUInt64();
                int alignmentOffset = (int)((4UL - ((baseAddress + regionOffset) & 3UL)) & 3UL);
                for (int offset = alignmentOffset; offset + 12 <= length; offset += 4)
                {
                    float x = BitConverter.ToSingle(buffer, offset);
                    float z = BitConverter.ToSingle(buffer, offset + 8);
                    if (float.IsNaN(x)
                        || float.IsInfinity(x)
                        || float.IsNaN(z)
                        || float.IsInfinity(z)
                        || Math.Abs(x - targetX) >= 0.003F
                        || Math.Abs(z - targetZ) >= 0.003F)
                    {
                        continue;
                    }

                    float y = BitConverter.ToSingle(buffer, offset + 4);
                    if (float.IsNaN(y) || float.IsInfinity(y) || Math.Abs(y) > 100000F)
                    {
                        continue;
                    }

                    int key = (int)Math.Round(y * 1000F);
                    int count;
                    yCounts.TryGetValue(key, out count);
                    yCounts[key] = count + 1;
                    List<long> addresses;
                    if (!addressesByY.TryGetValue(key, out addresses))
                    {
                        addresses = new List<long>();
                        addressesByY[key] = addresses;
                    }
                    addresses.Add((long)(baseAddress + regionOffset + (ulong)offset));
                }
            }
        }

        private static async Task<OcrResult> RecognizeAsync(OcrEngine engine, Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                byte[] bytes = memory.ToArray();
                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                {
                    await stream.WriteAsync(bytes.AsBuffer()).AsTask();
                    stream.Seek(0);
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream).AsTask();
                    using (SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied).AsTask())
                    {
                        return await engine.RecognizeAsync(softwareBitmap).AsTask();
                    }
                }
            }
        }

        private static Bitmap BuildOcrBitmap(Bitmap source, Rectangle bounds, bool threshold)
        {
            int width = Math.Max(1, bounds.Width * 8);
            int height = Math.Max(1, bounds.Height * 8);
            Bitmap result = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (Graphics graphics = Graphics.FromImage(result))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(source, new Rectangle(0, 0, width, height), bounds, GraphicsUnit.Pixel);
            }

            if (!threshold)
            {
                return result;
            }

            Bitmap filtered = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (Graphics graphics = Graphics.FromImage(filtered))
            using (ImageAttributes attributes = new ImageAttributes())
            {
                ColorMatrix grayscale = new ColorMatrix(new[]
                {
                    new[] { 0.299F, 0.299F, 0.299F, 0F, 0F },
                    new[] { 0.587F, 0.587F, 0.587F, 0F, 0F },
                    new[] { 0.114F, 0.114F, 0.114F, 0F, 0F },
                    new[] { 0F, 0F, 0F, 1F, 0F },
                    new[] { 0F, 0F, 0F, 0F, 1F }
                });
                attributes.SetColorMatrix(grayscale);
                attributes.SetThreshold(0.58F);
                graphics.DrawImage(result, new Rectangle(0, 0, width, height), 0, 0, width, height, GraphicsUnit.Pixel, attributes);
            }
            result.Dispose();
            return filtered;
        }

        private static Rectangle BuildScanBounds(Size size)
        {
            int left = (int)(size.Width * 0.30F);
            int top = (int)(size.Height * 0.28F);
            int width = (int)(size.Width * 0.40F);
            int height = (int)(size.Height * 0.16F);
            return Rectangle.Intersect(new Rectangle(left, top, width, height), new Rectangle(Point.Empty, size));
        }

        private static RectangleF GetLineBounds(OcrLine line)
        {
            if (line == null || line.Words == null || line.Words.Count == 0)
            {
                return RectangleF.Empty;
            }

            float left = (float)line.Words.Min(word => word.BoundingRect.Left);
            float top = (float)line.Words.Min(word => word.BoundingRect.Top);
            float right = (float)line.Words.Max(word => word.BoundingRect.Right);
            float bottom = (float)line.Words.Max(word => word.BoundingRect.Bottom);
            return RectangleF.FromLTRB(left, top, right, bottom);
        }

        private static void SaveCaptureDiagnostic(Bitmap bitmap, string fileName)
        {
            try
            {
                string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CaptureDiagnostics");
                Directory.CreateDirectory(directory);
                bitmap.Save(Path.Combine(directory, fileName), ImageFormat.Png);
            }
            catch
            {
                // Diagnostics must never prevent position capture.
            }
        }

        private static void SaveCaptureDiagnosticText(string text)
        {
            try
            {
                string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CaptureDiagnostics");
                Directory.CreateDirectory(directory);
                File.WriteAllText(
                    Path.Combine(directory, "npcgen-position-ocr.txt"),
                    text ?? string.Empty,
                    Encoding.UTF8);
            }
            catch
            {
                // Diagnostics must never prevent position capture.
            }
        }

        private static string NormalizeOcrText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string normalized = text
                .Replace('\u2212', '-')
                .Replace('\u2013', '-')
                .Replace('\u2014', '-')
                .Replace('|', '1')
                .Trim();
            normalized = Regex.Replace(normalized, @"(?<=\d)\s*\.\s*(?=\d)", ".");

            Match splitDecimal = Regex.Match(
                normalized,
                @"^\s*([-+]?\d{1,6})\s*,\s*(\d{1,6})(,?)\s*$",
                RegexOptions.CultureInvariant);
            if (splitDecimal.Success)
            {
                normalized = splitDecimal.Groups[1].Value
                    + "."
                    + splitDecimal.Groups[2].Value
                    + splitDecimal.Groups[3].Value;
            }
            return normalized;
        }

        private static bool TryParseCoordinate(string text, out float value)
        {
            return float.TryParse(
                (text ?? string.Empty).Replace(',', '.'),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out value);
        }

        private static IntPtr FindGameWindow()
        {
            IntPtr bestWindow = IntPtr.Zero;
            long bestArea = 0;
            EnumWindows(delegate(IntPtr window, IntPtr parameter)
            {
                if (!IsWindowVisible(window))
                {
                    return true;
                }

                StringBuilder title = new StringBuilder(512);
                GetWindowText(window, title, title.Capacity);
                if (!title.ToString().StartsWith("Forsaken World", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                uint processId;
                GetWindowThreadProcessId(window, out processId);
                try
                {
                    Process process = Process.GetProcessById((int)processId);
                    if (process.Id == Process.GetCurrentProcess().Id)
                    {
                        return true;
                    }
                }
                catch
                {
                    return true;
                }

                Rectangle bounds;
                if (TryGetClientScreenBounds(window, out bounds))
                {
                    long area = (long)bounds.Width * bounds.Height;
                    if (area > bestArea)
                    {
                        bestArea = area;
                        bestWindow = window;
                    }
                }
                return true;
            }, IntPtr.Zero);
            return bestWindow;
        }

        private static bool TryGetClientScreenBounds(IntPtr window, out Rectangle bounds)
        {
            bounds = Rectangle.Empty;
            NativeRect client;
            if (!GetClientRect(window, out client))
            {
                return false;
            }

            NativePoint topLeft = new NativePoint { X = client.Left, Y = client.Top };
            NativePoint bottomRight = new NativePoint { X = client.Right, Y = client.Bottom };
            if (!ClientToScreen(window, ref topLeft) || !ClientToScreen(window, ref bottomRight))
            {
                return false;
            }

            bounds = Rectangle.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
            return bounds.Width > 0 && bounds.Height > 0;
        }

        private static NpcGenGamePositionCaptureResult Failure(string message)
        {
            return new NpcGenGamePositionCaptureResult { Success = false, Error = message ?? string.Empty };
        }

        private sealed class PositionCandidate
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public bool HasCompletePosition { get; set; }
            public double Score { get; set; }
        }

        private sealed class CachedPositionSample
        {
            public PointF3 Position { get; set; }
            public int Count { get; set; }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryBasicInformation
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public UIntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr window);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr window, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr window, out NativeRect rectangle);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr window, ref NativePoint point);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr window);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr window, int command);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint access, bool inheritHandle, int processId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll")]
        private static extern UIntPtr VirtualQueryEx(
            IntPtr process,
            IntPtr address,
            out MemoryBasicInformation information,
            UIntPtr informationLength);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(
            IntPtr process,
            IntPtr address,
            byte[] buffer,
            UIntPtr size,
            out UIntPtr bytesRead);
    }
}
