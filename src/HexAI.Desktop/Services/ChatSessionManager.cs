using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HexAI.Desktop.ViewModels;

namespace HexAI.Desktop.Services;

public class ChatSessionManager
{
    private readonly string _sessionsDir;

    public ChatSessionManager()
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _sessionsDir = Path.Combine(userProfile, ".hexai", "sessions");
        
        if (!Directory.Exists(_sessionsDir))
        {
            Directory.CreateDirectory(_sessionsDir);
        }
    }

    public List<ChatSession> LoadAllSessions()
    {
        var sessions = new List<ChatSession>();
        var files = Directory.GetFiles(_sessionsDir, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var session = JsonSerializer.Deserialize<ChatSession>(json);
                if (session != null)
                {
                    sessions.Add(session);
                }
            }
            catch
            {
                // Ignore corrupted sessions
            }
        }

        // En yeniler en üstte
        return sessions.OrderByDescending(s => s.CreatedAt).ToList();
    }

    public void SaveSession(ChatSession session)
    {
        if (session == null || string.IsNullOrWhiteSpace(session.Id)) return;

        string filePath = Path.Combine(_sessionsDir, $"{session.Id}.json");
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(session, options);
            File.WriteAllText(filePath, json);
        }
        catch
        {
            // Fail silently
        }
    }

    public void DeleteSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return;

        string filePath = Path.Combine(_sessionsDir, $"{sessionId}.json");
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch { }
        }
    }

    /// <summary>
    /// Sohbeti Markdown formatında dışa aktarır.
    /// </summary>
    public string ExportToMarkdown(ChatSession session)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# {session.Title}");
        sb.AppendLine($"*Tarih: {session.CreatedAt:dd/MM/yyyy HH:mm}*");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        foreach (var msg in session.Messages)
        {
            string role = msg.IsUser ? "**Siz**" : "**HexAI**";
            sb.AppendLine($"{role}:");
            sb.AppendLine();
            sb.AppendLine(msg.Text);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Sohbeti dosyaya kaydeder (Markdown veya JSON).
    /// </summary>
    public string ExportToFile(ChatSession session, string format = "md")
    {
        string fileName = $"HexAI_Sohbet_{session.CreatedAt:yyyyMMdd_HHmm}";
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        string content;
        string ext;

        if (format == "json")
        {
            content = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
            ext = ".json";
        }
        else
        {
            content = ExportToMarkdown(session);
            ext = ".md";
        }

        string outputPath = Path.Combine(desktop, fileName + ext);
        File.WriteAllText(outputPath, content);
        return outputPath;
    }
}
