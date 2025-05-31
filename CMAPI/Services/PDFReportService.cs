using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using CMAPI.DTO.Avaria;
using System.Globalization;

namespace CMAPI.Services;

public class PDFReportService
{
    private readonly string _reportsDirectory;

    public PDFReportService(IConfiguration config)
    {
        _reportsDirectory = config.GetValue<string>("ReportsStorage:RootPath")
                       ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "reports");
        if (!Directory.Exists(_reportsDirectory))
            Directory.CreateDirectory(_reportsDirectory);
    }

    public async Task<string> GenerateTechnicianStatsReportAsync(IEnumerable<TechnicianStatsDTO> stats, DateTime startDate, DateTime endDate)
    {
        try
        {
            var fileName = $"technician_stats_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            var filePath = Path.Combine(_reportsDirectory, fileName);

            // Log the file path for debugging
            if (!Directory.Exists(_reportsDirectory))
                throw new Exception($"Reports directory does not exist: {_reportsDirectory}");

            // Check if directory is writable
            try
            {
                var testFile = Path.Combine(_reportsDirectory, "__test.txt");
                await File.WriteAllTextAsync(testFile, "test");
                File.Delete(testFile);
            }
            catch (Exception dirEx)
            {
                throw new Exception($"Cannot write to reports directory: {_reportsDirectory}. Error: {dirEx.Message}", dirEx);
            }

            // Log the file path
            if (filePath.Length > 250)
                throw new Exception($"File path too long: {filePath}");

            // Defensive: ensure stats is not null
            if (stats == null)
                stats = new List<TechnicianStatsDTO>();

            // Defensive: ensure all lists are not null
            foreach (var s in stats)
            {
                s.AvariaTypeStats ??= new List<AvariaTypeStatsDTO>();
                s.MonthlyStats ??= new List<MonthlyStatsDTO>();
            }

            try
            {
                // First, try to create a simple test PDF to verify iText7 is working
                var testPdfPath = Path.Combine(_reportsDirectory, "test.pdf");
                using (var testWriter = new PdfWriter(testPdfPath))
                using (var testPdf = new PdfDocument(testWriter))
                using (var testDoc = new Document(testPdf))
                {
                    testDoc.Add(new Paragraph("Test PDF"));
                    testDoc.Close();
                }

                // If test PDF was created successfully, proceed with the actual report
                using var writer = new PdfWriter(filePath);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                // Create and set font
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                // Add title
                var title = new Paragraph("Relatório de Desempenho dos Técnicos")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(20)
                    .SetFont(boldFont);
                document.Add(title);

                // Add date range
                var dateRange = new Paragraph($"Período: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(12)
                    .SetMarginBottom(20);
                document.Add(dateRange);

                // Add global statistics
                var globalStats = new Paragraph("Estatísticas Globais")
                    .SetFontSize(16)
                    .SetFont(boldFont)
                    .SetMarginTop(20);
                document.Add(globalStats);

                var totalResolved = stats.Sum(s => s.TotalAvariaResolved);
                var avgResolutionTime = stats.Any() ? stats.Average(s => s.AverageResolutionTime) : 0;
                var totalOnTime = stats.Sum(s => s.OnTimeResolutions);
                var totalDelayed = stats.Sum(s => s.DelayedResolutions);

                var globalStatsTable = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20);

                globalStatsTable.AddCell(new Cell().Add(new Paragraph("Total de Avarias Resolvidas").SetFont(boldFont)));
                globalStatsTable.AddCell(new Cell().Add(new Paragraph(totalResolved.ToString())));

                globalStatsTable.AddCell(new Cell().Add(new Paragraph("Tempo Médio de Resolução").SetFont(boldFont)));
                globalStatsTable.AddCell(new Cell().Add(new Paragraph($"{avgResolutionTime:F2} horas")));

                globalStatsTable.AddCell(new Cell().Add(new Paragraph("Resoluções no Prazo").SetFont(boldFont)));
                globalStatsTable.AddCell(new Cell().Add(new Paragraph(totalOnTime.ToString())));

                globalStatsTable.AddCell(new Cell().Add(new Paragraph("Resoluções com Atraso").SetFont(boldFont)));
                globalStatsTable.AddCell(new Cell().Add(new Paragraph(totalDelayed.ToString())));

                document.Add(globalStatsTable);

                // Add individual technician statistics
                foreach (var tech in stats)
                {
                    var techTitle = new Paragraph($"Técnico: {tech.TechnicianName}")
                        .SetFontSize(14)
                        .SetFont(boldFont)
                        .SetMarginTop(20);
                    document.Add(techTitle);

                    var techStatsTable = new Table(2)
                        .SetWidth(UnitValue.CreatePercentValue(100))
                        .SetMarginBottom(20);

                    techStatsTable.AddCell(new Cell().Add(new Paragraph("Total de Avarias Resolvidas").SetFont(boldFont)));
                    techStatsTable.AddCell(new Cell().Add(new Paragraph(tech.TotalAvariaResolved.ToString())));

                    techStatsTable.AddCell(new Cell().Add(new Paragraph("Tempo Médio de Resolução").SetFont(boldFont)));
                    techStatsTable.AddCell(new Cell().Add(new Paragraph($"{tech.AverageResolutionTime:F2} horas")));

                    techStatsTable.AddCell(new Cell().Add(new Paragraph("Resoluções no Prazo").SetFont(boldFont)));
                    techStatsTable.AddCell(new Cell().Add(new Paragraph(tech.OnTimeResolutions.ToString())));

                    techStatsTable.AddCell(new Cell().Add(new Paragraph("Resoluções com Atraso").SetFont(boldFont)));
                    techStatsTable.AddCell(new Cell().Add(new Paragraph(tech.DelayedResolutions.ToString())));

                    document.Add(techStatsTable);

                    // Add avaria type statistics
                    if (tech.AvariaTypeStats.Any())
                    {
                        var typeTitle = new Paragraph("Estatísticas por Tipo de Avaria")
                            .SetFontSize(12)
                            .SetFont(boldFont)
                            .SetMarginTop(10);
                        document.Add(typeTitle);

                        var typeTable = new Table(3)
                            .SetWidth(UnitValue.CreatePercentValue(100))
                            .SetMarginBottom(20);

                        typeTable.AddHeaderCell(new Cell().Add(new Paragraph("Tipo de Avaria").SetFont(boldFont)));
                        typeTable.AddHeaderCell(new Cell().Add(new Paragraph("Quantidade").SetFont(boldFont)));
                        typeTable.AddHeaderCell(new Cell().Add(new Paragraph("Tempo Médio (horas)").SetFont(boldFont)));

                        foreach (var type in tech.AvariaTypeStats)
                        {
                            typeTable.AddCell(new Cell().Add(new Paragraph(type.AvariaType)));
                            typeTable.AddCell(new Cell().Add(new Paragraph(type.Count.ToString())));
                            typeTable.AddCell(new Cell().Add(new Paragraph($"{type.AverageResolutionTime:F2}")));
                        }

                        document.Add(typeTable);
                    }

                    // Add monthly statistics
                    if (tech.MonthlyStats.Any())
                    {
                        var monthlyTitle = new Paragraph("Estatísticas Mensais")
                            .SetFontSize(12)
                            .SetFont(boldFont)
                            .SetMarginTop(10);
                        document.Add(monthlyTitle);

                        var monthlyTable = new Table(5)
                            .SetWidth(UnitValue.CreatePercentValue(100))
                            .SetMarginBottom(20);

                        monthlyTable.AddHeaderCell(new Cell().Add(new Paragraph("Mês/Ano").SetFont(boldFont)));
                        monthlyTable.AddHeaderCell(new Cell().Add(new Paragraph("Total Resolvidas").SetFont(boldFont)));
                        monthlyTable.AddHeaderCell(new Cell().Add(new Paragraph("Tempo Médio").SetFont(boldFont)));
                        monthlyTable.AddHeaderCell(new Cell().Add(new Paragraph("No Prazo").SetFont(boldFont)));
                        monthlyTable.AddHeaderCell(new Cell().Add(new Paragraph("Com Atraso").SetFont(boldFont)));

                        foreach (var month in tech.MonthlyStats)
                        {
                            monthlyTable.AddCell(new Cell().Add(new Paragraph($"{month.Month}/{month.Year}")));
                            monthlyTable.AddCell(new Cell().Add(new Paragraph(month.TotalAvariaResolved.ToString())));
                            monthlyTable.AddCell(new Cell().Add(new Paragraph($"{month.AverageResolutionTime:F2} horas")));
                            monthlyTable.AddCell(new Cell().Add(new Paragraph(month.OnTimeResolutions.ToString())));
                            monthlyTable.AddCell(new Cell().Add(new Paragraph(month.DelayedResolutions.ToString())));
                        }

                        document.Add(monthlyTable);
                    }
                }

                document.Close();

                return Path.Combine("reports", fileName).Replace("\\", "/");
            }
            catch (IOException ioEx)
            {
                throw new Exception($"IO Error creating PDF: {ioEx.Message}", ioEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating PDF: {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            // Log the full exception details (optionally use a logger)
            var errorMsg = $"PDF generation failed: {ex.Message}\n{ex.StackTrace}";
            throw new Exception(errorMsg, ex);
        }
    }
} 