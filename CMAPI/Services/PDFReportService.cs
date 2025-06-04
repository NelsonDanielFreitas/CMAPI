using DinkToPdf;
using DinkToPdf.Contracts;
using System.Text;
using CMAPI.DTO.Avaria;
using System.Net.Mail;
using System.Net;

namespace CMAPI.Services;

public class PDFReportService
{
    private readonly string _reportsDirectory;
    private readonly IConverter _converter;
    private readonly IConfiguration _configuration;

    public PDFReportService(IConfiguration config, IConverter converter)
    {
        _reportsDirectory = config.GetValue<string>("ReportsStorage:RootPath")
                       ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "reports");
        _converter = converter;
        _configuration = config;
        
        if (!Directory.Exists(_reportsDirectory))
            Directory.CreateDirectory(_reportsDirectory);
    }

    public async Task<string> GenerateAndSendTechnicianStatsReportAsync(IEnumerable<TechnicianStatsDTO> stats, DateTime startDate, DateTime endDate, string email)
    {
        string filePath = null;
        try
        {
            // Generate the PDF
            filePath = await GenerateTechnicianStatsReportAsync(stats, startDate, endDate);

            // Send the email
            await SendEmailWithAttachmentAsync(email, filePath);

            return filePath;
        }
        catch (Exception ex)
        {
            if (filePath != null && File.Exists(filePath))
            {
                try { File.Delete(filePath); } catch { }
            }
            throw new Exception($"Error creating and sending PDF: {ex.Message}", ex);
        }
    }

    private async Task SendEmailWithAttachmentAsync(string recipientEmail, string pdfPath)
    {
        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_configuration["Email:Username"], "CMAPI Reports");
            message.To.Add(recipientEmail);
            message.Subject = "Relatório de Desempenho dos Técnicos";
            message.Body = "Segue em anexo o relatório de desempenho dos técnicos solicitado.";
            message.IsBodyHtml = false;

            // Attach the PDF
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", pdfPath);
            message.Attachments.Add(new Attachment(fullPath));

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(_configuration["Email:Username"], _configuration["Email:Password"])
            };

            await smtp.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error sending email: {ex.Message}", ex);
        }
    }

    public async Task<string> GenerateTechnicianStatsReportAsync(IEnumerable<TechnicianStatsDTO> stats, DateTime startDate, DateTime endDate)
    {
        string filePath = null;
        try
        {
            var fileName = $"technician_stats_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            filePath = Path.Combine(_reportsDirectory, fileName);

            // Generate HTML content
            var htmlContent = GenerateHtmlContent(stats);

            // Configure PDF conversion settings
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 },
                    Out = filePath
                },
                Objects = {
                    new ObjectSettings
                    {
                        PagesCount = true,
                        HtmlContent = htmlContent,
                        WebSettings = { DefaultEncoding = "utf-8" },
                        HeaderSettings = { FontSize = 9, Right = "Page [page] of [toPage]", Line = true },
                        UseLocalLinks = true
                    }
                }
            };

            // Convert HTML to PDF
            _converter.Convert(doc);

            return Path.Combine("reports", fileName).Replace("\\", "/");
        }
        catch (Exception ex)
        {
            if (File.Exists(filePath))
            {
                try { File.Delete(filePath); } catch { }
            }
            throw new Exception($"Error creating PDF: {ex.Message}", ex);
        }
    }

    private string GenerateHtmlContent(IEnumerable<TechnicianStatsDTO> stats)
    {
        var html = new StringBuilder();
        html.Append(@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    body { font-family: Arial, sans-serif; }
                    .header { text-align: center; margin-bottom: 20px; }
                    .title { font-size: 24px; font-weight: bold; margin-bottom: 10px; }
                    .subtitle { font-size: 18px; margin-bottom: 20px; }
                    table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
                    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                    th { background-color: #f2f2f2; }
                    .section { margin-top: 30px; }
                    .section-title { font-size: 20px; font-weight: bold; margin-bottom: 15px; }
                </style>
            </head>
            <body>
                <div class='header'>
                    <div class='title'>Relatório de Desempenho dos Técnicos</div>
                    <div class='subtitle'>Estatísticas Gerais</div>
                </div>");

        // Global Statistics
        var totalResolved = stats.Sum(s => s.TotalAvariaResolved);
        var avgResolutionTime = stats.Any() ? stats.Average(s => s.AverageResolutionTime) : 0;
        var totalOnTime = stats.Sum(s => s.OnTimeResolutions);
        var totalDelayed = stats.Sum(s => s.DelayedResolutions);

        html.Append(@"
            <div class='section'>
                <div class='section-title'>Estatísticas Globais</div>
                <table>
                    <tr>
                        <th>Métrica</th>
                        <th>Valor</th>
                    </tr>
                    <tr>
                        <td>Total de Avarias Resolvidas</td>
                        <td>" + totalResolved + @"</td>
                    </tr>
                    <tr>
                        <td>Tempo Médio de Resolução</td>
                        <td>" + avgResolutionTime.ToString("F2") + @" horas</td>
                    </tr>
                    <tr>
                        <td>Resoluções no Prazo</td>
                        <td>" + totalOnTime + @"</td>
                    </tr>
                    <tr>
                        <td>Resoluções com Atraso</td>
                        <td>" + totalDelayed + @"</td>
                    </tr>
                </table>
            </div>");

        // Individual Technician Statistics
        foreach (var tech in stats)
        {
            html.Append($@"
                <div class='section'>
                    <div class='section-title'>Técnico: {tech.TechnicianName}</div>
                    <table>
                        <tr>
                            <th>Métrica</th>
                            <th>Valor</th>
                        </tr>
                        <tr>
                            <td>Total de Avarias Resolvidas</td>
                            <td>{tech.TotalAvariaResolved}</td>
                        </tr>
                        <tr>
                            <td>Tempo Médio de Resolução</td>
                            <td>{tech.AverageResolutionTime:F2} horas</td>
                        </tr>
                        <tr>
                            <td>Resoluções no Prazo</td>
                            <td>{tech.OnTimeResolutions}</td>
                        </tr>
                        <tr>
                            <td>Resoluções com Atraso</td>
                            <td>{tech.DelayedResolutions}</td>
                        </tr>
                    </table>");

            // Avaria Type Statistics
            if (tech.AvariaTypeStats?.Any() == true)
            {
                html.Append(@"
                    <div class='section-title'>Estatísticas por Tipo de Avaria</div>
                    <table>
                        <tr>
                            <th>Tipo de Avaria</th>
                            <th>Quantidade</th>
                            <th>Tempo Médio (horas)</th>
                        </tr>");

                foreach (var type in tech.AvariaTypeStats)
                {
                    html.Append($@"
                        <tr>
                            <td>{type.AvariaType ?? "N/A"}</td>
                            <td>{type.Count}</td>
                            <td>{type.AverageResolutionTime:F2}</td>
                        </tr>");
                }
                html.Append("</table>");
            }

            // Monthly Statistics
            if (tech.MonthlyStats?.Any() == true)
            {
                html.Append(@"
                    <div class='section-title'>Estatísticas Mensais</div>
                    <table>
                        <tr>
                            <th>Mês/Ano</th>
                            <th>Total Resolvidas</th>
                            <th>Tempo Médio</th>
                            <th>No Prazo</th>
                            <th>Com Atraso</th>
                        </tr>");

                foreach (var month in tech.MonthlyStats)
                {
                    html.Append($@"
                        <tr>
                            <td>{month.Month}/{month.Year}</td>
                            <td>{month.TotalAvariaResolved}</td>
                            <td>{month.AverageResolutionTime:F2} horas</td>
                            <td>{month.OnTimeResolutions}</td>
                            <td>{month.DelayedResolutions}</td>
                        </tr>");
                }
                html.Append("</table>");
            }

            html.Append("</div>");
        }

        html.Append("</body></html>");
        return html.ToString();
    }
} 