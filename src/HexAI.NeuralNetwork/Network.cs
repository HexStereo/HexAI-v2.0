using HexAI.Core.Activation;
using HexAI.Core.Interfaces;
using HexAI.Core.Math;
using HexAI.Core.Data;
using HexAI.Core.Loss;
using HexAI.NeuralNetwork.Layers;

namespace HexAI.NeuralNetwork;

/// <summary>
/// Çok Katmanlı Sinir Ağı (Multi-Layer Perceptron / MLP).
/// 
/// Bu sınıf, katmanları bir araya getirerek tam bir sinir ağı oluşturur.
/// Eğitim (backpropagation) ve tahmin (forward pass) işlemlerini yönetir.
/// 
/// Kullanım:
///   var network = new Network();
///   network.AddLayer(new DenseLayer(2, 4, new Sigmoid()));
///   network.AddLayer(new DenseLayer(4, 1, new Sigmoid()));
///   network.Train(dataset, epochs: 10000, learningRate: 0.5);
///   var result = network.Predict(new[] { 1.0, 0.0 });
/// </summary>
public class Network : ITrainable, IPredictable
{
    private readonly List<ILayer> _layers = [];
    private readonly Random _rng;

    /// <summary>Ağdaki katmanlar (salt okunur).</summary>
    public IReadOnlyList<ILayer> Layers => _layers.AsReadOnly();

    /// <summary>Toplam katman sayısı.</summary>
    public int LayerCount => _layers.Count;

