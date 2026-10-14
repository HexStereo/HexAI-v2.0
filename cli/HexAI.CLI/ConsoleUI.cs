namespace HexAI.CLI;

/// <summary>
/// Terminal arayüzü yardımcı sınıfı.
/// Renkli çıktı, yazma animasyonu, banner ve formatlama sağlar.
/// </summary>
public static class ConsoleUI
{
    /// <summary>
    /// HexAI ASCII art bannerını yazdırır.
    /// </summary>
    public static void PrintBanner()
    {
        try { Console.Clear(); } catch { }
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
  ╔═══════════════════════════════════════════════════╗
  ║                                                   ║
  ║   ██╗  ██╗███████╗██╗  ██╗     █████╗ ██╗        ║
  ║   ██║  ██║██╔════╝╚██╗██╔╝    ██╔══██╗██║        ║
  ║   ███████║█████╗   ╚███╔╝     ███████║██║        ║
  ║   ██╔══██║██╔══╝   ██╔██╗     ██╔══██║██║        ║
  ║   ██║  ██║███████╗██╔╝ ██╗    ██║  ██║██║        ║
  ║   ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝    ╚═╝  ╚═╝╚═╝        ║
  ║                                                   ║
  ║       Kişisel Yapay Zeka Asistanın  v1.0          ║
  ║       Powered by Custom Transformer Engine        ║
  ║                                                   ║
  ╚═══════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    /// <summary>
    /// Durum bilgilerini gösterir.
    /// </summary>
    public static void PrintStatus(bool modelReady, string? lastModel)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        string statusIcon = modelReady ? "✅" : "⚠️";
        string statusText = modelReady ? "Hazır" : "Eğitilmemiş";
        string modelText = lastModel ?? "(yok)";

        Console.WriteLine($"  {statusIcon} Model: {statusText}  │  📦 Son Model: {modelText}  │  📅 {DateTime.Now:dd/MM/yyyy}");
        Console.WriteLine($"  💡 Yardım için /help yazın  │  Çıkmak için /exit yazın");
        Console.WriteLine();
        Console.ResetColor();
    }

    /// <summary>
    /// Kullanıcı promptu yazdırır.
    /// </summary>
    public static void PrintPrompt(string assistantName = "HexAI")
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"  {assistantName}");
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.Write("> ");
        Console.ResetColor();
    }

    /// <summary>
    /// Asistan yanıtını yazma animasyonu ile gösterir.
    /// </summary>
    public static void PrintResponse(string response, bool typewriterEffect = true, int delayMs = 15)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("  🤖 ");

        if (typewriterEffect && delayMs > 0)
        {
            foreach (char c in response)
            {
                Console.Write(c);

                // Noktalama işaretlerinde biraz daha uzun dur
                int pause = c switch
                {
                    '.' or '!' or '?' => delayMs * 5,
                    ',' or ':' or ';' => delayMs * 3,
                    '\n' => delayMs * 2,
                    _ => delayMs
                };

                Thread.Sleep(pause);
            }
        }
        else
        {
            Console.Write(response);
        }

        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine();
    }

    /// <summary>
    /// Hata mesajı yazdırır.
    /// </summary>
    public static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n  ❌ {message}\n");
        Console.ResetColor();
    }

    /// <summary>
    /// Bilgi mesajı yazdırır.
    /// </summary>
    public static void PrintInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n  ℹ️  {message}\n");
        Console.ResetColor();
    }

    /// <summary>
    /// Başarı mesajı yazdırır.
    /// </summary>
    public static void PrintSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n  ✅ {message}\n");
        Console.ResetColor();
    }

    /// <summary>
    /// Yükleniyor animasyonu gösterir.
    /// </summary>
    public static void PrintLoading(string message = "İşleniyor")
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"\n  ⏳ {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Yükleme animasyonunu bitirir.
    /// </summary>
    public static void ClearLoading()
    {
        try
        {
            int width = Math.Max(20, Console.WindowWidth - 1);
            Console.Write("\r" + new string(' ', width) + "\r");
        }
        catch
        {
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Ayırıcı çizgi yazdırır.
    /// </summary>
    public static void PrintSeparator()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  {"─",52}");
        Console.ResetColor();
    }

    /// <summary>
    /// Hoşgeldin mesajı yazdırır.
    /// </summary>
    public static void PrintWelcome()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  🌟 Hoş geldiniz! Benimle doğal Türkçe ile konuşabilirsiniz.");
        Console.WriteLine("     Dosya işlemleri, sistem sorguları, PowerShell komutları");
        Console.WriteLine("     ve daha fazlası için buradayım.");
        Console.ResetColor();
        Console.WriteLine();
    }
}
