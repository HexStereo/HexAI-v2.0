using HexAI.Core.Activation;
using HexAI.Core.Interfaces;
using HexAI.Core.Math;

namespace HexAI.NeuralNetwork.Layers;

/// <summary>
/// Tam Bağlantılı (Dense/Fully Connected) Katman.
/// 
/// Bu, sinir ağlarının temel yapı taşıdır. Her nöron bir önceki katmandaki
/// TÜM nöronlara bağlıdır.
/// 
/// Matematiksel olarak:
///   output = activation(weights * input + bias)
/// 
/// Backpropagation sırasında:
///   1. Aktivasyon türevini hesapla
///   2. Ağırlık gradientlerini hesapla
///   3. Bias gradientlerini hesapla
///   4. Giriş hatasını hesapla (önceki katmana gönderilecek)
///   5. Ağırlıkları ve bias'ları güncelle
/// </summary>
public class DenseLayer : ILayer
{
    // Ağırlık matrisi: [neuronCount x inputSize]
    private Matrix _weights;

    // Bias vektörü: [neuronCount x 1]
    private Matrix _biases;

    // Son hesaplama sonuçları (backprop için saklanır)
    private Matrix _lastInput = null!;
    private Matrix _lastOutput = null!;
    private Matrix _lastWeightedSum = null!; // Aktivasyon öncesi değerler

    public int NeuronCount { get; }
    public int InputSize { get; }
    public IActivation Activation { get; }
    public Matrix LastOutput => _lastOutput;
    public Matrix LastInput => _lastInput;

    /// <summary>
    /// Yeni bir Dense katman oluşturur.
    /// </summary>
    /// <param name="inputSize">Girdi boyutu (önceki katmanın nöron sayısı)</param>
    /// <param name="neuronCount">Bu katmandaki nöron sayısı</param>
    /// <param name="activation">Aktivasyon fonksiyonu</param>
    /// <param name="rng">Rastgele sayı üreteci (tekrarlanabilirlik için)</param>
    public DenseLayer(int inputSize, int neuronCount, IActivation activation, Random? rng = null)
    {
        InputSize = inputSize;
        NeuronCount = neuronCount;
        Activation = activation;

        // Xavier/Glorot initialization ile ağırlıkları başlat
        _weights = Matrix.Random(neuronCount, inputSize, rng);

        // Bias'ları sıfırla başlat
        _biases = new Matrix(neuronCount, 1);
    }

    /// <summary>
    /// İleri yön hesaplaması (Forward Pass).
    /// 
    /// Adımlar:
    ///   1. z = W * x + b    (ağırlıklı toplam)
    ///   2. a = f(z)          (aktivasyon)
    /// 
    /// Sonuçlar backpropagation için saklanır.
    /// </summary>
    public Matrix Forward(Matrix input)
    {
        _lastInput = input;

        // z = W * x + b
        _lastWeightedSum = _weights * input;
        for (int i = 0; i < NeuronCount; i++)
            _lastWeightedSum[i, 0] += _biases[i, 0];

        // a = f(z) — Aktivasyon fonksiyonunu uygula
        _lastOutput = _lastWeightedSum.Map(Activation.Activate);

        return _lastOutput;
    }

    /// <summary>
    /// Geri yön hesaplaması (Backward Pass / Backpropagation).
    /// 
    /// Bu, sinir ağlarının "öğrenme" mekanizmasıdır:
    ///   1. Çıktı hatasından gradient hesapla
    ///   2. Ağırlık ve bias gradientlerini bul
    ///   3. Gradientleri kullanarak ağırlıkları güncelle
    ///   4. Girdi hatasını hesaplayıp önceki katmana gönder
    /// 
    /// Matematiksel olarak:
    ///   delta = error ⊙ f'(output)           (Hadamard çarpımı)
    ///   ∂L/∂W = delta * input^T               (ağırlık gradienti)
    ///   ∂L/∂b = delta                          (bias gradienti)
    ///   input_error = W^T * delta              (önceki katmana hata)
    ///   W = W - lr * ∂L/∂W                     (ağırlık güncelleme)
    ///   b = b - lr * ∂L/∂b                     (bias güncelleme)
    /// </summary>
    public Matrix Backward(Matrix outputError, HexAI.NeuralNetwork.Optimizers.IOptimizer optimizer)
    {
        // 1. Aktivasyon türevini hesapla: f'(output)
        var activationDerivative = _lastOutput.Map(Activation.Derivative);

        // 2. Delta hesapla: error ⊙ f'(output)
        var delta = outputError.HadamardProduct(activationDerivative);

        // 3. Ağırlık gradientlerini hesapla: delta * input^T
        var weightGradients = delta * _lastInput.Transpose();

        // 4. Bir önceki katmana gönderilecek hatayı hesapla: W^T * delta
        var inputGradient = _weights.Transpose() * delta;

        // Ağırlık ve Biasları güncelle (Optimizer kullanarak)
        _weights.SubtractInPlace(optimizer.ComputeUpdate(weightGradients, _weights));
        _biases.SubtractInPlace(optimizer.ComputeUpdate(delta, _biases));

        return inputGradient;
    }



    /// <summary>
    /// Katman bilgilerini döndürür.
    /// </summary>
    public override string ToString()
    {
        return $"DenseLayer(in={InputSize}, out={NeuronCount}, activation={Activation.Name})";
    }
}

