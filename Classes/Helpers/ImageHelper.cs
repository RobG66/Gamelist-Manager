using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace Gamelist_Manager.Classes.Helpers
{
    public enum BackgroundRemovalMode
    {
        Automatic,
        Circle,
        Freeform,
        ConvexHull
    }

    public static class ImageHelper
    {
        // ─── Constants ────────────────────────────────────────────────────────────
        private const int MinImageWidth = 8;
        private const int MinImageHeight = 8;
        private const int ColumnSampleInterval = 3;
        private const int RowSampleInterval = 2;
        private const int ColorTolerance = 20;

        // ─── Public API ───────────────────────────────────────────────────────────

        public static void ConvertToPng(string inputFilePath, string outputFilePath)
        {
            using var input = SKBitmap.Decode(inputFilePath);
            if (input == null)
                throw new InvalidOperationException("Failed to decode image");

            using var image = SKImage.FromBitmap(input);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(outputFilePath);
            data.SaveTo(stream);
        }

        public static string CheckImage(string imagePath)
        {
            if (!File.Exists(imagePath))
                return "Missing";

            SKBitmap? bitmap = null;
            try
            {
                bitmap = SKBitmap.Decode(imagePath);
                if (bitmap == null) return "Invalid Format";
                if (bitmap.Width < MinImageWidth || bitmap.Height < MinImageHeight) return "Too Small";

                var firstPixel = bitmap.GetPixel(0, 0);
                for (var y = 0; y < bitmap.Height; y += RowSampleInterval)
                    for (var x = 0; x < bitmap.Width; x += ColumnSampleInterval)
                    {
                        var pixel = bitmap.GetPixel(x, y);
                        if (Math.Abs(pixel.Red - firstPixel.Red) > ColorTolerance ||
                            Math.Abs(pixel.Green - firstPixel.Green) > ColorTolerance ||
                            Math.Abs(pixel.Blue - firstPixel.Blue) > ColorTolerance ||
                            Math.Abs(pixel.Alpha - firstPixel.Alpha) > ColorTolerance)
                            return "OK";
                    }
                return "Single Color";
            }
            catch (OutOfMemoryException) { return "Too Large"; }
            catch (ArgumentException) { return "Invalid Format"; }
            catch { return "Corrupt"; }
            finally { bitmap?.Dispose(); }
        }

        public static Bitmap? LoadImageWithoutLock(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;
            try
            {
                var imageData = File.ReadAllBytes(filePath);
                using var ms = new MemoryStream(imageData);
                return new Bitmap(ms);
            }
            catch { return null; }
        }

        // Inverts the RGB values of every non-transparent pixel, leaving alpha unchanged.
        public static SKBitmap InvertColors(SKBitmap source)
        {
            var info = new SKImageInfo(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            var result = new SKBitmap(info);
            var src = source.Pixels;
            var dst = new SKColor[src.Length];

            for (int i = 0; i < src.Length; i++)
            {
                var px = src[i];
                dst[i] = px.Alpha > 0
                    ? new SKColor((byte)(255 - px.Red), (byte)(255 - px.Green), (byte)(255 - px.Blue), px.Alpha)
                    : px;
            }

            result.Pixels = dst;
            return result;
        }

        // Samples every pixel along the image border and picks the colour that
        // appears most often, using a clustering approach for JPEG-compressed images.
        public static SKColor DetectBackgroundColor(SKBitmap bitmap)
        {
            int w = bitmap.Width, h = bitmap.Height;
            var edgePixels = new List<SKColor>((w + h) * 2);

            for (int x = 0; x < w; x++)
            {
                edgePixels.Add(bitmap.GetPixel(x, 0));
                edgePixels.Add(bitmap.GetPixel(x, h - 1));
            }
            for (int y = 1; y < h - 1; y++)
            {
                edgePixels.Add(bitmap.GetPixel(0, y));
                edgePixels.Add(bitmap.GetPixel(w - 1, y));
            }

            int step = Math.Max(1, edgePixels.Count / 200);
            int bestCount = 0;
            long bestR = 0, bestG = 0, bestB = 0;

            for (int i = 0; i < edgePixels.Count; i += step)
            {
                var candidate = edgePixels[i];
                int count = 0;
                long sumR = 0, sumG = 0, sumB = 0;

                foreach (var px in edgePixels)
                {
                    if (ColorDistance(px, candidate) <= ColorTolerance * ColorTolerance)
                    {
                        count++;
                        sumR += px.Red;
                        sumG += px.Green;
                        sumB += px.Blue;
                    }
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    bestR = sumR;
                    bestG = sumG;
                    bestB = sumB;
                }
            }

            return bestCount > 0
                ? new SKColor((byte)(bestR / bestCount), (byte)(bestG / bestCount), (byte)(bestB / bestCount))
                : bitmap.GetPixel(0, 0);
        }

        // Main entry point — in Automatic mode, runs all strategies and picks the best result.
        public static (SKBitmap result, string strategy) RemoveBackground(SKBitmap source, SKColor backgroundColor, int tolerance,
            BackgroundRemovalMode mode = BackgroundRemovalMode.Automatic, bool removeEnclosed = false)
        {
            System.Diagnostics.Debug.WriteLine($"[BgRemove] Image: {source.Width}x{source.Height}, BgColor: #{backgroundColor.Red:X2}{backgroundColor.Green:X2}{backgroundColor.Blue:X2}, Tolerance: {tolerance}, Mode: {mode}");

            switch (mode)
            {
                case BackgroundRemovalMode.Circle:
                    return (ApplyCircleMask(source, backgroundColor, tolerance), "Circle");

                case BackgroundRemovalMode.Freeform:
                    System.Diagnostics.Debug.WriteLine("[BgRemove] Strategy: FLOOD FILL (forced)");
                    return (RemoveBackgroundFloodFill(source, backgroundColor, tolerance, removeEnclosed), "Flood Fill");

                case BackgroundRemovalMode.ConvexHull:
                {
                    var bmp = TryRemoveByConvexHull(source, backgroundColor, tolerance, out _);
                    if (bmp != null)
                    {
                        System.Diagnostics.Debug.WriteLine("[BgRemove] Strategy: CONVEX HULL");
                        return (bmp, "Outline");
                    }
                    System.Diagnostics.Debug.WriteLine("[BgRemove] Strategy: FLOOD FILL (convex hull fallback)");
                    return (RemoveBackgroundFloodFill(source, backgroundColor, tolerance, removeEnclosed), "Flood Fill");
                }

                default:
                    return AutoSelectBestStrategy(source, backgroundColor, tolerance, removeEnclosed);
            }
        }

        // Automatic mode:
        // 1. Flood fill to get the subject mask
        // 2. Measure circularity — if high enough, apply a fitted circle mask
        // 3. Otherwise compute the convex hull — if it has few vertices (simple shape), apply hull mask
        // 4. If the hull is complex (many vertices, irregular outline), keep the flood fill result
        private static (SKBitmap result, string strategy) AutoSelectBestStrategy(
            SKBitmap source, SKColor backgroundColor, int tolerance, bool removeEnclosed)
        {
            var floodResult = RemoveBackgroundFloodFill(source, backgroundColor, tolerance, removeEnclosed);
            var floodPixels = floodResult.Pixels;
            int w = source.Width, h = source.Height;

            // circularity = 4π × area / perimeter² — perfect circle = 1.0
            // Image-edge pixels are excluded from perimeter as they are canvas
            // boundaries, not part of the actual subject outline.
            int area = 0, perimeter = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (floodPixels[y * w + x].Alpha < 128) continue;
                    area++;
                    if (x == 0 || x == w - 1 || y == 0 || y == h - 1) continue;
                    bool edge = floodPixels[y * w + x - 1].Alpha < 128 ||
                                floodPixels[y * w + x + 1].Alpha < 128 ||
                                floodPixels[(y - 1) * w + x].Alpha < 128 ||
                                floodPixels[(y + 1) * w + x].Alpha < 128;
                    if (edge) perimeter++;
                }

            double circularity = perimeter > 0
                ? (4.0 * Math.PI * area) / ((double)perimeter * perimeter)
                : 0;

            System.Diagnostics.Debug.WriteLine($"[Auto] area={area}, perimeter={perimeter}, circularity={circularity:F4}");

            // Real discs score 0.90+; logos and rectangular art score well below 0.80.
            const double circularityThreshold = 0.85;

            if (circularity >= circularityThreshold)
            {
                System.Diagnostics.Debug.WriteLine($"[Auto] Circular (score={circularity:F4}) — applying circle mask");
                floodResult.Dispose();
                return (ApplyCircleMask(source, backgroundColor, tolerance), "Circle");
            }

            // Not circular — try convex hull. A simple shape (box, cartridge, logo on plain bg)
            // produces a hull with few vertices. A complex/irregular outline produces many.
            var hullResult = TryRemoveByConvexHull(source, backgroundColor, tolerance, out int hullVertices);
            System.Diagnostics.Debug.WriteLine($"[Auto] Not circular (score={circularity:F4}), hull vertices={hullVertices}");

            const int maxHullVertices = 25;

            if (hullResult != null && hullVertices <= maxHullVertices)
            {
                System.Diagnostics.Debug.WriteLine($"[Auto] Simple shape — using Outline (hull)");
                floodResult.Dispose();
                return (hullResult, "Outline");
            }

            hullResult?.Dispose();
            System.Diagnostics.Debug.WriteLine($"[Auto] Complex outline — using Flood Fill");
            return (floodResult, "Flood Fill");
        }

        // Fits a circle mask by scanning the source pixels directly to find the
        // subject bounding box, then derives centre and radius from that.
        // Scanning inward from each edge avoids reliance on flood fill accuracy.
        private static SKBitmap ApplyCircleMask(SKBitmap source, SKColor backgroundColor, int tolerance)
        {
            int w = source.Width, h = source.Height;
            int toleranceSq = tolerance * tolerance;
            var sourcePixels = source.Pixels;

            // Scan inward from each edge to find the outermost non-background pixel.
            int minX = w, maxX = 0, minY = h, maxY = 0;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var px = sourcePixels[y * w + x];
                    if (px.Alpha < 128 || IsBackgroundPixel(px, backgroundColor, toleranceSq)) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }

            if (maxX <= minX || maxY <= minY) return CreateTransparentCopy(source);

            double cx = (minX + maxX) / 2.0;
            double cy = (minY + maxY) / 2.0;

            // Use the smaller span so the circle fits within both dimensions.
            // Then clamp so the feathered outer edge never exceeds the image boundary.
            int feather = Math.Max(2, (int)Math.Round(Math.Min(maxX - minX, maxY - minY) / 2.0) / 60);
            double radius = Math.Min(maxX - minX, maxY - minY) / 2.0;
            double maxAllowedRadius = Math.Min(
                Math.Min(cx, w - 1 - cx),
                Math.Min(cy, h - 1 - cy)) - feather;
            radius = Math.Min(radius, maxAllowedRadius);

            int icx = (int)Math.Round(cx), icy = (int)Math.Round(cy), ir = (int)Math.Round(radius);

            System.Diagnostics.Debug.WriteLine($"[Circle] bbox=({minX},{minY})-({maxX},{maxY}), cx={icx}, cy={icy}, r={ir}");

            var result = CreateTransparentCopy(source);
            var pixels = result.Pixels;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double dist = Math.Sqrt((x - icx) * (x - icx) + (y - icy) * (y - icy));
                    double inner = ir - feather, outer = ir + feather;
                    byte alpha;
                    if (dist <= inner)
                        alpha = pixels[y * w + x].Alpha;
                    else if (dist >= outer)
                        alpha = 0;
                    else
                        alpha = (byte)(pixels[y * w + x].Alpha * (1.0 - (dist - inner) / (feather * 2.0)));
                    var px = pixels[y * w + x];
                    pixels[y * w + x] = new SKColor(px.Red, px.Green, px.Blue, alpha);
                }

            result.Pixels = pixels;
            return result;
        }

        // ─── Geometric Masking ────────────────────────────────────────────────────

        // No shape is assumed — the hull is derived entirely from the image content.
        private static SKBitmap? TryRemoveByConvexHull(SKBitmap source, SKColor backgroundColor, int tolerance, out int hullVertices)
        {
            hullVertices = 0;
            int w = source.Width, h = source.Height;
            int toleranceSq = tolerance * tolerance;
            var sourcePixels = source.Pixels;

            // Scan each row and column to collect the outermost non-background pixels.
            var points = new List<SKPointI>();
            int step = Math.Max(1, Math.Min(w, h) / 300);

            for (int y = 0; y < h; y += step)
            {
                for (int x = 0; x < w; x++)
                {
                    var px = sourcePixels[y * w + x];
                    if (px.Alpha > 0 && !IsBackgroundPixel(px, backgroundColor, toleranceSq))
                    { points.Add(new SKPointI(x, y)); break; }
                }
                for (int x = w - 1; x >= 0; x--)
                {
                    var px = sourcePixels[y * w + x];
                    if (px.Alpha > 0 && !IsBackgroundPixel(px, backgroundColor, toleranceSq))
                    { points.Add(new SKPointI(x, y)); break; }
                }
            }

            for (int x = 0; x < w; x += step)
            {
                for (int y = 0; y < h; y++)
                {
                    var px = sourcePixels[y * w + x];
                    if (px.Alpha > 0 && !IsBackgroundPixel(px, backgroundColor, toleranceSq))
                    { points.Add(new SKPointI(x, y)); break; }
                }
                for (int y = h - 1; y >= 0; y--)
                {
                    var px = sourcePixels[y * w + x];
                    if (px.Alpha > 0 && !IsBackgroundPixel(px, backgroundColor, toleranceSq))
                    { points.Add(new SKPointI(x, y)); break; }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[ConvexHull] Boundary scan: {points.Count} candidate points");

            if (points.Count < 3)
            {
                System.Diagnostics.Debug.WriteLine("[ConvexHull] Not enough points — returning null");
                return null;
            }

            var hull = ComputeConvexHull(points);
            hullVertices = hull.Count;
            System.Diagnostics.Debug.WriteLine($"[ConvexHull] Hull has {hull.Count} vertices");

            if (hull.Count < 3) return null;

            var result = CreateTransparentCopy(source);
            var pixels = result.Pixels;
            int feather = Math.Max(2, Math.Min(w, h) / 80);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    double d = MinSignedDistanceToHull(x, y, hull);

                    byte alpha;
                    if (d >= feather)
                        alpha = pixels[y * w + x].Alpha;
                    else if (d <= 0)
                        alpha = 0;
                    else
                        alpha = (byte)(pixels[y * w + x].Alpha * (d / feather));

                    var px = pixels[y * w + x];
                    pixels[y * w + x] = new SKColor(px.Red, px.Green, px.Blue, alpha);
                }
            }

            result.Pixels = pixels;
            return result;
        }

        // ─── Flood Fill (fallback) ────────────────────────────────────────────────

        private static SKBitmap RemoveBackgroundFloodFill(SKBitmap source, SKColor backgroundColor, int tolerance, bool removeEnclosed = false)
        {
            System.Diagnostics.Debug.WriteLine($"[Fill] Starting flood fill: bg=#{backgroundColor.Red:X2}{backgroundColor.Green:X2}{backgroundColor.Blue:X2}, tolerance={tolerance} (toleranceSq={tolerance * tolerance})");

            // Pad the image so edge-flush subjects aren't clipped by the flood fill seeder
            const int pad = 8;
            int paddedW = source.Width + pad * 2;
            int paddedH = source.Height + pad * 2;

            var paddedInfo = new SKImageInfo(paddedW, paddedH, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            var padded = new SKBitmap(paddedInfo);
            using (var canvas = new SKCanvas(padded))
            {
                canvas.Clear(new SKColor(backgroundColor.Red, backgroundColor.Green, backgroundColor.Blue, 255));
                canvas.DrawBitmap(source, pad, pad);
            }

            int width = paddedW;
            int height = paddedH;
            int toleranceSq = tolerance * tolerance;

            var pixels = padded.Pixels;
            var visited = new bool[pixels.Length];
            var removed = new bool[pixels.Length];
            var queue = new Queue<int>();

            for (int x = 0; x < width; x++)
            {
                SeedEdge(x, 0, width, pixels, visited, removed, backgroundColor, toleranceSq, queue);
                SeedEdge(x, height - 1, width, pixels, visited, removed, backgroundColor, toleranceSq, queue);
            }
            for (int y = 1; y < height - 1; y++)
            {
                SeedEdge(0, y, width, pixels, visited, removed, backgroundColor, toleranceSq, queue);
                SeedEdge(width - 1, y, width, pixels, visited, removed, backgroundColor, toleranceSq, queue);
            }

            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                int x = idx % width;
                int y = idx / width;

                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if ((uint)nx >= (uint)width || (uint)ny >= (uint)height) continue;
                        int ni = ny * width + nx;
                        if (!visited[ni])
                        {
                            visited[ni] = true;
                            if (pixels[ni].Alpha > 0 && IsBackgroundPixel(pixels[ni], backgroundColor, toleranceSq))
                            {
                                removed[ni] = true;
                                pixels[ni] = SKColors.Transparent;
                                queue.Enqueue(ni);
                            }
                        }
                    }
            }

            int removedCount = removed.Count(r => r);
            System.Diagnostics.Debug.WriteLine($"[Fill] Pixels removed: {removedCount} / {pixels.Length} ({(double)removedCount / pixels.Length * 100:F1}%)");

            // Second pass: remove background pixels enclosed inside content (e.g. hollow letters).
            // These were never reached by the edge-seeded fill because the content strokes blocked access.
            if (removeEnclosed)
            {
                int enclosedCount = 0;
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (!visited[i] && pixels[i].Alpha > 0 && IsBackgroundPixel(pixels[i], backgroundColor, toleranceSq))
                    {
                        pixels[i] = SKColors.Transparent;
                        removed[i] = true;
                        enclosedCount++;
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[Fill] Enclosed background pixels removed: {enclosedCount}");
            }

            visited = null;
            FeatherEdges(pixels, removed, width, height);
            padded.Pixels = pixels;

            // Crop padding back off
            var finalInfo = new SKImageInfo(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            var result = new SKBitmap(finalInfo);
            using (var canvas = new SKCanvas(result))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.DrawBitmap(padded,
                    new SKRect(pad, pad, pad + source.Width, pad + source.Height),
                    new SKRect(0, 0, source.Width, source.Height));
            }

            padded.Dispose();
            return result;
        }

        // ─── Shared Helpers ───────────────────────────────────────────────────────

        private static SKBitmap CreateTransparentCopy(SKBitmap source)
        {
            var info = new SKImageInfo(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            var result = new SKBitmap(info);
            using var canvas = new SKCanvas(result);
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(source, 0, 0);
            return result;
        }

        private static void SeedEdge(int x, int y, int width, SKColor[] pixels,
            bool[] visited, bool[] removed, SKColor bg, int toleranceSq, Queue<int> queue)
        {
            int idx = y * width + x;
            if (visited[idx]) return;
            visited[idx] = true;
            if (pixels[idx].Alpha > 0 && IsBackgroundPixel(pixels[idx], bg, toleranceSq))
            {
                removed[idx] = true;
                pixels[idx] = SKColors.Transparent;
                queue.Enqueue(idx);
            }
        }

        private static bool IsBackgroundPixel(SKColor pixel, SKColor bg, int toleranceSq)
        {
            int dr = pixel.Red - bg.Red;
            int dg = pixel.Green - bg.Green;
            int db = pixel.Blue - bg.Blue;
            return dr * dr + dg * dg + db * db <= toleranceSq;
        }

        private static int ColorDistance(SKColor a, SKColor b)
        {
            int dr = a.Red - b.Red;
            int dg = a.Green - b.Green;
            int db = a.Blue - b.Blue;
            return dr * dr + dg * dg + db * db;
        }

        // Andrew's monotone chain — returns hull vertices in winding order consistent
        // with screen coordinates (Y-down), which the signed distance formula expects.
        private static List<SKPointI> ComputeConvexHull(List<SKPointI> points)
        {
            int n = points.Count;
            if (n < 3) return new List<SKPointI>(points);

            points.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));

            var hull = new List<SKPointI>(2 * n);

            foreach (var p in points)
            {
                while (hull.Count >= 2 && HullCross(hull[^2], hull[^1], p) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }

            int lower = hull.Count + 1;
            for (int i = n - 2; i >= 0; i--)
            {
                while (hull.Count >= lower && HullCross(hull[^2], hull[^1], points[i]) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(points[i]);
            }

            hull.RemoveAt(hull.Count - 1);
            return hull;
        }

        private static long HullCross(SKPointI o, SKPointI a, SKPointI b)
            => (long)(a.X - o.X) * (b.Y - o.Y) - (long)(a.Y - o.Y) * (b.X - o.X);

        // Returns the minimum signed distance from (px, py) to the hull boundary.
        // Positive values mean inside; negative mean outside.
        // For inside points the magnitude equals the distance to the nearest edge.
        private static double MinSignedDistanceToHull(int px, int py, List<SKPointI> hull)
        {
            double minDist = double.MaxValue;
            int n = hull.Count;

            for (int i = 0; i < n; i++)
            {
                var a = hull[i];
                var b = hull[(i + 1) % n];
                double len = Math.Sqrt((double)(b.X - a.X) * (b.X - a.X) + (double)(b.Y - a.Y) * (b.Y - a.Y));
                if (len == 0) continue;
                double d = ((double)(b.X - a.X) * (py - a.Y) - (double)(b.Y - a.Y) * (px - a.X)) / len;
                if (d < minDist) minDist = d;
            }

            return minDist;
        }

        // Applies a smooth alpha ramp
        // of the removed/kept boundary. Interior pixels are never touched, so thin
        // strokes and fine detail keep full opacity.
        private static void FeatherEdges(SKColor[] pixels, bool[] removed, int width, int height)
        {
            const int featherWidth = 2;
            const float gamma = 1.5f;

            int length = pixels.Length;

            // BFS outward from every removed pixel to measure each kept pixel's
            // distance to the nearest removed neighbour (4-connectivity is enough).
            var dist = new int[length];
            Array.Fill(dist, int.MaxValue);
            var queue = new Queue<int>();

            for (int i = 0; i < length; i++)
            {
                if (removed[i])
                {
                    dist[i] = 0;
                    queue.Enqueue(i);
                }
            }

            int[] dx4 = [-1, 1, 0, 0];
            int[] dy4 = [0, 0, -1, 1];

            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                int d = dist[idx];
                if (d >= featherWidth) continue;

                int x = idx % width;
                int y = idx / width;

                for (int dir = 0; dir < 4; dir++)
                {
                    int nx = x + dx4[dir];
                    int ny = y + dy4[dir];
                    if ((uint)nx >= (uint)width || (uint)ny >= (uint)height) continue;
                    int ni = ny * width + nx;
                    if (dist[ni] == int.MaxValue)
                    {
                        dist[ni] = d + 1;
                        queue.Enqueue(ni);
                    }
                }
            }

            // Apply the alpha ramp only to boundary-adjacent kept pixels.
            for (int i = 0; i < length; i++)
            {
                if (removed[i]) continue;
                int d = dist[i];
                if (d == int.MaxValue || d == 0 || d > featherWidth) continue;

                float t = (float)d / featherWidth;               // 0 at boundary, 1 at featherWidth
                float alpha = MathF.Pow(t, gamma);               // nonlinear ramp, biased toward opacity
                byte newAlpha = (byte)(alpha * pixels[i].Alpha);
                pixels[i] = new SKColor(pixels[i].Red, pixels[i].Green, pixels[i].Blue, newAlpha);
            }
        }

        // Finds the tightest bounding box that contains all non-transparent pixels,
        // optionally adds padding, then returns a cropped copy.
        public static SKBitmap? AutoCrop(SKBitmap source, int paddingPercent = 0, bool cropAllSides = false)
        {
            int width = source.Width;
            int height = source.Height;
            var pixels = source.Pixels;

            int top = 0, bottom = height - 1, left = 0, right = width - 1;
            bool found = false;

            for (int y = 0; y < height && !found; y++)
                for (int x = 0; x < width && !found; x++)
                    if (pixels[y * width + x].Alpha > 0) { top = y; found = true; }

            if (!found) return null;

            found = false;
            for (int y = height - 1; y >= top && !found; y--)
                for (int x = 0; x < width && !found; x++)
                    if (pixels[y * width + x].Alpha > 0) { bottom = y; found = true; }

            found = false;
            for (int x = 0; x < width && !found; x++)
                for (int y = top; y <= bottom && !found; y++)
                    if (pixels[y * width + x].Alpha > 0) { left = x; found = true; }

            found = false;
            for (int x = width - 1; x >= left && !found; x--)
                for (int y = top; y <= bottom && !found; y++)
                    if (pixels[y * width + x].Alpha > 0) { right = x; found = true; }

            bool cropHorizontal = true;
            bool cropVertical = true;

            if (!cropAllSides)
            {
                int totalVertical = top + (height - 1 - bottom);
                int totalHorizontal = left + (width - 1 - right);

                if (totalHorizontal <= totalVertical)
                    cropVertical = false;
                else
                    cropHorizontal = false;
            }

            if (paddingPercent > 0)
            {
                int contentW = right - left + 1;
                int contentH = bottom - top + 1;
                int padX = contentW * paddingPercent / 100;
                int padY = contentH * paddingPercent / 100;

                if (cropVertical) { top = Math.Max(0, top - padY); bottom = Math.Min(height - 1, bottom + padY); }
                if (cropHorizontal) { left = Math.Max(0, left - padX); right = Math.Min(width - 1, right + padX); }
            }

            if (!cropVertical) { top = 0; bottom = height - 1; }
            if (!cropHorizontal) { left = 0; right = width - 1; }

            if (top == 0 && left == 0 && bottom == height - 1 && right == width - 1)
            {
                var copy = new SKBitmap(new SKImageInfo(width, height, source.ColorType, source.AlphaType));
                using var copyCanvas = new SKCanvas(copy);
                copyCanvas.DrawBitmap(source, 0, 0);
                return copy;
            }

            int cropWidth = right - left + 1;
            int cropHeight = bottom - top + 1;

            var info = new SKImageInfo(cropWidth, cropHeight, source.ColorType, source.AlphaType);
            var result = new SKBitmap(info);
            using var canvas = new SKCanvas(result);
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(source, new SKRect(left, top, right + 1, bottom + 1),
                new SKRect(0, 0, cropWidth, cropHeight));

            return result;
        }
    }
}