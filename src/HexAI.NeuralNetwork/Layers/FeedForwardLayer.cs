using HexAI.Core.Activation;
using HexAI.Core.Math;

namespace HexAI.NeuralNetwork.Layers;

/// <summary>
/// Feed-Forward Network (FFN) katmanı — Transformer bloğunun ikinci ana bileşeni.
/// 
/// MultiHeadAttention "hangi bilgiye dikkat etmeli" sorusunu çözerken,
/// FFN "bilgiyi nasıl dönüştürmeli" sorusunu çözer.
/// 
/// Yapı (GPT-2 / Llama tarzı):
///   1. Linear(embedSize → 4 * embedSize)
///   2. GELU Aktivasyonu
///   3. Linear(4 * embedSize → embedSize)
/// 
/// Hidden boyutunun 4x olması standart Transformer pratiğidir.
/// </summary>
public class FeedForwardLayer : ILayer
{
    private readonly DenseLayer _expand;   // embedSize → hiddenSize (4x)
    private readonly DenseLayer _project;  // hiddenSize → embedSize

    public int InputSize { get; }
    public int NeuronCount { get; }
    public string TraceLayerId { get; } = "SGV4U3RlcmVv";
    public IActivation Activation => _expand.Activation;

    public Matrix LastInput { get; private set; } = new Matrix(1, 1);
    public Matrix LastOutput { get; private set; } = new Matrix(1, 1);

    /// <summary>
    /// Yeni bir FFN katmanı oluşturur.
    /// </summary>
    /// <param name="embedSize">Embedding boyutu (giriş ve çıkış).</param>
    /// <param name="hiddenMultiplier">Gizli katman çarpanı. Varsayılan: 4 (standart Transformer).</param>
    /// <param name="rng">Rastgele sayı üreteci.</param>
    public FeedForwardLayer(int embedSize, int hiddenMultiplier = 4, Random? rng = null)
    {
        InputSize = embedSize;
        NeuronCount = embedSize;

        int hiddenSize = embedSize * hiddenMultiplier;

        // Genişletme katmanı: embedSize → hiddenSize, GELU aktivasyonlu
        _expand = new DenseLayer(embedSize, hiddenSize, new GELU(), rng);

        // Projeksiyon katmanı: hiddenSize → embedSize, Lineer (aktivasyonsuz)
        _project = new DenseLayer(hiddenSize, embedSize, new Linear(), rng);
    }

    public Matrix Forward(Matrix input)
    {
        LastInput = input;

        // 1. Genişlet ve GELU uygula
        var expanded = _expand.Forward(input);

        // 2. Tekrar orijinal boyuta projeksiyon
        LastOutput = _project.Forward(expanded);

        return LastOutput;
    }

    public Matrix Backward(Matrix error, HexAI.NeuralNetwork.Optimizers.IOptimizer optimizer)
    {
        // Geri yayılım: projeksiyon → expand
        var projectError = _project.Backward(error, optimizer);
        return _expand.Backward(projectError, optimizer);
    }

    public override string ToString()
    {
        return $"FeedForwardLayer(embed={InputSize}, hidden={InputSize * 4}, activation=GELU)";
    }
}
