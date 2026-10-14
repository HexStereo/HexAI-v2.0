using HexAI.Core.Activation;
using HexAI.Core.Math;

namespace HexAI.NeuralNetwork.Layers;

/// <summary>
/// Evrişimli Sinir Ağlarında boyut indirgemesi (Downsampling) için kullanılan Max Pooling katmanı.
/// Verilen havuz boyutu (Örn: 2x2) içindeki en büyük değeri seçerek öznitelik haritasını küçültür.
/// </summary>
public class MaxPooling2DLayer : ILayer
{
    public int InputHeight { get; }
    public int InputWidth { get; }
    public int Channels { get; } // Önceki katmandan gelen filtre sayısı
    public int PoolSize { get; }

    public int OutputHeight => InputHeight / PoolSize;
    public int OutputWidth => InputWidth / PoolSize;

    public int InputSize => InputHeight * InputWidth * Channels;
    public int NeuronCount => OutputHeight * OutputWidth * Channels;

    // Pooling katmanında aktivasyon olmaz, varsayılan olarak Linear (etkisiz) döner.
    public IActivation Activation { get; } = new Linear(); 

    public Matrix LastOutput { get; private set; } = new Matrix(1, 1);
    public Matrix LastInput { get; private set; } = new Matrix(1, 1);

    // Geri yayılım (Backpropagation) sırasında hatayı sadece MAX olan değere yansıtmak için index tutarız.
    private int[] _maxIndices = [];

    public MaxPooling2DLayer(int inputHeight, int inputWidth, int channels, int poolSize = 2)
    {
        InputHeight = inputHeight;
        InputWidth = inputWidth;
        Channels = channels;
        PoolSize = poolSize;
    }

    public Matrix Forward(Matrix input)
    {
        LastInput = input;
        var output1D = new Matrix(NeuronCount, 1);
        _maxIndices = new int[NeuronCount]; // Hangi pikseller Max seçildi?

        int outIdx = 0;
        int inArea = InputHeight * InputWidth;

        for (int c = 0; c < Channels; c++)
        {
            int channelOffset = c * inArea;

            for (int y = 0; y < OutputHeight; y++)
            {
                for (int x = 0; x < OutputWidth; x++)
                {
                    double maxVal = double.MinValue;
                    int maxIdx = -1;

                    // Pool penceresi içinde gez
                    for (int py = 0; py < PoolSize; py++)
                    {
                        for (int px = 0; px < PoolSize; px++)
                        {
                            int inY = y * PoolSize + py;
                            int inX = x * PoolSize + px;
                            int localIdx = channelOffset + (inY * InputWidth) + inX;

                            double val = input[localIdx, 0];
                            if (val > maxVal)
                            {
                                maxVal = val;
                                maxIdx = localIdx;
                            }
                        }
                    }

                    output1D[outIdx, 0] = maxVal;
                    _maxIndices[outIdx] = maxIdx; // Geri yayılım için kaydet
                    outIdx++;
                }
            }
        }

        LastOutput = output1D;
        return LastOutput;
    }

    public Matrix Backward(Matrix error, HexAI.NeuralNetwork.Optimizers.IOptimizer optimizer)
    {
        // Havuzlamada ağırlık yoktur, sadece hatayı bir önceki katmandaki "Max" pikselin yerine iletir.
        var prevError1D = new Matrix(InputSize, 1);

        // prevError1D varsayılan olarak sıfırlarla doludur.
        // Gelen hatayı sadece maxIndices listesindeki konumlara yansıtıyoruz.
        for (int i = 0; i < NeuronCount; i++)
        {
            int maxIdx = _maxIndices[i];
            prevError1D[maxIdx, 0] = error[i, 0];
        }

        return prevError1D;
    }
}
