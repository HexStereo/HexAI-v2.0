using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HexAI.NLP;

/// <summary>
/// Kelimeleri benzersiz sayısal kimliklere (Token ID) eşleyen sözlük sınıfı.
/// ChatGPT benzeri kelime düzeyinde çalışma altyapısını sağlar.
/// </summary>
public class WordVocabulary
{
    public const string PadToken = "<pad>";
    public const string UnkToken = "<unk>";
    public const string SosToken = "<s>";
    public const string EosToken = "</s>";

    public int PadId => 0;
    public int UnkId => 1;
    public int SosId => 2;
    public int EosId => 3;

    private readonly List<string> _idToWord = new();
    private readonly Dictionary<string, int> _wordToId = new();

    public int Size => _idToWord.Count;

    public WordVocabulary()
    {
        // Özel belirteçleri ekle
        AddWord(PadToken);
        AddWord(UnkToken);
        AddWord(SosToken);
        AddWord(EosToken);
    }

    /// <summary>
    /// Sözlüğe yeni bir kelime ekler.
    /// </summary>
    public int AddWord(string word)
    {
        word = word.ToLower(new System.Globalization.CultureInfo("tr-TR"));
        if (_wordToId.TryGetValue(word, out int id))
        {
            return id;
        }

        int newId = _idToWord.Count;
        _idToWord.Add(word);
        _wordToId[word] = newId;
        return newId;
    }

    /// <summary>
    /// Verilen kelimenin Token ID'sini döndürür. Bulunamazsa UNK (Bilinmeyen) döner.
    /// </summary>
    public int GetId(string word)
    {
        word = word.ToLower(new System.Globalization.CultureInfo("tr-TR"));
        if (_wordToId.TryGetValue(word, out int id))
        {
            return id;
        }
        return UnkId;
    }

    /// <summary>
    /// Token ID'den karşılık gelen kelimeyi döndürür.
    /// </summary>
    public string GetWord(int id)
    {
        if (id >= 0 && id < _idToWord.Count)
        {
            return _idToWord[id];
        }
        return UnkToken;
    }

    /// <summary>
    /// Bir metni tarayarak tüm kelimeleri sözlüğe ekler.
    /// </summary>
    public void BuildFromText(string text)
    {
        var tokens = Tokenizer.Tokenize(text);
        foreach (var token in tokens)
        {
            AddWord(token);
        }
    }

    /// <summary>
    /// Cümleyi Token ID dizisine çevirir.
    /// </summary>
    public int[] Encode(string sentence)
    {
        var tokens = Tokenizer.Tokenize(sentence);
        return tokens.Select(GetId).ToArray();
    }

    /// <summary>
    /// Token ID dizisini tekrar okunabilir cümleye dönüştürür.
    /// </summary>
    public string Decode(IEnumerable<int> ids)
    {
        var words = ids.Select(GetWord)
                       .Where(w => w != PadToken && w != SosToken && w != EosToken);
        return string.Join(" ", words);
    }

    /// <summary>
    /// Sözlüğü JSON dosyasına kaydeder.
    /// </summary>
    public void SaveToFile(string path)
    {
        var dto = new VocabularyDto
        {
            Words = _idToWord.ToList()
        };
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Sözlüğü JSON dosyasından yükler.
    /// </summary>
    public static WordVocabulary LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var dto = JsonSerializer.Deserialize<VocabularyDto>(json)
                  ?? throw new InvalidOperationException("Sözlük dosyası okunamadı.");

        var vocab = new WordVocabulary();
        // Özel belirteçler zaten constructor'da ekleniyor, geri kalanları ekle
        for (int i = 4; i < dto.Words.Count; i++)
        {
            vocab.AddWord(dto.Words[i]);
        }
        return vocab;
    }

    /// <summary>
    /// Başka bir sözlükteki tüm kelimeleri bu sözlüğe ekler (artırımlı eğitim için).
    /// </summary>
    public void MergeVocabulary(WordVocabulary other)
    {
        for (int i = 0; i < other.Size; i++)
        {
            AddWord(other.GetWord(i));
        }
    }

    /// <summary>
    /// Tüm kelimelerin listesini döndürür.
    /// </summary>
    public IReadOnlyList<string> GetAllWords() => _idToWord.AsReadOnly();

    /// <summary>
    /// Kelime sözlüğünün seri hale getirilebilir veri transfer nesnesi.
    /// </summary>
    private class VocabularyDto
    {
        public List<string> Words { get; set; } = new();
    }
}