    /// <summary>
    /// Yeni bir sinir ağı oluşturur.
    /// </summary>
    /// <param name="seed">Rastgele tohum (tekrarlanabilir sonuçlar için)</param>
    public Network(int? seed = null)
    {
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>
    /// Ağa yeni bir katman ekler.
    /// Katmanlar sırayla eklenir: ilk eklenen → ilk katman.
    /// </summary>
    public Network AddLayer(ILayer layer)
    {
        // Boyut uyumluluğunu kontrol et
        if (_layers.Count > 0)
        {
            var prevLayer = _layers[^1];
            if (layer.InputSize != prevLayer.NeuronCount)
                throw new ArgumentException(
                    $"Katman boyutu uyuşmuyor: önceki katmanın çıktısı {prevLayer.NeuronCount}, " +
                    $"yeni katmanın girdisi {layer.InputSize}");
        }

        _layers.Add(layer);
        return this; // Fluent API desteği
    }

    /// <summary>
    /// Kolaylık metodu: Hızlıca Dense katman ekler.
    /// </summary>
    public Network AddDenseLayer(int inputSize, int neuronCount, IActivation? activation = null)
    {
        activation ??= new Sigmoid();
        return AddLayer(new DenseLayer(inputSize, neuronCount, activation, _rng));
    }

    /// <summary>
    /// İleri yön hesaplaması — girdiden çıktıya.
    /// Veri her katmandan sırayla geçer.
    /// </summary>
    public double[] Predict(double[] inputs)
    {
        if (_layers.Count == 0)
            throw new InvalidOperationException("Ağda hiç katman yok!");

        // Girdiyi sütun vektörüne dönüştür
        var current = Matrix.FromArray(inputs);

        // Her katmandan sırayla geçir
        foreach (var layer in _layers)
        {
            if (layer is DropoutLayer dropout) dropout.IsTraining = false;
            current = layer.Forward(current);
        }

        return current.ToArray();
    }

    /// <summary>
    /// Ağı verilen veri seti ile eğitir.
    /// 
    /// Her epoch'ta:
    ///   1. Veri setini karıştır
    ///   2. Her örnek için:
    ///      a. Forward pass — tahmin üret
    ///      b. Hata hesapla — tahmin vs beklenen
    ///      c. Backward pass — ağırlıkları güncelle
    ///   3. Ortalama hatayı raporla
    /// </summary>
    public void Train(DataSet dataSet, int epochs, double learningRate)
    {
        Train(dataSet, epochs, learningRate, null);
    }

    public void Train(DataSet dataSet, int epochs, double learningRate, ILoss? lossFunction = null)
    {
        Train(dataSet, epochs, new HexAI.NeuralNetwork.Optimizers.SGDOptimizer(learningRate), lossFunction, reportInterval: epochs / 10);
    }
    
    public void Train(DataSet dataSet, int epochs, HexAI.NeuralNetwork.Optimizers.IOptimizer optimizer)
    {
        Train(dataSet, epochs, optimizer, null, reportInterval: epochs / 10);
    }

    public void Train(DataSet dataSet, int epochs, HexAI.NeuralNetwork.Optimizers.IOptimizer optimizer, ILoss? lossFunction = null)
    {
        Train(dataSet, epochs, optimizer, lossFunction, reportInterval: epochs / 10);
    }

    /// <summary>
    /// Eğer ağda tekrarlayan (RNN) katmanlar varsa onların hafızalarını sıfırlar.
    /// Yeni bir diziye (sequence) başlarken çağrılmalıdır.
    /// </summary>
    public void ResetState()
    {
        foreach (var layer in _layers)
        {
            if (layer is IRecurrentLayer rnn)
            {
                rnn.ResetState();
            }
        }
    }



    /// <summary>
    /// Gelişmiş eğitim metodu — ilerleme raporlama desteği ile.
    /// </summary>
    public void Train(DataSet dataSet, int epochs, double learningRate, ILoss? lossFunction = null,
                      int reportInterval = 1000,
                      Action<int, double>? onEpochComplete = null)
    {
        Train(dataSet, epochs, new HexAI.NeuralNetwork.Optimizers.SGDOptimizer(learningRate), lossFunction, reportInterval, onEpochComplete);
    }

    /// <summary>
    /// Gelişmiş eğitim metodu — ilerleme raporlama ve Optimizer desteği ile.
    /// </summary>
    public void Train(DataSet dataSet, int epochs, HexAI.NeuralNetwork.Optimizers.IOptimizer optimizer, ILoss? lossFunction = null,
                      int reportInterval = 1000,
                      Action<int, double>? onEpochComplete = null)
    {
        lossFunction ??= new MSELoss();

        if (_layers.Count == 0)
            throw new InvalidOperationException("Ağda hiç katman yok! Önce katman ekleyin.");

        bool isSilent = reportInterval == int.MaxValue;

        if (!isSilent)
        {
            Console.WriteLine($"╔══════════════════════════════════════════════════╗");
            Console.WriteLine($"║  🧠 HEX.AI Sinir Ağı Eğitimi Başlıyor         ║");
            Console.WriteLine($"║  Katmanlar: {LayerCount,-5}  Veri: {dataSet.Count,-5}              ║");
            Console.WriteLine($"║  Epoch: {epochs,-8}  Opt: {optimizer.GetType().Name,-15}║");
            Console.WriteLine($"╚══════════════════════════════════════════════════╝");
            Console.WriteLine();
        }

        for (int epoch = 1; epoch <= epochs; epoch++)
        {
            double totalError = 0;
            ResetState(); // Yeni epoch (veya sekans) başladığında hafızayı sıfırla

            // Her eğitim örneği için
            foreach (var sample in dataSet.Samples)
            {
                // 1. Forward pass — tahmin üret
                var current = Matrix.FromArray(sample.Inputs);
                foreach (var layer in _layers)
                {
                    if (layer is DropoutLayer dropout) dropout.IsTraining = true;
                    current = layer.Forward(current);
                }

                // 2. Hata hesapla (çıktı - beklenen)
                var expected = Matrix.FromArray(sample.ExpectedOutputs);
                
                // Loss nesnesini kullanarak toplam kaybı ve türevi al
                totalError += lossFunction.Calculate(current, expected);
                var currentError = lossFunction.Derivative(current, expected);

                // 3. Backward pass — sondan başa her katmandan geç
                for (int i = _layers.Count - 1; i >= 0; i--)
                    currentError = _layers[i].Backward(currentError, optimizer);
            }

            double avgError = totalError / dataSet.Count;

            if (!isSilent && (epoch % reportInterval == 0 || epoch == epochs))
            {
                // İlerleme çubuğu
                int progress = (int)((double)epoch / epochs * 20);
                string bar = new string('█', progress) + new string('░', 20 - progress);

                Console.WriteLine($"  Epoch {epoch,6}/{epochs}  │  Hata: {avgError:F6}  │  {bar}");
            }

            if (epoch % reportInterval == 0 || epoch == epochs)
            {
                onEpochComplete?.Invoke(epoch, avgError);
            }
        }

        if (!isSilent)
        {
            Console.WriteLine();
            Console.WriteLine("  ✅ Eğitim tamamlandı!");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Modelin doğruluğunu test verileri üzerinde hesaplar.
    /// </summary>
    /// <param name="testData">Test veri seti</param>
    /// <param name="threshold">Sınıflandırma eşiği (varsayılan: 0.5)</param>
    /// <returns>Doğruluk oranı (0.0 - 1.0)</returns>
    public double Evaluate(DataSet testData, double threshold = 0.5)
    {
        int correct = 0;
        foreach (var sample in testData.Samples)
        {
            var prediction = Predict(sample.Inputs);
            bool allCorrect = true;
            for (int i = 0; i < prediction.Length; i++)
            {
                double predicted = prediction[i] >= threshold ? 1.0 : 0.0;
                if (System.Math.Abs(predicted - sample.ExpectedOutputs[i]) > 0.01)
                {
                    allCorrect = false;
                    break;
                }
            }
            if (allCorrect) correct++;
        }
        return (double)correct / testData.Count;
    }

    /// <summary>
    /// Ağ mimarisinin özetini yazdırır.
    /// </summary>
    public void PrintSummary()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════╗");
        Console.WriteLine("║          🧠 HEX.AI - Ağ Mimarisi              ║");
        Console.WriteLine("╠══════════════════════════════════════════════════╣");

        int totalParams = 0;
        for (int i = 0; i < _layers.Count; i++)
        {
            var layer = _layers[i];
            int layerParams = layer.InputSize * layer.NeuronCount + layer.NeuronCount;
            totalParams += layerParams;
            Console.WriteLine($"║  Katman {i}: {layer,-38}║");
            Console.WriteLine($"║           Parametreler: {layerParams,-25}║");
        }

        Console.WriteLine("╠══════════════════════════════════════════════════╣");
        Console.WriteLine($"║  Toplam Parametreler: {totalParams,-27}║");
        Console.WriteLine("╚══════════════════════════════════════════════════╝");
    }

}
