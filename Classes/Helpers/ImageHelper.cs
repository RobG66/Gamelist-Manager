using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace Gamelist_Manager.Classes.Helpers
{
    public enum BackgroundRemovalMode
    {
        Circle,
        CircleEdge,
        Freeform,
        ConvexHull,
        CircleFit
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

            unsafe
            {
                byte* src = (byte*)source.GetPixels();
                byte* dst = (byte*)result.GetPixels();
                int w = source.Width;
                int h = source.Height;

                Parallel.For(0, h, y =>
                {
                    byte* sRow = src + y * source.RowBytes;
                    byte* dRow = dst + y * result.RowBytes;
                    for (int x = 0; x < w; x++)
                    {
                        int i = x * 4;
                        byte a = sRow[i + 3];
                        dRow[i + 0] = a > 0 ? (byte)(255 - sRow[i + 0]) : sRow[i + 0];
                        dRow[i + 1] = a > 0 ? (byte)(255 - sRow[i + 1]) : sRow[i + 1];
                        dRow[i + 2] = a > 0 ? (byte)(255 - sRow[i + 2]) : sRow[i + 2];
                        dRow[i + 3] = a;
                    }
                });
            }

            return result;
        }

        // Samples every pixel along the image border and picks the colour that
        // appears most often, using a clustering approach for JPEG-compressed images.
        public static SKColor DetectBackgroundColor(SKBitmap bitmap)
        {
            int w = bitmap.Width, h = bitmap.Height;
            var edgePixels = new List<SKColor>((w + h) * 2);

            unsafe
            {
                byte* p = (byte*)bitmap.GetPixels();
                int rb = bitmap.RowBytes;

                for (int x = 0; x < w; x++)
                {
                    edgePixels.Add(ReadPixel(p, rb, x, 0));
                    edgePixels.Add(ReadPixel(p, rb, x, h - 1));
                }
                for (int y = 1; y < h - 1; y++)
                {
                    edgePixels.Add(ReadPixel(p, rb, 0, y));
                    edgePixels.Add(ReadPixel(p, rb, w - 1, y));
                }
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
                : edgePixels[0];
        }

        // Main entry point — applies the user-selected removal strategy.
        public static (SKBitmap result, string strategy) RemoveBackground(SKBitmap source, SKColor backgroundColor, int tolerance,
            BackgroundRemovalMode mode, bool removeEnclosed = false, SKPointI? clickPoint = null, float edgeThreshold = 0.15f)
        {
            System.Diagnostics.Debug.WriteLine($"[BgRemove] Image: {source.Width}x{source.Height}, BgColor: #{backgroundColor.Red:X2}{backgroundColor.Green:X2}{backgroundColor.Blue:X2}, Tolerance: {tolerance}, Mode: {mode}");

            switch (mode)
            {
                // Circle — fully automatic Hough-based circle detection, no click needed.
                case BackgroundRemovalMode.Circle:
                    return (ApplyHoughCircleMask(source, null), "Circle - Automatic");

                // CircleEdge — same as Circle but ray threshold is user-controlled.
                case BackgroundRemovalMode.CircleEdge:
                    return (ApplyHoughCircleMask(source, null, edgeThreshold, useOutermostEdge: true), "Circle - Edge Detection");

                // CircleFit — Hough-based but uses click point to select the right circle
                // when multiple are detected. User can click anywhere on the disc.
                case BackgroundRemovalMode.CircleFit:
                    return (ApplyHoughCircleMask(source, clickPoint), "Circle 2");

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

                default: // Freeform
                    System.Diagnostics.Debug.WriteLine("[BgRemove] Strategy: FLOOD FILL");
                    return (RemoveBackgroundFloodFill(source, backgroundColor, tolerance, removeEnclosed), "Flood Fill");
            }
        }

        // ─── Hough Circle Detection ───────────────────────────────────────────────
        //
        // Shared by Circle (auto) and CircleFit (click hint).
        //
        // Pipeline:
        //   1. Downsample to a working resolution to keep the accumulator fast.
        //   2. Convert to grayscale float array.
        //   3. 5x5 Gaussian blur — kills JPEG compression noise.
        //   4. Sobel edge magnitude map.
        //   5. Collect strong edge pixels (top 15% by magnitude).
        //   6. Vote in a 2-D accumulator: for each edge pixel and each candidate
        //      radius, vote for the center that would put this pixel on that circle.
        //      Voting along the gradient direction keeps it fast — we only vote
        //      two points per edge pixel (inward and outward along the gradient).
        //   7. Find the accumulator peak closest to the click point (or image center
        //      if no click).  Apply a 5x5 box-blur to the accumulator first to merge
        //      nearby votes.
        //   8. Scale the winning center and radius back to full resolution.
        //   9. Apply feathered circle mask to the original source bitmap.
        //
        private static SKBitmap ApplyHoughCircleMask(SKBitmap source, SKPointI? clickPoint, float edgeThreshold = 0.15f, bool useOutermostEdge = false)
        {
            int srcW = source.Width, srcH = source.Height;

            // ── Step 1: downsample for speed ──────────────────────────────────────
            // Work at most 400px on the short side — plenty for circle detection.
            const int workSize = 400;
            float scale = Math.Min(1.0f, workSize / (float)Math.Min(srcW, srcH));
            int w = Math.Max(1, (int)(srcW * scale));
            int h = Math.Max(1, (int)(srcH * scale));

            float[] gray;
            if (scale < 0.99f)
            {
                using var smallBmp = new SKBitmap(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Unpremul));
                using (var canvas = new SKCanvas(smallBmp))
                {
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.Medium };
                    canvas.DrawBitmap(source, new SKRect(0, 0, w, h), paint);
                }
                gray = ToGrayscale(smallBmp, w, h);
            }
            else
            {
                gray = ToGrayscale(source, w, h);
            }

            // ── Step 2: blur + edges ───────────────────────────────────────────────
            float[] blurred = GaussianBlur5(gray, w, h);
            float[] edgeMag;
            float[] edgeGx, edgeGy;
            SobelEdges(blurred, w, h, out edgeMag, out edgeGx, out edgeGy);

            // ── Step 3: collect strong edge pixels ────────────────────────────────
            float maxMag = 0;
            for (int i = 0; i < edgeMag.Length; i++)
                if (edgeMag[i] > maxMag) maxMag = edgeMag[i];

            float thresh = maxMag * 0.15f;
            var edgePts = new List<(int x, int y, float gx, float gy)>();
            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++)
                {
                    int idx = y * w + x;
                    if (edgeMag[idx] >= thresh)
                        edgePts.Add((x, y, edgeGx[idx], edgeGy[idx]));
                }

            System.Diagnostics.Debug.WriteLine($"[Hough] work={w}x{h}, scale={scale:F2}, edgePts={edgePts.Count}, maxMag={maxMag:F1}");

            if (edgePts.Count < 20) return CreateTransparentCopy(source);

            // ── Step 4: radius range ───────────────────────────────────────────────
            // Disc should be between 20% and 49% of the shorter working dimension.
            int minR = (int)(Math.Min(w, h) * 0.20);
            int maxR = (int)(Math.Min(w, h) * 0.49);
            int rStep = Math.Max(1, (maxR - minR) / 40); // ~40 radius levels

            // ── Step 5: accumulator ────────────────────────────────────────────────
            // 2-D array [cy * w + cx] — we sum over all tested radii.
            // For each edge pixel, vote for center candidates along the gradient
            // direction at each tested radius.  Two votes per pixel per radius:
            // one inward (center is behind the gradient) and one outward.
            var acc = new float[w * h];

            foreach (var (ex, ey, gx, gy) in edgePts)
            {
                float gLen = MathF.Sqrt(gx * gx + gy * gy);
                if (gLen < 1e-4f) continue;
                float nx = gx / gLen; // unit gradient (points away from disc center)
                float ny = gy / gLen;

                for (int r = minR; r <= maxR; r += rStep)
                {
                    // Center candidate inward along gradient
                    int cx0 = (int)Math.Round(ex - nx * r);
                    int cy0 = (int)Math.Round(ey - ny * r);
                    if ((uint)cx0 < (uint)w && (uint)cy0 < (uint)h)
                        acc[cy0 * w + cx0] += 1.0f;

                    // Center candidate outward (handles inverted gradients)
                    int cx1 = (int)Math.Round(ex + nx * r);
                    int cy1 = (int)Math.Round(ey + ny * r);
                    if ((uint)cx1 < (uint)w && (uint)cy1 < (uint)h)
                        acc[cy1 * w + cx1] += 1.0f;
                }
            }

            // ── Step 6: smooth accumulator then find peak ─────────────────────────
            float[] accSmooth = BoxBlur5(acc, w, h);

            // Convert click point to working resolution for peak selection.
            double refCx = clickPoint.HasValue ? clickPoint.Value.X * scale : w / 2.0;
            double refCy = clickPoint.HasValue ? clickPoint.Value.Y * scale : h / 2.0;

            // Score = accSmooth value / (1 + distanceToRef * 0.01)
            // This biases toward the click point without ignoring strong distant peaks.
            float bestScore = -1;
            int bestIdx = 0;
            for (int i = 0; i < accSmooth.Length; i++)
            {
                if (accSmooth[i] < 1) continue;
                int ix = i % w, iy = i / w;
                double dist = Math.Sqrt((ix - refCx) * (ix - refCx) + (iy - refCy) * (iy - refCy));
                float score = accSmooth[i] / (float)(1.0 + dist * 0.01);
                if (score > bestScore) { bestScore = score; bestIdx = i; }
            }

            int peakCx = bestIdx % w;
            int peakCy = bestIdx / w;
            System.Diagnostics.Debug.WriteLine($"[Hough] peak at ({peakCx},{peakCy}) score={bestScore:F1}");

            // ── Step 7: find best radius for the winning center ───────────────────
            // Cast 360 rays from the peak center outward.
            // Automatic: strongest brightness edge wins (original behaviour).
            // Edge Detection: outermost qualifying edge wins, avoiding inner decorative rings.
            const int rayCount = 360;
            var radii = new List<double>(rayCount);
            float rayThresh = 8f * (edgeThreshold / 0.15f);

            for (int a = 0; a < rayCount; a++)
            {
                double angle = a * Math.PI / 180.0;
                double dx = Math.Cos(angle);
                double dy = Math.Sin(angle);

                int sampleCount = (int)(maxR * 1.1);
                float prev = gray[Math.Clamp(peakCy, 0, h - 1) * w + Math.Clamp(peakCx, 0, w - 1)];

                double bestEdge = 0;
                double bestR = -1;

                for (int r = 2; r < sampleCount; r++)
                {
                    int px = (int)Math.Round(peakCx + dx * r);
                    int py = (int)Math.Round(peakCy + dy * r);
                    if ((uint)px >= (uint)w || (uint)py >= (uint)h) break;

                    float cur = gray[py * w + px];
                    double edge = Math.Abs(cur - prev);

                    if (useOutermostEdge)
                    {
                        // Keep overwriting — last qualifying edge wins = outermost edge
                        if (edge > rayThresh && r > minR * 0.8)
                            bestR = r;
                    }
                    else
                    {
                        if (edge > bestEdge) { bestEdge = edge; bestR = r; }
                    }

                    prev = cur;
                }

                if (useOutermostEdge)
                {
                    if (bestR > 0)
                        radii.Add(bestR);
                }
                else
                {
                    if (bestR > minR * 0.8 && bestEdge > 8)
                        radii.Add(bestR);
                }
            }

            if (radii.Count < 10)
            {
                System.Diagnostics.Debug.WriteLine($"[Hough] too few radius hits ({radii.Count}), using bbox fallback");
                // Fallback: use bbox of strong edge pixels closest to peak
                radii.Clear();
                foreach (var (ex, ey, _, _) in edgePts)
                {
                    double d = Math.Sqrt((ex - peakCx) * (ex - peakCx) + (ey - peakCy) * (ey - peakCy));
                    if (d >= minR * 0.7 && d <= maxR * 1.1)
                        radii.Add(d);
                }
            }

            if (radii.Count < 5) return CreateTransparentCopy(source);

            radii.Sort();
            double workRadius = radii[(int)(radii.Count * 0.75)];

            // ── Step 8: scale back to full resolution ─────────────────────────────
            double fullCx = peakCx / scale;
            double fullCy = peakCy / scale;
            double fullRadius = workRadius / scale;

            System.Diagnostics.Debug.WriteLine($"[Hough] center=({fullCx:F1},{fullCy:F1}), r={fullRadius:F1}");

            // ── Step 9: apply feathered circle mask ───────────────────────────────
            int feather = Math.Max(2, (int)Math.Round(fullRadius) / 60);
            int icx = (int)Math.Round(fullCx);
            int icy = (int)Math.Round(fullCy);
            int ir = (int)Math.Round(fullRadius);

            var result = CreateTransparentCopy(source);

            unsafe
            {
                byte* dst = (byte*)result.GetPixels();
                int rb = result.RowBytes;
                double inner = ir - feather;
                double outer = ir + feather;
                double feather2 = feather * 2.0;

                Parallel.For(0, srcH, y =>
                {
                    byte* row = dst + y * rb;
                    double dy2 = y - icy;
                    for (int x = 0; x < srcW; x++)
                    {
                        int i = x * 4;
                        byte origAlpha = row[i + 3];
                        if (origAlpha == 0) continue;

                        double dist = Math.Sqrt((x - icx) * (x - icx) + dy2 * dy2);
                        byte alpha;
                        if (dist <= inner)
                            alpha = origAlpha;
                        else if (dist >= outer)
                            alpha = 0;
                        else
                            alpha = (byte)(origAlpha * (1.0 - (dist - inner) / feather2));

                        row[i + 3] = alpha;
                    }
                });
            }

            return result;
        }

        // ─── Image Processing Helpers ─────────────────────────────────────────────

        // Convert SKBitmap to grayscale float array (0-255).
        private static float[] ToGrayscale(SKBitmap bmp, int w, int h)
        {
            var gray = new float[w * h];
            unsafe
            {
                byte* p = (byte*)bmp.GetPixels();
                int rb = bmp.RowBytes;
                for (int y = 0; y < h; y++)
                {
                    byte* row = p + y * rb;
                    for (int x = 0; x < w; x++)
                    {
                        int off = x * 4;
                        gray[y * w + x] = row[off] * 0.299f + row[off + 1] * 0.587f + row[off + 2] * 0.114f;
                    }
                }
            }
            return gray;
        }

        // 5x5 Gaussian blur — kills JPEG compression ringing around disc edges.
        private static float[] GaussianBlur5(float[] src, int w, int h)
        {
            // Separable kernel: [1 4 6 4 1] / 16
            float[] tmp = new float[w * h];
            float[] dst = new float[w * h];

            // Horizontal pass
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float v = 0;
                    v += src[y * w + Math.Clamp(x - 2, 0, w - 1)] * 1;
                    v += src[y * w + Math.Clamp(x - 1, 0, w - 1)] * 4;
                    v += src[y * w + x] * 6;
                    v += src[y * w + Math.Clamp(x + 1, 0, w - 1)] * 4;
                    v += src[y * w + Math.Clamp(x + 2, 0, w - 1)] * 1;
                    tmp[y * w + x] = v / 16f;
                }

            // Vertical pass
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float v = 0;
                    v += tmp[Math.Clamp(y - 2, 0, h - 1) * w + x] * 1;
                    v += tmp[Math.Clamp(y - 1, 0, h - 1) * w + x] * 4;
                    v += tmp[y * w + x] * 6;
                    v += tmp[Math.Clamp(y + 1, 0, h - 1) * w + x] * 4;
                    v += tmp[Math.Clamp(y + 2, 0, h - 1) * w + x] * 1;
                    dst[y * w + x] = v / 16f;
                }

            return dst;
        }

        // Sobel edge detection — returns magnitude, Gx, and Gy arrays.
        private static void SobelEdges(float[] src, int w, int h,
            out float[] mag, out float[] gx, out float[] gy)
        {
            mag = new float[w * h];
            gx = new float[w * h];
            gy = new float[w * h];

            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++)
                {
                    float tl = src[(y - 1) * w + (x - 1)], tc = src[(y - 1) * w + x], tr = src[(y - 1) * w + (x + 1)];
                    float ml = src[y * w + (x - 1)], mr = src[y * w + (x + 1)];
                    float bl = src[(y + 1) * w + (x - 1)], bc = src[(y + 1) * w + x], br = src[(y + 1) * w + (x + 1)];

                    float gxv = -tl - 2 * ml - bl + tr + 2 * mr + br;
                    float gyv = -tl - 2 * tc - tr + bl + 2 * bc + br;

                    int idx = y * w + x;
                    gx[idx] = gxv;
                    gy[idx] = gyv;
                    mag[idx] = MathF.Sqrt(gxv * gxv + gyv * gyv);
                }
        }

        // 5x5 box blur for accumulator smoothing.
        private static float[] BoxBlur5(float[] src, int w, int h)
        {
            float[] tmp = new float[w * h];
            float[] dst = new float[w * h];

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float v = 0;
                    for (int k = -2; k <= 2; k++)
                        v += src[y * w + Math.Clamp(x + k, 0, w - 1)];
                    tmp[y * w + x] = v / 5f;
                }

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float v = 0;
                    for (int k = -2; k <= 2; k++)
                        v += tmp[Math.Clamp(y + k, 0, h - 1) * w + x];
                    dst[y * w + x] = v / 5f;
                }

            return dst;
        }

        // ─── Original Circle methods (preserved for reference) ────────────────────
        /*
        private static SKBitmap ApplyCircleMask(SKBitmap source, SKColor backgroundColor, int tolerance)
        {
            int w = source.Width, h = source.Height;
            int toleranceSq = tolerance * tolerance;

            int minX = w, maxX = 0, minY = h, maxY = 0;

            unsafe
            {
                byte* p = (byte*)source.GetPixels();
                int rb = source.RowBytes;

                for (int y = 0; y < h; y++)
                {
                    byte* row = p + y * rb;
                    for (int x = 0; x < w; x++)
                    {
                        int i = x * 4;
                        byte a = row[i + 3];
                        if (a < 128) continue;
                        var px = new SKColor(row[i], row[i + 1], row[i + 2], a);
                        if (IsBackgroundPixel(px, backgroundColor, toleranceSq)) continue;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (maxX <= minX || maxY <= minY) return CreateTransparentCopy(source);

            double cx = (minX + maxX) / 2.0;
            double cy = (minY + maxY) / 2.0;
            int feather = Math.Max(2, (int)Math.Round(Math.Min(maxX - minX, maxY - minY) / 2.0) / 60);
            double radius = Math.Min(maxX - minX, maxY - minY) / 2.0;
            double maxAllowedRadius = Math.Min(Math.Min(cx, w - 1 - cx), Math.Min(cy, h - 1 - cy)) - feather;
            radius = Math.Min(radius, maxAllowedRadius);
            int icx = (int)Math.Round(cx), icy = (int)Math.Round(cy), ir = (int)Math.Round(radius);

            var result = CreateTransparentCopy(source);

            unsafe
            {
                byte* dst = (byte*)result.GetPixels();
                int rb = result.RowBytes;
                double inner = ir - feather, outer = ir + feather;
                double feather2 = feather * 2.0;

                Parallel.For(0, h, y =>
                {
                    byte* row = dst + y * rb;
                    double dy = y - icy;
                    for (int x = 0; x < w; x++)
                    {
                        int i = x * 4;
                        byte origAlpha = row[i + 3];
                        if (origAlpha == 0) continue;

                        double dist = Math.Sqrt((x - icx) * (x - icx) + dy * dy);
                        byte alpha;
                        if (dist <= inner)
                            alpha = origAlpha;
                        else if (dist >= outer)
                            alpha = 0;
                        else
                            alpha = (byte)(origAlpha * (1.0 - (dist - inner) / feather2));

                        row[i + 3] = alpha;
                    }
                });
            }

            return result;
        }

        private static SKBitmap ApplyCircleFitMask(SKBitmap source, SKColor backgroundColor, int tolerance, SKPointI? clickPoint)
        {
            int w = source.Width, h = source.Height;

            using var filled = RemoveBackgroundFloodFill(source, backgroundColor, tolerance, removeEnclosed: false);

            int minX = w, maxX = 0, minY = h, maxY = 0;

            unsafe
            {
                byte* p = (byte*)filled.GetPixels();
                int rb = filled.RowBytes;
                for (int y = 0; y < h; y++)
                {
                    byte* row = p + y * rb;
                    for (int x = 0; x < w; x++)
                    {
                        if (row[x * 4 + 3] >= 128)
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
            }

            if (maxX <= minX || maxY <= minY) return CreateTransparentCopy(source);

            double cx = (minX + maxX) / 2.0;
            double cy = (minY + maxY) / 2.0;

            const int rayCount = 720;
            var radii = new List<double>(rayCount);

            unsafe
            {
                byte* p = (byte*)filled.GetPixels();
                int rb = filled.RowBytes;

                for (int a = 0; a < rayCount; a++)
                {
                    double angle = a * Math.PI / 360.0;
                    double dx = Math.Cos(angle);
                    double dy = Math.Sin(angle);

                    double maxDist = double.MaxValue;
                    if (Math.Abs(dx) > 1e-9)
                    {
                        double tRight = (w - 1 - cx) / dx;
                        double tLeft = -cx / dx;
                        double tPos = dx > 0 ? tRight : tLeft;
                        if (tPos > 0 && tPos < maxDist) maxDist = tPos;
                    }
                    if (Math.Abs(dy) > 1e-9)
                    {
                        double tBottom = (h - 1 - cy) / dy;
                        double tTop = -cy / dy;
                        double tPos = dy > 0 ? tBottom : tTop;
                        if (tPos > 0 && tPos < maxDist) maxDist = tPos;
                    }
                    if (maxDist <= 1 || maxDist == double.MaxValue) continue;

                    double edgeR = -1;
                    for (double r = Math.Floor(maxDist); r >= 1; r -= 1.0)
                    {
                        int px = (int)Math.Round(cx + dx * r);
                        int py = (int)Math.Round(cy + dy * r);
                        if ((uint)px >= (uint)w || (uint)py >= (uint)h) continue;

                        byte alpha = *((byte*)p + py * rb + px * 4 + 3);
                        if (alpha >= 128)
                        {
                            edgeR = r;
                            break;
                        }
                    }

                    if (edgeR > 0)
                        radii.Add(edgeR);
                }
            }

            if (radii.Count < 10) return CreateTransparentCopy(source);

            radii.Sort();
            int idx75 = (int)(radii.Count * 0.75);
            double radius = radii[Math.Min(idx75, radii.Count - 1)];

            int feather = Math.Max(2, (int)Math.Round(radius) / 60);
            int icx = (int)Math.Round(cx);
            int icy = (int)Math.Round(cy);
            int ir = (int)Math.Round(radius);

            var result = CreateTransparentCopy(source);

            unsafe
            {
                byte* dst = (byte*)result.GetPixels();
                int rb = result.RowBytes;
                double inner = ir - feather;
                double outer = ir + feather;
                double feather2 = feather * 2.0;

                Parallel.For(0, h, y =>
                {
                    byte* row = dst + y * rb;
                    double dy2 = y - icy;
                    for (int x = 0; x < w; x++)
                    {
                        int i = x * 4;
                        byte origAlpha = row[i + 3];
                        if (origAlpha == 0) continue;

                        double dist = Math.Sqrt((x - icx) * (x - icx) + dy2 * dy2);
                        byte alpha;
                        if (dist <= inner)
                            alpha = origAlpha;
                        else if (dist >= outer)
                            alpha = 0;
                        else
                            alpha = (byte)(origAlpha * (1.0 - (dist - inner) / feather2));

                        row[i + 3] = alpha;
                    }
                });
            }

            return result;
        }
        */

        // ─── Geometric Masking ────────────────────────────────────────────────────

        private static SKBitmap? TryRemoveByConvexHull(SKBitmap source, SKColor backgroundColor, int tolerance, out int hullVertices)
        {
            hullVertices = 0;
            int w = source.Width, h = source.Height;
            int toleranceSq = tolerance * tolerance;

            var points = new List<SKPointI>();
            int step = Math.Max(1, Math.Min(w, h) / 300);

            unsafe
            {
                byte* p = (byte*)source.GetPixels();
                int rb = source.RowBytes;

                for (int y = 0; y < h; y += step)
                {
                    byte* row = p + y * rb;
                    for (int x = 0; x < w; x++)
                    {
                        int i = x * 4;
                        var px = new SKColor(row[i], row[i + 1], row[i + 2], row[i + 3]);
                        if (px.Alpha > 0 && !IsBackgroundPixel(px, backgroundColor, toleranceSq))
                        { points.Add(new SKPointI(x, y)); break; }
                    }
                    for (int x = w - 1; x >= 0; x--)
                    {
                        int i = x * 4;
                        var px = new SKColor(row[i], row[i + 1], row[i + 2], row[i + 3]);
                        if (px.Alpha > 0 && !IsBackgroundPixel(px, backgroundColor, toleranceSq))
                        { points.Add(new SKPointI(x, y)); break; }
                    }
                }

                for (int x = 0; x < w; x += step)
                {
                    for (int y = 0; y < h; y++)
                    {
                        byte* row = p + y * rb;
                        int i = x * 4;
                        var px = new SKColor(row[i], row[i + 1], row[i + 2], row[i + 3]);
                        if (px.Alpha > 0 && !IsBackgroundPixel(px, backgroundColor, toleranceSq))
                        { points.Add(new SKPointI(x, y)); break; }
                    }
                    for (int y = h - 1; y >= 0; y--)
                    {
                        byte* row = p + y * rb;
                        int i = x * 4;
                        var px = new SKColor(row[i], row[i + 1], row[i + 2], row[i + 3]);
                        if (px.Alpha > 0 && !IsBackgroundPixel(px, backgroundColor, toleranceSq))
                        { points.Add(new SKPointI(x, y)); break; }
                    }
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
            int feather = Math.Max(2, Math.Min(w, h) / 80);

            var hullArray = hull;

            unsafe
            {
                byte* dst = (byte*)result.GetPixels();
                int rb = result.RowBytes;

                Parallel.For(0, h, y =>
                {
                    byte* row = dst + y * rb;
                    for (int x = 0; x < w; x++)
                    {
                        int i = x * 4;
                        byte origAlpha = row[i + 3];
                        if (origAlpha == 0) continue;

                        double d = MinSignedDistanceToHull(x, y, hullArray);
                        byte alpha;
                        if (d >= feather)
                            alpha = origAlpha;
                        else if (d <= 0)
                            alpha = 0;
                        else
                            alpha = (byte)(origAlpha * (d / feather));

                        row[i + 3] = alpha;
                    }
                });
            }

            return result;
        }

        // ─── Flood Fill ────────────────────────────────────────────────────────────

        private static SKBitmap RemoveBackgroundFloodFill(SKBitmap source, SKColor backgroundColor, int tolerance, bool removeEnclosed = false)
        {
            System.Diagnostics.Debug.WriteLine($"[Fill] Starting flood fill: {source.Width}x{source.Height}, bg=#{backgroundColor.Red:X2}{backgroundColor.Green:X2}{backgroundColor.Blue:X2}, tolerance={tolerance}");

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
            int totalPixels = width * height;
            int toleranceSq = tolerance * tolerance;

            var pixels = new uint[totalPixels];
            unsafe
            {
                uint* src = (uint*)padded.GetPixels();
                for (int i = 0; i < totalPixels; i++)
                    pixels[i] = src[i];
            }

            var visited = new bool[totalPixels];
            var removed = new bool[totalPixels];
            var queue = new Queue<int>(totalPixels / 4);

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
                            if (IsBackgroundPixelUint(pixels[ni], backgroundColor, toleranceSq))
                            {
                                removed[ni] = true;
                                pixels[ni] = 0;
                                queue.Enqueue(ni);
                            }
                        }
                    }
            }

            System.Diagnostics.Debug.WriteLine($"[Fill] Flood fill complete");

            if (removeEnclosed)
            {
                int enclosedCount = 0;
                for (int i = 0; i < totalPixels; i++)
                {
                    if (!visited[i] && IsBackgroundPixelUint(pixels[i], backgroundColor, toleranceSq))
                    {
                        pixels[i] = 0;
                        removed[i] = true;
                        enclosedCount++;
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[Fill] Enclosed pixels removed: {enclosedCount}");
            }

            visited = null!;
            FeatherEdges(pixels, removed, width, height);

            unsafe
            {
                uint* dst = (uint*)padded.GetPixels();
                for (int i = 0; i < totalPixels; i++)
                    dst[i] = pixels[i];
            }

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

        private static void SeedEdge(int x, int y, int width, uint[] pixels,
            bool[] visited, bool[] removed, SKColor bg, int toleranceSq, Queue<int> queue)
        {
            int idx = y * width + x;
            if (visited[idx]) return;
            visited[idx] = true;
            if (IsBackgroundPixelUint(pixels[idx], bg, toleranceSq))
            {
                removed[idx] = true;
                pixels[idx] = 0;
                queue.Enqueue(idx);
            }
        }

        private static bool IsBackgroundPixelUint(uint packed, SKColor bg, int toleranceSq)
        {
            byte a = (byte)(packed >> 24);
            if (a == 0) return true;
            int dr = (byte)(packed >> 0) - bg.Red;
            int dg = (byte)(packed >> 8) - bg.Green;
            int db = (byte)(packed >> 16) - bg.Blue;
            return dr * dr + dg * dg + db * db <= toleranceSq;
        }

        private static bool IsBackgroundPixel(SKColor pixel, SKColor bg, int toleranceSq)
        {
            int dr = pixel.Red - bg.Red;
            int dg = pixel.Green - bg.Green;
            int db = pixel.Blue - bg.Blue;
            return dr * dr + dg * dg + db * db <= toleranceSq;
        }

        private static unsafe SKColor ReadPixel(byte* p, int rowBytes, int x, int y)
        {
            byte* px = p + y * rowBytes + x * 4;
            return new SKColor(px[0], px[1], px[2], px[3]);
        }

        private static int ColorDistance(SKColor a, SKColor b)
        {
            int dr = a.Red - b.Red;
            int dg = a.Green - b.Green;
            int db = a.Blue - b.Blue;
            return dr * dr + dg * dg + db * db;
        }

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

        private static void FeatherEdges(uint[] pixels, bool[] removed, int width, int height)
        {
            const int featherWidth = 2;
            const float gamma = 1.5f;

            int length = pixels.Length;
            var dist = new int[length];
            Array.Fill(dist, int.MaxValue);
            var queue = new Queue<int>(length / 8);

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

            for (int i = 0; i < length; i++)
            {
                if (removed[i]) continue;
                int d = dist[i];
                if (d == int.MaxValue || d == 0 || d > featherWidth) continue;

                uint packed = pixels[i];
                byte origAlpha = (byte)(packed >> 24);
                float t = (float)d / featherWidth;
                byte newAlpha = (byte)(MathF.Pow(t, gamma) * origAlpha);
                pixels[i] = (packed & 0x00FFFFFFu) | ((uint)newAlpha << 24);
            }
        }

        public static SKBitmap? AutoCrop(SKBitmap source, int paddingPercent = 0, bool cropVertical = true, bool cropHorizontal = true)
        {
            int width = source.Width;
            int height = source.Height;

            int top = 0, bottom = height - 1, left = 0, right = width - 1;
            bool found = false;

            unsafe
            {
                byte* p = (byte*)source.GetPixels();
                int rb = source.RowBytes;

                for (int y = 0; y < height && !found; y++)
                {
                    byte* row = p + y * rb;
                    for (int x = 0; x < width && !found; x++)
                        if (row[x * 4 + 3] > 0) { top = y; found = true; }
                }

                if (!found) return null;

                found = false;
                for (int y = height - 1; y >= top && !found; y--)
                {
                    byte* row = p + y * rb;
                    for (int x = 0; x < width && !found; x++)
                        if (row[x * 4 + 3] > 0) { bottom = y; found = true; }
                }

                found = false;
                for (int x = 0; x < width && !found; x++)
                    for (int y = top; y <= bottom && !found; y++)
                        if ((p + y * rb)[x * 4 + 3] > 0) { left = x; found = true; }

                found = false;
                for (int x = width - 1; x >= left && !found; x--)
                    for (int y = top; y <= bottom && !found; y++)
                        if ((p + y * rb)[x * 4 + 3] > 0) { right = x; found = true; }
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

        public static Bitmap ToAvaloniaBitmap(SKBitmap skBitmap)
        {
            var src = skBitmap.GetPixels();
            if (src == IntPtr.Zero)
                throw new InvalidOperationException("SKBitmap has no pixel data");

            var wb = new WriteableBitmap(
                new PixelSize(skBitmap.Width, skBitmap.Height),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Unpremul);

            using var fb = wb.Lock();

            int rowBytes = skBitmap.Width * 4;
            bool needsSwizzle = skBitmap.ColorType != SKColorType.Bgra8888;

            if (!needsSwizzle && rowBytes == fb.RowBytes)
            {
                unsafe
                {
                    Buffer.MemoryCopy(
                        (void*)src,
                        (void*)fb.Address,
                        (long)fb.RowBytes * skBitmap.Height,
                        (long)skBitmap.RowBytes * skBitmap.Height);
                }
            }
            else
            {
                unsafe
                {
                    byte* s = (byte*)src;
                    byte* d = (byte*)fb.Address;
                    int w = skBitmap.Width;
                    int h = skBitmap.Height;

                    for (int y = 0; y < h; y++)
                    {
                        uint* srcRow = (uint*)(s + y * skBitmap.RowBytes);
                        uint* dstRow = (uint*)(d + y * fb.RowBytes);
                        for (int x = 0; x < w; x++)
                        {
                            uint px = srcRow[x];
                            dstRow[x] = (px & 0xFF00FF00u)
                                      | ((px & 0x000000FFu) << 16)
                                      | ((px & 0x00FF0000u) >> 16);
                        }
                    }
                }
            }

            return wb;
        }

        public static void SaveBitmapAsPng(SKBitmap bitmap, string path)
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            var temp = path + ".tmp";
            using (var fs = File.OpenWrite(temp))
                data.SaveTo(fs);
            File.Move(temp, path, overwrite: true);
        }

        public static SKBitmap ResizeBitmap(SKBitmap source, int targetWidth, int targetHeight)
        {
            var info = new SKImageInfo(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            var result = new SKBitmap(info);
            using var canvas = new SKCanvas(result);
            canvas.Clear(SKColors.Transparent);
            using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };
            canvas.DrawBitmap(source,
                new SKRect(0, 0, source.Width, source.Height),
                new SKRect(0, 0, targetWidth, targetHeight),
                paint);
            return result;
        }

        public static ImageBrush CreateCheckerboardBrush()
        {
            const int tileSize = 8;
            const int bitmapSize = tileSize * 2;

            var bmp = new WriteableBitmap(
                new PixelSize(bitmapSize, bitmapSize),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Opaque);

            using var fb = bmp.Lock();
            for (int y = 0; y < bitmapSize; y++)
                for (int x = 0; x < bitmapSize; x++)
                {
                    bool isLight = (x < tileSize) == (y < tileSize);
                    int argb = isLight ? unchecked((int)0xFFFFFFFF) : unchecked((int)0xFFCCCCCC);
                    Marshal.WriteInt32(fb.Address + y * fb.RowBytes + x * 4, argb);
                }

            return new ImageBrush(bmp)
            {
                TileMode = TileMode.Tile,
                DestinationRect = new RelativeRect(0, 0, bitmapSize, bitmapSize, RelativeUnit.Absolute)
            };
        }
    }
}