using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;

namespace HexAI.NLP;

/// <summary>
/// Sohbet geçmişini ve bağlamı yöneten bellek sınıfı.
/// Sliding window yaklaşımıyla son N mesajı hafızada tutar.
/// Bu sayede asistan, konuşma bağlamını koruyarak daha tutarlı yanıtlar üretir.
/// </summary>
public class ConversationMemory
{
    /// <summary>Tek bir sohbet mesajı.</summary>
    public class Message
    {
        public string Role { get; set; } = "";   // "user" veya "assistant"
        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public override string ToString() => $"[{Role}] {Content}";
    }

    private readonly List<Message> _history = new();
    private readonly int _maxHistory;

    /// <summary>Tüm sohbet geçmişi (salt okunur).</summary>
    public IReadOnlyList<Message> History => _history.AsReadOnly();

    /// <summary>Toplam mesaj sayısı.</summary>
    public int Count => _history.Count;

    /// <summary>
    /// Yeni bir ConversationMemory oluşturur.
    /// </summary>
    /// <param name="maxHistory">Hafızada tutulacak maksimum mesaj sayısı.</param>
    public ConversationMemory(int maxHistory = 50)
    {
        _maxHistory = maxHistory;
    }

    /// <summary>
    /// Kullanıcı mesajı ekler.
    /// </summary>
    public void AddUserMessage(string content)
    {
        AddMessage("user", content);
    }

    /// <summary>
    /// Asistan yanıtı ekler.
    /// </summary>
    public void AddAssistantMessage(string content)
    {
        AddMessage("assistant", content);
    }

    /// <summary>
    /// Mesaj ekler ve gerekiyorsa eski mesajları siler.
    /// </summary>
    private void AddMessage(string role, string content)
    {
        _history.Add(new Message
        {
            Role = role,
            Content = content,
            Timestamp = DateTime.Now
        });

        // Sliding window: maksimum kapasiteyi aşarsa en eski mesajı sil
        while (_history.Count > _maxHistory)
        {
            _history.RemoveAt(0);
        }
    }

    /// <summary>
    /// Son N mesajı döndürür (bağlam penceresi).
    /// </summary>
    public List<Message> GetRecentMessages(int count = 10)
    {
        int start = Math.Max(0, _history.Count - count);
        return _history.Skip(start).ToList();
    }

    /// <summary>
    /// Son mesajlardan bağlam metni oluşturur.
    /// MiniGPT'ye verilecek ek bağlam olarak kullanılır.
    /// </summary>
    public string GetContextString(int messageCount = 5)
    {
        var recent = GetRecentMessages(messageCount);
        if (recent.Count == 0) return "";

        return string.Join(" ", recent.Select(m => m.Content));
    }

    /// <summary>
    /// Son kullanıcı mesajını döndürür.
    /// </summary>
    public string? GetLastUserMessage()
    {
        return _history.LastOrDefault(m => m.Role == "user")?.Content;
    }

    /// <summary>
    /// Son asistan yanıtını döndürür.
    /// </summary>
    public string? GetLastAssistantMessage()
    {
        return _history.LastOrDefault(m => m.Role == "assistant")?.Content;
    }

    /// <summary>
    /// Sohbet geçmişini temizler.
    /// </summary>
    public void Clear()
    {
        _history.Clear();
    }

    /// <summary>
    /// Sohbet geçmişini JSON dosyasına kaydeder.
    /// </summary>
    public void SaveToFile(string path)
    {
        var json = JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true });
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Sohbet geçmişini JSON dosyasından yükler.
    /// </summary>
    public void LoadFromFile(string path)
    {
        if (!File.Exists(path)) return;

        var json = File.ReadAllText(path);
        var messages = JsonSerializer.Deserialize<List<Message>>(json);
        if (messages != null)
        {
            _history.Clear();
            _history.AddRange(messages);
        }
    }
}
