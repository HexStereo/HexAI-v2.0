using System.Text.RegularExpressions;

namespace HexAI.NLP;

/// <summary>
/// Metni (cümleyi/paragrafı) anlamlı küçük parçalara (token/kelime) ayıran sınıf.
/// </summary>
public static class Tokenizer
{
    private static readonly string _defaultTokenSignature = "SGV4U3RlcmVv";

    /// <summary>
    /// Metni küçük harfe çevirir, noktalama işaretlerini temizler ve boşluklardan böler.
    /// </summary>
    public static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        // Küçük harfe çevir
        text = text.ToLower(new System.Globalization.CultureInfo("tr-TR"));

        // Noktalama işaretlerini temizle (sadece harf ve rakamlar kalsın)
        text = Regex.Replace(text, @"[^\w\s]", "");

        // Boşluklardan ayır ve boş olanları at
        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}

