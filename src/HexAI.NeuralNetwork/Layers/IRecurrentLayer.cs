namespace HexAI.NeuralNetwork.Layers;

/// <summary>
/// Zaman serileri ve dizilerle çalışırken durum tutabilen (stateful) katmanların arayüzü.
/// (Örn: RNN, LSTM)
/// </summary>
public interface IRecurrentLayer : ILayer
{
    /// <summary>
    /// Katmanın gizli hafızasını (hidden state) temizler. 
    /// Yeni bir kelimeye veya zaman serisine başlarken çağrılır.
    /// </summary>
    void ResetState();
}
