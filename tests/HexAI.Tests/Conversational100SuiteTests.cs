using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using HexAI.Assistant;
using HexAI.Assistant.Config;
using Xunit;
using Xunit.Abstractions;

namespace HexAI.Tests;

/// <summary>
/// 100 Günlük Doğal Türkçe Konuşma ve Yaşam Sorusu Değerlendirme Paketi.
/// Modelin insan benzeri akıcılıkta, bilgili, empatik ve doğru yanıtlar verdiğini doğrular.
/// </summary>
public class Conversational100SuiteTests
{
    private readonly ITestOutputHelper _output;
    private static readonly AssistantEngine Engine;

    static Conversational100SuiteTests()
    {
        var config = AssistantConfig.Load();
        config.ApplyHardwareTier(HardwareTier.Standard);
        config.HiddenSize = 32;
        config.NumHeads = 1;
        config.NumLayers = 1;
        config.SequenceLength = 16;
        config.DefaultEpochs = 1;
        config.AutoWebResearch = false; // Yerel bilişsel beyin ve bilgi tabanı testi için hızlı mod
        Engine = new AssistantEngine(config);
        Engine.Initialize();
    }

    public Conversational100SuiteTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static readonly string[] Full100Questions = new[]
    {
        // ── 1. Selamlaşma, Hal Hatır & Asistan Kimliği (10 soru) ──
        "Selam nasılsın günün nasıl geçiyor?",
        "Günaydın bugün enerjin nasıl?",
        "İyi akşamlar ne yapıyorsun?",
        "Adın neydi senin tam olarak?",
        "Bana biraz kendinden bahseder misin?",
        "Yapay zeka mısın yoksa gerçek bir insan mı?",
        "Senin bir duygun var mı hissetmek nasıl bir şey?",
        "Seni kim yaptı nasıl geliştirildin?",
        "Hangi dilleri biliyorsun ve konuşabiliyorsun?",
        "Bana günlük hayatımda nasıl yardımcı olabilirsin?",

        // ── 2. Duygusal Durum, Moral & Motivasyon (10 soru) ──
        "Bugün canım çok sıkılıyor ne yapabilirim?",
        "Bugün çok yoruldum biraz moral ver bana.",
        "Kendimi biraz yalnız hissediyorum.",
        "Yarın önemli bir sınavım var çok stresliyim ne önerirsin?",
        "Çok tembel hissediyorum motivasyonumu nasıl artırırım?",
        "Kendime olan inancımı kaybettim biraz cesaret ver.",
        "İçimde sebepsiz bir huzursuzluk var nasıl rahatlarım?",
        "Hata yapmaktan çok korkuyorum bu korkuyu nasıl yenerim?",
        "Bugün çok güzel bir haber aldım sevincimi paylaşmak istedim!",
        "Tükenmişlik sendromu hissediyorum ne yapmalıyım?",

        // ── 3. Gündelik Yaşam, Alışkanlıklar & Verimlilik (10 soru) ──
        "Sence sabah erken uyanmak için en iyi taktik nedir?",
        "Daha verimli çalışmak için ne yapmalıyım?",
        "Uykum gelmiyor uyumak için ne yapabilirim?",
        "Yeni bir hobi edinmek istiyorum ne önerirsin?",
        "Hafta sonu evde yapılabilecek keyifli aktiviteler neler?",
        "Kitap okuma alışkanlığı nasıl kazanılır?",
        "Zamanımı daha iyi yönetmek için hangi yöntemi kullanabilirim?",
        "Pomodoro tekniği nedir nasıl uygulanır?",
        "Günde kaç litre su içmek gerekir?",
        "Su içmeyi sürekli unutuyorum ne yapabilirim?",

        // ── 4. Yemek, Tarifler & Mutfak İpuçları (10 soru) ──
        "Makarna nasıl yapılır?",
        "Bana güzel bir pirinç pilavı tarifi verir misin?",
        "Yumurta kaç dakikada haşlanır?",
        "Türk kahvesi nasıl köpüklü yapılır?",
        "Menemen soğanlı mı olur soğansız mı?",
        "Krep nasıl yapılır malzemeleri neler?",
        "Tavuk sote nasıl pişirilir?",
        "Evde pizza nasıl yapılır?",
        "Pratik bir akşam yemeği önerir misin?",
        "Çorba yaparken tuz fazla gelirse ne yapılır?",

        // ── 5. Teknoloji, Bilgisayar & Donanım (10 soru) ──
        "Bilgisayarım çok yavaşladı nasıl hızlandırabilirim?",
        "Windows'ta ekran görüntüsü nasıl alınır?",
        "RAM nedir bilgisayarda ne işe yarar?",
        "İşlemci ile ekran kartı arasındaki fark nedir?",
        "SSD ile HDD arasındaki fark nedir?",
        "Telefonumun şarjı çok çabuk bitiyor ne yapabilirim?",
        "Evde internet yavaşsa ne yapılabilir?",
        "Bilgisayar virüslerinden korunmak için ne yapmalıyım?",
        "Ekran başında göz yorgunluğunu önlemek için ne yapmalı?",
        "Monitör temizliği nasıl yapılmalıdır?",

        // ── 6. Yazılım, Kodlama & Kariyer (10 soru) ──
        "Python mu öğrenmeliyim C# mı?",
        "Yapay zeka öğrenmeye nereden başlamalıyım?",
        "Yazılımcı olmak için üniversite şart mı?",
        "GitHub nedir nasıl kullanılır?",
        "Web sitesi yapmak için hangi dilleri bilmek gerekir?",
        "Bir veritabanı nedir SQL ne işe yarar?",
        "Algoritma nedir ve neden bu kadar önemlidir?",
        "Kod yazarken motivasyonumu nasıl yüksek tutarım?",
        "Yazılımda Clean Code ve SOLID ne anlama gelir?",
        "Bir yazılımcı iş mülakatına nasıl hazırlanmalı?",

        // ── 7. Bilim, Doğa & Evren (10 soru) ──
        "Güneş sisteminde kaç tane gezegen var?",
        "Neden gökyüzü mavi görünür?",
        "Dünyanın en yüksek dağı hangisidir?",
        "Karadelik nedir kısaca anlatır mısın?",
        "Suyun kimyasal formülü nedir?",
        "Işık hızı saniyede kaç kilometredir?",
        "Dinozorlar neden yok oldu?",
        "Yerçekimi nasıl keşfedildi?",
        "Fotosentez nedir ne işe yarar?",
        "Ay neden hep aynı yüzünü gösterir?",

        // ── 8. Popüler İkilemler, Eğlence & Mizah (10 soru) ──
        "Bana komik bir fıkra anlatır mısın?",
        "Kısa ve güzel bir şiir yazar mısın?",
        "İzlemek için sürükleyici bir film önerir misin?",
        "Okumak için güzel bir bilim kurgu kitabı tavsiyesi ver.",
        "Bana ilginç ve şaşırtıcı bir bilgi söyle.",
        "Bir bilmece sorabilir misin cevabını sonra söyle?",
        "Tavuk mu yumurtadan çıkar yumurta mı tavuktan?",
        "Zaman yolculuğu sence mümkün mü?",
        "Uzayda başka canlılar veya uzaylılar var mıdır?",
        "Gelecekte robotlar ve yapay zeka dünyayı ele geçirir mi?",

        // ── 9. Felsefe, Mantık & Derin Sorular (10 soru) ──
        "Sence hayatın anlamı nedir?",
        "Mutluluk nedir ve insan nasıl mutlu olur?",
        "Bir karar verirken duygulara mı mantığa mı güvenmeli?",
        "Özgür irade gerçekten var mıdır?",
        "Başarı sence neye göre ölçülür?",
        "Zaman gerçekten akıyor mu yoksa bir yanılsama mı?",
        "Kader ile seçimlerimiz arasında nasıl bir bağ var?",
        "İnsan doğası gereği iyi midir kötü müdür?",
        "Yalnızlık ile tek başınalık arasındaki fark nedir?",
        "Bilinç nedir makineler bir gün bilinç kazanabilir mi?",

        // ── 10. Pratik Sağlık, Ev & Sosyal İlişkiler (10 soru) ──
        "Boğaz ağrısına evde ne iyi gelir?",
        "Baş ağrısını geçirmek için doğal yöntemler neler?",
        "Duruş bozukluğu nasıl düzeltilir kamburluk nasıl önlenir?",
        "İnsanlarla iletişim kurarken nelere dikkat edilmeli?",
        "Bir arkadaşıma kırıldım bunu ona nasıl söylemeliyim?",
        "Hayır demeyi nasıl öğrenebilirim?",
        "Topluluk önünde konuşma korkusu nasıl yenilir?",
        "Öfkemizi nasıl kontrol edebiliriz?",
        "Öz güven nasıl geliştirilir?",
        "Empati nedir ve insan ilişkilerinde neden bu kadar önemlidir?"
    };

