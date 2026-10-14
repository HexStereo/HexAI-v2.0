using System;
using System.Collections.Generic;
using System.Linq;

namespace HexAI.NLP;

/// <summary>
/// Kullanıcının niyetini (intent) algılayan sınıf.
/// Başlangıçta kural tabanlı (rule-based) çalışır.
/// İleride neural sınıflandırıcıya geçilebilir.
/// </summary>
public class IntentClassifier
{
    /// <summary>Algılanan niyet türleri.</summary>
    public enum Intent
    {
        /// <summary>Genel sohbet, selamlama, bilgi sorma</summary>
        Chat,
        /// <summary>Dosya işlemleri: aç, oku, yaz, sil, listele</summary>
        FileOperation,
        /// <summary>PowerShell komutu çalıştır</summary>
        RunCommand,
        /// <summary>Sistem bilgisi: tarih, saat, disk, RAM, CPU</summary>
        SystemInfo,
        /// <summary>Matematik hesaplama</summary>
        Math,
        /// <summary>Model eğitimi / veri ekleme</summary>
        Train,
        /// <summary>Model yönetimi: kaydet, yükle, durum</summary>
        ModelManage,
        /// <summary>Program çalıştır / uygulama aç</summary>
        OpenApplication,
        /// <summary>Yardım isteme</summary>
        Help,
        /// <summary>Belirlenemedi</summary>
        Unknown
    }

    /// <summary>Intent algılama sonucu.</summary>
    public class IntentResult
    {
        public Intent Intent { get; set; }
        public double Confidence { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new();
        public string OriginalInput { get; set; } = "";
    }

    // Anahtar kelime → intent eşlemeleri
    private static readonly Dictionary<Intent, string[]> _intentKeywords = new()
    {
        [Intent.FileOperation] = new[]
        {
            "dosya", "dosyala", "klasör", "dizin", "listele", "göster", "aç", "oku",
            "yaz", "oluştur", "sil", "kopyala", "taşı", "masaüstü", "desktop",
            "belgeler", "documents", "indir", "downloads", "dosya aç", "dosyaları",
            "klasörleri", "folder", "file", "directory", "ls", "dir"
        },
        [Intent.RunCommand] = new[]
        {
            "komut", "çalıştır", "komutu", "terminal", "powershell", "cmd", "execute",
            "run", "pip", "npm", "git", "yükle", "kur", "install"
        },
        [Intent.SystemInfo] = new[]
        {
            "saat", "tarih", "zaman", "gün", "disk", "ram", "bellek", "cpu", "işlemci",
            "sistem", "bilgisayar", "pc", "alan", "kapasite", "pil", "batarya",
            "ip", "ağ", "network", "kullanıcı", "hostname", "bilgi", "sürüm",
            "windows", "ne kadar yer", "boş alan", "dolu", "kullanım"
        },
        [Intent.Math] = new[]
        {
            "hesapla", "topla", "çıkar", "çarp", "böl", "karekök", "üs", "faktöriyel",
            "yüzde", "oran", "kaç eder", "kaçtır", "sonucu", "artı", "eksi",
            "çarpı", "bölü", "=", "+", "-", "*", "/", "^"
        },
        [Intent.Train] = new[]
        {
            "eğit", "öğren", "öğret", "train", "eğitim", "corpus", "veri",
            "dataset", "dosyadan öğren", "metin öğren", "eğitim ver"
        },
        [Intent.ModelManage] = new[]
        {
            "kaydet", "yükle", "model", "save", "load", "durum", "status",
            "ağırlık", "parametre", "vocab", "sözlük"
        },
        [Intent.OpenApplication] = new[]
        {
            "uygulama", "program", "başlat", "aç", "chrome", "firefox", "notepad",
            "explorer", "tarayıcı", "editör", "hesap makinesi", "calc",
            "müzik", "oynat", "spotify", "kod", "vscode"
        },
        [Intent.Help] = new[]
        {
            "yardım", "yardim", "help", "nasıl", "nasil", "ne yapabilirsin", "neler yapabilirsin",
            "neler yapabiliyorsun", "ne yapabiliyorsun", "ne özelliklere sahipsin", "ne ozelliklere sahipsin",
            "özelliklerin neler", "ozelliklerin neler", "yeteneklerin neler", "yeteneklerin", "komutlar",
            "neler yaparsın", "neler yaparsin", "özellikler", "ozellikler", "kullanım", "kullanim",
            "sen kimsin", "kendini tanıt", "kendini tanit", "kendinden bahset", "ne işe yararsın", "ne ise yararsin"
        },
        [Intent.Chat] = new[]
        {
            "merhaba", "selam", "naber", "nasılsın", "nasilsin", "günaydın", "gunaydin", "iyi akşamlar", "iyi aksamlar",
            "teşekkür", "tesekkur", "sağol", "sagol", "hoşçakal", "hoscakal", "görüşürüz", "gorusuruz", "bay bay",
            "anlat", "açıkla", "acikla", "nedir", "kimdir", "neden", "niçin", "nicin", "hakkında", "hakkinda",
            "fikrin", "düşüncen", "dusuncen", "sence", "bence"
        }
    };

