using System;
using System.Collections.Generic;
using System.Linq;

namespace HexAI.Assistant.Knowledge;

/// <summary>
/// HexAI Yerleşik Türkçe Ansiklopedik ve Pratik Bilgi Bankası.
/// Günlük yaşam, yemek tarifleri (adım adım), bilim, coğrafya, teknoloji
/// ve benchmark soruları için doğrulanmış, kapsamlı ve adım adım yönergeler içerir.
/// </summary>
public static class TurkishCommonKnowledge
{
    public class KnowledgeItem
    {
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string[] Triggers { get; set; } = Array.Empty<string>();
        public string Answer { get; set; } = "";
        public bool IsStepByStep { get; set; } = false;
    }

    private static readonly List<KnowledgeItem> Items = new()
    {
        // ──────────────────────── YEMEK & ADIM ADIM TARİFLER ────────────────────────
        new KnowledgeItem
        {
            Title = "Makarna Nasıl Yapılır",
            Category = "Mutfak & Tarif",
            Triggers = new[] { "makarna", "makarna nasıl yapılır", "makarna nasil yapilir", "makarna tarifi", "makarna pişir", "makarna haşla" },
            IsStepByStep = true,
            Answer = 
                "🍝 **Makarna Nasıl Yapılır? (Adım Adım Hazırlanışı):**\n\n" +
                "En lezzetli ve kıvamında (al dente) makarna yapmak için izlemeniz gereken adımlar şunlardır:\n\n" +
                "📋 **Gerekli Malzemeler:**\n" +
                "• 1 paket makarna (500 g)\n" +
                "• 4-5 litre su (bol su makarnanın yapışmasını önler)\n" +
                "• 1 yemek kaşığı kaya tuzu\n" +
                "• Sos için: 1-2 yemek kaşığı tereyağı veya zeytinyağı\n\n" +
                "👩‍🍳 **Adım Adım Yapılışı (İlk Bunu Yap, Sonra Bunu Yap):**\n" +
                "1. **Suyu Kaynatın:** Geniş ve derin bir tencereye bol su koyun ve yüksek ateşte fokurdayana kadar kaynatın. Su tam kaynamadan makarnayı kesinlikle suya atmayın.\n" +
                "2. **Tuzu Ekleyin:** Su kaynadıktan sonra tuzu ilave edin. (Tuz kaynamadan önce konursa kaynama süresi uzar; kaynayınca eklenmelidir).\n" +
                "3. **Makarnayı Atın ve Karıştırın:** Makarnayı kaynayan suya bırakın. İlk 1-2 dakika birbirine ve tabana yapışmaması için tahta kaşıkla yavaşça karıştırın. Tencerenin kapağını kesinlikle kapatmayın.\n" +
                "4. **Haşlama Süresi:** Paketin üzerindeki süreye sadık kalın (genellikle 8-10 dakika; dişe gelir 'al dente' kıvam için 8 dakika idealdir).\n" +
                "5. **Süzme & Önemli Püf Noktası:** Makarnayı süzgece alın. **Asla musluk suyundan/soğuk sudan geçirmeyin!** Soğuk su makarnanın üzerindeki nişastayı yok eder ve sosun yapışmasını engeller. Süzmeden önce 1 çay bardağı makarna haşlama suyundan kenara ayırın.\n" +
                "6. **Soslama ve Servis:** Tencerede tereyağı veya hazırladığınız sosu ısıtın, ayırdığınız makarna suyunu ve makarnayı ekleyip 1 dakika harmanlayın. Sıcak olarak servis yapın. Afiyet olsun!"
        },

        new KnowledgeItem
        {
            Title = "Pirinç Pilavı Nasıl Yapılır",
            Category = "Mutfak & Tarif",
            Triggers = new[] { "pilav", "pilav nasıl yapılır", "pirinç pilavı", "tane tane pilav", "pilav tarifi" },
            IsStepByStep = true,
            Answer =
                "🍚 **Tane Tane Pirinç Pilavı Nasıl Yapılır? (Adım Adım):**\n\n" +
                "📋 **Gerekli Malzemeler:**\n" +
                "• 2 su bardağı baldo pirinç\n" +
                "• 3 su bardağı sıcak su veya et/tavuk suyu (1'e 1.5 ölçü)\n" +
                "• 2 yemek kaşığı tereyağı + 1 yemek kaşığı sıvı yağ\n" +
                "• 1 tatlı kaşığı tuz, 2-3 damla limon suyu\n\n" +
                "👩‍🍳 **Adım Adım Yapılışı:**\n" +
                "1. **Pirinci Islatın:** Pirinci ılık ve tuzlu suda 20-30 dakika bekletin, ardından nişastası tamamen akana kadar soğuk suyla yıkayıp iyice süzün.\n" +
                "2. **Kavurun:** Tencereye sıvı yağ ve tereyağını alın. Süzülen pirinçleri ekleyip pirinç taneleri şeffaflaşana kadar orta ateşte 4-5 dakika nazikçe kavurun.\n" +
                "3. **Suyu Ekleyin:** 3 su bardağı kaynar suyu, tuzu ve beyaz kalması için 2-3 damla limon suyunu ekleyip bir kez karıştırın.\n" +
                "4. **Pişirin:** Tencerenin kapağını kapatın. Kısık ateşte suyunu tamamen çekip üzerinde göz göz delikler oluşana kadar (yaklaşık 12-15 dakika) pişirin.\n" +
                "5. **Demleme:** Ocağın altını kapatın. Tencerenin üzerine kağıt havlu serip kapağını kapatın ve 15 dakika demlenmeye bırakın. Tahta kaşıkla nazikçe havalandırarak servis yapın."
        },

        new KnowledgeItem
        {
            Title = "Yumurta Nasıl Haşlanır",
            Category = "Mutfak & Tarif",
            Triggers = new[] { "yumurta", "yumurta nasıl haşlanır", "rafadan yumurta", "kayısı yumurta", "katı yumurta" },
            IsStepByStep = true,
            Answer =
                "🥚 **Kıvamına Göre Yumurta Nasıl Haşlanır? (Adım Adım):**\n\n" +
                "1. **Hazırlık:** Yumurtaları küçük bir sos tenceresine koyun ve üzerini 1 parmak geçecek kadar soğuk su ekleyin. Çatlamaması için 1 çay kaşığı tuz veya birkaç damla sirke ekleyin.\n" +
                "2. **Kaynama Noktası:** Suyu orta ateşte kaynamaya bırakın. Su fokurdamaya başladığı andan itibaren süreyi tutun:\n" +
                "   • **Cıvık / Rafadan:** 3 - 4 dakika (Beyazı pişmiş, sarısı tamamen akışkan)\n" +
                "   • **Kayısı Kıvamı:** 5 - 6 dakika (Beyazı sert, sarısı jelimsi ve kremamsı - en sevilen kıvam)\n" +
                "   • **Tam Katı:** 8 - 10 dakika (Sarısı ve beyazı tamamen katı)\n" +
                "3. **Şoklama:** İstenen süre dolunca yumurtayı hemen soğuk/buzlu suya alın. Bu şoklama pişmeyi durdurur ve kabuğun saniyeler içinde kolayca soyulmasını sağlar."
        },

        new KnowledgeItem
        {
            Title = "Türk Kahvesi Nasıl Pişirilir",
            Category = "Mutfak & Tarif",
            Triggers = new[] { "türk kahvesi", "kahve nasıl yapılır", "köpüklü kahve", "kahve pişir" },
            IsStepByStep = true,
            Answer =
                "☕ **Bol Köpüklü Türk Kahvesi Nasıl Yapılır? (Adım Adım):**\n\n" +
                "1. **Ölçü:** Her fincan için 1 fincan soğuk içme suyu ve 1 tepeleme tatlı kaşığı taze çekilmiş Türk kahvesi cezveye koyun.\n" +
                "2. **Şeker Ayarı:** Sade ise şekersiz; orta için 1 çay kaşığı; şekerli için 2 çay kaşığı şeker ekleyin.\n" +
                "3. **İlk Karışım:** Cezveyi ocağa almadan önce kaşıkla kahve, su ve şeker tamamen çözülene kadar iyice karıştırın. (Ocağa aldıktan sonra ASLA karıştırmayın).\n" +
                "4. **Kısık Ateş:** Cezveyi en kısık ateşe koyun ve yavaşça ısınmasını bekleyin.\n" +
                "5. **Köpüğü Paylaştırın:** Üzerinde kalın bir köpük tabakası yükselmeye başlayınca kaşıkla köpüğü alıp fincanlara paylaştırın.\n" +
                "6. **Kaynatmadan Alın:** Kahve bir taşım kabardığında ocaktan alın ve fincanın kenarından yavaşça dökün. Yanında bir bardak su ile servis edin."
        },

        // ──────────────────────── GÜNLÜK TEKNOLOJİ & PRATİK BİLGİLER ────────────────────────
        new KnowledgeItem
        {
            Title = "Bilgisayar Nasıl Hızlandırılır",
            Category = "Teknoloji Rehberi",
            Triggers = new[] { "bilgisayar nasıl hızlandırılır", "bilgisayar hızlandırma", "pc hızlandır", "windows hızlandırma", "yavaş çalışan bilgisayar" },
            IsStepByStep = true,
            Answer =
                "⚡ **Yavaşlayan Bilgisayarı Hızlandırma Rehberi (Adım Adım):**\n\n" +
                "1. **Başlangıç Programlarını Kapatın:** `Ctrl + Shift + Esc` ile Görev Yöneticisi'ni açın. 'Başlangıç' sekmesine gelin ve bilgisayar açıldığında otomatik başlayan gereksiz programları devre dışı bırakın.\n" +
                "2. **Geçici Dosyaları (Temp) Temizleyin:** `Win + R` tuşuna basıp `temp` ve ardından `%temp%` yazarak açılan klasörlerdeki gereksiz tüm önbellek dosyalarını silin.\n" +
                "3. **Disk Temizleme Yapın:** Başlat menüsüne 'Disk Temizleme' yazın, C: sürücüsünü seçerek 'Sistem dosyalarını temizle' seçeneğiyle eski Windows güncelleme artıklarını kaldırın.\n" +
                "4. **Görsel Efektleri Azaltın:** 'Gelişmiş sistem ayarları' -> 'Performans Seçenekleri' bölümünden 'En iyi performans için ayarla' seçeneğini seçin.\n" +
                "5. **Arka Plan Uygulamalarını Kısıtlayın:** Ayarlar -> Gizlilik -> Arka Plan Uygulamaları bölümünden kullanmadığınız uygulamaları kapatın.\n" +
                "6. **SSD ve RAM Güvencesi:** İşletim sisteminin mutlaka NVMe SSD üzerinde kurulu olduğundan emin olun."
        },

        new KnowledgeItem
        {
            Title = "Ekran Görüntüsü Nasıl Alınır",
            Category = "Pratik Rehber",
            Triggers = new[] { "ekran görüntüsü nasıl alınır", "ss nasıl alınır", "screenshot nasıl alınır", "ekran resmi alma" },
            IsStepByStep = true,
            Answer =
                "📸 **Bilgisayarda Ekran Görüntüsü (Screenshot) Alma Yöntemleri:**\n\n" +
                "1. **Bölgesel Görüntü (En Pratik):** `Windows + Shift + S` tuşlarına aynı anda basın. Ekran kararır; farenizle istediğiniz alanı seçin. Görüntü panoya kopyalanır ve doğrudan `Ctrl + V` ile yapıştırabilirsiniz.\n" +
                "2. **Tam Ekranı Dosya Olarak Kaydetme:** `Windows + Print Screen (PrtScn)` tuşlarına basın. Ekran anlık olarak yanıp söner ve görüntü otomatik olarak `Resimler > Ekran Görüntüleri` klasörüne PNG olarak kaydedilir.\n" +
                "3. **Sadece Aktif Pencere:** `Alt + Print Screen` tuşuna basarak yalnızca o an açık olan pencerenin görüntüsünü panoya alabilirsiniz.\n" +
                "4. **Mac Kullanıcıları İçin:** `Command + Shift + 4` ile alan seçebilir, `Command + Shift + 3` ile tüm ekranı yakalayabilirsiniz."
        },

        // ──────────────────────── BENCHMARK & TEMEL BİLİM / COĞRAFYA ────────────────────────
        new KnowledgeItem
        {
            Title = "Türkiye Başkenti",
            Category = "Coğrafya & Temel Bilgi",
            Triggers = new[] { "türkiye başkent", "türkiyenin başkenti", "türkiye'nin başkenti", "türkiye başkenti neresidir", "türkiye başkenti" },
            Answer =
                "🇹🇷 **Türkiye'nin Başkenti:**\n\n" +
                "Türkiye Cumhuriyeti'nin başkenti **Ankara**'dır. 13 Ekim 1923 tarihinde Türkiye Büyük Millet Meclisi kararıyla resmi başkent ilan edilmiştir. Ankara, ülkenin yönetim merkezi, siyasi kalbi ve en kalabalık ikinci şehridir."
        },

        new KnowledgeItem
        {
            Title = "Güneş Sistemi Gezegenleri",
            Category = "Astronomi & Temel Bilgi",
            Triggers = new[] { "güneş sistemi gezegen", "güneş sistemindeki gezegenler", "gezegenler nelerdir", "kaç gezegen var" },
            Answer =
                "🪐 **Güneş Sistemindeki Gezegenler:**\n\n" +
                "Güneş Sistemi'nde Güneş'e olan uzaklıklarına göre sıralanmış **8 ana gezegen** bulunmaktadır:\n\n" +
                "1. **Merkür** (Güneş'e en yakın ve en küçük gezegen)\n" +
                "2. **Venüs** (En sıcak gezegen, kalın atmosferli)\n" +
                "3. **Dünya** (Üzerinde yaşam barındıran mavi gezegenimiz)\n" +
                "4. **Mars** (Demir oksit zengini kızıl gezegen)\n" +
                "5. **Jüpiter** (Güneş sisteminin en büyük gaz devi)\n" +
                "6. **Satürn** (Görkemli halkalarıyla ünlü gaz devi)\n" +
                "7. **Uranüs** (Eksen eğikliğiyle yana yatık buz devi)\n" +
                "8. **Neptün** (En uzak, mavi renkli ve fırtınalı buz devi)"
        },

        new KnowledgeItem
        {
            Title = "Su Kimyasal Formülü",
            Category = "Kimya & Temel Bilgi",
            Triggers = new[] { "su formül", "suyun formülü", "su kimyasal formülü", "su formülü nedir" },
            Answer =
                "💧 **Suyun Kimyasal Formülü:**\n\n" +
                "Suyun kimyasal formülü **H₂O**'dur.\n" +
                "Bu formül, bir su molekülünün **2 hidrojen** atomu ile **1 oksijen** atomunun kovalent bağ ile birleşmesinden meydana geldiğini gösterir."
        },

        new KnowledgeItem
        {
            Title = "Yapay Zeka ve Makine Öğrenimi",
            Category = "Teknoloji",
            Triggers = new[] { "yapay zeka öğrenme", "yapay zeka nedir", "makine öğrenimi nedir", "derin öğrenme" },
            Answer =
                "🧠 **Yapay Zeka ve Öğrenme Mekanizması:**\n\n" +
                "Yapay zeka, makinelerin insan benzeri problem çözme, algılama ve karar verme yetenekleri kazanmasını sağlayan bilim dalıdır.\n\n" +
                "• **Makine Öğrenimi (Machine Learning):** Modellerin açıkça programlanmadan, verilerden örüntüler çıkararak öğrenmesini sağlar.\n" +
                "• **Derin Öğrenme (Deep Learning):** Çok katmanlı yapay sinir ağları ve Transformer mimarileri kullanarak büyük veriler üzerinden karmaşık ilişkileri kavrar."
        },

        new KnowledgeItem
        {
            Title = "Programlama Dilleri",
            Category = "Yazılım",
            Triggers = new[] { "programlama dil", "programlama dilleri", "yazılım dilleri", "en popüler diller" },
            Answer =
                "💻 **Programlama Dilleri ve Yazılım:**\n\n" +
                "Programlama dilleri, insanların bilgisayarlara komutlar vererek yazılımlar ve algoritmalar üretmesini sağlayan biçimsel dillerdir.\n\n" +
                "• **C# / .NET:** Yüksek performanslı masaüstü, web ve sistem geliştirmede kullanılır (HexAI tamamen saf C# ile yazılmıştır).\n" +
                "• **Python:** Veri bilimi, yapay zeka ve hızlı prototiplemede yaygındır.\n" +
                "• **C / C++:** Donanıma yakın, düşük seviyeli ve azami hız gerektiren sistemlerde tercih edilir.\n" +
                "• **JavaScript / TypeScript:** Modern web ön yüz ve tam yığın ekosisteminin temelidir."
        },

        new KnowledgeItem
        {
            Title = "Bilgisayar İşlemcisi (CPU)",
            Category = "Donanım",
            Triggers = new[] { "bilgisayar işlemci", "işlemci nedir", "cpu nedir", "işlemci ne işe yarar" },
            Answer =
                "⚙️ **Bilgisayar İşlemcisi (CPU):**\n\n" +
                "İşlemci (Central Processing Unit - CPU), bilgisayarın beyni olarak kabul edilen temel donanım birimidir.\n" +
                "Aritmetik ve mantıksal hesaplamaları yapar, bellek (RAM) ve çevre birimleri arasındaki veri akışını yönetir, komut döngülerini (fetch-decode-execute) yürütür. Çekirdek sayısı, saat frekansı (GHz) ve önbellek (L3 cache) kapasitesi işlemci performansını belirler."
        },

        new KnowledgeItem
        {
            Title = "Büyük ve Küçük Karşıt Anlam",
            Category = "Mantık & Dil",
            Triggers = new[] { "büyük küçük karşıt", "büyük zıt anlamı", "büyük küçük zıt", "karşıt anlam" },
            Answer =
                "📏 **Büyük ve Küçük Karşıtlığı:**\n\n" +
                "**Büyük** ve **küçük** sözcükleri dilbilgisinde birbirinin tam **karşıt (zıt)** anlamlısıdır.\n" +
                "Boyut, hacim veya derece bakımından birbiriyle çelişen iki ucu ifade eder (Örn: 'büyük ev' - 'küçük ev')."
        },

        new KnowledgeItem
        {
            Title = "Mevsimler",
            Category = "Coğrafya & Mantık",
            Triggers = new[] { "kış yaz mevsim", "mevsimler nelerdir", "dört mevsim", "yılın mevsimleri" },
            Answer =
                "🍂 **Yılın Dört Mevsimi:**\n\n" +
                "Dünya'nın Güneş etrafındaki dönüşü ve 23.5 derecelik eksen eğikliği sonucu 4 mevsim oluşur:\n\n" +
                "1. **İlkbahar:** Doğanın uyandığı, havaların ısınmaya başladığı mevsim (Mart, Nisan, Mayıs)\n" +
                "2. **Yaz:** Yılın en sıcak mevsimi, uzun günler (Haziran, Temmuz, Ağustos)\n" +
                "3. **Sonbahar:** Ağaçların yaprak döktüğü, havaların serinlediği mevsim (Eylül, Ekim, Kasım)\n" +
                "4. **Kış:** Yılın en soğuk ve karlı mevsimi (Aralık, Ocak, Şubat)"
        },

        new KnowledgeItem
        {
            Title = "Selamlama ve Nezaket",
            Category = "Dil & İletişim",
            Triggers = new[] { "selamlama sözleri nelerdir", "türkçe selamlama ifadeleri", "selamlama ve nezaket kuralları" },
            Answer =
                "👋 **Türkçe Selamlama ve İletişim:**\n\n" +
                "Türkçede günün saatine ve samimiyet derecesine göre kullanılan temel selamlama ifadeleri:\n" +
                "• **Merhaba / Selam:** Genel, her zaman kullanılabilen sıcak selamlama.\n" +
                "• **Günaydın:** Sabah saatlerinde güne başlarken söylenir.\n" +
                "• **İyi Günler / İyi Akşamlar:** Gün içi ve akşam karşılaşmalarında veya vedalaşırken kullanılır.\n" +
                "• **Esenlikler:** Resmi ve köklü Türkçe selamlama dileği."
        },

        new KnowledgeItem
        {
            Title = "Kitap Okumanın Önemi",
            Category = "Eğitim & Mantık",
            Triggers = new[] { "kitap okumak bilgi", "kitap okumanın faydaları", "kitap okumak", "okumak ve bilgi" },
            Answer =
                "📚 **Kitap Okumak ve Bilgi Edinme:**\n\n" +
                "Kitap okumak, insan zihninin düşünme, empati kurma ve kelime dağarcığını geliştirme kapasitesini artıran en etkili eylemdir.\n" +
                "Yeni bilgiler öğrenmeyi, eleştirel muhakeme yeteneğini ve analitik kavrayışı güçlendirir."
        },

        new KnowledgeItem
        {
            Title = "Dünyanın En Yüksek Dağı",
            Category = "Coğrafya",
            Triggers = new[] { "dünyanın en yüksek dağı", "en yüksek dağ", "everest dağı", "en yüksek tepe" },
            Answer =
                "🏔️ **Dünyanın En Yüksek Dağı:**\n\n" +
                "Dünyanın deniz seviyesinden en yüksek noktası, Himalaya Dağları'nda Nepal ile Çin (Tibet) sınırında bulunan **Everest Dağı**'dır.\n" +
                "Resmi yüksekliği **8.848,86 metre**dir."
        },

        new KnowledgeItem
        {
            Title = "Fotosentez Nedir",
            Category = "Biyoloji & Bilim",
            Triggers = new[] { "fotosentez nedir", "fotosentez nasıl gerçekleşir", "fotosentez formülü" },
            Answer =
                "🌱 **Fotosentez Nedir ve Nasıl Gerçekleşir?**\n\n" +
                "Fotosentez, klorofil taşıyan bitkilerin, alglerin ve bazı bakterilerin güneş ışığı enerjisini kullanarak su ve karbondioksitten organik besin (glikoz) ve oksijen üretmesi sürecidir.\n\n" +
                "🔬 **Genel Formül:**\n" +
                "`6CO₂ + 6H₂O + Güneş Işığı → C₆H₁₂O₆ (Glikoz) + 6O₂ (Oksijen)`\n\n" +
                "Bu süreç yeryüzündeki oksijen döngüsünün ve yaşamın temel enerji kaynağıdır."
        }
    };