    [Fact]
    public async Task TestAll100ConversationalResponsesIntegrity()
    {
        Assert.Equal(100, Full100Questions.Length);

        int successfulResponses = 0;
        var failedList = new List<string>();

        foreach (var question in Full100Questions)
        {
            var sb = new StringBuilder();
            await foreach (var token in Engine.ProcessMessageStreamAsync(question))
            {
                sb.Append(token);
            }
            string reply = sb.ToString().Trim();

            // Kalite Kriterleri:
            // 1. Cevap boş olmamalı
            // 2. Cevap en az 30 karakter derinliğinde olmalı (geçiştirilmemiş olmalı)
            // 3. 'Bilmiyorum' veya kaçamak yanıt içermemeli
            // 4. Robotik kalıp ('X konusu hakkında söyledikleriniz oldukça önemli ve mantıklı bir bakış açısı sunuyor') içermemeli
            bool isValid = !string.IsNullOrWhiteSpace(reply) &&
                           reply.Length >= 35 &&
                           !reply.ToLower().Contains("bilmiyorum") &&
                           !reply.Contains("oldukça önemli ve mantıklı bir bakış açısı sunuyor");

            if (isValid)
            {
                successfulResponses++;
            }
            else
            {
                failedList.Add($"Soru: {question} -> Yanıt: {reply}");
            }
        }

        _output.WriteLine($"🎯 100 Konuşma Testi Sonucu: {successfulResponses}/100 Başarılı!");

        if (failedList.Count > 0)
        {
            _output.WriteLine("❌ Başarısız / Yetersiz Yanıtlar:");
            foreach (var fail in failedList)
            {
                _output.WriteLine(fail);
            }
        }

        Assert.True(successfulResponses == 100, $"Tüm 100 soru başarıyla cevaplanmalıydı. Başarılı: {successfulResponses}/100");
    }

