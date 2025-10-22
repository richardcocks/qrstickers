using QRStickers.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace QRStickers.Services;

/// <summary>
/// Service for generating PDF exports with grid layouts for device stickers
/// </summary>
public class PdfExportService
{
    /// <summary>
    /// Page size definitions in millimeters
    /// </summary>
    public static class PageSizes
    {
        public static PageSize A4 => new PageSize(210, 297);         // mm
        public static PageSize A5 => new PageSize(148, 210);
        public static PageSize A6 => new PageSize(105, 148);
        public static PageSize FourBySix => new PageSize(101.6f, 152.4f);  // 4" × 6"
        public static PageSize Letter => new PageSize(215.9f, 279.4f);     // 8.5" × 11"
        public static PageSize Legal => new PageSize(215.9f, 355.6f);      // 8.5" × 14"

        public static PageSize GetPageSize(string pageSizeName)
        {
            return pageSizeName.ToLowerInvariant() switch
            {
                "a4" => A4,
                "a5" => A5,
                "a6" => A6,
                "4x6" => FourBySix,
                "letter" => Letter,
                "legal" => Legal,
                _ => A4  // Default to A4
            };
        }
    }

    /// <summary>
    /// Page size structure
    /// </summary>
    public record PageSize(float WidthMm, float HeightMm);