    /// <summary>
    /// Kullanıcı girdisinden niyetini algılar.
    /// </summary>
    public IntentResult Classify(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new IntentResult { Intent = Intent.Unknown, Confidence = 0, OriginalInput = input };
        }

        string normalizedInput = input.ToLower(new System.Globalization.CultureInfo("tr-TR")).Trim();
        var result = new IntentResult { OriginalInput = input };

        // Öncelik 1: Özel komutları kontrol et (/ ile başlayanlar)
        if (normalizedInput.StartsWith("/"))
        {
            return ClassifySlashCommand(normalizedInput, result);
        }

        // Öncelik 2: Asistan yetenekleri, özellikleri ve kendini tanıtma soruları
        string[] capabilityQueries = new[]
        {
            "neler yapabiliyorsun", "ne yapabiliyorsun", "ne yapabilirsin", "neler yapabilirsin",
            "ne özelliklere sahipsin", "ne ozelliklere sahipsin", "özelliklerin neler", "ozelliklerin neler",
            "yeteneklerin neler", "yeteneklerin", "neler yaparsın", "neler yaparsin", "ne işe yararsın",
            "sen kimsin", "kendini tanıt", "kendini tanit", "kendinden bahset", "bana neler yapabileceğini anlat"
        };
        if (capabilityQueries.Any(q => normalizedInput.Contains(q)))
        {
            result.Intent = Intent.Help;
            result.Confidence = 1.0;
            return result;
        }

        // Öncelik 3: Matematiksel ifade kontrolü
        if (IsMathExpression(normalizedInput))
        {
            result.Intent = Intent.Math;
            result.Confidence = 0.9;
            result.Parameters["expression"] = normalizedInput;
            return result;
        }

        // Öncelik 4: Anahtar kelime eşleştirme
        var scores = new Dictionary<Intent, int>();
        foreach (var (intent, keywords) in _intentKeywords)
        {
            int score = 0;
            foreach (var keyword in keywords)
            {
                if (normalizedInput.Contains(keyword))
                {
                    score += keyword.Length; // Uzun eşleşmeler daha yüksek skor alır
                }
            }
            if (score > 0)
            {
                scores[intent] = score;
            }
        }

        if (scores.Count > 0)
        {
            var best = scores.OrderByDescending(kv => kv.Value).First();
            result.Intent = best.Key;
            result.Confidence = Math.Min(1.0, best.Value / 15.0); // Normalize

            // Dosya/klasör yolu çıkarma
            if (result.Intent == Intent.FileOperation)
            {
                ExtractFilePath(normalizedInput, result);
            }

            return result;
        }

