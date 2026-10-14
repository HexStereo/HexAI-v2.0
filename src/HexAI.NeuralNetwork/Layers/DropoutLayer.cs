using System;
using HexAI.Core.Activation;
using HexAI.Core.Math;
using HexAI.NeuralNetwork.Optimizers;

namespace HexAI.NeuralNetwork.Layers;

/// <summary>
/// Ağın ezberlemesini (Overfitting) önlemek için eğitim sırasında 
/// nöronların belirli bir yüzdesini rastgele "kapatan" katman.
/// Test (Prediction) sırasında tüm nöronlar açık kalır ancak çıktı olasılıkla çarpılarak dengelenir.
/// </summary>
public class DropoutLayer : ILayer
{
    public int NeuronCount { get; }
    public int InputSize { get; }
    public IActivation Activation { get; }

    public Matrix LastOutput { get; private set; } = new Matrix(1, 1);
    public Matrix LastInput { get; private set; } = new Matrix(1, 1);

    public double DropoutRate { get; }
    
    // Eğitim sırasında hangi nöronların açık/kapalı tutulduğunu gösteren maske
    private Matrix _mask = new Matrix(1, 1);
    private readonly Random _rng;

    // Ağı test mi yoksa eğitim için mi kullandığımızı bilmeliyiz
    // Standart ILayer Forward'da eğitim durumu için ekstra flag olmadığından
    // DropoutLayer genellikle global bir Network.IsTraining flag'ine güvenir.
    // Ancak basitlik adına Dropout'u hep uygulayıp test öncesi dropoutRate=0 olarak set edebiliriz
    // veya ağda Test aşamasında Dropout'un atlanmasını sağlayabiliriz.
    public bool IsTraining { get; set; } = true;

    public DropoutLayer(int size, double dropoutRate, Random? rng = null)
    {
        NeuronCount = size;
        InputSize = size;
        DropoutRate = dropoutRate;
        Activation = new Linear(); // Sadece geçiş yapar
        _rng = rng ?? new Random();
    }

    public Matrix Forward(Matrix input)
    {
        LastInput = input;
        
        if (!IsTraining || DropoutRate == 0)
        {
            // Test modunda kapatma yapılmaz, beklenen değer (E) dengelenmek için 
            // dropoutRate ile ölçeklenebilir ama biz Inverted Dropout kullanacağız.
            LastOutput = input;
            return LastOutput;
        }

        // Inverted Dropout mantığı (Test aşamasında ekstra ölçekleme yapmamak için)
        double scale = 1.0 / (1.0 - DropoutRate);
        
        _mask = new Matrix(input.Rows, input.Cols);
        LastOutput = new Matrix(input.Rows, input.Cols);

        for (int i = 0; i < input.Data.Length; i++)
        {
            if (_rng.NextDouble() >= DropoutRate)
            {
                // Nöron açık
                _mask.Data[i] = 1.0 * scale;
                LastOutput.Data[i] = input.Data[i] * _mask.Data[i];
            }
            else
            {
                // Nöron kapalı
                _mask.Data[i] = 0.0;
                LastOutput.Data[i] = 0.0;
            }
        }

        return LastOutput;
    }

    public Matrix Backward(Matrix error, IOptimizer optimizer)
    {
        // Kapatılan nöronlardan geriye hata aktarılmaz.
        Matrix inputError = new Matrix(error.Rows, error.Cols);
        for (int i = 0; i < error.Data.Length; i++)
        {
            inputError.Data[i] = error.Data[i] * _mask.Data[i];
        }

        return inputError;
    }
}
