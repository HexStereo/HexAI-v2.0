using HexAI.Core.Activation;
using HexAI.Core.Math;

namespace HexAI.NeuralNetwork.Layers;

/// <summary>
/// Tam Transformer Bloğu — Modern LLM'lerin temel yapı taşı.
/// 
/// Her blok iki ana bileşenden oluşur (Pre-LayerNorm, GPT-2+ tarzı):
///   1. Pre-Norm → Multi-Head Self-Attention → Residual Connection
///   2. Pre-Norm → Feed-Forward Network (FFN) → Residual Connection
/// 
/// Matematiksel olarak:
///   h = x + MultiHeadAttention(LayerNorm(x))
///   output = h + FFN(LayerNorm(h))
/// 
/// Pre-LayerNorm (vs Post-LayerNorm):
///   GPT-2 ve sonraki modeller Pre-LN kullanır çünkü:
///   - Eğitim daha kararlı
///   - Derin ağlarda gradient akışı daha iyi
///   - Büyük learning rate'ler kullanılabilir
/// 
/// Residual Connections:
///   Giriş doğrudan çıkışa eklenir (skip connection).
///   Bu, gradient'in katmanlardan kaybolmadan akmasını sağlar.
///   Çok derin ağlarda (100+ katman) bile eğitimi mümkün kılar.
/// </summary>
public class TransformerBlock : ILayer
{
    private readonly LayerNormLayer _norm1;
    private readonly MultiHeadAttentionLayer _attention;
    private readonly LayerNormLayer _norm2;
    private readonly FeedForwardLayer _ffn;

    private Matrix _inputCache = null!;
    private Matrix _afterAttentionResidual = null!;

    public int InputSize { get; }
    public int NeuronCount { get; }
    public IActivation Activation => new Linear();
    public Matrix LastInput { get; private set; } = new Matrix(1, 1);
    public Matrix LastOutput { get; private set; } = new Matrix(1, 1);

    /// <summary>
    /// Yeni bir Transformer blok oluşturur.
    /// </summary>
    /// <param name="embedSize">Token embedding boyutu.</param>
    /// <param name="numHeads">Attention head sayısı.</param>
    /// <param name="sequenceLength">Bağlam penceresi uzunluğu (token sayısı).</param>
    /// <param name="ffnMultiplier">FFN gizli katman çarpanı (varsayılan: 4).</param>
    /// <param name="rng">Rastgele sayı üreteci.</param>
    public TransformerBlock(int embedSize, int numHeads, int sequenceLength,
                           int ffnMultiplier = 4, Random? rng = null)
    {
        InputSize = embedSize * sequenceLength;
        NeuronCount = embedSize * sequenceLength;

        // Pre-LayerNorm 1 (Attention öncesi)
        _norm1 = new LayerNormLayer(sequenceLength, embedSize);

        // Multi-Head Self-Attention
        _attention = new MultiHeadAttentionLayer(sequenceLength, embedSize, numHeads, rng);

        // Pre-LayerNorm 2 (FFN öncesi)
        _norm2 = new LayerNormLayer(sequenceLength, embedSize);

        // Feed-Forward Network
        _ffn = new FeedForwardLayer(InputSize, ffnMultiplier, rng);
    }

    public Matrix Forward(Matrix input)
    {
        LastInput = input;
        _inputCache = input;

        // === Sub-block 1: Pre-Norm Attention + Residual ===
        // h = x + Attention(LayerNorm(x))
        var normed1 = _norm1.Forward(input);
        var attended = _attention.Forward(normed1);
        _afterAttentionResidual = AddResidual(input, attended);

        // === Sub-block 2: Pre-Norm FFN + Residual ===
        // output = h + FFN(LayerNorm(h))
        var normed2 = _norm2.Forward(_afterAttentionResidual);
        var ffnOut = _ffn.Forward(normed2);
        LastOutput = AddResidual(_afterAttentionResidual, ffnOut);

        return LastOutput;
    }

    public Matrix Backward(Matrix error, HexAI.NeuralNetwork.Optimizers.IOptimizer optimizer)
    {
        // === Sub-block 2 backward ===
        // Residual connection: gradient doğrudan + FFN backward
        var ffnError = _ffn.Backward(error, optimizer);
        var norm2Error = _norm2.Backward(ffnError, optimizer);
        var subBlock2Error = AddResidual(error, norm2Error); // Residual gradient

        // === Sub-block 1 backward ===
        var attentionError = _attention.Backward(subBlock2Error, optimizer);
        var norm1Error = _norm1.Backward(attentionError, optimizer);
        var totalError = AddResidual(subBlock2Error, norm1Error); // Residual gradient

        return totalError;
    }

    /// <summary>
    /// Residual (skip) connection: x + sublayer(x)
    /// </summary>
    private static Matrix AddResidual(Matrix residual, Matrix sublayerOutput)
    {
        int rows = residual.Rows;
        int cols = residual.Cols;
        var result = new Matrix(rows, cols);

        for (int i = 0; i < rows * cols; i++)
        {
            result.Data[i] = residual.Data[i] + sublayerOutput.Data[i];
        }

        return result;
    }

    public override string ToString()
    {
        return $"TransformerBlock(size={InputSize}, attention={_attention}, ffn={_ffn})";
    }
}
