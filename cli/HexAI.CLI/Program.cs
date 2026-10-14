using HexAI.Assistant;
using HexAI.Assistant.Config;

namespace HexAI.CLI;

/// <summary>
/// HexAI Kişisel Yapay Zeka Asistanı — Ana Giriş Noktası.
/// PowerShell veya komut isteminden çalıştırılır ve etkileşimli metin sohbeti sunar.
/// 
/// Kullanım:
///   dotnet run --project cli/HexAI.CLI
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        // Yapılandırmayı yükle
        var config = AssistantConfig.Load();
        config.VoiceOutputEnabled = false;

        // Banner ve hoşgeldin mesajı
        ConsoleUI.PrintBanner();
        ConsoleUI.PrintWelcome();

        // Asistan motorunu başlat
        ConsoleUI.PrintLoading("Asistan ve Yerel Yapay Zeka Motoru başlatılıyor...");
        var engine = new AssistantEngine(config);

        // Araştırma / araç durum güncellemelerini ekrana yaz
        engine.OnStatus += status =>
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"  {status}");
            Console.ResetColor();
        };

        // Motoru başlat (Öz Transformer, NLU, Bilgi Tabanı ve Araştırma Motoru)
        engine.Initialize();
        ConsoleUI.ClearLoading();

        // Yerel model durumu
        string modelName = config.LastModelName ?? "LargeGPT (Öz Transformer)";
        ConsoleUI.PrintStatus(engine.IsModelReady, modelName);

        // === ANA METİN SOHBET DÖNGÜSÜ (REPL) ===
        while (true)
        {
            ConsoleUI.PrintPrompt(config.AssistantName);

            // Kullanıcı girdisini oku
            string? input = Console.ReadLine();

            // EOF veya stdin kapanması durumunda döngüyü sonlandır
            if (input == null)
                break;

            if (string.IsNullOrWhiteSpace(input))
                continue;

            string trimmed = input.Trim().ToLower(new System.Globalization.CultureInfo("tr-TR"));

            // Çıkış kontrolü
            if (trimmed is "/exit" or "/quit" or "/q" or "çıkış" or "çık" or "exit" or "quit" or "kapat")
            {
                ConsoleUI.PrintResponse("Hoşça kal! 👋 Tekrar görüşmek üzere!", 
                    config.TypewriterEffect, config.TypewriterDelayMs);
                break;
            }

            try
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("  🤖 ");

                var responseBuffer = new System.Text.StringBuilder();

                await foreach (var token in engine.ProcessMessageStreamAsync(input.Trim()))
                {
                    responseBuffer.Append(token);
                    Console.Write(token);
                }

                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine();

                string fullResponse = responseBuffer.ToString().Trim();

                // "exit" özel sinyali
                if (fullResponse == "exit")
                {
                    ConsoleUI.PrintResponse("Hoşça kal! 👋 Tekrar görüşmek üzere!", false, 0);
                    break;
                }
            }
            catch (Exception ex)
            {
                ConsoleUI.PrintError($"Beklenmeyen hata: {ex.Message}");
            }
        }
    }
}
