using HexAI.Core.Activation;
using HexAI.Core.Math;

namespace HexAI.NeuralNetwork.Layers;

/// <summary>
/// Evrişimli Sinir Ağı (CNN) için basit 2D Evrişim (Convolution) Katmanı.
/// Karmaşıklığı yönetmek adına şimdilik tek kanallı (Grayscale - Channel=1) girdilerle çalışır.
/// </summary>
public class Conv2DLayer : ILayer
{
    public int InputHeight { get; }
    public int InputWidth { get; }
    public int FilterCount { get; }
    public int KernelSize { get; }

    public int OutputHeight => InputHeight - KernelSize + 1;
    public int OutputWidth => InputWidth - KernelSize + 1;

    public int InputSize => InputHeight * InputWidth;
    public int NeuronCount => OutputHeight * OutputWidth * FilterCount;

    public IActivation Activation { get; }
    public Matrix LastOutput { get; private set; } = new Matrix(1, 1);
    public Matrix LastInput { get; private set; } = new Matrix(1, 1);

    // Filtreler [filterIndex][y, x]
    private readonly Matrix[] _filters;
    private readonly double[] _biases;
    private Matrix[] _lastConvolvedMaps = []; // Aktivasyon öncesi haritalar

    public Conv2DLayer(int inputHeight, int inputWidth, int filterCount, int kernelSize, IActivation activation, Random? rng = null)
    {
        InputHeight = inputHeight;
        InputWidth = inputWidth;
        FilterCount = filterCount;
        KernelSize = kernelSize;
        Activation = activation;

        rng ??= new Random();

        _filters = new Matrix[filterCount];
        _biases = new double[filterCount];

        for (int i = 0; i < filterCount; i++)
        {
            _filters[i] = new Matrix(kernelSize, kernelSize);
            _biases[i] = 0;

            // Glorot/Xavier Initialization
            double variance = 2.0 / (kernelSize * kernelSize + NeuronCount);
            for (int y = 0; y < kernelSize; y++)
            {
                for (int x = 0; x < kernelSize; x++)
                {
                    _filters[i][y, x] = (rng.NextDouble() * 2 - 1) * Math.Sqrt(variance);
                }
            }
        }
    }

    public Matrix Forward(Matrix input)
    {
        LastInput = input;

        // Girdiyi 1D'den 2D matrise çevir (Varsayım: Girdi 1 Kanallı)
        var input2D = new Matrix(InputHeight, InputWidth);
        int idx = 0;
        for (int y = 0; y < InputHeight; y++)
        {
            for (int x = 0; x < InputWidth; x++)
            {
                input2D[y, x] = input[idx++, 0];
            }
        }

        _lastConvolvedMaps = new Matrix[FilterCount];
        var output1D = new Matrix(NeuronCount, 1);
        int outIdx = 0;

        // Her filtre için evrişim işlemi (Cross-correlation)
        for (int f = 0; f < FilterCount; f++)
        {
            var filter = _filters[f];
            var bias = _biases[f];
            var convMap = new Matrix(OutputHeight, OutputWidth);

            for (int y = 0; y < OutputHeight; y++)
            {
                for (int x = 0; x < OutputWidth; x++)
                {
                    double sum = 0;
                    // Bölgesel çarpım ve toplama
                    for (int ky = 0; ky < KernelSize; ky++)
                    {
                        for (int kx = 0; kx < KernelSize; kx++)
                        {
                            sum += input2D[y + ky, x + kx] * filter[ky, kx];
                        }
                    }
                    sum += bias;
                    
                    double activatedVal = Activation.Activate(sum);
                    convMap[y, x] = activatedVal;
                    
                    // 1D çıktı dizisine aktar
                    output1D[outIdx++, 0] = activatedVal;
                }
            }
            _lastConvolvedMaps[f] = convMap;
        }

        LastOutput = output1D;
        return LastOutput;
    }

    public Matrix Backward(Matrix error, HexAI.NeuralNetwork.Optimizers.IOptimizer optimizer)
    {
        // Girdiyi tekrar 2D yap
        var input2D = new Matrix(InputHeight, InputWidth);
        int idx = 0;
        for (int y = 0; y < InputHeight; y++)
        {
            for (int x = 0; x < InputWidth; x++)
            {
                input2D[y, x] = LastInput[idx++, 0];
            }
        }

        var inputError2D = new Matrix(InputHeight, InputWidth);
        int errIdx = 0;

        for (int f = 0; f < FilterCount; f++)
        {
            var filter = _filters[f];
            
            // Bu filtre için türevleri tutacağımız matrisler
            var filterGrad = new Matrix(KernelSize, KernelSize);
            double biasGrad = 0;

            for (int y = 0; y < OutputHeight; y++)
            {
                for (int x = 0; x < OutputWidth; x++)
                {
                    // Zincir kuralı: Dışarıdan gelen hata * Aktivasyon türevi
                    double grad = error[errIdx, 0] * Activation.Derivative(_lastConvolvedMaps[f][y, x]);
                    errIdx++;

                    biasGrad += grad;

                    // Filtre ağırlıklarının gradyanı ve Girdi hatasının hesaplanması
                    for (int ky = 0; ky < KernelSize; ky++)
                    {
                        for (int kx = 0; kx < KernelSize; kx++)
                        {
                            filterGrad[ky, kx] += input2D[y + ky, x + kx] * grad;
                            inputError2D[y + ky, x + kx] += filter[ky, kx] * grad;
                        }
                    }
                }
            }

            // Ağırlıkları güncelle (Optimizer kullanarak)
            _filters[f] = _filters[f] - optimizer.ComputeUpdate(filterGrad, _filters[f]);
            _biases[f] -= biasGrad * optimizer.LearningRate;
        }

        // Önceki katmana hatayı 1D olarak ilet
        var prevError1D = new Matrix(InputSize, 1);
        int pIdx = 0;
        for (int y = 0; y < InputHeight; y++)
        {
            for (int x = 0; x < InputWidth; x++)
            {
                prevError1D[pIdx++, 0] = inputError2D[y, x];
            }
        }

        return prevError1D;
    }
}
