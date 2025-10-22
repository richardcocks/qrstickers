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

            // Create PDF document
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(pageSize.WidthMm, pageSize.HeightMm, Unit.Millimetre);
                    page.Margin(10, Unit.Millimetre);

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
    /// Generate auto-fit grid layout (maximize stickers per page)
    /// </summary>
    private void GenerateAutoFitLayout(List<DeviceImageData> images, ColumnDescriptor column, PageSize pageSize)
    {
        if (!images.Any())
            return;

        // Get sticker dimensions from first image (assume all same size)
        var stickerWidth = images[0].WidthMm;
        var stickerHeight = images[0].HeightMm;

        // Calculate usable area (page minus margins)
        var marginMm = 10f;
        var usableWidth = pageSize.WidthMm - (2 * marginMm);
        var usableHeight = pageSize.HeightMm - (2 * marginMm);

        // Calculate grid dimensions
        var cols = Math.Max(1, (int)Math.Floor(usableWidth / stickerWidth));
        var rows = Math.Max(1, (int)Math.Floor(usableHeight / stickerHeight));
        var stickersPerPage = cols * rows;

        // Calculate spacing to center grid on page
        var totalGridWidth = cols * stickerWidth;
        var totalGridHeight = rows * stickerHeight;
        var horizontalSpacing = (usableWidth - totalGridWidth) / (cols + 1);
        var verticalSpacing = (usableHeight - totalGridHeight) / (rows + 1);

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
                // Add page break (new page)
                column.Item().PageBreak();
            }

            // Create grid for this page
            column.Item().Column(pageColumn =>
            {
                for (int row = 0; row < rows; row++)
                {
                    pageColumn.Item().Row(rowContainer =>
                    {
                        rowContainer.Spacing((float)verticalSpacing, Unit.Millimetre);

                        for (int col = 0; col < cols; col++)
                        {
                            var imageIndex = row * cols + col;
                            if (imageIndex < pageImages.Count)
                            {
                                var imageData = pageImages[imageIndex];
                                rowContainer.RelativeItem().Width((float)stickerWidth, Unit.Millimetre)
                                    .Height((float)stickerHeight, Unit.Millimetre)
                                    .PaddingHorizontal((float)horizontalSpacing / 2, Unit.Millimetre)
                                    .Image(Convert.FromBase64String(imageData.ImageBase64));
                            }
                            else
                            {
                                // Empty cell
                                rowContainer.RelativeItem().Width((float)stickerWidth, Unit.Millimetre);
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
                // Add page break (new page)
                column.Item().PageBreak();
            }

            // Center sticker on page
            column.Item()
                .AlignCenter()
                .AlignMiddle()
                .Width((float)imageData.WidthMm, Unit.Millimetre)
                .Height((float)imageData.HeightMm, Unit.Millimetre)
                .Image(Convert.FromBase64String(imageData.ImageBase64));
        }
    }
}