        // Varsayılan: Sohbet
        result.Intent = Intent.Chat;
        result.Confidence = 0.3;
        return result;
    }

    /// <summary>
    /// Slash (/) komutu sınıflandırma.
    /// </summary>
    private IntentResult ClassifySlashCommand(string input, IntentResult result)
    {
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        string command = parts[0].ToLower();
        string args = parts.Length > 1 ? parts[1] : "";

        switch (command)
        {
            case "/train":
            case "/train-dir":
                result.Intent = Intent.Train;
                result.Confidence = 1.0;
                result.Parameters["path"] = args;
                result.Parameters["command"] = command;
                break;

            case "/save":
                result.Intent = Intent.ModelManage;
                result.Confidence = 1.0;
                result.Parameters["action"] = "save";
                result.Parameters["name"] = args;
                break;

            case "/load":
                result.Intent = Intent.ModelManage;
                result.Confidence = 1.0;
                result.Parameters["action"] = "load";
                result.Parameters["name"] = args;
                break;

            case "/models":
                result.Intent = Intent.ModelManage;
                result.Confidence = 1.0;
                result.Parameters["action"] = "list";
                break;

            case "/status":
                result.Intent = Intent.ModelManage;
                result.Confidence = 1.0;
                result.Parameters["action"] = "status";
                break;

            case "/ps":
                result.Intent = Intent.RunCommand;
                result.Confidence = 1.0;
                result.Parameters["command"] = args;
                break;

            case "/files":
                result.Intent = Intent.FileOperation;
                result.Confidence = 1.0;
                result.Parameters["action"] = "list";
                result.Parameters["path"] = args;
                break;

            case "/read":
                result.Intent = Intent.FileOperation;
                result.Confidence = 1.0;
                result.Parameters["action"] = "read";
                result.Parameters["path"] = args;
                break;

            case "/help":
                result.Intent = Intent.Help;
                result.Confidence = 1.0;
                break;

            case "/clear":
                result.Intent = Intent.Chat;
                result.Confidence = 1.0;
                result.Parameters["action"] = "clear";
                break;

            case "/history":
                result.Intent = Intent.Chat;
                result.Confidence = 1.0;
                result.Parameters["action"] = "history";
                break;

            case "/exit":
            case "/quit":
            case "/q":
                result.Intent = Intent.Chat;
                result.Confidence = 1.0;
                result.Parameters["action"] = "exit";
                break;

            default:
                result.Intent = Intent.Unknown;
                result.Confidence = 0.5;
                break;
        }

        return result;
    }

    /// <summary>
    /// Matematiksel ifade olup olmadığını kontrol eder.
    /// </summary>
    private bool IsMathExpression(string input)
    {
        // Sayı ve operatör ağırlıklı girdi kontrolü
        int digitCount = input.Count(char.IsDigit);
        int operatorCount = input.Count(c => "+-*/^%()".Contains(c));
        int totalRelevant = digitCount + operatorCount;

        // Girdinin %50'den fazlası rakam ve operatörden oluşuyorsa matematik
        if (input.Length > 0 && totalRelevant > input.Replace(" ", "").Length * 0.5)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Girdiden dosya yolunu çıkarmaya çalışır.
    /// </summary>
    private void ExtractFilePath(string input, IntentResult result)
    {
        // Windows yolu algılama (C:\..., D:\... gibi)
        var words = input.Split(' ');
        foreach (var word in words)
        {
            if (word.Length > 2 && word[1] == ':' && (word[2] == '\\' || word[2] == '/'))
            {
                result.Parameters["path"] = word;
                return;
            }
            // Özel klasör adları
            if (word == "masaüstü" || word == "desktop")
            {
                result.Parameters["path"] = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                return;
            }
            if (word == "belgeler" || word == "documents")
            {
                result.Parameters["path"] = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return;
            }
            if (word == "indirilenler" || word == "downloads")
            {
                result.Parameters["path"] = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                return;
            }
        }
    }
}