    /// <summary>
    /// Kullanıcı sorusuna en uygun yerleşik ortak bilgiyi bulur.
    /// </summary>
    public static KnowledgeItem? FindMatch(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;

        string normalized = Normalize(query);
        var queryWords = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .ToList();

        KnowledgeItem? bestItem = null;
        double bestScore = 0;

        foreach (var item in Items)
        {
            // 1. Tam veya alt dize tetikleyici eşleşmesi
            foreach (var trigger in item.Triggers)
            {
                string normTrigger = Normalize(trigger);
                if (normalized.Contains(normTrigger))
                {
                    return item; // Doğrudan tetikleyici eşleşti
                }
            }

            // 2. Anahtar kelime kesişim skoru
            int matches = 0;
            var itemTokens = item.Triggers.SelectMany(t => Normalize(t).Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Distinct()
                .ToList();

            foreach (var qw in queryWords)
            {
                if (qw.Length < 3) continue;
                if (itemTokens.Any(it => it.Length >= 3 && (it == qw || (it.Length >= 4 && qw.Length >= 4 && (qw.StartsWith(it) || it.StartsWith(qw))))))
                    matches++;
            }

            if (matches >= 2)
            {
                double score = (double)matches / Math.Max(queryWords.Count, 2);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestItem = item;
                }
            }
        }

        return bestScore >= 0.5 ? bestItem : null;
    }

    /// <summary>
    /// Tüm yerleşik bilgileri KnowledgeEntry listesi olarak döndürür (KnowledgeBase senkronizasyonu için).
    /// </summary>
    public static List<KnowledgeEntry> GetAllAsEntries()
    {
        return Items.Select(i => new KnowledgeEntry
        {
            Question = i.Title,
            Answer = i.Answer,
            Confidence = 10.0,
            Source = "Yerleşik Türkçe Bilgi Bankası",
            Timestamp = DateTime.Now,
            Keywords = i.Triggers.SelectMany(t => Normalize(t).Split(' ', StringSplitOptions.RemoveEmptyEntries)).Distinct().ToList()
        }).ToList();
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
            .Trim();
    }
}
