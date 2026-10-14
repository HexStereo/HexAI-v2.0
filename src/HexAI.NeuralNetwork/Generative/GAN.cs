using System;
using HexAI.Core.Data;
using HexAI.Core.Math;
using HexAI.NeuralNetwork.Optimizers;
using HexAI.NeuralNetwork.Layers;

namespace HexAI.NeuralNetwork.Generative;

/// <summary>
/// Çekişmeli Üretici Ağlar (Generative Adversarial Networks) Yöneticisi.
/// </summary>
public class GAN
{
    public Network Generator { get; }
    public Network Discriminator { get; }
    
    private readonly NoOpOptimizer _noOpOptimizer = new NoOpOptimizer();

    /// <summary>
    /// Bir GAN modeli oluşturur.
    /// </summary>
    /// <param name="generator">Üretici ağ (Rastgele gürültüden veri üretir)</param>
    /// <param name="discriminator">Ayırt Edici ağ (Verinin gerçek mi sahte mi olduğunu anlar)</param>
    public GAN(Network generator, Network discriminator)
    {
        Generator = generator;
        Discriminator = discriminator;
    }

    /// <summary>
    /// GAN Ağını verilen gerçek verilerle eğitir.
    /// </summary>
    /// <param name="realData">Sadece girdilerden oluşan (etiketsiz) gerçek veriler</param>
    /// <param name="epochs">Tur sayısı</param>
    /// <param name="noiseSize">Generator'a verilecek rastgele gürültü boyutu (z)</param>
    /// <param name="genOptimizer">Generator için optimize edici (Örn: Adam)</param>
    /// <param name="discOptimizer">Discriminator için optimize edici (Örn: Adam)</param>
    /// <param name="reportInterval">Log aralığı</param>
    public void Train(double[][] realData, int epochs, int noiseSize, IOptimizer genOptimizer, IOptimizer discOptimizer, int reportInterval = 100)
    {
        Random rng = new Random();
        int dataCount = realData.Length;

        Console.WriteLine($"╔══════════════════════════════════════════════════╗");
        Console.WriteLine($"║  🧠 HEX.AI GAN Eğitimi Başlıyor               ║");
        Console.WriteLine($"║  Veri Adedi: {dataCount,-5}   Gürültü Boyutu: {noiseSize,-5}   ║");
        Console.WriteLine($"║  Epoch: {epochs,-8}                            ║");
        Console.WriteLine($"╚══════════════════════════════════════════════════╝");
        Console.WriteLine();

        for (int epoch = 1; epoch <= epochs; epoch++)
        {
            double totalDError = 0;
            double totalGError = 0;

            for (int i = 0; i < dataCount; i++)
            {
                // Rastgele bir gerçek veri seç
                double[] realSample = realData[rng.Next(dataCount)];
                
                // 1. Rastgele Gürültü (z) oluştur
                double[] noise = new double[noiseSize];
                for (int j = 0; j < noiseSize; j++) noise[j] = rng.NextDouble() * 2 - 1; // -1 ile 1 arası
                
                // 2. Generator ile Sahte (Fake) veri üret
                // Normal Predict yerine Forward kullanmalıyız ki Matrix formunda kalsın. 
                // Ama `Network.Predict` double[] döner ve state tutmaz. 
                // Discriminator'ı backprop yapmak için Network'ün kendi Train'i gibi manuel geçiş yapmalıyız.
                
                // --- A. DİSCRİMİNATÖR EĞİTİMİ ---
                
                // A.1 Gerçek veri için D(x) -> Hedef: 1
                var realInput = Matrix.FromArray(realSample);
                var currentDReal = realInput;
                foreach (var layer in Discriminator.Layers)
                {
                    if (layer is DropoutLayer dropout) dropout.IsTraining = true;
                    currentDReal = layer.Forward(currentDReal);
                }
                
                var realError = currentDReal - Matrix.FromArray(new double[] { 1.0 }); // Binary Cross Entropy yaklaşımı için (basit MSE kullanıyoruz şimdilik)
                totalDError += realError[0,0] * realError[0,0];
                
                var currentGradDReal = realError;
                for (int l = Discriminator.LayerCount - 1; l >= 0; l--)
                {
                    currentGradDReal = Discriminator.Layers[l].Backward(currentGradDReal, discOptimizer);
                }

                // A.2 Sahte veri için D(G(z)) -> Hedef: 0
                var currentG = Matrix.FromArray(noise);
                foreach (var layer in Generator.Layers)
                {
                    if (layer is DropoutLayer dropout) dropout.IsTraining = true;
                    currentG = layer.Forward(currentG);
                }
                var fakeSample = currentG; // Bu artık fake veri

                var currentDFake = fakeSample;
                foreach (var layer in Discriminator.Layers)
                {
                    if (layer is DropoutLayer dropout) dropout.IsTraining = true;
                    currentDFake = layer.Forward(currentDFake);
                }
                
                var fakeError = currentDFake - Matrix.FromArray(new double[] { 0.0 }); // Hedef: 0
                totalDError += fakeError[0,0] * fakeError[0,0];
                
                var currentGradDFake = fakeError;
                for (int l = Discriminator.LayerCount - 1; l >= 0; l--)
                {
                    currentGradDFake = Discriminator.Layers[l].Backward(currentGradDFake, discOptimizer);
                }

                // --- B. GENERATOR EĞİTİMİ ---
                
                // B.1 Sahte veri üret (tekrar ileri yayılım)
                currentG = Matrix.FromArray(noise);
                foreach (var layer in Generator.Layers)
                {
                    if (layer is DropoutLayer dropout) dropout.IsTraining = true;
                    currentG = layer.Forward(currentG);
                }
                fakeSample = currentG;

                // B.2 Discriminator'dan geçir (Hedef: 1) -- Generator, Discriminator'ı kandırmak ister!
                currentDFake = fakeSample;
                foreach (var layer in Discriminator.Layers)
                {
                    if (layer is DropoutLayer dropout) dropout.IsTraining = true;
                    currentDFake = layer.Forward(currentDFake);
                }

                var genErrorTarget = currentDFake - Matrix.FromArray(new double[] { 1.0 }); // Hedef: 1
                totalGError += genErrorTarget[0,0] * genErrorTarget[0,0];

                // B.3 Discriminator'dan geriye yay (SADECE GRADIENT, UPDATE YOK)
                var gradForGen = genErrorTarget;
                for (int l = Discriminator.LayerCount - 1; l >= 0; l--)
                {
                    gradForGen = Discriminator.Layers[l].Backward(gradForGen, _noOpOptimizer);
                }

                // B.4 Generator'dan geriye yay ve GÜNCELLE
                for (int l = Generator.LayerCount - 1; l >= 0; l--)
                {
                    gradForGen = Generator.Layers[l].Backward(gradForGen, genOptimizer);
                }
            }

            if (epoch % reportInterval == 0 || epoch == epochs)
            {
                double avgDError = totalDError / (dataCount * 2);
                double avgGError = totalGError / dataCount;
                
                int progress = (int)((double)epoch / epochs * 20);
                string bar = new string('█', progress) + new string('░', 20 - progress);

                Console.WriteLine($"  Epoch {epoch,5}/{epochs} │ D Hata: {avgDError:F4} │ G Hata: {avgGError:F4} │ {bar}");
            }
        }
        
        Console.WriteLine("\n  ✅ GAN Eğitimi tamamlandı!\n");
    }
}