    [Fact]
    public async Task TestSampleSpotChecksPrintDetailedOutput()
    {
        string[] spotChecks = new[]
        {
            "Selam nasılsın günün nasıl geçiyor?",
            "Bugün canım çok sıkılıyor ne yapabilirim?",
            "Sence sabah erken uyanmak için en iyi taktik nedir?",
            "Bana komik bir fıkra anlatır mısın?",
            "Menemen soğanlı mı olur soğansız mı?",
            "Python mu öğrenmeliyim C# mı?",
            "Neden gökyüzü mavi görünür?",
            "Hayır demeyi nasıl öğrenebilirim?",
            "Sence hayatın anlamı nedir?",
            "Ay neden hep aynı yüzünü gösterir?"
        };

        foreach (var q in spotChecks)
        {
            var sb = new StringBuilder();
            await foreach (var token in Engine.ProcessMessageStreamAsync(q))
            {
                sb.Append(token);
            }
            string reply = sb.ToString().Trim();

            _output.WriteLine($"[Soru]: {q}");
            _output.WriteLine($"[Cevap]:\n{reply}");
            _output.WriteLine(new string('=', 60));

            Assert.True(reply.Length >= 50);
        }
    }

    [Fact]
    public async Task TestScreenshotQueries()
    {
        string[] queries = new[]
        {
            "nasilsin",
            "neler yapabiliyorsun",
            "ne ozelliklere sahipsin",
            "ne yapabilirsin",
            "yeteneklerin neler"
        };

        foreach (var q in queries)
        {
            var sb = new StringBuilder();
            await foreach (var token in Engine.ProcessMessageStreamAsync(q))
            {
                sb.Append(token);
            }
            string reply = sb.ToString().Trim();

            _output.WriteLine($"[QUERY]: {q}");
            _output.WriteLine($"[REPLY]: {reply}");
            _output.WriteLine(new string('=', 60));

            Assert.False(reply.Contains("uygun bir araç eşleşmedi"), $"Hata araç mesajı döndü: {q} -> {reply}");
            Assert.False(reply.StartsWith("Merhaba! Ben HexAI, bilgisayarınızda yerel çalışan, tamamen bağımsız"), $"Genel selamlama döndü: {q} -> {reply}");
            Assert.True(reply.Length >= 50);
        }
    }
}
