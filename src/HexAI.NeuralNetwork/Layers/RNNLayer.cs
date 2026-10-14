using HexAI.Core.Activation;
using HexAI.Core.Math;

namespace HexAI.NeuralNetwork.Layers;

/// <summary>
/// Tekrarlayan Sinir Ağı Katmanı (Recurrent Neural Network - RNN).
/// Zaman içindeki (sequence) verileri "gizli durum" (hidden state) yardımıyla hatırlar.
/// </summary>
public class RNNLayer : IRecurrentLayer
{
    public int InputSize { get; }
    public int NeuronCount { get; }
    public IActivation Activation { get; }

    public Matrix LastOutput => _hiddenState;
    public Matrix LastInput { get; private set; } = new Matrix(1, 1);

    private Matrix _hiddenState;
    private Matrix _prevHiddenState;

    private Matrix _weightHx; // Girdi (Input) ağırlıkları
    private Matrix _weightHh; // Gizli hafıza (Hidden State) ağırlıkları
    private Matrix _biasH;

    public RNNLayer(int inputSize, int hiddenSize, IActivation activation, Random? rng = null)
    {
        InputSize = inputSize;
        NeuronCount = hiddenSize;
        Activation = activation;

        _hiddenState = new Matrix(NeuronCount, 1);
        _prevHiddenState = new Matrix(NeuronCount, 1);

        _weightHx = new Matrix(NeuronCount, InputSize);
        _weightHh = new Matrix(NeuronCount, NeuronCount);
        _biasH = new Matrix(NeuronCount, 1);

        rng ??= new Random();
        double varianceHx = 2.0 / (inputSize + hiddenSize);
        double varianceHh = 2.0 / (hiddenSize + hiddenSize);

        for (int i = 0; i < NeuronCount; i++)
        {
            _biasH[i, 0] = 0;
            
            for (int j = 0; j < InputSize; j++)
                _weightHx[i, j] = (rng.NextDouble() * 2 - 1) * System.Math.Sqrt(varianceHx);
                
            for (int j = 0; j < NeuronCount; j++)
                _weightHh[i, j] = (rng.NextDouble() * 2 - 1) * System.Math.Sqrt(varianceHh);
        }
    }

    public void ResetState()
    {
        // Kelime veya dizi bittiğinde hafızayı sıfırlıyoruz.
        _hiddenState = new Matrix(NeuronCount, 1);
        _prevHiddenState = new Matrix(NeuronCount, 1);
    }

    public Matrix Forward(Matrix input)
    {
        LastInput = input;
        
        // Geri yayılımda kullanmak üzere eski hafızayı sakla
        _prevHiddenState = new Matrix(NeuronCount, 1);
        for(int i=0; i<NeuronCount; i++) _prevHiddenState[i,0] = _hiddenState[i,0]; 

        // h_t = f(W_hx * x_t + W_hh * h_{t-1} + b)
        var z = (_weightHx * input) + (_weightHh * _prevHiddenState) + _biasH;
        _hiddenState = z.Map(Activation.Activate);

        return _hiddenState;
    }

    public Matrix Backward(Matrix error, HexAI.NeuralNetwork.Optimizers.IOptimizer optimizer)
    {
        // error = dL/dh_t
        // Bu kod, 1 adımlı Kesik BPTT (Truncated Backpropagation Through Time) uygular.
        
        var activationDerivative = _hiddenState.Map(Activation.Derivative);
        var delta = error.HadamardProduct(activationDerivative);

        // Gradient hesaplamaları
        var gradWx = delta * LastInput.Transpose();
        var gradWh = delta * _prevHiddenState.Transpose();
        
        // Önceki katmana (eğer varsa) iletilecek hata
        var inputError = _weightHx.Transpose() * delta;

        _weightHh.SubtractInPlace(optimizer.ComputeUpdate(gradWh, _weightHh));
        _weightHx.SubtractInPlace(optimizer.ComputeUpdate(gradWx, _weightHx));
        _biasH.SubtractInPlace(optimizer.ComputeUpdate(delta, _biasH));

        return inputError;
    }
}