    /// <summary>
    /// Generate a PDF with multiple device stickers in grid layout
    /// </summary>
    public async Task<byte[]> GenerateBulkPdfAsync(PdfExportRequest request)
    {
        return await Task.Run(() =>
        {
            // Get page size
            var pageSize = PageSizes.GetPageSize(request.PageSize);

            // Validate that stickers fit on page
            ValidateStickersFitPage(request.Images, pageSize, request.Layout);

            // Create PDF document
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(pageSize.WidthMm, pageSize.HeightMm, Unit.Millimetre);
                    // Label printing: no horizontal margins, 2mm vertical margins
                    page.MarginVertical(2, Unit.Millimetre);
                    page.MarginHorizontal(0, Unit.Millimetre);

                    page.Content().Column(column =>
                    {
                        if (request.Layout.ToLowerInvariant() == "one-per-page")
                        {
                            // One sticker per page layout
                            GenerateOnePerPageLayout(request.Images, column, pageSize);
                        }
                        else
                        {
                            // Auto-fit grid layout (default)
                            GenerateAutoFitLayout(request.Images, column, pageSize);
                        }
                    });
                });
            });

            // Generate PDF bytes
            return document.GeneratePdf();
        });
    }

    /// <summary>
    /// Validates that all stickers can fit on the selected page size
    /// </summary>
    private void ValidateStickersFitPage(List<DeviceImageData> images, PageSize pageSize, string layout)
    {
        if (!images.Any())
            return;

        // Get page dimensions minus margins (label printing: no horizontal, 2mm vertical)
        var verticalMarginMm = 2f;
        var horizontalMarginMm = 0f;
        var usableWidth = pageSize.WidthMm - (2 * horizontalMarginMm);
        var usableHeight = pageSize.HeightMm - (2 * verticalMarginMm);

        // Check each sticker
        foreach (var image in images)
        {
            var stickerWidth = (float)image.WidthMm;
            var stickerHeight = (float)image.HeightMm;

            // Allow 2mm tolerance for near-fits (printer tolerance + rounding)
            var tolerance = 2f;

            // Check if sticker fits in landscape orientation
            bool fitsLandscape = stickerWidth <= usableWidth + tolerance && stickerHeight <= usableHeight + tolerance;

            // Check if sticker fits in portrait orientation (rotated 90°)
            bool fitsPortrait = stickerHeight <= usableWidth + tolerance && stickerWidth <= usableHeight + tolerance;

            // Sticker must fit in at least one orientation
            if (!fitsLandscape && !fitsPortrait)
            {
                throw new InvalidOperationException(
                    $"Sticker size ({stickerWidth:F1}mm × {stickerHeight:F1}mm) is too large for {GetPageSizeName(pageSize)} " +
                    $"page (usable area: {usableWidth:F1}mm × {usableHeight:F1}mm, no side margins, 2mm top/bottom margins). " +
                    $"Device: {image.DeviceName}. " +
                    $"Tried both landscape and portrait (90° rotated) orientations. " +
                    $"Please choose a larger page size (e.g., Letter or A4).");
            }
        }
    }

    /// <summary>
    /// Gets a friendly name for the page size
    /// </summary>
    private string GetPageSizeName(PageSize pageSize)
    {
        if (pageSize.WidthMm == 210 && pageSize.HeightMm == 297) return "A4";
        if (pageSize.WidthMm == 148 && pageSize.HeightMm == 210) return "A5";
        if (pageSize.WidthMm == 105 && pageSize.HeightMm == 148) return "A6";
        if (Math.Abs(pageSize.WidthMm - 101.6f) < 1 && Math.Abs(pageSize.HeightMm - 152.4f) < 1) return "4\"×6\"";
        if (Math.Abs(pageSize.WidthMm - 215.9f) < 1 && Math.Abs(pageSize.HeightMm - 279.4f) < 1) return "Letter";
        if (Math.Abs(pageSize.WidthMm - 215.9f) < 1 && Math.Abs(pageSize.HeightMm - 355.6f) < 1) return "Legal";
        return $"{pageSize.WidthMm:F1}mm × {pageSize.HeightMm:F1}mm";
    }

    /// <summary>
    /// Checks if sticker should be rotated 90° to fit better on page
    /// </summary>
    private bool ShouldRotateSticker(float stickerWidth, float stickerHeight, PageSize pageSize)
    {
        var usableWidth = pageSize.WidthMm;
        var usableHeight = pageSize.HeightMm - 4f; // Account for vertical margins

        // Check if sticker doesn't fit in landscape but would fit in portrait
        bool fitsLandscape = stickerWidth <= usableWidth && stickerHeight <= usableHeight;
        bool fitsPortrait = stickerHeight <= usableWidth && stickerWidth <= usableHeight;

        // Rotate if it doesn't fit landscape but does fit portrait
        return !fitsLandscape && fitsPortrait;
    }

    /// <summary>
    /// Generate auto-fit grid layout (maximize stickers per page)
    /// </summary>
    private void GenerateAutoFitLayout(List<DeviceImageData> images, ColumnDescriptor column, PageSize pageSize)
    {
        if (!images.Any())
            return;

        // Get sticker dimensions from first image (assume all same size)
        var stickerWidth = (float)images[0].WidthMm;
        var stickerHeight = (float)images[0].HeightMm;

        // Check if we should rotate stickers 90° for better fit
        var shouldRotate = ShouldRotateSticker(stickerWidth, stickerHeight, pageSize);
        if (shouldRotate)
        {
            // Swap dimensions for rotated stickers
            (stickerWidth, stickerHeight) = (stickerHeight, stickerWidth);
            Console.WriteLine($"[PDF Export] Rotating stickers 90° for better fit on {GetPageSizeName(pageSize)}");
        }

        // Calculate usable area (page minus margins)
        var verticalMarginMm = 2f;  // Top/bottom margins
        var horizontalMarginMm = 0f;  // No left/right margins (full width)
        var usableWidth = pageSize.WidthMm - (2 * horizontalMarginMm);
        var usableHeight = pageSize.HeightMm - (2 * verticalMarginMm);

        // Calculate grid dimensions with buffer for QuestPDF rounding (mm→points→mm)
        // Add 2mm buffer to each sticker dimension to prevent floating-point edge cases
        var bufferMm = 2f;
        var cols = Math.Max(1, (int)Math.Floor(usableWidth / (stickerWidth + bufferMm)));
        var rows = Math.Max(1, (int)Math.Floor(usableHeight / (stickerHeight + bufferMm)));
        var stickersPerPage = cols * rows;

        // Calculate spacing
        var gapX = cols > 1 ? (usableWidth - (cols * stickerWidth)) / (cols - 1) : 0;
        var gapY = rows > 1 ? (usableHeight - (rows * stickerHeight)) / (rows - 1) : 0;

        // Group images into pages
        var pageGroups = images
            .Select((image, index) => new { image, index })
            .GroupBy(x => x.index / stickersPerPage)
            .Select(g => g.Select(x => x.image).ToList())
            .ToList();

        // Generate each page
        for (int pageIndex = 0; pageIndex < pageGroups.Count; pageIndex++)
        {
            var pageImages = pageGroups[pageIndex];

            if (pageIndex > 0)
            {
                column.Item().PageBreak();
            }

            // Create grid for this page using simple column/row layout
            column.Item().Column(pageColumn =>
            {
                for (int row = 0; row < rows; row++)
                {
                    if (row > 0)
                    {
                        pageColumn.Item().Height(gapY, Unit.Millimetre);
                    }

                    pageColumn.Item().Row(rowContainer =>
                    {
                        for (int col = 0; col < cols; col++)
                        {
                            var imageIndex = row * cols + col;

                            if (col > 0)
                            {
                                rowContainer.ConstantItem(gapX, Unit.Millimetre);
                            }

                            if (imageIndex < pageImages.Count)
                            {
                                var imageData = pageImages[imageIndex];
                                var imageBytes = Convert.FromBase64String(imageData.ImageBase64);

                                var container = rowContainer.ConstantItem(stickerWidth, Unit.Millimetre)
                                    .Height(stickerHeight, Unit.Millimetre);

                                if (shouldRotate)
                                {
                                    container.RotateRight().Image(imageBytes).FitArea();
                                }
                                else
                                {
                                    container.Image(imageBytes).FitArea();
                                }
                            }
                            else
                            {
                                rowContainer.ConstantItem(stickerWidth, Unit.Millimetre);
                            }
                        }
                    });
                }
            });
        }
    }

    /// <summary>
    /// Generate one-per-page layout (each sticker centered on its own page)
    /// </summary>
    private void GenerateOnePerPageLayout(List<DeviceImageData> images, ColumnDescriptor column, PageSize pageSize)
    {
        for (int i = 0; i < images.Count; i++)
        {
            var imageData = images[i];

            if (i > 0)
            {
                column.Item().PageBreak();
            }

            // Convert base64 to bytes
            var imageBytes = Convert.FromBase64String(imageData.ImageBase64);

            // Check if we should rotate this sticker
            var stickerWidth = (float)imageData.WidthMm;
            var stickerHeight = (float)imageData.HeightMm;
            var shouldRotate = ShouldRotateSticker(stickerWidth, stickerHeight, pageSize);

            if (shouldRotate)
            {
                // Swap dimensions for rotated sticker
                (stickerWidth, stickerHeight) = (stickerHeight, stickerWidth);
            }

            // Center sticker on page
            var container = column.Item()
                .AlignCenter()
                .AlignMiddle()
                .Width(stickerWidth, Unit.Millimetre)
                .Height(stickerHeight, Unit.Millimetre);

            if (shouldRotate)
            {
                container.RotateRight().Image(imageBytes).FitArea();
            }
            else
            {
                container.Image(imageBytes).FitArea();
            }
        }
    }
}
