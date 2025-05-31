using System.Net.Http.Json;
using System.Text.Json;
using CMAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace CMAPI.Services;

public class OllamaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _ollamaBaseUrl;
    private readonly AppDbContext _context;

    public OllamaService(HttpClient httpClient, IConfiguration configuration, AppDbContext context)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _ollamaBaseUrl = _configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _context = context;
    }

    public async Task<string> GetLLMResponseAsync(string message)
    {
        try
        {
            // Get context data
            var contextData = await GetContextDataAsync();

            // Create a prompt that includes the context
            var fullPrompt = $@"Context Information:
{contextData}

User Question: {message}

Please provide a response based on the context information above and the user's question. If the question is not related to the context, you can still provide a general response.";

            var request = new
            {
                model = "llama2",
                prompt = fullPrompt,
                stream = false
            };

            var response = await _httpClient.PostAsJsonAsync($"{_ollamaBaseUrl}/api/generate", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
            return result?.Response ?? "No response from LLM";
        }
        catch (Exception ex)
        {
            throw new Exception($"Error getting response from Ollama: {ex.Message}", ex);
        }
    }

    private async Task<string> GetContextDataAsync()
    {
        var contextBuilder = new System.Text.StringBuilder();

        // Get recent avarias
        var recentAvarias = await _context.Avaria
            .Include(a => a.Technician)
            .Include(a => a.Status)
            .Include(a => a.Urgencia)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToListAsync();

        contextBuilder.AppendLine("Recent Avarias:");
        foreach (var avaria in recentAvarias)
        {
            contextBuilder.AppendLine($"- ID: {avaria.Id}, Type: {avaria.Descricao}, Status: {avaria.Status?.Name}, Urgency: {avaria.Urgencia?.Name}, Technician: {avaria.Technician?.FirstName} {avaria.Technician?.LastName ?? "Unassigned"}");
        }

        // Get recent notifications
        var recentNotifications = await _context.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .ToListAsync();

        contextBuilder.AppendLine("\nRecent Notifications:");
        foreach (var notification in recentNotifications)
        {
            contextBuilder.AppendLine($"- ID: {notification.Id}, Message: {notification.Message}, Type: {notification.Type}");
        }

        // Get technician users
        var technicians = await _context.Users
            .Include(u => u.Role)
            .Where(u => u.Role.RoleName == "TECNICO")
            .ToListAsync();

        contextBuilder.AppendLine("\nTechnician Users:");
        foreach (var tech in technicians)
        {
            contextBuilder.AppendLine($"- ID: {tech.Id}, Name: {tech.FirstName} {tech.LastName}, Email: {tech.Email}");
        }

        return contextBuilder.ToString();
    }

    private class OllamaResponse
    {
        public string Response { get; set; }
        public bool Done { get; set; }
    }
} 