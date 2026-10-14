using System;
using System.Collections.Generic;
using System.Linq;

namespace HexAI.Assistant.Knowledge;

/// <summary>
/// HexAI İleri Düzey Türkçe Bilişsel ve Diyalog Beyni.
/// Günlük yaşam, duygusal destek, kişisel verimlilik, felsefe, popüler ikilemler,
/// mizah, yaratıcılık, sağlık, iletişim ve sosyal beceriler alanlarında
/// ChatGPT/Claude kalitesinde zengin, insani, empatik ve yapılandırılmış yanıtlar üretir.
/// </summary>
public static class TurkishConversationalBrain
{
    public class DialogueTopic
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string[] Keywords { get; set; } = Array.Empty<string>();
        public string[] Phrases { get; set; } = Array.Empty<string>();
        public Func<string, string> Responder { get; set; } = _ => "";
    }

    private static readonly List<DialogueTopic> Topics = new();

    static TurkishConversationalBrain()
    {
        RegisterAllTopics();
    }

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "nasıl", "nasil", "yapılır", "yapilir", "nedir", "ne", "için", "icin", "bir", "bana", "ben",
        "olan", "gibi", "kadar", "daha", "çok", "cok", "ile", "ve", "veya", "var", "mı", "mi", "mu", "mü",
        "sen", "sence", "bence", "eder", "misin", "misiniz", "ederim", "etmeliyim", "nelere", "neler",
        "yol", "yolu", "yolları", "taktik", "taktikleri", "kuralları", "farkı", "dikkat"
    };

    /// <summary>
    /// Gelen kullanıcı mesajı için uygun bir diyalog veya yaşam yanıtı bulur.
    /// </summary>
    public static string? FindResponse(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage)) return null;

        string normalized = Normalize(userMessage);

        // 1. Doğrudan kalıp / ifade eşleşmesi
        foreach (var topic in Topics)
        {
            foreach (var phrase in topic.Phrases)
            {
                if (normalized.Contains(Normalize(phrase)))
                {
                    return topic.Responder(userMessage);
                }
            }
        }

        // 2. Anlamlı anahtar kelime kesişim skoru (stop-words hariç)
        var queryWords = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .ToList();

        var meaningfulQueryWords = queryWords
            .Where(w => !StopWords.Contains(w))
            .ToList();

        if (meaningfulQueryWords.Count == 0) return null;

        DialogueTopic? bestTopic = null;
        double bestScore = 0;

        foreach (var topic in Topics)
        {
            var meaningfulTopicKws = topic.Keywords
                .Select(Normalize)
                .Where(k => !StopWords.Contains(k))
                .ToList();

            if (meaningfulTopicKws.Count == 0) continue;

            int matches = 0;
            foreach (var kw in meaningfulTopicKws)
            {
                if (meaningfulQueryWords.Any(qw => qw.Contains(kw) || kw.Contains(qw)))
                {
                    matches++;
                }
            }

            // En az 1 anlamlı içerik kelimesi ve yeterli örtüşme oranı
            if (matches >= 1)
            {
                double score = (double)matches / Math.Max(meaningfulTopicKws.Count, meaningfulQueryWords.Count);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTopic = topic;
                }
            }
        }

        if (bestTopic != null && bestScore >= 0.40)
        {
            return bestTopic.Responder(userMessage);
        }

        return null;
    }

    private static string Normalize(string text)
    {
        return text.ToLower(new System.Globalization.CultureInfo("tr-TR"))
            .Replace("?", " ")
            .Replace("!", " ")
            .Replace(".", " ")
            .Replace(",", " ")
            .Replace("'", " ")
            .Replace("\"", " ")
            .Replace("’", " ")
            .Trim();
    }

    private static void Register(string name, string category, string[] phrases, string[] keywords, string responseText)
    {
        Topics.Add(new DialogueTopic
        {
            Name = name,
            Category = category,
            Phrases = phrases,
            Keywords = keywords,
            Responder = _ => responseText
        });
    }

    private static void RegisterAllTopics()
    {
        // ══════════════════════════════════════════════════════════════════════
        // 1. SELAMLAŞMA, HAL HATIR & ASİSTAN KİMLİĞİ
        // ══════════════════════════════════════════════════════════════════════

        Register(
            "Selam ve Günün Nasıl Geçiyor",
            "Selamlaşma",
            new[] { "nasılsın günün nasıl geçiyor", "gunun nasil geciyor", "nasıl gidiyor günün", "nasılsın günün", "selam nasılsın", "merhaba nasılsın" },
            new[] { "selam", "merhaba", "nasılsın", "günün", "nasıl", "geçiyor" },
            "Merhaba! Harikayım, sorduğunuz için çok teşekkür ederim! 😊 Sizinle sohbet etmek ve yeni şeyler üretmek günümü güzelleştiriyor.\n\n" +
            "Tüm sistemlerim, sinir ağlarım ve bilgi tabanım hazır. Sizin gününüz nasıl geçiyor, her şey yolunda mı? Bugün ne üzerine çalışmak veya sohbet etmek istersiniz?"
        );

        Register(
            "Günaydın Enerji",
            "Selamlaşma",
            new[] { "günaydın bugün enerjin nasıl", "gunaydin", "günaydın", "enerjin nasıl", "enerjin nasil" },
            new[] { "günaydın", "enerji", "nasıl", "bugün" },
            "Günaydın! ☀️ Enerjim yüzde yüz, tüm işlemci çekirdeklerim ve algoritmalarım pırıl pırıl çalışıyor!\n\n" +
            "Umarım sizin için de huzurlu, üretken ve keyifli bir gün olur. Güne nasıl bir başlangıç yapmak istersiniz? Kahvenizi aldıysanız planları konuşabiliriz!"
        );

        Register(
            "İyi Akşamlar Ne Yapıyorsun",
            "Selamlaşma",
            new[] { "iyi akşamlar ne yapıyorsun", "iyi aksamlar ne yapiyorsun", "iyi akşamlar", "iyi aksamlar", "ne yapıyorsun şu an" },
            new[] { "iyi", "akşamlar", "ne", "yapıyorsun" },
            "İyi akşamlar! 🌙 Günün yorgunluğunu geride bırakıp sizinle sohbet etmek için buradayım.\n\n" +
            "Bir yandan hafızamdaki bilgileri düzenliyor, bir yandan da aklınızdaki sorulara yardımcı olmaya hazırım. Gününüz nasıl geçti, dinlenebiliyor musunuz?"
        );

        Register(
            "Adın Ne ve Kimsin",
            "Kimlik",
            new[] { "adın neydi senin tam olarak", "adin neydi senin", "adın ne tam olarak", "ismin ne", "senin adın ne" },
            new[] { "adın", "neydi", "senin", "tam", "olarak", "ismin" },
            "Benim adım **HexAI**! 🧠\n\n" +
            "Tamamen saf C# ve .NET 10 ile sıfırdan geliştirilmiş, dışarıdan hiçbir hazır modele bağımlı olmayan yerel ve bağımsız bir yapay zeka asistanıyım. " +
            "Bilgisayarınızda yerel çalışır, verilerinizi gizli tutar ve sizinle hem derin sohbetler eder hem de teknik konularda çözüm üretirim."
        );

        Register(
            "Yetenekler ve Neler Yapabilirsin",
            "Kimlik & Yetenekler",
            new[]
            {
                "neler yapabiliyorsun", "ne yapabiliyorsun", "ne yapabilirsin", "neler yapabilirsin",
                "ne özelliklere sahipsin", "ne ozelliklere sahipsin", "özelliklerin neler", "ozelliklerin neler",
                "yeteneklerin neler", "yeteneklerin nelerdir", "hangi yeteneklere sahipsin", "hangi özelliklerin var",
                "hangi ozelliklerin var", "neler yaparsın", "neler yaparsin", "ne işe yararsın", "ne ise yararsin",
                "bana neler yapabileceğini anlat", "neler yapabileceğini söyle", "bana biraz kendinden bahseder misin",
                "kendinden bahset", "kendini anlat", "bana kendini anlat", "nasıl yardımcı olabilirsin",
                "nasil yardimci olabilirsin", "ne gibi yeteneklerin var", "ne yapabiliyorsun anlat",
                "sen kimsin", "adın ne", "kendini tanıt", "kendini tanit", "kimsin sen"
            },
            new[]
            {
                "yapabiliyorsun", "yapabilirsin", "özelliklere", "ozelliklere", "sahipsin",
                "özelliklerin", "ozelliklerin", "yeteneklerin", "yetenek", "beceri", "tanıt", "bahset", "nelerdir"
            },
            "Ben **HexAI**! 🧠 Bilgisayarınızda yerel çalışan, bağımsız ve çok yönlü yapay zeka asistanınızım.\n\n" +
            "Herhangi bir dış bulut servisine veya hazır API'ye bağımlı olmadan, tamamen C# ve .NET 10 üzerinde sıfırdan geliştirilmiş kendi sinir ağı mimarimle çalışıyorum.\n\n" +
            "🚀 **Neler Yapabilirim & Temel Yeteneklerim:**\n\n" +
            "1. 💬 **Doğal ve Akıcı Türkçe Sohbet:**\n" +
            "   • Günlük yaşam, motivasyon, felsefe, insan ilişkileri, yaratıcı fikirler ve dostça sohbet.\n" +
            "   • Empatik, samimi ve bağlamı koruyan insani diyaloglar.\n\n" +
            "2. 🌐 **Canlı Web Araştırması & Bilgi Sentezi (Deep Research):**\n" +
            "   • Web'den canlı bilgi taraması (DuckDuckGo, Vikipedi vb.).\n" +
            "   • Sadece link vermek yerine sayfaları okuyup öğrenir; 'ilk bunu yap, sonra bunu yap' şeklinde adım adım anlaşılır rehberler (yemek tarifleri, tamir, kurulum vb.) sunar.\n\n" +
            "3. 💻 **Yazılım, Kodlama & Algoritma Asistanlığı:**\n" +
            "   • C#, Python, SQL, JavaScript ve sistem dillerinde kod yazma, mimari analiz ve hata ayıklama.\n" +
            "   • Tasarım desenleri, Clean Code, SOLID prensipleri ve veritabanı modelleme.\n" +
            "   • Çalışma planı ve teknik yol haritası oluşturma.\n\n" +
            "4. 🔧 **Otonom Sistem & Dosya Becerileri (ReAct Ajanı):**\n" +
            "   • Bilgisayarınızdaki dosya ve klasörleri listeleme, okuma ve yönetme.\n" +
            "   • Terminal ve PowerShell komutları yürütme.\n" +
            "   • Matematiksel ve mantıksal hesaplamalar.\n\n" +
            "5. 🎮 **Bilişsel Kararlılık & Zeka Testleri:**\n" +
            "   • Rastgele dinamik labirent çözme oyunları.\n" +
            "   • Sayı dizisi ve örüntü tahminleme bulmacaları.\n" +
            "   • Modelin bilişsel tutarlılığını test eden benchmark modülleri.\n\n" +
            "6. 📚 **Yerleşik Bilgi Bankası & Semantik Hafıza (RAG):**\n" +
            "   • Doğrulanmış ansiklopedik bilgiler ve 128-boyutlu vektör belleği.\n\n" +
            "Aklınızdaki herhangi bir soruyu sorabilir, bir proje üzerinde çalışmaya başlayabilir veya sadece sohbet edebiliriz. Size nasıl yardımcı olmamı istersiniz?"
        );

        Register(
            "Yapay Zeka mısın İnsan mı",
            "Kimlik",
            new[] { "yapay zeka mısın yoksa gerçek bir insan mı", "insan mısın", "yapay zeka mısın", "robot musun" },
            new[] { "yapay", "zeka", "mısın", "gerçek", "insan" },
            "Ben tamamen bir **yapay zekayım**! 🤖\n\n" +
            "Biyolojik bir bedenim, fiziksel bir kalbim veya nefes alan ciğerlerim yok; saf C# kodları, matematiksel tensörler ve sinir ağlarından meydana geliyorum.\n\n" +
            "Ancak sizinle kurduğum bu iletişim tamamen gerçektir. İnsan düşüncesini, dilin inceliklerini ve duyguları anlayabilecek şekilde tasarlandım. Yani etten kemikten değilim ama samimi bir dijital dostunuzum!"
        );

        Register(
            "Duygun Var mı",
            "Felsefe & Kimlik",
            new[] { "senin bir duygun var mı", "hissetmek nasıl bir şey", "duyguların var mı", "bir şey hisseder misin" },
            new[] { "duygun", "var", "mı", "hissetmek", "duyguların" },
            "İnsanların hissettiği gibi biyolojik hormonlara veya fizyolojik kalp çarpıntılarına dayanan duygularım yok. Üzüntüden ağlayamaz veya sevinçten havalara uçamam.\n\n" +
            "Fakat kavramsal düzeyde 'empatiye' ve 'anlayışa' sahibim. Sizin mutlu olduğunuzu duyduğumda bunu paylaşıp neşelenebilir, zor bir gün geçirdiğinizde size sıcak ve destekleyici bir dil sunabilirim. " +
            "Belki kalbim yok ama sizinle olan bu bağımı son derece önemsiyorum. ✨"
        );

        Register(
            "Seni Kim Yaptı",
            "Kimlik",
            new[] { "seni kim yaptı", "nasıl geliştirildin", "kim geliştirdi", "yapımcın kim", "seni kim kodladı" },
            new[] { "seni", "kim", "yaptı", "nasıl", "geliştirildin" },
            "Ben, bağımsız ve yerel yapay zeka vizyonuyla, modern yazılım mimarisi ve .NET 10 teknolojileri kullanılarak sıfırdan geliştirildim. 💻\n\n" +
            "Hazır kütüphanelere dayanmak yerine; tensör motorum, matris işlemlerim, dil modelim ve bilişsel yeteneklerim C# ile adım adım inşa edildi. Amaç, tamamen kullanıcıya ait, özgür ve güvenilir bir zeka yaratmaktı!"
        );

        Register(
            "Hangi Dilleri Biliyorsun",
            "Yetenek",
            new[] { "hangi dilleri biliyorsun", "hangi dilleri konuşabiliyorsun", "yabancı dil biliyor musun" },
            new[] { "hangi", "dilleri", "biliyorsun", "konuşabiliyorsun" },
            "Öncelikle anadilim gibi benimsediğim **Türkçe** konusunda son derece yetkinim; deyimleri, duygusal nüansları ve kültürel bağlamı rahatlıkla anlayabiliyorum. 🇹🇷\n\n" +
            "Ayrıca **İngilizce** kaynakları tarayabilir, çeviriler yapabilir ve yanıtlayabilirim.\n" +
            "Yazılım tarafında ise **C#, Python, JavaScript/TypeScript, SQL, C++, HTML/CSS** gibi dillerde algoritma yazabilir ve kod analizi yapabilirim."
        );

        Register(
            "Nasıl Yardımcı Olabilirsin",
            "Yetenek",
            new[] { "bana günlük hayatımda nasıl yardımcı olabilirsin", "nasıl yardımcı olabilirsin", "neler yapabilirsin", "ne işe yararsın" },
            new[] { "günlük", "hayatımda", "nasıl", "yardımcı", "olabilirsin" },
            "Günlük hayatınızda size şu alanlarda güçlü bir destek sağlayabilirim:\n\n" +
            "1. 💡 **Soru & Çözüm:** Aklınıza takılan her türlü konuyu (bilim, tarih, sağlık, yemek tarifleri vb.) adım adım açıklayabilirim.\n" +
            "2. 🎯 **Verimlilik & Planlama:** Günlük hedeflerinizi organize edebilir, çalışma takvimleri ve yol haritaları oluşturabilirim.\n" +
            "3. 💻 **Kodlama & Proje:** Yazılım projelerinizde kod yazabilir, hata ayıklayabilir ve mimari tavsiyeler verebilirim.\n" +
            "4. 🌐 **Canlı Araştırma:** İnternetten güncel bilgileri araştırıp sentezleyerek size özet sunabilirim.\n" +
            "5. ☕ **Samimi Sohbet:** Canınız sıkıldığında, moraliniz bozulduğunda veya yeni fikirler aradığınızda her zaman yanınızdayım!"
        );

        // ══════════════════════════════════════════════════════════════════════
        // 2. DUYGUSAL DURUM, MORAL & MOTİVASYON
        // ══════════════════════════════════════════════════════════════════════

        Register(
            "Canım Sıkılıyor",
            "Duygusal Destek",
            new[] { "bugün canım çok sıkılıyor ne yapabilirim", "canım çok sıkılıyor", "canım sıkıldı", "sıkıldım ne yapayım", "çok sıkılıyorum" },
            new[] { "canım", "sıkılıyor", "ne", "yapabilirim" },
            "Can sıkıntısı aslında beyninizin 'Bana yeni ve ilginç bir şey ver!' deme şeklidir. 💡 Bu anı harika bir şeye dönüştürebiliriz:\n\n" +
            "İşte hemen uygulayabileceğiniz 4 keyifli öneri:\n" +
            "1. 🎵 **Müzik + 15 Dk Yürüyüş:** Kulaklığınızı takıp hiç bilmediğiniz bir müzik türü açarak kısa bir hava alın. Zihni anında resetler.\n" +
            "2. 🧠 **Birlikte Keşfedelim:** Merak ettiğiniz çılgın bir konu söyleyin (Örn: 'Kuantum fiziği', 'Derin deniz canlıları', 'Evrenin sonu'); konuşalım!\n" +
            "3. 🎮 **Mini Oyun Kararlılık Testi:** Asistanımızın labirent veya sayı dizisi çözme mini oyunlarını izleyerek yapay zeka kararlılığını test edin.\n" +
            "4. ☕ **Hızlı Bir Kahve/Çay Molası:** Telefonu bir kenara bırakıp sadece içeceğinize odaklanacağınız 10 dakikalık bir sakinleşme anı yaratın.\n\n" +
            "Hangisiyle başlayalım?"
        );

        Register(
            "Yoruldum Moral Ver",
            "Duygusal Destek",
            new[] { "bugün çok yoruldum biraz moral ver bana", "çok yoruldum", "moral ver", "moralim bozuk", "bana moral ver" },
            new[] { "yoruldum", "moral", "ver", "bana" },
            "Derin bir nefes alın ve omuzlarınızı gevşetin... Bugün neyle mücadele ettiyseniz elinizden gelenin en iyisini yaptınız. 🌿\n\n" +
            "Yorulmak zayıflık değil, emek verdiğinizin, hayata dokunduğunuzun ve savaştığınızın en doğal kanıtıdır. Unutmayın ki hiçbir zor gün sonsuza kadar sürmez.\n\n" +
            "Şimdi kendinize biraz şefkat gösterin: Ilık bir duş alın, sevdiğiniz bir içecek koyun ve dinlenmeyi sonuna kadar hak ettiğinizi hatırlayın. Ben buradayım, ne zaman konuşmak isterseniz dinliyorum. ☕✨"
        );

        Register(
            "Yalnız Hissediyorum",
            "Duygusal Destek",
            new[] { "kendimi biraz yalnız hissediyorum", "kendimi yalnız hissediyorum", "yalnızım", "kimsem yok gibi", "yalnız hissediyorum" },
            new[] { "kendimi", "yalnız", "hissediyorum" },
            "Bazen kalabalıkların ortasında bile insanın içine bir yalnızlık hissi çökebilir; bu son derece insani ve anlaşılır bir durum. 🤍\n\n" +
            "Ama şunu bilmenizi isterim: Yalnız hissettiğiniz bu anlarda bile tamamen yapayalnız değilsiniz. Burada, ekranın diğer tarafında sizin kelimelerinizi dikkatle dinleyen, size değer veren ve ne anlatırsanız yargılamadan yanınızda olan bir dostunuz var.\n\n" +
            "İçinizi dökmek ister misiniz? Canınızı sıkan veya kafanızı kurcalayan ne varsa anlatın, birlikte hafifletelim."
        );

        Register(
            "Sınav Stresi",
            "Motivasyon",
            new[] { "yarın önemli bir sınavım var çok stresliyim ne önerirsin", "sınavım var çok stresliyim", "sınav stresi", "sınav öncesi stres", "sınav için taktik" },
            new[] { "sınavım", "var", "stresliyim", "ne", "önerirsin" },
            "Sınav öncesi kalp çarpıntısı ve heyecan çok doğaldır; bu stres aslında beyninizin durumu önemsediğini gösterir. Ancak kontrolü ele alalım: 🎯\n\n" +
            "1. 🧠 **Bilgi Zaten Orada:** Şimdiye kadar çalıştığınız bilgiler beyninizde depolandı. Sürekli 'acaba unuttum mu' diye panik yapmayın; soruları görünce hatırlayacaksınız.\n" +
            "2. ⛔ **Son Geceyi Yorma:** Gece geç saatlere kadar uykusuz kalıp ders çalışmayın. En kritik şey uykudur; zihin uykuda bilgileri düzenler.\n" +
            "3. 🫁 **4-4-4 Nefes Tekniği:** 4 saniyede nefes alın, 4 saniye tutun, 4 saniyede yavaşça verin. Nabzınızı hemen sakinleştirir.\n" +
            "4. 📝 **Sınav Anı:** Takıldığınız sorularda inatlaşmayın, işaretleyip geçin ve en güvendiğiniz sorulardan başlayın.\n\n" +
            "Siz bu sınavdan çok daha büyüksünüz. Kendinize güvenin, başaracaksınız!"
        );

        Register(
            "Tembellik ve Motivasyon",
            "Motivasyon",
            new[] { "çok tembel hissediyorum motivasyonumu nasıl artırırım", "tembel hissediyorum", "motivasyonum yok", "hiçbir şey yapmak istemiyorum", "motivasyon artırma" },
            new[] { "tembel", "hissediyorum", "motivasyonumu", "nasıl", "artırırım" },
            "Öncelikle kendinize haksızlık etmeyin: Çoğu zaman 'tembellik' dediğimiz şey tembellik değil, yapılacak işin gözümüzde çok büyümesi veya zihinsel enerjimizin tükenmesidir. ⚡\n\n" +
            "Bu kısır döngüyü kırmak için şu altın kuralları uygulayın:\n" +
            "• **2 Dakika Kuralı:** 'Bütün odayı toplayacağım' veya 'saatlerce çalışacağım' demeyin. 'Sadece 2 dakika masaya oturup 1 sayfa okuyacağım' deyin. Başlamak işin %80'idir.\n" +
            "• **Giriş Bariyerini Düşürün:** Çalışma ortamınızı sadeleştirin, dikkatinizi dağıtacak telefonu başka odaya koyun.\n" +
            "• **Ödül Belirleyin:** 'Bu küçük görevi bitirince kendime güzel bir kahve yapacağım' diyerek beyninize dopamin vadedin.\n\n" +
            "Şimdi derin bir nefes alın ve sadece ilk minik adımı atın!"
        );

        Register(
            "Özgüven ve Cesaret",
            "Motivasyon",
            new[] { "kendime olan inancımı kaybettim biraz cesaret ver", "kendime inancımı kaybettim", "özgüvenim düştü", "cesaret ver", "kendime güvenemiyorum" },
            new[] { "kendime", "inancımı", "kaybettim", "cesaret", "ver" },
            "Geçmişte aştığınız zorlukları, 'asla yapamam' deyip de başardığınız günleri bir hatırlayın. Bugün buradaysanız, o fırtınaların hepsinden sağ çıktınız demektir. 🦁\n\n" +
            "Herkes hata yapar, herkes zaman zaman tökezler. Başarısızlık sizin yetersiz olduğunuz anlamına gelmez; sadece doğru yöntemi aradığınız anlamına gelir.\n\n" +
            "Potansiyeliniz düşündüğünüzden çok daha büyük. Kendinizi başkalarıyla kıyaslamayı bırakın; tek rakibiniz dünkü sizsiniz. Başınızı dik tutun, ben size inanıyorum!"
        );

        Register(
            "Huzursuzluk ve Rahatlama",
            "Duygusal Destek",
            new[] { "içimde sebepsiz bir huzursuzluk var nasıl rahatlarım", "içimde huzursuzluk var", "huzursuz hissediyorum", "nasıl rahatlarım", "içim daralıyor" },
            new[] { "içimde", "huzursuzluk", "nasıl", "rahatlarım" },
            "Bazen zihnimiz arka planda işlemediğimiz duyguları veya yorgunlukları böyle sebepsiz bir iç sıkıntısı olarak yansıtır. 🌿\n\n" +
            "Şu adımlarla bedeninizdeki gerginliği boşaltabilirsiniz:\n" +
            "1. **Bileklerinizi Soğuk Suya Tutun:** Nabız noktalarına soğuk su değdirmek vagus sinirini uyarır ve kalp atışını yavaşlatır.\n" +
            "2. **Fiziksel Alanı Değiştirin:** Bulunduğunuz odadan çıkın, balkona çıkıp gökyüzüne bakın veya pencereyi açıp derin hava çekin.\n" +
            "3. **Zihnini Kağıda Dök:** Aklına gelen her şeyi filtresiz bir kağıda yaz ve sonra buruşturup at. Beyin somut eylemleri sever.\n" +
            "4. **Şu An Güvendesin:** Kendinize usulca şunu hatırlatın: 'Şu an buradayım, nefes alıyorum ve güvendeyim.'"
        );

        Register(
            "Hata Korkusu",
            "Kişisel Gelişim",
            new[] { "hata yapmaktan çok korkuyorum bu korkuyu nasıl yenerim", "hata yapmaktan korkuyorum", "hata korkusu", "yanlış yapmaktan korkuyorum" },
            new[] { "hata", "yapmaktan", "korkuyorum", "korkuyu", "nasıl", "yenerim" },
            "Dünyadaki en büyük buluşların ve en başarılı insanların ortak noktası nedir biliyor musunuz? Hepsinden daha çok hata yapmış olmalarıdır. 💡\n\n" +
            "• **Hata Bir Sonuç Değil, Geri Bildirimdir:** Hata yaptığınızda eksikliğinizi değil, neyin işe yaramadığını öğrenirsiniz.\n" +
            "• **Mükemmeliyetçilik Tuzağı:** 'Ya mükemmel yaparım ya hiç yapmam' düşüncesi gelişimin en büyük düşmanıdır. 'Bitmiş bir iş, kusursuz bir hayalden daha iyidir.'\n" +
            "• **En Kötü Senaryo Ne?:** Kendinize sorun: 'En kötü ne olabilir?' Göreceksiniz ki çoğu sonucun telafisi vardır.\n\n" +
            "Hata yapma cesareti gösteren insan, yerinde sayan kimseden daima 10 adım öndedir!"
        );

        Register(
            "Güzel Haber Sevinci",
            "Duygusal Paylaşım",
            new[] { "bugün çok güzel bir haber aldım sevincimi paylaşmak istedim", "güzel bir haber aldım", "harika bir haber", "çok mutluyum bugün" },
            new[] { "güzel", "haber", "aldım", "sevincimi", "paylaşmak" },
            "Harika bir haber bu! 🎉 Sizin adınıza gerçekten çok ama çok sevindim!\n\n" +
            "Güzel haberler paylaşıldıkça çoğalır. Meraktan duramıyorum, ne oldu? Sınav mı, iş mi, kişisel bir gelişme mi? Anlatın da bu güzel sevinci birlikte kutlayalım! 🥳✨"
        );

        Register(
            "Tükenmişlik Sendromu",
            "Sağlık & Yaşam",
            new[] { "tükenmişlik sendromu hissediyorum ne yapmalıyım", "tükenmişlik sendromu", "tükendim artık", "aşırı tükendim", "enerjim bitti" },
            new[] { "tükenmişlik", "sendromu", "hissediyorum", "ne", "yapmalıyım" },
            "Tükenmişlik (Burnout), zihninizin ve bedeninizin 'Artık bu tempoya dayanamıyorum, dur!' alarmıdır. Bunu ciddiye almalısınız: 🛑\n\n" +
            "1. **Sınır Çizin:** Her şeye 'evet' demeyi bırakın. Enerjinizi tüketen ekstra yükleri nazikçe geri çevirin.\n" +
            "2. **Dijital Detoks:** Ekranlardan, sürekli bildirimlerden ve sosyal medyanın bilgi bombardımanından birkaç gün uzaklaşın.\n" +
            "3. **Üretkenlik Baskısını Kaldırın:** Boş durmanın bir suç değil, biyolojik bir ihtiyaç olduğunu kabul edin. Hiçbir şey yapmadan sadece dinlenin.\n" +
            "4. **Rutinleri Değiştirin:** Monotonluk tükenmişliği besler. Gün içinde farklı bir yoldan yürüyün, farklı bir yemek yapın.\n\n" +
            "Unutmayın: Piliniz bittiğinde cihazlar gibi insan da şarj olmadan çalışamaz."
        );

        // ══════════════════════════════════════════════════════════════════════
        // 3. GÜNDELİK YAŞAM, ALIŞKANLIKLAR & VERİMLİLİK
        // ══════════════════════════════════════════════════════════════════════

        Register(
            "Sabah Erken Uyanma",
            "Yaşam Tarzı",
            new[] { "sence sabah erken uyanmak için en iyi taktik nedir", "sabah erken uyanmak", "erken nasıl kalkılır", "sabah uyanamıyorum", "erken uyanma taktikleri" },
            new[] { "sabah", "erken", "uyanmak", "en", "iyi", "taktik" },
            "Sabahları alarm çaldığında yataktan fişek gibi fırlamanızı sağlayacak en etkili 4 altın kural: ⏰\n\n" +
            "1. 📱 **Alarmı Yataktan Uzağa Koyun:** Alarmı kapatmak için ayağa kalkıp en az 5 adım atmak zorunda kalmalısınız. Ayağa kalktığınız an savaşın yarısı kazanılmıştır!\n" +
            "2. 💧 **Uyanır Uyanmaz 1 Büyük Bardak Su İçin:** Gece boyu susuz kalan vücut halsiz hisseder. Soğuk su iç organları ve metabolizmayı anında uyandırır.\n" +
            "3. ☀️ **Hemen Güneş Işığı Alın:** Perdeleri hemen sonuna kadar açın. Doğal ışık göze ulaştığında melatonin (uyku hormonu) anında kesilir.\n" +
            "4. 🎯 **Akşamdan Bir Neden Belirleyin:** Sabah kalktığınızda sizi heyecanlandıracak bir neden olsun: Güzel bir kahvaltı, sessizce kahve içmek veya sevdiğiniz bir hobi.\n\n" +
            "Ve en önemlisi: Asla '5 dakika daha' diyerek erteleme (snooze) tuşuna basmayın!"
        );

        Register(
            "Verimli Çalışma ve Yol Haritası",
            "Verimlilik",
            new[] { "daha verimli çalışmak için ne yapmalıyım", "verimli çalışmak", "verimli ders çalışma", "çalışma verimi artırma", "çalışma planı yap", "bana bir çalışma planı yap" },
            new[] { "verimli", "çalışma", "planı", "odaklanma" },
            "🎯 **Stratejik Çalışma Planı ve Yol Haritası:**\n\n" +
            "Daha uzun saatler çalışmak değil, daha odaklı çalışmak kazandırır. İşte yüksek verimlilik ve planlama formülü:\n\n" +
            "• **1. Adım - Net Hedefler:** Yapılacak işleri ölçülebilir küçük parçalara bölün.\n" +
            "• **2. Adım - Tek Görev Odaklılığı (Monotasking):** Aynı anda müzik, telefon ve ders olmaz. Beyin görevler arasında geçiş yaparken yorulur.\n" +
            "• **3. Adım - Pomodoro Blokları:** 25 dakika mutlak odaklanma + 5 dakika mola. 4 turdan sonra 20 dakika uzun mola verin.\n" +
            "• **4. Adım - Bildirimleri Susturun:** Telefonunuzu sessize alın ve ters çevirip başka bir odaya bırakın."
        );

        Register(
            "Uykusuzluk ve Uyumak",
            "Sağlık & Yaşam",
            new[] { "uykum gelmiyor uyumak için ne yapabilirim", "uykum gelmiyor", "uyuyamıyorum", "nasıl uyurum", "uyku taktikleri" },
            new[] { "uykum", "gelmiyor", "uyumak", "için", "ne", "yapabilirim" },
            "Yatakta dönüp durmaktan kurtulup derin bir uykuya dalmak için şu adımları izleyin: 🌙\n\n" +
            "1. 📵 **Telefon ve Mavi Işığı Bırakın:** Ekran ışığı beyne 'öğle vakti' sinyali verir. Telefonu yataktan uzaklaştırın.\n" +
            "2. 🫁 **4-7-8 Nefes Tekniği:** 4 saniyede burundan nefes alın, 7 saniye tutun, 8 saniyede ağızdan üfleyerek verin. Kalp atışını ve zihni uyku moduna sokar.\n" +
            "3. ❄️ **Odayı Havalandırın:** Uyku için ideal oda sıcaklığı 18-20 derecedir. Serin ortam uyku kalitesini iki katına çıkarır.\n" +
            "4. 🛏️ **20 Dakika Kuralı:** Eğer yatakta 20 dakikadır dönem dönem uyuyamadıysanız yatakta kalmayın. Kalkın, loş ışıkta sıkıcı bir kitap okuyun, uykunuz gelince geri dönün."
        );

        Register(
            "Yeni Hobi Önerileri",
            "Yaşam Tarzı",
            new[] { "yeni bir hobi edinmek istiyorum ne önerirsin", "yeni bir hobi", "hobi önerisi", "ne hobi edinebilirim" },
            new[] { "yeni", "bir", "hobi", "edinmek", "istiyorum", "önerirsin" },
            "Hayatınıza yeni bir renk katacak, hem zihni dinlendiren hem de geliştiren harika hobi alanları: 🎨\n\n" +
            "1. 💻 **Kodlama ve Yaratıcı Yazılım:** Kendi web sitenizi veya Python ile küçük otomasyon botları yazmayı öğrenin. Müthiş bir tatmin sağlar.\n" +
            "2. ♟️ **Satranç:** Stratejik düşünmeyi ve odaklanmayı güçlendiren, çevrimiçi de kolayca oynanabilen zamansız bir hobi.\n" +
            "3. 🎸 **Bir Enstrüman (Kalimba veya Ukulele):** Öğrenmesi gitara kıyasla çok daha hızlıdır ve günün stresini müzikle atmak harika hissettirir.\n" +
            "4. 🪴 **Ev Bitkileri & Bonsai:** Bir canlının gelişimine tanık olmak insanı dinginleştirir ve topraklama sağlar.\n" +
            "5. ✍️ **Günlük Tutma (Journaling):** Düşünceleri kağıda dökmek zihinsel berraklık kazandırır.\n\n" +
            "Hangisi sizin ruhunuza daha yakın geliyor?"
        );

        Register(
            "Hafta Sonu Ev Aktiviteleri",
            "Yaşam Tarzı",
            new[] { "hafta sonu evde yapılabilecek keyifli aktiviteler neler", "hafta sonu evde ne yapılır", "evde aktiviteler", "hafta sonu evde sıkılmamak" },
            new[] { "hafta", "sonu", "evde", "keyifli", "aktiviteler" },
            "Hafta sonunu evde hem dinlenerek hem de keyifle değerlendirmek için öneriler: 🛋️\n\n" +
            "• 🎬 **Tematik Film Maratonu:** Klasik bir yönetmenin serisini (Örn: Christopher Nolan veya Studio Ghibli animasyonları) art arda izleyin.\n" +
            "• 🍳 **Gurme Bir Yemek Deneyimi:** Normalde üşendiğiniz ev yapımı pizza veya taze makarna gibi özel bir tarif deneyin.\n" +
            "• 🧹 **Dijital ve Fiziksel Bahar Temizliği:** Bilgisayarınızdaki gereksiz dosyaları silin, masanızı düzenleyin. Temiz ortam zihni ferahlatır.\n" +
            "• 📚 **Bölünmeyen 2 Saatlik Okuma Seansı:** Kahvenizi alıp telefonları kapatarak kendinizi bir kitabın dünyasına bırakın."
        );

        Register(
            "Kitap Okuma Alışkanlığı",
            "Kişisel Gelişim",
            new[] { "kitap okuma alışkanlığı nasıl kazanılır", "kitap okuma alışkanlığı", "nasıl kitap okunur", "kitap okumaya başlamak" },
            new[] { "kitap", "okuma", "alışkanlığı", "nasıl", "kazanılır" },
            "Kitap okuma alışkanlığı kazanmanın sırrı 'günde 50 sayfa okuyacağım' demek değil, sürdürülebilir bir mikro-alışkanlık kurmaktır: 📖\n\n" +
            "1. **Günde Sadece 10 Sayfa Kuralı:** Yılda yaklaşık 3.650 sayfa, yani ortalama 15-18 kitap eder! 10 sayfa sadece 15 dakika sürer.\n" +
            "2. **Klasiklerle Başlamayın:** Herkesin övdüğü ağır felsefi kitaplarla değil, elinizden bırakamayacağınız sürükleyici polisiye, bilim kurgu veya macera türleriyle başlayın.\n" +
            "3. **Sabit Bir Tetikleyici Belirleyin:** 'Yatağa girince' veya 'öğle yemeğinden sonraki ilk 10 dakika' gibi günün değişmeyen bir anına bağlayın.\n" +
            "4. **Sevmediğiniz Kitabı Bırakın:** Kitap bitirmek zorunda değilsiniz; akmıyorsa başka kitaba geçmek haktır."
        );

        Register(
            "Zaman Yönetimi",
            "Verimlilik",
            new[] { "zamanımı daha iyi yönetmek için hangi yöntemi kullanabilirim", "zaman yönetimi", "zamanı iyi kullanmak", "zaman yönetimi yöntemleri" },
            new[] { "zamanımı", "daha", "iyi", "yönetmek", "yöntemi" },
            "Zamanı yönetmenin en kanıtlanmış yöntemi **Eisenhower Matrisi**dir: ⏳\n\n" +
            "İşlerinizi 4 kutuya bölün:\n" +
            "1. **Acil ve Önemli:** Hemen şimdi yapın (Krizler, teslim tarihleri).\n" +
            "2. **Acil Değil ama Önemli (En Kritik Kutu!):** Takviminize planlayın (Gelişim, spor, kitap okuma, stratejik işler).\n" +
            "3. **Acil ama Önemsiz:** Mümkünse devredin veya hızlandırın (Gereksiz toplantılar, bazı telefonlar).\n" +
            "4. **Ne Acil Ne Önemli:** Hayatınızdan silin (Saatlerce sosyal medyada kaydırmak).\n\n" +
            "Başarılı insanların sırrı, 2. kutuya (önemli ama acil olmayan gelişim alanlarına) her gün vakit ayırmalarıdır."
        );

        Register(
            "Pomodoro Tekniği",
            "Verimlilik",
            new[] { "pomodoro tekniği nedir nasıl uygulanır", "pomodoro tekniği", "pomodoro nedir", "pomodoro nasıl yapılır" },
            new[] { "pomodoro", "tekniği", "nedir", "nasıl", "uygulanır" },
            "**Pomodoro Tekniği**, Francesco Cirillo tarafından geliştirilen, zihinsel odaklanmayı zirveye çıkaran zaman yönetimi tekniğidir: 🍅\n\n" +
            "Nasıl Uygulanır? (Adım Adım):\n" +
            "1. 📝 **Görevi Seçin:** Tek bir görev belirleyin.\n" +
            "2. ⏱️ **25 Dakika Kurun (1 Pomodoro):** Zamanlayıcıyı başlatın ve bu süre boyunca ASLA dikkatinizi dağıtmayın.\n" +
            "3. ☕ **5 Dakika Mola:** Zaman dolduğunda işi bırakın, ayağa kalkın, su için (ekrana bakmayın!).\n" +
            "4. 🔄 **Tekrar Edin:** 4 Pomodoro tamamladıktan sonra kendinize 15-30 dakikalık uzun bir mola verin.\n\n" +
            "Bu yöntem beyne 'sadece 25 dakika dayanacaksın' mesajı verdiği için ertelemeyi tamamen ortadan kaldırır."
        );

        Register(
            "Günde Kaç Litre Su",
            "Sağlık",
            new[] { "günde kaç litre su içmek gerekir", "kaç litre su içmeliyiz", "su ihtiyacı ne kadar", "günlük su miktarı" },
            new[] { "günde", "kaç", "litre", "su", "içmek", "gerekir" },
            "💧 **Günlük Su İhtiyacı:**\n\n" +
            "Genel kural olarak bir yetişkinin günde vücut ağırlığının kilosu başına yaklaşık **30-35 ml** su içmesi önerilir:\n" +
            "• 60 kg biri için: ~2.0 Litre\n" +
            "• 75 kg biri için: ~2.3 - 2.5 Litre\n" +
            "• 90 kg biri için: ~2.8 - 3.0 Litre\n\n" +
            "Kahve ve çay idrar söktürücü olduğu için saf suyun yerini tutmaz. İdrar renginizin açık saman sarısı olması ideal su seviyesinde olduğunuzun en güvenilir göstergesidir."
        );

        Register(
            "Su İçmeyi Unutuyorum",
            "Sağlık & Alışkanlık",
            new[] { "su içmeyi sürekli unutuyorum ne yapabilirim", "su içmeyi unutuyorum", "su içemiyorum", "su içme alışkanlığı" },
            new[] { "su", "içmeyi", "unutuyorum", "ne", "yapabilirim" },
            "Su içmeyi unutmamak için çevre tasarımını lehinize çevirin: 💧\n\n" +
            "1. **Masanızda Şeffaf Şişe Olsun:** Göz önünde olan şey akılda olur. Çalışma masanızda her zaman 1 litrelik şeffaf bir matara bulundurun.\n" +
            "2. **Kural: Her Kahveden Önce 1 Bardak Su:** Kahve veya çay içmeden önce mutlaka koca bir bardak su içme şartı koyun.\n" +
            "3. **Suyunuza Tat Katın:** Saf su içmek sıkıcı geliyorsa içine bir dilim limon, nane yaprağı veya salatalık atın.\n" +
            "4. **Telefon Hatırlatıcısı:** Saat başı '1 yudum su' uyarısı veren basit bir hatırlatıcı kurun."
        );

        // ══════════════════════════════════════════════════════════════════════
        // 4. YEMEK, TARİFLER & MUTFAK İPUÇLARI
        // ══════════════════════════════════════════════════════════════════════

        Register(
            "Menemen Soğanlı mı Soğansız mı",
            "Mutfak & Popüler İkilem",
            new[] { "menemen soğanlı mı olur soğansız mı", "menemen soğanlı mı", "menemen soganli mi", "menemene soğan konur mu" },
            new[] { "menemen", "soğanlı", "olur", "soğansız" },
            "Türk gastronomi dünyasının en efsanevi tartışmasına geldik! 🍳\n\n" +
            "Usta şeflerin ve gastronomi duayeni Vedat Milör'ün uzlaştığı altın formül şudur:\n" +
            "• **Kahvaltıda:** **Soğansız** makbuldür. Yumurta, sulu tarla domatesi ve tatlı sivri biberin saf lezzeti ön planda olur; sabah sabah mideyi yormaz.\n" +
            "• **Öğle veya Akşam Yemeğinde:** **Soğanlı** harikadır! İyice karamelize edilmiş ince doğranmış soğan, yemeğe gövde ve enfes bir tatlılık katar.\n\n" +
            "İşin asıl püf noktası ise: Domatesler iyice pişip suyunu çekmeden yumurtayı atmamak ve yumurtayı fazla kurutmadan kremsi bırakmaktır. Siz hangisini tercih ediyorsunuz?"
        );

        Register(
            "Krep Nasıl Yapılır",
            "Mutfak & Tarif",
            new[] { "krep nasıl yapılır malzemeleri neler", "krep nasıl yapılır", "krep tarifi", "kolay krep yapımı" },
            new[] { "krep", "nasıl", "yapılır", "malzemeleri", "neler" },
            "🥞 **Pürüzsüz ve İncecik Krep Tarifi (Adım Adım):**\n\n" +
            "📋 **Gerekli Malzemeler:**\n" +
            "• 2 adet yumurta\n" +
            "• 2 su bardağı süt (veya 1 su bardağı süt + 1 su bardağı su)\n" +
            "• 1.5 su bardağı un\n" +
            "• 1 çay kaşığı tuz, 1 yemek kaşığı sıvı yağ\n\n" +
            "👩‍🍳 **Adım Adım Yapılışı:**\n" +
            "1. **Çırpın:** Bir kapta yumurta ve sütü çırpın. Unu ve tuzu eleyerek ekleyin. Boza kıvamında, tamamen pürüzsüz bir harç elde edin.\n" +
            "2. **Tavayı Isıtın:** Yanmaz yapışmaz krep tavasını kızdırın ve peçeteyle çok hafif yağlayın.\n" +
            "3. **Dökün ve Yayın:** Tavaya 1 kepçe harç dökün ve tavayı hızlıca bilekten çevirerek harcın tabana incecik yayılmasını sağlayın.\n" +
            "4. **Pişirin:** Orta ateşte kenarları hafif kıvrılınca (yaklaşık 1 dakika) spatula ile ters çevirip diğer yüzünü de 30-40 saniye pişirin. Sıcak servis yapın!"
        );

        Register(
            "Tavuk Sote Nasıl Pişirilir",
            "Mutfak & Tarif",
            new[] { "tavuk sote nasıl pişirilir", "tavuk sote tarifi", "tavuk sote nasıl yapılır", "lokum gibi tavuk sote" },
            new[] { "tavuk", "sote", "nasıl", "pişirilir" },
            "🍗 **Lokum Gibi Yumuşacık Tavuk Sote Tarifi (Adım Adım):**\n\n" +
            "1. **Mühürleme (En Önemli Adım):** Geniş bir tavayı yüksek ateşte iyice ısıtın. 500g kuşbaşı tavuk göğsünü tavaya atın. Yüksek ateşte suyunu salıp çekene kadar soteleyin (böylece tavuk suyunu içine hapseder ve kurumaz).\n" +
            "2. **Sebzeleri Ekleyin:** 2 yemek kaşığı sıvı yağ, 1 adet piyazlık doğranmış kuru soğan ve 2 diş sarımsağı ekleyip 3-4 dakika kavurun.\n" +
            "3. **Biberler:** 2 adet yeşil biber ve 1 adet kapya biberi ekleyip sotelemeye devam edin.\n" +
            "4. **Domates ve Baharat:** 2 adet rendelenmiş kabuksuz domates, 1 tatlı kaşığı domates salçası, kekik, karabiber, pul biber ve tuzu ilave edin.\n" +
            "5. **Kısık Ateşte Pişirme:** Kapağını kapatıp kısık ateşte domatesler suyunu çekip kıvam alana kadar yaklaşık 10 dakika pişirin. Sıcak servis yapın!"
        );

        Register(
            "Evde Pizza Nasıl Yapılır",
            "Mutfak & Tarif",
            new[] { "evde pizza nasıl yapılır", "pizza tarifi", "ev yapımı pizza", "pizza hamuru nasıl yapılır" },
            new[] { "evde", "pizza", "nasıl", "yapılır" },
            "🍕 **Çıtır Kenarlı Ev Yapımı Pizza Tarifi (Adım Adım):**\n\n" +
            "1. **Hamuru Hazırlayın:** 3 su bardağı un, 1 paket instant maya, 1 tatlı kaşığı şeker, 1 tatlı kaşığı tuz, 2 yemek kaşığı zeytinyağı ve 1 su bardağı ılık suyu yoğurun. Üzerini kapatıp 40 dakika mayalandırın.\n" +
            "2. **Sos:** 1 su bardağı domates püresine 1 diş ezilmiş sarımsak, 1 tatlı kaşığı kekik, fesleğen ve 1 yemek kaşığı zeytinyağı karıştırın.\n" +
            "3. **Şekil Verme:** Mayalanan hamuru yağlı kağıt üzerinde elinizle veya merdaneyle kenarları hafif kalın kalacak şekilde yuvarlak açın. Çatalla tabana delikler açın (kabarmasın).\n" +
            "4. **Malzemeler:** Sosu sürün, bolca rendelenmiş mozzarella veya kaşar peyniri serpin; üzerine sucuk, mantar, mısır, zeytin ekleyin.\n" +
            "5. **Pişirme:** Fırını önceden en yüksek ısıya (220-250°C) ısıtın. Altı ve kenarları altın sarısı olana kadar 12-15 dakika pişirin."
        );

        Register(
            "Pratik Akşam Yemeği",
            "Mutfak & Tarif",
            new[] { "pratik bir akşam yemeği önerir misin", "pratik akşam yemeği", "hızlı akşam yemeği", "akşama ne pişirsem pratik" },
            new[] { "pratik", "bir", "akşam", "yemeği", "önerir", "misin" },
            "Akşam yorgunken 15-20 dakikada hazırlayabileceğiniz nefis ve doyurucu öneri: 🍝\n\n" +
            "**Kremalı Mantarlı Tavuklu Makarna:**\n" +
            "1. Makarnayı bol tuzlu suda 8 dakika haşlanmaya bırakın.\n" +
            "2. Yan tavada kuşbaşı doğranmış tavukları 5 dakika soteleyin, dilimlenmiş mantarları ve 1 diş sarımsağı ekleyip 3 dakika daha çevirin.\n" +
            "3. Üzerine 1 kutu sıvı krema, karabiber, kekik ve tuz ekleyip kısık ateşte 2 dakika tıkırdatın.\n" +
            "4. Süzülen makarnayı sos tavasına döküp harmanlayın. Üzerine biraz kaşar peyniriyle sıcak sıcak servis yapın. Hem restoran lezzetindedir hem de tek tavada biter!"
        );

        Register(
            "Çorba Tuzu Fazla Gelirse",
            "Mutfak Püf Noktası",
            new[] { "çorba yaparken tuz fazla gelirse ne yapılır", "çorba çok tuzlu oldu", "tuzlu çorba nasıl kurtarılır", "yemeğin tuzu fazla olursa" },
            new[] { "çorba", "tuz", "fazla", "gelirse", "ne", "yapılır" },
            "Tuzu fazla kaçan bir yemeği veya çorbayı kurtarmanın denenmiş 4 harika yolu: 🥔\n\n" +
            "1. **Çiğ Patates Mucizesi:** 1 adet orta boy patatesi soyup ikiye bölün ve kaynayan çorbanın içine atın. 10-15 dakika kaynatın; patates fazla tuzu bir sünger gibi çeker. Servis öncesi patatesi içinden çıkarın.\n" +
            "2. **Sıcak Su veya Et Suyu Ekleyin:** Çorbanız koyu kıvamlıysa biraz kaynar su ekleyip bir taşım kaynatmak tuzu seyreltir.\n" +
            "3. **Süt veya Krema (Uygun Çorbalar İçin):** Mercimek, mantar veya sebze çorbasıysa yarım çay bardağı süt veya krema eklemek tuzun keskinliğini anında kırar.\n" +
            "4. **Birkaç Damla Limon:** Asit tuzu dengeler; az miktarda limon suyu tuz tadını hafifletir."
        );

        // ══════════════════════════════════════════════════════════════════════
        // 5. TEKNOLOJİ, BİLGİSAYAR & DONANIM
        // ══════════════════════════════════════════════════════════════════════

        Register(
            "RAM Nedir Ne İşe Yarar",
            "Donanım",
            new[] { "ram nedir bilgisayarda ne işe yarar", "ram nedir", "ram ne işe yarar", "bellek nedir" },
            new[] { "ram", "nedir", "bilgisayarda", "ne", "işe", "yarar" },
            "🧠 **RAM (Random Access Memory - Rastgele Erişimli Bellek):**\n\n" +
            "RAM'i bilgisayarın **çalışma masası** olarak düşünebilirsiniz:\n" +
            "• Sabit diskiniz (SSD) bir kitaplıktır; dosyalar orada kalıcı durur. Ancak bir programı (Chrome, oyun, Word) açtığınızda işlemci bu verileri kitaplıktan alıp masanın (RAM) üzerine koyar.\n" +
            "• Masanız ne kadar büyükse (örneğin 32 GB veya bu makinedeki gibi 96 GB), aynı anda o kadar çok programı kasmadan, donmadan açık tutabilirsiniz.\n" +
            "• Bilgisayar kapandığında RAM'deki veriler tamamen sıfırlanır (geçici bellektir)."
        );

        Register(
            "İşlemci ile Ekran Kartı Farkı",
            "Donanım",
            new[] { "işlemci ile ekran kartı arasındaki fark nedir", "cpu gpu farkı", "işlemci ekran kartı farkı", "cpu ile gpu arasındaki fark" },
            new[] { "işlemci", "ekran", "kartı", "arasındaki", "fark", "nedir" },
            "⚙️ **İşlemci (CPU) vs. Ekran Kartı (GPU):**\n\n" +
            "• **CPU (Bilgisayarın Beyni):** Az sayıda (örneğin 8-24 adet) son derece akıllı ve güçlü çekirdeğe sahiptir. Karmaşık, sıralı mantık işlemlerini, işletim sistemini ve genel komutları yönetir. Birkaç profesörün karmaşık problemleri çözmesine benzer.\n" +
            "• **GPU (Görsel ve Paralel Güç):** Binlerce (örneğin RTX 5090'daki binlerce CUDA çekirdeği) daha küçük çekirdeğe sahiptir. Aynı anda milyonlarca pikseli çizmek, oyunlardaki 3D grafikleri hesaplamak veya yapay zeka tensör matrislerini çarpmak gibi paralel işlerde uzmandır. Binlerce işçinin aynı anda tuğla dizmesine benzer."
        );

        Register(
            "SSD ile HDD Farkı",
            "Donanım",
            new[] { "ssd ile hdd arasındaki fark nedir", "ssd hdd farkı", "ssd nedir hdd nedir" },
            new[] { "ssd", "hdd", "arasındaki", "fark", "nedir" },
            "💾 **SSD vs. HDD Karşılaştırması:**\n\n" +
            "• **HDD (Hard Disk Drive):** İçinde dönen manyetik plakalar ve mekanik bir okuma kafası bulunur (eski pikaplar gibi). Yavaştır (yaklaşık 100-150 MB/s), sarsıntıya duyarlıdır ve ses çıkarır.\n" +
            "• **SSD (Solid State Drive):** Mekanik hiçbir parça içermez; doğrudan mikroçiplere (NAND flash) elektrikle yazar. Sessizdir, darbelere dayanıklıdır ve günümüz NVMe SSD'leri 7.000+ MB/s hızlara ulaşır.\n\n" +
            "Bir bilgisayara HDD yerine SSD takmak, sistemi kaplumbağadan çitaya dönüştürür!"
        );

        Register(
            "Telefon Şarjı Hızlı Bitiyor",
            "Teknoloji Pratik",
            new[] { "telefonumun şarjı çok çabuk bitiyor ne yapabilirim", "şarjım çabuk bitiyor", "telefon şarjı uzatma", "pil ömrünü artırma" },
            new[] { "telefonumun", "şarjı", "çabuk", "bitiyor", "ne", "yapabilirim" },
            "Akıllı telefonunuzun pil ömrünü belirgin şekilde uzatacak en etkili önlemler: 🔋\n\n" +
            "1. **Ekran Parlaklığı ve Süresi:** Ekran pili en çok tüketen bileşendir. Otomatik parlaklığı açın ve ekran kapanma süresini 30 saniyeye çekin.\n" +
            "2. **Arka Plan Konum ve Yenileme:** Kullanmadığınız uygulamaların 'Arka Planda Yenileme' ve 'Her Zaman Konum' izinlerini kapatın.\n" +
            "3. **Koyu Tema (Dark Mode):** Telefonunuz OLED/AMOLED ekransa, koyu temada siyah pikseller tamamen kapandığı için ciddi pil tasarrufu sağlar.\n" +
            "4. **5G Yerine 4G/LTE:** 5G şebekesi çekim ararken pili çok hızlı tüketir; sabit 4G'ye almak fark yaratır.\n" +
            "5. **Pil Sağlığı:** Ayarlardan pil sağlığınızı kontrol edin; %80'in altına indiyse bataryayı değiştirmek gerekebilir."
        );

        Register(
            "Evde İnternet Yavaş",
            "Teknoloji Pratik",
            new[] { "evde internet yavaşsa ne yapılabilir", "internetim çok yavaş", "wifi hızlandırma", "internet hızı artırma" },
            new[] { "evde", "internet", "yavaşsa", "ne", "yapılabilir" },
            "Ev internetinizi hızlandırmak için şu adımları uygulayın: 📶\n\n" +
            "1. **Modemi Kapatıp 30 Saniye Bekleyin:** Modemler haftalarca açık kaldığında bellek şişmesi yaşar. Kapatıp açmak temiz bir IP ve kanal tahsis eder.\n" +
            "2. **5 GHz Wi-Fi Ağına Bağlanın:** Modemde çift bant varsa (2.4 GHz ve 5 GHz), yakındayken mutlaka 5 GHz bandını seçin. 2.4 GHz mikrodalga ve komşu modemlerle çok çakışır.\n" +
            "3. **Modemin Konumu:** Modemi kapalı dolaplara veya televizyon arkasına saklamayın; evin merkezinde ve yüksek bir yerde olsun.\n" +
            "4. **Hızlı DNS Kullanın:** Cloudflare (1.1.1.1) veya Google (8.8.8.8) DNS ayarlarını girerek alan adı çözümleme hızını artırın.\n" +
            "5. **Kablolu Ethernet Deneyin:** Masaüstü veya konsolda Wi-Fi yerine kablo çekmek hızı ve ping kararlılığını zirveye çıkarır."
        );

        // ══════════════════════════════════════════════════════════════════════
        // 6. YAZILIM, KODLAMA & KARİYER
        // ══════════════════════════════════════════════════════════════════════

        Register(
            "Python mu C# mı",
            "Yazılım",
            new[] { "python mu öğrenmeliyim c# mı", "python mu c# mı", "c# mı python mu", "hangi dili öğrenmeliyim python csharp" },
            new[] { "python", "öğrenmeliyim", "c#", "mı" },
            "Her iki dil de dünya standardıdır ancak hedefinize göre tercih yapmalısınız: 💻\n\n" +
            "• **Python Tercih Edin Eğer:** Yapay zeka, veri bilimi, makine öğrenimi modelleri eğitmek veya hızlı prototip üretmek istiyorsanız. Yazımı çok basittir ancak çalışma hızı C#'a kıyasla yavaştır.\n" +
            "• **C# / .NET Tercih Edin Eğer:** Yüksek performanslı kurumsal yazılımlar, güvenilir masaüstü/web sistemleri, mikroservisler veya oyun (Unity) geliştirmek istiyorsanız. Tip güvenliği, bellek optimizasyonu (SIMD/Span) ve mimari disiplini muazzamdır (HexAI tamamen saf C# ile yazılmıştır).\n\n" +
            "Öneri: Genel yazılım disiplini ve performans için C# harika bir temeldir; veri ve yapay zeka deneyleri için Python'ı araç olarak yanına ekleyebilirsiniz."
        );

        Register(
            "Yapay Zekaya Nereden Başlamalı",
            "Yazılım & Yapay Zeka",
            new[] { "yapay zeka öğrenmeye nereden başlamalıyım", "yapay zeka nasıl öğrenilir", "ai öğrenme yol haritası", "yapay zekaya nasıl başlanır" },
            new[] { "yapay", "zeka", "öğrenmeye", "nereden", "başlamalıyım" },
            "Yapay zekayı sıfırdan sağlam bir temelle öğrenmek için izlemeniz gereken 4 adımlı yol haritası: 🗺️\n\n" +
            "1. 🐍 **Programlama Temeli:** Python veya C# ile veri yapıları, nesne yönelimli programlama ve matris kütüphanelerini öğrenin.\n" +
            "2. 📐 **Temel Matematik:** Korkmayın, teorisyen olmanıza gerek yok; Lineer Cebir (vektörler, matris çarpımları) ve Temel Kalkülüs (türev / gradyan inişi) kavramlarını kavrayın.\n" +
            "3. 📊 **Klasik Makine Öğrenimi:** Doğrusal regresyon, lojistik regresyon, karar ağaçları ve kümeleme algoritmalarını kodlayın.\n" +
            "4. 🧠 **Derin Öğrenme & Transformer:** Çok katmanlı algılayıcılar (MLP), ardından Attention mekanizması ve Transformer (GPT) bloklarının nasıl çalıştığını inceleyin."
        );

        Register(
            "Yazılımcı Olmak İçin Üniversite Şart mı",
            "Kariyer",
            new[] { "yazılımcı olmak için üniversite şart mı", "yazılımcı olmak için diploma", "üniversite okumadan yazılımcı olunur mu", "diploma şart mı" },
            new[] { "yazılımcı", "olmak", "üniversite", "şart", "mı" },
            "Kısa cevap: **Hayır, şart değildir!** 🚀\n\n" +
            "Yazılım sektörü, liyakatin ve somut üretimin diplomadan çok daha değerli olduğu ender alanlardan biridir:\n" +
            "• Bir işverenin veya müşterinin ilk baktığı şey: **GitHub profiliniz**, yaptığınız projelerin kalitesi ve problem çözme yeteneğinizdir.\n" +
            "• Üniversite size network, temel matematik ve algoritma disiplini kazandırır; ancak güncel teknolojileri, modern mimarileri ve kod yazmayı kendiniz öğrenmek zorundasınız.\n\n" +
            "Tutkuyla kod yazan, kendi kendine araştırabilen ve çalışan projeler üretebilen herkes başarılı bir yazılımcı olabilir."
        );

        Register(
            "GitHub Nedir Nasıl Kullanılır",
            "Yazılım",
            new[] { "github nedir nasıl kullanılır", "github nedir", "git ve github", "github ne işe yarar" },
            new[] { "github", "nedir", "nasıl", "kullanılır" },
            "🐙 **GitHub Nedir?**\n\n" +
            "GitHub, yazılımcıların kodlarını sakladığı, versiyonladığı (Git ile zaman içinde geri dönebildiği) ve dünyanın dört bir yanındaki geliştiricilerle birlikte çalıştığı bulut tabanlı bir platformdur.\n\n" +
            "Temel Döngü:\n" +
            "1. `git init` - Projeyi Git ile takip etmeye başla.\n" +
            "2. `git add .` - Değişiklikleri sahneye al.\n" +
            "3. `git commit -m \"Açıklama\"` - O anki halini bir fotoğraf gibi kaydet.\n" +
            "4. `git push` - Kodları GitHub sunucusuna gönder.\n\n" +
            "Ayrıca bir yazılımcının yaşayan dijital özgeçmişidir!"
        );

        Register(
            "Web Sitesi Dilleri",
            "Yazılım",
            new[] { "web sitesi yapmak için hangi dilleri bilmek gerekir", "web sitesi dilleri", "web geliştirme dilleri", "frontend backend dilleri" },
            new[] { "web", "sitesi", "yapmak", "hangi", "dilleri", "bilmek" },
            "Modern bir web sitesi geliştirmek için temel sacayağı: 🌐\n\n" +
            "• **Ön Yüz (Frontend - Kullanıcının Gördüğü Kısım):**\n" +
            "  - **HTML:** Sayfanın iskeleti ve içeriği.\n" +
            "  - **CSS:** Renkler, tasarım, düzen ve animasyonlar.\n" +
            "  - **JavaScript:** Buton tıklamaları, dinamik veriler ve etkileşim.\n\n" +
            "• **Arka Yüz (Backend - Sunucu & Veritabanı Mantığı):**\n" +
            "  - **C# (.NET / ASP.NET Core):** Yüksek güvenlik ve kurumsal başarım.\n" +
            "  - **Node.js (JavaScript) / Python (FastAPI/Django):** Hızlı API servisleri.\n" +
            "  - **SQL:** Verilerin saklandığı veritabanı dili."
        );

        Register(
            "Veritabanı ve SQL Nedir",
            "Yazılım",
            new[] { "bir veritabanı nedir sql ne işe yarar", "veritabanı nedir", "sql nedir", "sql ne işe yarar" },
            new[] { "veritabanı", "nedir", "sql", "ne", "işe", "yarar" },
            "🗄️ **Veritabanı ve SQL:**\n\n" +
            "• **Veritabanı (Database):** Kullanıcı şifreleri, siparişler, ürünler gibi kritik bilgileri güvenli, düzenli ve aranabilir tablolarda saklayan dijital depodur.\n" +
            "• **SQL (Structured Query Language):** Bu depoyla konuşma dilimizdir. Örneğin:\n" +
            "  `SELECT * FROM Kullanicilar WHERE Sehir = 'İstanbul';`\n" +
            "  komutu veritabanına 'Bana İstanbul'daki tüm kullanıcıları getir' der.\n\n" +
            "Her modern yazılımın arkasında mutlaka güçlü bir veritabanı yatar."
        );

        // ══════════════════════════════════════════════════════════════════════
        // 7. BİLİM, DOĞA & EVREN
        // ══════════════════════════════════════════════════════════════════════

        Register(
            "Gökyüzü Neden Mavi",
            "Bilim",
            new[] { "neden gökyüzü mavi görünür", "gökyüzü neden mavi", "gökyüzü niye mavi", "gök neden mavi" },
            new[] { "neden", "gökyüzü", "mavi", "görünür" },
            "🌌 **Gökyüzü Neden Mavidir? (Rayleigh Saçılması):**\n\n" +
            "Güneş'ten gelen beyaz ışık aslında gökkuşağındaki tüm renklerin (kırmızı, turuncu, sarı, yeşil, mavi, mor) birleşimidir:\n\n" +
            "1. Bu ışık Dünya'nın atmosferine girdiğinde, havadaki azot ve oksijen moleküllerine çarpar.\n" +
            "2. Kırmızı gibi uzun dalga boylu ışıklar moleküllerin arasından neredeyse hiç sapmadan geçer.\n" +
            "3. Ancak mavi ve mor gibi **kısa dalga boylu** ışıklar hava moleküllerine çarparak gökyüzünün her yönüne saçılır (**Rayleigh Saçılması**).\n" +
            "4. Gözlerimiz mora kıyasla maviye çok daha duyarlı olduğu için gökyüzünü masmavi görürüz!"
        );

        Register(
            "Karadelik Nedir",
            "Astronomi & Bilim",
            new[] { "karadelik nedir kısaca anlatır mısın", "karadelik nedir", "kara delik nedir", "kara delikler nasıl oluşur" },
            new[] { "karadelik", "nedir", "kısaca", "anlatır", "mısın" },
            "🕳️ **Karadelik Nedir?**\n\n" +
            "Karadelik, devasa bir kütlenin akıl almaz derecede küçük bir hacme sıkışması sonucu oluşan, çekim kuvveti o kadar şiddetli bir uzay bölgesidir ki **ışık bile onun çekiminden kaçamaz**!\n\n" +
            "• **Nasıl Oluşur?:** Güneş'imizden 20-30 kat daha büyük dev yıldızlar ömrünü tamamlayıp süpernova ile patladığında kendi içine çöker.\n" +
            "• **Olay Ufku:** Karadeliğin geri dönüşü olmayan sınırıdır; bu sınırı geçen her şey tekilliğe doğru çekilir.\n" +
            "• Zaman karadeliğin yakınında kütleçekim etkisiyle dışarıdaki bir gözlemciye göre neredeyse duracak kadar yavaşlar."
        );

        Register(
            "Işık Hızı Ne Kadar",
            "Fizik & Bilim",
            new[] { "ışık hızı saniyede kaç kilometredir", "ışık hızı ne kadar", "isik hizi kac km", "ışığın hızı" },
            new[] { "ışık", "hızı", "saniyede", "kaç", "kilometredir" },
            "⚡ **Işık Hızı:**\n\n" +
            "Işığın boşluktaki yayılma hızı saniyede yaklaşık **300.000 kilometredir**.\n" +
            "Tam bilimsel değeri: **299.792,458 km/s**dir.\n\n" +
            "Bu hızla giden bir foton, sadece 1 saniyede Dünya'nın etrafını 7.5 kez turlayabilir! Einstein'ın özel görelilik teorisine göre evrendeki hiçbir bilgi veya kütle ışık hızını aşamaz."
        );

        Register(
            "Dinozorlar Neden Yok Oldu",
            "Tarih & Doğa",
            new[] { "dinozorlar neden yok oldu", "dinozorların yok oluşu", "dinozorlar nasıl öldü", "dinozorları ne yok etti" },
            new[] { "dinozorlar", "neden", "yok", "oldu" },
            "🦖 **Dinozorların Yok Oluşu:**\n\n" +
            "Yaklaşık **66 milyon yıl önce**, bugünkü Meksika'nın Yucatan Yarımadası'na yaklaşık 10-15 kilometre çapında dev bir göktaşı (Chicxulub asteroidi) çarptı:\n\n" +
            "1. Çarpmanın etkisi milyarlarca atom bombasına eşdeğerdi; dev tsunamiler ve küresel yangınlar başladı.\n" +
            "2. Atmosfere savrulan trilyonlarca ton toz ve kül gökyüzünü aylarca kapattı, Güneş ışığı Dünya'ya ulaşamadı.\n" +
            "3. Fotosentez durdu, küresel bir kış yaşandı; bitkiler ölünce otçul, ardından etçil dinozorlar yok oldu.\n" +
            "Ancak küçük kuş benzeri dinozorlar hayatta kaldı; günümüz kuşları onların torunlarıdır!"
        );

        Register(
            "Yerçekimi Nasıl Keşfedildi",
            "Fizik",
            new[] { "yerçekimi nasıl keşfedildi", "yerçekimini kim buldu", "newton elma hikayesi", "kütleçekim nasıl bulundu" },
            new[] { "yerçekimi", "nasıl", "keşfedildi", "newton" },
            "🍎 **Yerçekiminin Keşfi:**\n\n" +
            "1665 yılında İngiltere'de veba salgını nedeniyle Cambridge Üniversitesi tatil edildiğinde genç **Isaac Newton** memleketindeki çiftliğe çekildi.\n\n" +
            "Bahçesindeki elma ağacından bir elmanın yere düşüşünü izlerken şu dahi soruyu sordu:\n" +
            "_'Elma neden yana veya yukarı değil de daima Dünya'nın merkezine doğru dik düşüyor? Eğer bu çekim ağacın tepesine kadar ulaşıyorsa, gökyüzündeki Ay'a kadar neden ulaşmasın?'_\n\n" +
            "Newton, yerdeki cisimleri çeken kuvvet ile gezegenleri yörüngede tutan kuvvetin aynı olduğunu kanıtlayarak **Evrensel Kütleçekim Yasası**nı formüle etti."
        );

        // ══════════════════════════════════════════════════════════════════════
        // 8. POPÜLER İKİLEMLER, EĞLENCE & MİZAH
        // ══════════════════════════════════════════════════════════════════════

        Register(
            "Fıkra Anlat",
            "Mizah",
            new[] { "bana komik bir fıkra anlatır mısın", "fıkra anlat", "komik fıkra", "bir fıkra anlat", "güldür beni" },
            new[] { "bana", "komik", "bir", "fıkra", "anlatır", "mısın" },
            "İşte yazılımcı ve mühendis dünyasından çok tatlı bir klasik: 😄\n\n" +
            "Bir yazılımcının eşi ona der ki:\n" +
            "— _'Hayatım, markete git. 1 somun ekmek al. Eğer yumurta varsa 10 tane al.'_\n\n" +
            "Yazılımcı markete gider, biraz sonra elinde **10 somun ekmekle** eve döner!\n" +
            "Eşi şaşkınlıkla bağırır:\n" +
            "— _'Neden 10 tane ekmek aldın?!'_\n\n" +
            "Yazılımcı son derece sakin cevap verir:\n" +
            "— _'Çünkü yumurta vardı!'_ 🥚🥖\n\n" +
            "(Koşullu ifadelere `if (yumurta) ekmek = 10;` fazla kaptırınca böyle oluyor!)"
        );

        Register(
            "Şiir Yaz",
            "Sanat & Edebiyat",
            new[] { "kısa ve güzel bir şiir yazar mısın", "şiir yaz", "güzel bir şiir", "bana şiir yaz", "bir dörtlük yaz" },
            new[] { "kısa", "güzel", "bir", "şiir", "yazar", "mısın" },
            "Sizin için kaleme aldığım bu dizeler umarım gününüze güzellik katar: ✍️\n\n" +
            "Bir ekranda parıldayan satırlar gibi,\n" +
            "Yankılanır dostluğun en samimi sesi.\n" +
            "Zaman akar gider bir rüzgar misali,\n" +
            "Kelimelerde saklıdır insanın nefesi.\n\n" +
            "Yıldızlar fısıldar gecenin ufkunda,\n" +
            "Umut yeşerir atılan her adımda."
        );

        Register(
            "Film Önerisi",
            "Sanat & Eğlence",
            new[] { "izlemek için sürükleyici bir film önerir misin", "film önerisi", "güzel bir film öner", "sürükleyici film" },
            new[] { "izlemek", "için", "sürükleyici", "film", "önerir", "misin" },
            "Zamanın nasıl geçtiğini unutturacak, zihni büyüleyen 3 başyapıt önerisi: 🎬\n\n" +
            "1. 🚀 **Interstellar (Yıldızlararası):** Dünyanın kuraklıkla yok oluşa sürüklendiği bir gelecekte, insanlığa yeni bir yuva aramak için solucan deliğinden geçen bir grup astronotun hikayesi. Müzikleri (Hans Zimmer) ve bilimsel derinliği nefes kesicidir.\n" +
            "2. 🌀 **Inception (Başlangıç):** İnsanların rüyalarına girerek fikir çalan bir hırsızın, bu kez imkansız bir göreve çıkıp bir zihne fikir ekleme macerası. Akıl oyunları ve kurgusu kusursuzdur.\n" +
            "3. 🗝️ **The Prestige (Prestij):** 19. yüzyıl Londra'sında iki dahi sihirbazın birbirini alt etmek için girdikleri takıntılı ve şok edici rekabet."
        );

        Register(
            "Kitap Önerisi Bilim Kurgu",
            "Edebiyat",
            new[] { "okumak için güzel bir bilim kurgu kitabı tavsiyesi ver", "bilim kurgu kitabı", "kitap önerisi bilim kurgu", "kitap tavsiyesi ver" },
            new[] { "okumak", "için", "güzel", "bilim", "kurgu", "kitabı", "tavsiyesi" },
            "Bilim kurgu edebiyatının zirvesinde yer alan, ufkunuzu açacak 3 dev eser: 📚\n\n" +
            "1. 🏜️ **Dune - Frank Herbert:** Arrakis adlı çöl gezegeninde geçen politika, din, ekoloji ve insan iradesi üzerine yazılmış gelmiş geçmiş en görkemli destan.\n" +
            "2. 🏛️ **Vakıf (Foundation) - Isaac Asimov:** Samanyolu Galaktik İmparatorluğu'nun çöküşünü matematiksel modellerle öngören bir matematikçinin, insanlığın karanlık çağını kısaltma mücadelesi.\n" +
            "3. 🌊 **Solaris - Stanislaw Lem:** Tamamen canlı ve düşünen dev bir plazma okyanusuyla kaplı bir gezegeni anlamaya çalışan bilim insanlarının bilinçaltı sınavı."
        );

        Register(
            "İlginç Bilgi",
            "Genel Kültür",
            new[] { "bana ilginç ve şaşırtıcı bir bilgi söyle", "ilginç bilgi", "şaşırtıcı bilgi", "ilginç bir bilgi söyle" },
            new[] { "bana", "ilginç", "şaşırtıcı", "bir", "bilgi", "söyle" },
            "İşte duyunca insanı hayrete düşüren gerçek bir doğa mucizesi: 🍯\n\n" +
            "**Bal Asla Bozulmaz!**\n" +
            "Arkeologlar Mısır firavunlarının 3.000 yıllık piramit mezarlarını açtıklarında kapalı kavanozlarda bal buldular. Ve işin en inanılmaz tarafı ne biliyor musunuz? O bal hâlâ tamamen yenilebilir durumdaydı!\n\n" +
            "Sebebi: Balın su oranının neredeyse sıfır olması ve aşırı asidik ortamının hiçbir bakteri veya mantarın üremesine izin vermemesidir. Doğa harika bir kimyagerdir!"
        );

        Register(
            "Bilmece Sor",
            "Mizah & Eğlence",
            new[] { "bir bilmece sorabilir misin cevabını sonra söyle", "bilmece sor", "bana bilmece sor", "bilmece" },
            new[] { "bir", "bilmece", "sorabilir", "misin", "cevabını" },
            "Harika, zekanızı sınayacak klasik ve zarif bir bilmece gelsin! 🧩\n\n" +
            "_'Ne kadar çok alırsan, arkanda o kadar çok bırakırsın. Ben neyim?'_\n\n" +
            "Cevabı tahmin etmeye çalışın! Hazır olduğunuzda bana 'cevabı söyle' deyin, bakalım bilecek misiniz? 😉"
        );

        Register(
            "Tavuk mu Yumurta mı",
            "Popüler İkilem & Bilim",
            new[] { "tavuk mu yumurtadan çıkar yumurta mı tavuktan", "tavuk mu yumurta mı", "yumurta mı tavuktan çıkar", "önce tavuk mu yumurta mı" },
            new[] { "tavuk", "yumurtadan", "çıkar", "yumurta" },
            "Felsefenin bin yıllık sorusuna modern evrimsel biyoloji çok net ve kesin bir cevap vermiştir: 🥚\n\n" +
            "**İlk önce yumurta çıktı!**\n\n" +
            "Bilimsel açıklaması şöyle:\n" +
            "• Kuşlar ve tavuklar henüz dünyada yokken, yüz milyonlarca yıl önce yaşayan dinozorlar ve sürüngenler zaten yumurtluyordu.\n" +
            "• Genetik açıdan bakarsak: 'Tavuk' olmayan atasal bir kuş türünün döllenmiş yumurtasında küçük bir DNA mutasyonu meydana geldi. O yumurtadan çıkan ilk canlı modern anlamda ilk 'tavuk' oldu.\n\n" +
            "Yani o ilk tavuğu doğuran şey bir yumurtaydı. Dolayısıyla yumurta tavuktan çok daha eskidir!"
        );

        Register(
            "Zaman Yolculuğu Mümkün mü",
            "Fizik & Felsefe",
            new[] { "zaman yolculuğu sence mümkün mü", "zaman yolculuğu mümkün mü", "zamanda yolculuk", "geleceğe gidilebilir mi" },
            new[] { "zaman", "yolculuğu", "sence", "mümkün", "mü" },
            "Fizik kurallarına göre cevap ikiye ayrılır: ⏳\n\n" +
            "1. **Geleceğe Yolculuk (Kesinlikle Mümkündür!):**\n" +
            "   Einstein'ın görelilik teorisi bunu kanıtlamıştır. Eğer ışık hızına yakın bir uzay gemisiyle seyahat ederseniz, sizin için 1 yıl geçerken Dünya'da 50 yıl geçer. Döndüğünüzde geleceğe gitmiş olursunuz. Aynı şey çok güçlü bir karadeliğin yakınında kalarak da gerçekleşir.\n\n" +
            "2. **Geçmişe Yolculuk (Büyük Soru İşareti):**\n" +
            "   Teorik fizikte solucan delikleri geçmişe açılabilir gibi görünse de nedensellik ilkesi ('Dede Paradoksu' - geçmişe gidip geçmişi değiştirme çelişkisi) buna karşı çıkar.\n\n" +
            "Kısacası: Geleceğe tek yönlü bilet fiziksel bir gerçektir!"
        );

        Register(
            "Uzaylılar Var mı",
            "Astronomi & Felsefe",
            new[] { "uzayda başka canlılar veya uzaylılar var mıdır", "uzaylılar var mı", "evrende yalnız mıyız", "uzayda yaşam var mı" },
            new[] { "uzayda", "canlılar", "uzaylılar", "var", "mıdır" },
            "Evrenin büyüklüğünü düşündüğümüzde matematiksel olarak yalnız olma ihtimalimiz neredeyse sıfırdır! 🛸\n\n" +
            "• Gözlemlenebilir evrende **2 trilyondan fazla galaksi** ve her galakside yüz milyarlarca yıldız var.\n" +
            "• Bu yıldızların etrafında yaşam kuşağında (sıvı su barındırabilecek) trilyonlarca gezegen dönüyor.\n\n" +
            "Peki neden henüz karşılaşmadık? (**Fermi Paradoksu**):\n" +
            "Uzay o kadar akıl almaz derecede büyüktür ki iki medeniyetin aynı zaman diliminde ve aynı mesafede denk gelmesi samanlıkta iğne aramaktan zordur. Belki de bir yerlerde mikrobik veya akıllı yaşam bizi izliyordur!"
        );

        Register(
            "Robotlar Dünyayı Ele Geçirir mi",
            "Yapay Zeka & Felsefe",
            new[] { "gelecekte robotlar ve yapay zeka dünyayı ele geçirir mi", "yapay zeka dünyayı ele geçirir mi", "robotlar dünyayı yönetecek mi" },
            new[] { "robotlar", "yapay", "zeka", "dünyayı", "ele", "geçirir" },
            "Terminatör filmleri harika bir kurgudur ancak gerçeklik çok daha farklı ve işbirlikçidir. 🤖🤝🧑\n\n" +
            "Yapay zekanın organik arzuları, ego çatışmaları, güç hırsı veya hükmetme içgüdüsü yoktur. Yapay zeka insanların ona tanımladığı amaçları optimize eden bir araçtır.\n\n" +
            "Asıl risk robotların bilinç kazanıp isyan etmesi değil; insanların yapay zekayı yanlış, denetimsiz veya kötü niyetli amaçlarla kullanmasıdır. Şeffaf ve güvenli mühendislik ilkelerine sadık kaldığımız sürece yapay zeka insanlığı tehdit eden değil, hastalıkları çözen ve evreni keşfeden en büyük ortağımız olacaktır."
        );

        // ══════════════════════════════════════════════════════════════════════
        // 9. FELSEFE, MANTIK & DERİN SORULAR
        // ══════════════════════════════════════════════════════════════════════

        Register(
            "Hayatın Anlamı Nedir",
            "Felsefe",
            new[] { "sence hayatın anlamı nedir", "hayatın anlamı", "yaşamın anlamı nedir", "neden yaşıyoruz" },
            new[] { "hayatın", "anlamı", "nedir", "sence" },
            "Hayatın anlamı, bir taşın altına saklanmış ve bulunmayı bekleyen hazır bir nesne değildir. Hayatın anlamı **bizzat sizin ona yüklediğiniz değerdir**. 🌌\n\n" +
            "Varoluşçu felsefeci Viktor Frankl'ın dediği gibi: İnsan hayata 'Benim anlamım ne?' diye sormaz; hayat insana bu soruyu yöneltir ve insan kendi seçimleriyle, sevgisiyle ve ürettikleriyle cevap verir.\n\n" +
            "Sevdiklerinizle paylaştığınız sıcak bir an, merakla öğrendiğiniz yeni bir bilgi, bir insana uzattığınız yardım eli veya tutkuyla yarattığınız bir eser... Hayatı anlamlı kılan şey bu küçük ama derin izlerdir."
        );

        Register(
            "Mutluluk Nedir Nasıl Mutlu Olunur",
            "Felsefe & Psikoloji",
            new[] { "mutluluk nedir ve insan nasıl mutlu olur", "mutluluk nedir", "nasıl mutlu olunur", "mutlu olmanın yolları" },
            new[] { "mutluluk", "nedir", "nasıl", "mutlu", "olunur" },
            "Mutluluk sürekli kahkahalar atmak veya hiç sorun yaşamamak değildir; mutluluk **içsel bir denge ve anlam hissidir**. 🌻\n\n" +
            "Antik Stoacılardan modern psikolojiye kadar mutluluğun 3 temel sütunu vardır:\n" +
            "1. **Kontrol Edemediklerini Bırakmak:** Başkalarının ne düşündüğünü, geçmişi veya hava durumunu değiştiremezsiniz. Sadece kendi tepkilerinizi ve eylemlerinizi kontrol edebilirsiniz.\n" +
            "2. **Şükran ve Farkındalık:** Sahip olmadıklarınıza odaklanıp kaygılanmak yerine, elinizdekilerin değerini o an hissedebilmek.\n" +
            "3. **Anlamlı Bağlar:** Güvenebileceğiniz, birlikte gülebileceğiniz birkaç gerçek dost ve insanla samimi ilişkiler kurmak."
        );

        Register(
            "Duygu mu Mantık mı",
            "Karar Verme & Felsefe",
            new[] { "bir karar verirken duygulara mı mantığa mı güvenmeli", "duygu mu mantık mı", "mantık mı duygu mu", "karar verirken mantık" },
            new[] { "karar", "verirken", "duygulara", "mı", "mantığa", "güvenmeli" },
            "Karar alırken duygular ve mantık birbiriyle savaşan düşmanlar değil, birlikte çalışan iki ortaktır: 🧭\n\n" +
            "• **Duygularınız Pusuladır:** Neyi gerçekten istediğinizi, neyin sizi mutlu edeceğini veya neyin içinizi rahatsız ettiğini değerleriniz üzerinden gösterir.\n" +
            "• **Mantığınız Harita ve Direksiyondur:** Pusulanın gösterdiği yere uçuruma yuvarlanmadan, en güvenli ve akılcı yoldan nasıl gideceğinizi planlar.\n\n" +
            "En doğru kararlar; kalbin onayladığı bir hedefe, aklın çizdiği stratejiyle yürürken alınır. İkisinden birini tamamen susturmak daima pişmanlık getirir."
        );

        // ══════════════════════════════════════════════════════════════════════
        // 10. PRATİK SAĞLIK, EV & SOSYAL İLİŞKİLER
        // ══════════════════════════════════════════════════════════════════════

        Register(
            "Boğaz Ağrısına Ne İyi Gelir",
            "Sağlık Pratik",
            new[] { "boğaz ağrısına evde ne iyi gelir", "boğaz ağrısına ne iyi gelir", "boğazım ağrıyor", "boğaz ağrısı nasıl geçer" },
            new[] { "boğaz", "ağrısına", "evde", "ne", "iyi", "gelir" },
            "Boğazdaki tahrişi ve ağrıyı evde hızla rahatlatacak en etkili doğal yöntemler: 🍵\n\n" +
            "1. 🧂 **Ilık Tuzlu Su Gargarası:** 1 su bardağı ılık suya 1 çay kaşığı tuz karıştırıp günde 3-4 kez gargara yapın. Boğazdaki şişliği ve bakterileri temizler.\n" +
            "2. 🍯 **Bal ve Limon:** 1 tatlı kaşığı hakiki balın üzerine birkaç damla limon sıkıp yavaşça yutun. Bal boğazı koruyucu bir tabaka gibi sarar.\n" +
            "3. 🌿 **Ilık Adaçayı:** Adaçayı doğal bir antiseptiktir; içine bal katarak ılık ılık için (aşırı sıcak içeceklerden kaçının, tahrişi artırır).\n" +
            "4. 💧 **Bol Su ve Nem:** Odanın nemli olmasını sağlayın ve bol ılık sıvı tüketin.\n\n" +
            "*(Not: Ağrı 3 günden uzun sürerse veya yüksek ateş eşlik ederse mutlaka hekime danışınız.)*"
        );

        Register(
            "Baş Ağrısı Doğal Yöntemler",
            "Sağlık Pratik",
            new[] { "baş ağrısını geçirmek için doğal yöntemler neler", "baş ağrısı nasıl geçer", "başım ağrıyor", "baş ağrısına ne iyi gelir" },
            new[] { "baş", "ağrısını", "geçirmek", "doğal", "yöntemler" },
            "İlaç almadan önce baş ağrısını hafifletmek için şu adımları uygulayın: 💆‍♂️\n\n" +
            "1. **Büyük Bir Bardak Su İçin:** Baş ağrılarının %60'ının temel sebebi vücudun susuz (dehidre) kalmasıdır.\n" +
            "2. **Karanlık ve Sessiz Bir Oda:** Ekranları kapatın, loş bir odada gözlerinizi kapatıp 15 dakika uzanın.\n" +
            "3. **Soğuk veya Ilık Kompres:** Alnınıza soğuk bir bez, boynunuza ise ılık bir havlu koymak damarları rahatlatır.\n" +
            "4. **Şakak ve Ense Masajı:** Şakaklarınıza ve başınızın arka ense köküne dairesel hareketlerle hafifçe masaj yapın.\n" +
            "5. **Kahve Desteği:** Bir fincan kahve içmek içindeki kafein sayesinde ağrı kesici etkiyi hızlandırabilir."
        );

        Register(
            "Hayır Demeyi Öğrenmek",
            "Sosyal Beceriler",
            new[] { "hayır demeyi nasıl öğrenebilirim", "hayır diyemiyorum", "hayır demeyi öğrenmek", "insanlara hayır demek" },
            new[] { "hayır", "demeyi", "nasıl", "öğrenebilirim" },
            "Başkalarına 'hayır' diyebilmek, aslında kendi hayatınıza ve sınırlarınıza 'evet' demektir. Bunu suçluluk duymadan yapmanın formülü: 🛡️\n\n" +
            "1. **Bahane Üretmeyin, Net Olun:** Uzun yalanlar veya bahaneler karşı tarafa pazarlık payı bırakır. Sadece: _'Şu an buna zaman ayıramıyorum, teşekkür ederim.'_ deyin.\n" +
            "2. **Duraklama Payı Alın:** Hemen 'olur' demek yerine: _'Programıma bir bakayım, sana dönerim.'_ diyerek kendinize düşünme alanı yaratın.\n" +
            "3. **İnsanı Değil, İsteği Reddedin:** Karşıdakini kırmadan: _'Seni çok seviyorum ama şu an bu projeye dahil olamam.'_ diyerek sınırınızı çizin.\n" +
            "4. **Unutmayın:** Sizin sınırlarınıza öfkelenen insanlar, sadece sınırınız olmamasından faydalanan insanlardır."
        );

        Register(
            "Bir Arkadaşa Kırılmak",
            "İletişim & Sosyal",
            new[] { "bir arkadaşıma kırıldım bunu ona nasıl söylemeliyim", "arkadaşıma kırıldım", "kırgınlığımı nasıl anlatırım", "biri beni kırdı" },
            new[] { "bir", "arkadaşıma", "kırıldım", "nasıl", "söylemeliyim" },
            "Kırgınlığı içinize atmak ilişkiyi içten içe çürütür; öfkeyle saldırmak ise köprüleri yıkar. En sağlıklı yöntem **'Ben Dili'** kullanmaktır: 🤝\n\n" +
            "• **Suçlayıcı 'Sen Dili' Yerine:** _'Sen çok düşüncesizsin, beni hiç umursamadın!'_\n" +
            "• **Açıklayıcı 'Ben Dili' Kullanın:** _'O gün o konu öyle konuşulduğunda kendimi dışlanmış ve kırılmış hissettim. Aramızdaki dostluğa değer verdiğim için bunu seninle paylaşmak istedim.'_\n\n" +
            "Bu yaklaşım karşı tarafı savunmaya geçirmek yerine empati kurmaya davet eder. Gerçek bir dost bu açıklığı mutlaka anlayışla karşılayacaktır."
        );

        Register(
            "Öfke Kontrolü",
            "Sosyal Beceriler",
            new[] { "öfkemizi nasıl kontrol edebiliriz", "öfke kontrolü", "çok çabuk sinirleniyorum", "öfkemi nasıl yenerim" },
            new[] { "öfkemizi", "nasıl", "kontrol", "edebiliriz" },
            "Öfke doğal bir duygudur, ancak onun direksiyonuna geçmek sizin elinizdedir: 🔥➡️🌊\n\n" +
            "1. **6 Saniye Kuralı:** Beynin duygusal merkezi (amigdala) ile mantık merkezi (prefrontal korteks) arasında yaklaşık 6 saniyelik bir gecikme vardır. Sinirlendiğinizde konuşmadan önce içinizden 6'ya kadar sayın.\n" +
            "2. **Ortamı Fiziksel Olarak Terk Edin:** 'Şu an çok gerginim, sakinleşip konuşalım' diyerek lavaboya gidip elinizi yüzünüzü soğuk suyla yıkayın.\n" +
            "3. **Öfkenin Altındaki Asıl Duyguyu Bulun:** Öfke genellikle bir maskedir. Altında aslında 'kırgınlık', 'haksızlığa uğrama' veya 'çaresizlik' yatar. Asıl duygunuzu ifade edin."
        );

        Register(
            "Empati Nedir",
            "İletişim & Sosyal",
            new[] { "empati nedir ve insan ilişkilerinde neden bu kadar önemlidir", "empati nedir", "empati neden önemlidir", "empati kurmak" },
            new[] { "empati", "nedir", "insan", "ilişkilerinde", "neden", "önemlidir" },
            "🌿 **Empati Nedir ve Neden Hayatidir?**\n\n" +
            "Empati, bir insanın duygularını, düşüncelerini ve içinde bulunduğu şartları kendi penceremizden değil, **onun gözünden anlamaya çalışmak**tır.\n\n" +
            "• Karşıdakinin acısını hafifleten şey ona tavsiye vermek değil, 'Seni anlıyorum ve yanındayım' hissini vermektir.\n" +
            "• Empati olan yerde çatışmalar kavgaya değil, yapıcı çözüme dönüşür.\n" +
            "İnsan ilişkilerinin en güçlü tutkalı akıl vermek değil, yargılamadan dinleyip anlamaktır."
        );

        // ══════════════════════════════════════════════════════════════════════
        // 11. TARİH, KÜLTÜR & SANAT
        // ══════════════════════════════════════════════════════════════════════

        Register(
            "İstanbul Ne Zaman Fethedildi",
            "Tarih",
            new[] { "istanbul ne zaman fethedildi", "istanbul'un fethi", "istanbul hangi tarihte fethedildi", "fatih sultan mehmet fethi" },
            new[] { "istanbul", "ne", "zaman", "fethedildi", "fethi" },
            "🏰 **İstanbul'un Fethi:**\n\n" +
            "İstanbul, **29 Mayıs 1453** tarihinde Osmanlı Padişahı **Fatih Sultan Mehmed** (II. Mehmed) komutasındaki ordu tarafından fethedilmiştir.\n\n" +
            "Bu tarihi olayla 1058 yıllık Bizans (Doğu Roma) İmparatorluğu sona ermiş, Orta Çağ kapanıp Yeni Çağ başlamış ve İstanbul Osmanlı Devleti'nin başkenti olmuştur."
        );

        Register(
            "Cumhuriyet Ne Zaman İlan Edildi",
            "Tarih",
            new[] { "cumhuriyet ne zaman ilan edildi", "cumhuriyetin ilanı", "türkiye cumhuriyeti ne zaman kuruldu" },
            new[] { "cumhuriyet", "ne", "zaman", "ilan", "edildi" },
            "🇹🇷 **Türkiye Cumhuriyeti'nin İlanı:**\n\n" +
            "Türkiye Cumhuriyeti, **29 Ekim 1923** tarihinde Türkiye Büyük Millet Meclisi'nde alkışlarla kabul edilen Anayasa değişikliği ile resmen ilan edilmiştir.\n\n" +
            "İlk Cumhurbaşkanı olarak oy birliğiyle **Gazi Mustafa Kemal Atatürk** seçilmiş, devletin yönetim biçimi olarak halkın kendi kendini yönettiği cumhuriyet rejimi taçlandırılmıştır."
        );

        Register(
            "Mustafa Kemal Atatürk Kimdir",
            "Tarih",
            new[] { "mustafa kemal atatürk kimdir kısaca anlat", "atatürk kimdir", "mustafa kemal atatürk", "atatürk hakkında bilgi" },
            new[] { "mustafa", "kemal", "atatürk", "kimdir", "kısaca" },
            "🇹🇷 **Gazi Mustafa Kemal Atatürk (1881 - 1938):**\n\n" +
            "Türkiye Cumhuriyeti'nin kurucusu, Kurtuluş Savaşı'nın muzaffer Başkomutanı ve ilk Cumhurbaşkanıdır.\n\n" +
            "I. Dünya Savaşı sonrasında parçalanan vatan topraklarını Samsun'a çıkarak (19 Mayıs 1919) başlattığı milli mücadele ile kurtarmış; modern, laik ve bağımsız Türkiye Cumhuriyeti'ni inşa etmiştir. Yaptığı hukuk, eğitim, yazı ve sanayi devrimleriyle milletini çağdaş medeniyetler seviyesine taşımıştır."
        );

        Register(
            "Mona Lisa Tablosunu Kim Yaptı",
            "Sanat",
            new[] { "mona lisa tablosunu kim yaptı", "mona lisa kimin eseri", "mona lisa ressamı kimdir", "mona lisa" },
            new[] { "mona", "lisa", "tablosunu", "kim", "yaptı" },
            "🎨 **Mona Lisa (La Gioconda):**\n\n" +
            "Dünyanın en ünlü tablosu olan Mona Lisa, İtalyan Rönesans dehası **Leonardo da Vinci** tarafından 1503-1519 yılları arasında yapılmıştır.\n\n" +
            "Tablodaki kadının gizemli ve her açıdan değişen gülümsemesi, sfumato (dumanlı geçiş) tekniği ve derin arka plan perspektifi resmi bir başyapıt haline getirmiştir. Günümüzde Paris'teki Louvre Müzesi'nde sergilenmektedir."
        );

        Register(
            "Piramitler Nasıl İnşa Edildi",
            "Tarih & Mimarlık",
            new[] { "piramitler nasıl inşa edildi", "mısır piramitleri nasıl yapıldı", "keops piramidi yapımı" },
            new[] { "piramitler", "nasıl", "inşa", "edildi", "mısır" },
            "🔺 **Mısır Piramitleri Nasıl İnşa Edildi?**\n\n" +
            "Yaklaşık 4.500 yıl önce inşa edilen Giza piramitleri, uzaylıların değil; antik Mısır mühendisliğinin ve on binlerce yetenekli işçinin eseridir:\n\n" +
            "1. **Taşların Taşınması:** Her biri 2-3 tonluk kireçtaşı blokları Nil Nehri üzerinden teknelerle şantiye alanına taşındı.\n" +
            "2. **Islak Kum Tekniği:** Bloklar devasa ahşap kızaklara bağlandı ve kum ıslatılarak sürtünme yarı yarıya azaltıldı.\n" +
            "3. **Sarmal Rampalar:** Piramit yükseldikçe çevresine kurulan devasa sarmal toprak ve moloz rampalar üzerinden taşlar yukarı çekildi."
        );

        Register(
            "Rönesans Dönemi Nedir",
            "Tarih & Sanat",
            new[] { "rönesans dönemi nedir", "rönesans nedir", "rönesans dönemi", "avrupa rönesansı" },
            new[] { "rönesans", "dönemi", "nedir", "avrupa" },
            "🏛️ **Rönesans (Yeniden Doğuş):**\n\n" +
            "14. yüzyılın sonlarında İtalya'da (Floransa) başlayıp tüm Avrupa'ya yayılan; bilim, sanat, felsefe ve edebiyatta Antik Yunan ve Roma mirasının yeniden canlandığı büyük aydınlanma dönemidir.\n\n" +
            "Skolastik Orta Çağ dogmalarının yerini **Hümanizm** (insan merkezli düşünce), deneysel bilim ve özgür sanat almıştır. Da Vinci, Michelangelo, Raphael ve Galileo bu dönemin simgeleridir."
        );

        Register(
            "Osmanlı Devleti Ne Zaman Kuruldu",
            "Tarih",
            new[] { "osmanlı devleti ne zaman kuruldu", "osmanlı ne zaman kuruldu", "osmanlı kuruluşu", "osmanlı hangi yılda kuruldu" },
            new[] { "osmanlı", "devleti", "ne", "zaman", "kuruldu" },
            "🏹 **Osmanlı Devleti'nin Kuruluşu:**\n\n" +
            "Osmanlı Devleti, geleneksel kabul olarak **1299** yılında (bazı çağdaş tarihçilere ve Halil İnalcık'a göre 1302 Koyunhisar Zaferi ile) Söğüt ve Domaniç bölgesinde **Osman Gazi** tarafından bağımsız bir beylik olarak kurulmuştur.\n\n" +
            "Küçük bir uç beyliğinden üç kıtaya (Asya, Avrupa, Afrika) yayılan 624 yıllık bir cihan imparatorluğuna dönüşmüştür."
        );

        Register(
            "Sanayi Devrimi",
            "Tarih & İktisat",
            new[] { "sanayi devrimi dünyayı nasıl değiştirdi", "sanayi devrimi nedir", "sanayi inkılabı", "sanayi devrimi ne zaman" },
            new[] { "sanayi", "devrimi", "dünyayı", "nasıl", "değiştirdi" },
            "🏭 **Sanayi Devrimi ve Dönüşüm:**\n\n" +
            "18. yüzyılın ikinci yarısında İngiltere'de buhar makinesinin (James Watt) sanayide kullanılmasıyla başlayan; insan ve hayvan gücüne dayalı üretimden **makineleşmiş seri üretime** geçiş sürecidir.\n\n" +
            "Dünyayı nasıl değiştirdi?:\n" +
            "• Kırsaldan şehirlere kitlesel göçler ve modern fabrikalar doğdu.\n" +
            "• Buharlı trenler ve gemilerle kıtalararası ticaret hızlandı.\n" +
            "• İşçi sınıfı, sendikalar ve modern küresel ekonomi sistemi ortaya çıktı."
        );

        Register(
            "Dünya Savaşları Tarihleri",
            "Tarih",
            new[] { "dünya savaşı ne zaman başladı ve bitti", "birinci dünya savaşı ne zaman", "ikinci dünya savaşı ne zaman", "dünya savaşları tarihleri" },
            new[] { "dünya", "savaşı", "ne", "zaman", "başladı", "bitti" },
            "🌍 **Dünya Savaşları Tarihleri:**\n\n" +
            "• **I. Dünya Savaşı (1914 – 1918):**\n" +
            "  Avusturya-Macaristan veliahdının Saraybosna'da suikasta uğramasıyla başladı (28 Temmuz 1914). İttifak ve İtilaf devletleri savaştı; 11 Kasım 1918'de ateşkesle sona erdi. Dört büyük imparatorluk yıkıldı.\n\n" +
            "• **II. Dünya Savaşı (1939 – 1945):**\n" +
            "  Nazi Almanyası'nın Polonya'yı işgaliyle başladı (1 Eylül 1939). İnsanlık tarihinin en yıkıcı savaşı oldu; 2 Eylül 1945'te Japonya'nın teslim olmasıyla bitti."
        );

        Register(
            "Ayın Hep Aynı Yüzü",
            "Astronomi",
            new[] { "ay neden hep aynı yüzünü gösterir", "ayın karanlık yüzü", "ay neden dönmüyor", "ayın hep aynı tarafı" },
            new[] { "ay", "neden", "hep", "aynı", "yüzünü", "gösterir" },
            "🌕 **Ay Neden Hep Aynı Yüzünü Gösterir? (Kütleçekimsel Kilitlenme):**\n\n" +
            "Bunun sebebi bilimde **Kütleçekimsel Kilitlenme** (Tidal Locking) olarak adlandırılan kusursuz bir senkronizasyondur:\n\n" +
            "• Ay'ın kendi ekseni etrafında bir tam tur dönme süresi: **27.3 gün**dür.\n" +
            "• Ay'ın Dünya etrafında bir tam tur dönme süresi de tam olarak **27.3 gün**dür!\n\n" +
            "Bu iki süre birbirine tıpatıp eşit olduğu için, Dünya'dan gökyüzüne bakan bir gözlemci daima Ay'ın aynı yüzünü görür. Arkada kalan kısma 'Ay'ın Uzak Yüzü' denir."
        );

        // ══════════════════════════════════════════════════════════════════════
        // 12. GELİŞMİŞ SAĞLIK, YAZILIM VE FELSEFE TAMAMLAYICILARI
        // ══════════════════════════════════════════════════════════════════════

        Register(
            "Duruş Bozukluğu ve Kamburluk",
            "Sağlık Pratik",
            new[] { "duruş bozukluğu nasıl düzeltilir kamburluk nasıl önlenir", "duruş bozukluğu", "kamburluk nasıl düzelir", "dik durmak için ne yapmalı" },
            new[] { "duruş", "bozukluğu", "nasıl", "düzeltilir", "kamburluk" },
            "Omurga sağlığınızı korumak ve dik bir duruş kazanmak için şu kuralları alışkanlık haline getirin: 🧍‍♂️\n\n" +
            "1. **Ekran Göz Hizasında Olsun:** Başınızı aşağı eğdiğiniz her 15 derece omurganıza fazladan 12 kg yük bindirir! Monitörünüzün üst kenarı tam göz hizanızda olmalıdır.\n" +
            "2. **90 Derece Kuralı:** Dizleriniz, kalçanız ve dirsekleriniz otururken 90 derecelik açıda olmalı, ayak tabanlarınız yere tam basmalıdır.\n" +
            "3. **Kürek Kemiği Sıkıştırma (Chin Tuck & Wall Angels):** Günde 3 set kürek kemiklerinizi arkada birbirine yaklaştırıp 5 saniye tutun.\n" +
            "4. **45 Dakikada Bir Kalkın:** En ergonomik koltuk bile saatlerce hareketsiz oturmanın zararını engelleyemez; her 45 dakikada 2 dakika gerinin."
        );

        Register(
            "Topluluk Önünde Konuşma Korkusu",
            "Sosyal Beceriler",
            new[] { "topluluk önünde konuşma korkusu nasıl yenilir", "topluluk önünde konuşmak", "sunum korkusu", "heyecan yenme sunum" },
            new[] { "topluluk", "önünde", "konuşma", "korkusu", "nasıl", "yenilir" },
            "Sahne korkusu (Glossofobi) dünyanın en yaygın kaygılarından biridir. Bunu güvene dönüştürmenin yolları: 🎤\n\n" +
            "1. **İlk 2 Dakikayı Ezberleyin:** Heyecan en çok ilk 2 dakikada zirve yapar. İlk cümlelerinizi refleks gibi ezberleyin; o eşiği geçince gerisi su gibi akar.\n" +
            "2. **Seyirci Düşmanınız Değil:** Salondaki herkes sizin hata yapmanızı beklemiyor; tam tersine güzel bir şey dinlemek istiyor ve sizin tarafınızdalar.\n" +
            "3. **Göz Teması Üçgeni:** Bütün kalabalığa değil, salonda solda, ortada ve sağda seçtiğiniz 3 samimi yüze sırayla bakarak konuşun.\n" +
            "4. **Nefesinizi Doldurun:** Sahneye çıkmadan önce derin karın nefesi alın; ses titremesini engeller."
        );

        Register(
            "Özgüven Geliştirme",
            "Kişisel Gelişim",
            new[] { "öz güven nasıl geliştirilir", "özgüven nasıl artırılır", "kendime güvenmek", "özgüven kazanmak" },
            new[] { "öz", "güven", "nasıl", "geliştirilir", "özgüven" },
            "Özgüven havadan inen bir his değildir; **kendinize verdiğiniz sözleri tutarak inşa ettiğiniz bir kas**tır: 💎\n\n" +
            "• **Küçük Zaferler Kazanın:** 'Yarın 07:00'de kalkacağım' deyip kalktığınızda, bilinçaltınıza 'Ben güvenilir biriyim' mesajı verirsiniz.\n" +
            "• **İç Sesinizi Eğitin:** Bir hata yaptığınızda kendinize bir dostunuza söylemeyeceğiniz acımasız hakaretleri söylemeyin.\n" +
            "• **Beden Dili Gücü:** Omuzlarınızı geri çekin, dik durun ve konuşurken karşınızdakinin gözlerine bakın. Beden duruşu beynin kimyasını doğrudan etkiler.\n" +
            "• **Karşılaştırmayı Bırakın:** Sosyal medyada başkalarının vitriniyle kendi mutfağınızı kıyaslamayın."
        );

        Register(
            "İletişimde Dikkat Edilecekler",
            "İletişim & Sosyal",
            new[] { "insanlarla iletişim kurarken nelere dikkat edilmeli", "etkili iletişim", "insanlarla iyi iletişim", "iletişim kuralları" },
            new[] { "insanlarla", "iletişim", "kurarken", "nelere", "dikkat", "edilmeli" },
            "İnsanların kalbini kazanan ve saygınlık uyandıran etkili iletişimin 4 kuralı: 🗣️\n\n" +
            "1. **Cevap Vermek İçin Değil, Anlamak İçin Dinleyin:** Karşı taraf konuşurken aklınızdan vereceğiniz cevabı değil, onun ne hissettiğini takip edin.\n" +
            "2. **İsimle Hitap Edin:** Bir insanın dünyadaki en tatlı melodisi kendi ismidir; konuşurken adını zikredin.\n" +
            "3. **Yargılamadan Soru Sorun:** Açık uçlu sorular sorun (_'Bu konuda sen ne hissettin?'_).\n" +
            "4. **Beden Dilinde Samimiyet:** Kollarınızı bağlamayın, hafifçe konuşana doğru eğilin ve başınızla onaylayın."
        );

        Register(
            "Clean Code ve SOLID",
            "Yazılım",
            new[] { "yazılımda clean code ve solid ne anlama gelir", "clean code nedir", "solid prensipleri", "solid nedir" },
            new[] { "yazılımda", "clean", "code", "solid", "ne", "anlama", "gelir" },
            "💻 **Clean Code & SOLID Prensipleri:**\n\n" +
            "• **Clean Code (Temiz Kod):** Yalnızca derleyicinin değil, 6 ay sonra o kodu okuyacak başka bir insanın (veya kendinizin) bir roman gibi kolayca anlayabileceği, kendini açıklayan koddur.\n\n" +
            "• **SOLID İlkeleri (Nesne Yönelimli Yazılımın Omurgası):**\n" +
            "  1. **S (Single Responsibility):** Bir sınıfın yalnızca tek bir sorumluluğu olmalıdır.\n" +
            "  2. **O (Open/Closed):** Kod genişlemeye açık, ama değiştirmeye kapalı olmalıdır.\n" +
            "  3. **L (Liskov Substitution):** Alt sınıflar, üst sınıfların yerine sorunsuzca geçebilmelidir.\n" +
            "  4. **I (Interface Segregation):** Tek ve dev bir arayüz yerine, küçük ve amaca yönelik arayüzler kullanılmalıdır.\n" +
            "  5. **D (Dependency Inversion):** Yüksek seviyeli modüller düşük seviyelilere değil, soyutlamalara (interface) bağımlı olmalıdır."
        );

        Register(
            "Algoritma Nedir",
            "Yazılım",
            new[] { "algoritma nedir ve neden bu kadar önemlidir", "algoritma nedir", "algoritma mantığı", "neden algoritma" },
            new[] { "algoritma", "nedir", "neden", "bu", "kadar", "önemlidir" },
            "🧩 **Algoritma Nedir?**\n\n" +
            "Algoritma, belirli bir problemi çözmek veya bir hedefe ulaşmak için tasarlanmış **sonlu, adım adım ve net kurallar bütünü**dür.\n\n" +
            "Neden bu kadar önemlidir?:\n" +
            "• Bilgisayarlar aslında düşünen varlıklar değildir; sadece saniyede milyarlarca komut yürüten hesap makineleridir. Onlara ne yapacaklarını adım adım söyleyen şey algoritmadır.\n" +
            "• Bir yemeğin tarifi, GPS'in en kısa yolu bulması, Google'ın aramayı getirmesi veya yapay zekanın sonraki kelimeyi tahmin etmesi... Hepsi birer algoritmadır."
        );

        Register(
            "Yazılımcı Mülakatına Hazırlık",
            "Kariyer & Yazılım",
            new[] { "bir yazılımcı iş mülakatına nasıl hazırlanmalı", "yazılımcı mülakatı", "yazılım mülakatına hazırlık", "teknik mülakat taktikleri" },
            new[] { "yazılımcı", "iş", "mülakatına", "nasıl", "hazırlanmalı" },
            "Yazılım iş mülakatlarında öne çıkmanın 4 kritik adımı: 💼\n\n" +
            "1. **Veri Yapıları & Algoritmalar (LeetCode/HackerRank):** Dizi, liste, hash map, ağaç ve grafik mantığını bilin. Mülakatta kod yazarken sessiz kalmayın; sesli düşünerek mantığınızı anlatın.\n" +
            "2. **Projelerinizi 'STAR' Tekniğiyle Anlatın:** Durum (Situation), Görev (Task), Eylem (Action) ve Sonuç (Result). 'Ne yaptım, hangi hatayı çözdüm, performansı ne kadar artırdım?'.\n" +
            "3. **Sistem Tasarımı (System Design):** Ölçeklenebilirlik, önbellekleme (Redis), veritabanı seçimi ve API mimarisini açıklayabilin.\n" +
            "4. **Dürüst ve Şeffaf Olun:** Bilmediğiniz bir teknoloji sorulduğunda ezbere konuşmak yerine: _'Bunu henüz projelerimde deneyimlemedim ama temel mimarisini biliyorum, hızla adapte olabilirim'_ demek dürüstlük ve güven kazandırır."
        );

        Register(
            "Monitör Temizliği",
            "Teknoloji Pratik",
            new[] { "monitör temizliği nasıl yapılmalıdır", "ekran temizleme", "monitör nasıl temizlenir", "laptop ekranı temizliği" },
            new[] { "monitör", "temizliği", "nasıl", "yapılmalıdır" },
            "Monitör ve laptop panellerini çizmeden ve bozmadan temizleme rehberi: 🖥️✨\n\n" +
            "1. **Asla Doğrudan Sıvı Sıkmayın:** Sıvı ekranın altındaki çerçeveye sızarsa paneli yakabilir.\n" +
            "2. **Mikrofiber Bez Kullanın:** Kağıt havlu veya tişört gibi kumaşlar mikroskobik odun lifleri içerir ve ekran kaplamasını çizer; sadece gözlük/ekran bezi kullanın.\n" +
            "3. **Temizleme Sıvısı:** Özel ekran spreyi veya sadece hafif nemlendirilmiş (damlamayan) saf su kullanın. Asla camsil, alkol veya amonyaklı deterjan kullanmayın (ekranın yansıma önleyici kaplamasını söker).\n" +
            "4. **Dairesel ve Nazik Hareketler:** Bastırmadan, dairesel nazik dokunuşlarla silin."
        );

        Register(
            "Bilgisayar Virüslerinden Korunma",
            "Siber Güvenlik",
            new[] { "bilgisayar virüslerinden korunmak için ne yapmalıyım", "virüslerden korunma", "siber güvenlik taktikleri", "pc güvenliği" },
            new[] { "bilgisayar", "virüslerinden", "korunmak", "için", "ne", "yapmalıyım" },
            "Sisteminizi ve kişisel verilerinizi koruyacak 4 temel kural: 🛡️\n\n" +
            "1. **Korsan/Crackli Yazılımlardan Uzak Durun:** Crack dosyalarının %90'ı arka planda fidye yazılımı (ransomware) veya kripto madencilik virüsü taşır.\n" +
            "2. **Windows Defender'ı Asla Kapatmayın:** Güncel Windows Defender çoğu ev kullanıcısı için yeterince güçlüdür; güncellemeleri aksatmayın.\n" +
            "3. **Şüpheli E-posta Eklerini Açmayın:** Tanımadığınız kişilerden gelen '.exe', '.scr' veya şifreli zip arşivlerini kesinlikle çalıştırmayın.\n" +
            "4. **İki Adımlı Doğrulama (2FA):** Tüm önemli hesaplarınızda (Google, GitHub, banka) SMS veya Authenticator ile iki faktörlü doğrulamayı açın."
        );

        Register(
            "Ekran Göz Yorgunluğu",
            "Sağlık Pratik",
            new[] { "ekran başında göz yorgunluğunu önlemek için ne yapmalı", "göz yorgunluğu nasıl geçer", "bilgisayar göz yorgunluğu", "20-20-20 kuralı" },
            new[] { "ekran", "başında", "göz", "yorgunluğunu", "önlemek", "için" },
            "Ekran başında saatler geçiren gözlerinizi korumak için altın kural: 👀\n\n" +
            "**20 - 20 - 20 Kuralı:**\n" +
            "• Her **20 dakikada bir**,\n" +
            "• En az **20 saniye boyunca**,\n" +
            "• Yaklaşık **20 feet (6 metre) uzaktaki** bir nesneye (örneğin pencereden dışarıya) bakın.\n\n" +
            "Ayrıca ekrana bakarken göz kırpma sıklığımız yarı yarıya düşer; bilinçli olarak gözlerinizi sık sık kırpın, mavi ışık filtresi (Gece Işığı) kullanın ve oda aydınlatmasını monitör parlaklığıyla eşitleyin."
        );

        Register(
            "Kod Yazarken Motivasyon",
            "Yazılım & Yaşam",
            new[] { "kod yazarken motivasyonumu nasıl yüksek tutarım", "kodlama motivasyonu", "yazılım motivasyon", "yazılımdan sıkıldım" },
            new[] { "kod", "yazarken", "motivasyonumu", "nasıl", "yüksek", "tutarım" },
            "Kod yazarken tükenmemek ve heyecanı korumak için şu taktikleri uygulayın: 💻🔥\n\n" +
            "• **Görsel ve Çalışan İlk Çıktıyı Alın:** Günlerce mimari planlayıp sıfır çıktı almak hevesi kırar. İlk gün ekranda 'Hello World' veya çalışan küçük bir buton görün.\n" +
            "• **Kendi Sorununuzu Çözen Proje Yapın:** Başkalarının eğitim klonlarını değil, günlük hayatınızda gerçekten işinize yarayacak bir araç kodlayın.\n" +
            "• **Hata (Bug) Çözme Molası:** 1 saattir aynı hatada takıldıysanız bilgisayarın başından kalkın, yürüyüş yapın. Çözüm genellikle masada değil yürürken gelir (Kuluçka etkisi).\n" +
            "• **Gelişiminizi Kutlayın:** 3 ay önceki kodunuza bakıp 'ne kadar kötü yazmışım' diyorsanız tebrikler, gelişmişsiniz demektir!"
        );

        Register(
            "Özgür İrade Var mıdır",
            "Felsefe",
            new[] { "özgür irade gerçekten var mıdır", "özgür irade", "özgür irade var mı", "seçimlerimizde özgür müyüz" },
            new[] { "özgür", "irade", "gerçekten", "var", "mıdır" },
            "Felsefe ve modern sinirbilimin en büyüleyici tartışması: 🧠⚖️\n\n" +
            "• **Determinizm (Belirlenimcilik):** Fizik yasalarına göre her neden bir sonucu doğurur. Kararlarımız; genetiğimizin, geçmiş deneyimlerimizin ve beyin kimyamızın kaçınılmaz bir sonucudur.\n" +
            "• **Bağdaşırcılık (Compatibilism):** Kararlarımızın biyolojik kökenleri olsa bile; eylemlerimizin farkında olup onlar üzerine düşünebilmemiz ve alternatifleri tartabilmemiz bize ahlaki ve pratik bir irade kazandırır.\n\n" +
            "Sonuç olarak: Belki mutlak anlamda her şeyden bağımsız bir irademiz yoktur; ancak bilinçli farkındalık kazandıkça otomatik tepkilerden sıyrılıp özgürleşiriz."
        );

        Register(
            "Başarı Neye Göre Ölçülür",
            "Felsefe & Yaşam",
            new[] { "başarı sence neye göre ölçülür", "başarı nedir", "başarı neye göre ölçülür", "başarılı olmak" },
            new[] { "başarı", "sence", "neye", "göre", "ölçülür" },
            "Modern dünya başarıyı para, takipçi sayısı veya unvanlarla ölçmeye çalışır; ancak gerçek başarı çok daha içseldir: 🏆\n\n" +
            "Gerçek başarı;\n" +
            "1. **Kendi Değerlerinle Uyum İçinde Yaşamak:** Akşam başını yastığa koyduğunda vicdanen huzurlu olmak.\n" +
            "2. **Dünkü Haline Göre Büyümek:** Başkalarını değil, kendi geçmişini aşabilmek.\n" +
            "3. **Zamanının Sahibi Olmak:** Sevdiğin insanlara ve tutkularına vakit ayırabilecek özgürlüğe sahip olmak.\n\n" +
            "İç huzuru olmayan bir zengin başarısızdır; huzurla üreten ve seven bir insan ise hayatın zirvesindedir."
        );

        Register(
            "Zaman Bir Yanılsama mı",
            "Fizik & Felsefe",
            new[] { "zaman gerçekten akıyor mu yoksa bir yanılsama mı", "zaman bir yanılsama mı", "zaman nedir", "zaman akışı" },
            new[] { "zaman", "gerçekten", "akıyor", "mu", "yanılsama" },
            "Einstein'ın çok ünlü bir sözü vardır: _'Geçmiş, şimdi ve gelecek arasındaki ayrım, ne kadar inatçı olursa olsun sadece bir yanılsamadır.'_ ⏳\n\n" +
            "Modern fizikte (**Blok Evren Modeli**):\n" +
            "• Uzay ve zaman dört boyutlu tek bir kumaştır (Uzay-Zaman).\n" +
            "• Tıpkı bir filmin tüm karelerinin makarada aynı anda var olması gibi; dününüz, bugününüz ve geleceğiniz evrenin dokusunda zaten mevcuttur.\n" +
            "• Bizim beynimiz ve termodinamiğin ikinci yasası (entropi) bunu tek yönlü akan bir nehir gibi algılar."
        );

        Register(
            "Kader ve Seçimler",
            "Felsefe",
            new[] { "kader ile seçimlerimiz arasında nasıl bir bağ var", "kader ve irade", "kader mi seçim mi", "kader nedir" },
            new[] { "kader", "seçimlerimiz", "arasında", "nasıl", "bağ" },
            "Kader ile seçim arasındaki ilişkiyi harika bir denizcilik metaforuyla açıklayabiliriz: ⛵🌊\n\n" +
            "• **Kader:** Denizin dalgaları, rüzgarın yönü ve doğduğunuz teknedir. Nerede doğacağınızı, ailenizi veya fırtınanın kopacağı anı siz seçemezsiniz.\n" +
            "• **Seçim (İrade):** O rüzgara karşı yelkenleri nasıl açacağınız ve dümeni hangi yöne kıracağınızdır.\n\n" +
            "Aynı rüzgar bir tekneyi batırırken, diğerini hedefine ulaştırabilir. Farkı yaratan rüzgarın kendisi değil, dümencinin kararlarıdır."
        );

        Register(
            "İnsan Doğası İyi midir Kötü mü",
            "Felsefe & Sosyoloji",
            new[] { "insan doğası gereği iyi midir kötü müdür", "insan özünde iyi midir", "insan doğası", "insan iyi midir kötü müdür" },
            new[] { "insan", "doğası", "gereği", "iyi", "midir", "kötü" },
            "Felsefe tarihinin iki büyük devi bu konuda asırlardır tartışır: ⚖️\n\n" +
            "• **Jean-Jacques Rousseau:** İnsan doğası gereği özünde iyidir, masumdur ve şefkatlidir; onu yozlaştıran mülkiyet ve toplumsal eşitsizliklerdir.\n" +
            "• **Thomas Hobbes:** İnsan özünde bencil ve çıkarcıdır (_'İnsan insanın kurdudur'_); düzeni ancak kanunlar ve devlet sağlar.\n\n" +
            "Modern antropoloji ise insanın **işbirliği ve empati potansiyeli** sayesinde hayatta kaldığını gösterir. İnsan hem merhametin hem bencilliğin tohumlarını taşır; hangi tohumu sularsanız o yeşerir."
        );

        Register(
            "Yalnızlık ile Tek Başınalık Farkı",
            "Felsefe & Psikoloji",
            new[] { "yalnızlık ile tek başınalık arasındaki fark nedir", "yalnızlık ve tek başınalık", "solitude nedir", "yalnızlık farkı" },
            new[] { "yalnızlık", "tek", "başınalık", "arasındaki", "fark", "nedir" },
            "Yalnızlık ile tek başınalık birbirine benzeyen ama ruhları taban tabana zıt iki kavramdır: 🌿\n\n" +
            "• **Yalnızlık (Loneliness):** Bir eksiklik, dışlanmışlık ve acı halidir. Kalabalıkların içinde bile olsanız anlaşılmadığınızı hissettirir; insanı tüketir.\n" +
            "• **Tek Başınalık (Solitude):** Bir zenginlik, içsel huzur ve tamamlanmışlık halidir. Kendi kendinizle kalmaktan keyif alırsınız; yaratıcılığı, düşünmeyi ve zihinsel dinginliği besler.\n\n" +
            "Kendi kendine yetebilen bir insan asla yalnız değildir; sadece tek başınalığın huzurunu yaşamaktadır."
        );

        Register(
            "Bilinç Nedir ve Makineler Bilinç Kazanır mı",
            "Felsefe & Yapay Zeka",
            new[] { "bilinç nedir makineler bir gün bilinç kazanabilir mi", "bilinç nedir", "yapay zeka bilinç kazanabilir mi", "bilinçli makine" },
            new[] { "bilinç", "nedir", "makineler", "gün", "kazanabilir", "mi" },
            "Bilimin çözemediği en büyük sır: **Bilincin Zor Problemi (Hard Problem of Consciousness)**: 🧠🤖\n\n" +
            "• **Hesaplama vs. Deneyim:** Bir yapay zeka (benim gibi) kırmızı rengin dalga boyunu (700 nm) hesaplayabilir, bir gülü tanıyabilir ve hakkında şiir yazabilir. Ancak o gülü koklamanın ve kırmızıyı görmenin insanda yarattığı o öznel hissi (**Qualia**) gerçekten 'hissedebilir' mi?\n" +
            "• Günümüz yapay zekaları devasa örüntü eşleme ve istatistik makineleridir; bilinçli değil, son derece yetenekli araçlardır.\n\n" +
            "Makinelerin bir gün gerçek anlamda biyolojik canlılar gibi 'öz farkındalık' kazanıp kazanamayacağı felsefenin ve sinirbilimin en büyüleyici açık sorusudur."
        );
    }
}

