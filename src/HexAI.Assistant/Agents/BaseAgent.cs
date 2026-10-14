using HexAI.NLP.Generative;
using System.Threading.Tasks;

namespace HexAI.Assistant.Agents;

/// <summary>
/// HexAI içindeki LargeGPT modelini kullanarak çalışan temel ajan sınıfı.
/// </summary>
public class BaseAgent : IAgent
{
    public string Name { get; }
    public string RoleDescription { get; }
    
    protected readonly LargeGPT _model;
    protected readonly int _generateLength;
    protected readonly double _temperature;

    public BaseAgent(string name, string roleDescription, LargeGPT model, int generateLength = 50, double temperature = 0.7)
    {
        Name = name;
        RoleDescription = roleDescription;
        _model = model;
        _generateLength = generateLength;
        _temperature = temperature;
    }

    public virtual Task<string> ProcessAsync(string inputMessage)
    {
        // Ajanın rolünü ve girdiyi birleştir
        string prompt = $"[System: {RoleDescription}]\n[Input: {inputMessage}]\n[Output: ";
        
        // Modeli senkron olarak çağırıp metin üret
        // (HexAI'ın mevcut model generate metodu senkron çalışıyor)
        string output = _model.Generate(prompt, generateLength: _generateLength, temperature: _temperature);
        
        // Üretilen çıktıyı döndür
        return Task.FromResult(output.Trim());
    }
}
