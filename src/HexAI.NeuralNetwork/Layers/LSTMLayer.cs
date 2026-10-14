using System;
using HexAI.Core.Activation;
using HexAI.Core.Math;
using HexAI.NeuralNetwork.Optimizers;

namespace HexAI.NeuralNetwork.Layers;

/// <summary>
/// Uzun Kısa Vadeli Hafıza (LSTM) Katmanı.
/// Standart RNN'in yaşadığı "Vanishing Gradient" (Zayıflayan Gradyan) problemini
/// Hücre Durumu (Cell State) ve Kapılar (Forget, Input, Output) ile çözerek 
/// ağın çok daha uzun metinleri veya serileri hatırlamasını sağlar.
/// </summary>
public class LSTMLayer : ILayer, IRecurrentLayer
{
    public int NeuronCount { get; }
    public int InputSize { get; }
    public IActivation Activation { get; } = new TanhActivation(); // Default LSTM cell activation is Tanh

    public Matrix LastInput { get; private set; } = new Matrix(1, 1);
    public Matrix LastOutput { get; private set; } = new Matrix(1, 1);

    // Kapı Ağırlıkları (W_f, W_i, W_c, W_o) - [h_{t-1}, x_t] ile çarpılır
    private Matrix _Wf, _Wi, _Wc, _Wo;
    private Matrix _bf, _bi, _bc, _bo;

    // Hafıza Durumları
    private Matrix _h_prev, _c_prev;
    private Matrix _c_t;

    // Geri yayılım için ara değerler
    private Matrix _concat; // [h_{t-1}, x_t]
    private Matrix _ft, _it, _c_tilde, _ot, _tanh_c_t;

    private readonly IActivation _sigmoid = new Sigmoid();
    private readonly IActivation _tanh = new TanhActivation();
    
    public LSTMLayer(int inputSize, int hiddenSize, Random? rng = null)
    {
        InputSize = inputSize;
        NeuronCount = hiddenSize;
        rng ??= new Random();

        int concatSize = hiddenSize + inputSize;

        // Ağırlıkların başlatılması
        _Wf = Matrix.Random(hiddenSize, concatSize, rng);
        _Wi = Matrix.Random(hiddenSize, concatSize, rng);
        _Wc = Matrix.Random(hiddenSize, concatSize, rng);
        _Wo = Matrix.Random(hiddenSize, concatSize, rng);

        _bf = new Matrix(hiddenSize, 1);
        _bi = new Matrix(hiddenSize, 1);
        _bc = new Matrix(hiddenSize, 1);
        _bo = new Matrix(hiddenSize, 1);
        
        // Forget gate bias genellikle başlangıçta 1.0 yapılır (Hemen unutmasın diye)
        for (int i = 0; i < hiddenSize; i++) _bf[i, 0] = 1.0;

        _h_prev = new Matrix(hiddenSize, 1);
        _c_prev = new Matrix(hiddenSize, 1);
    }

    public void ResetState()
    {
        _h_prev = new Matrix(NeuronCount, 1);
        _c_prev = new Matrix(NeuronCount, 1);
    }

    public Matrix Forward(Matrix input)
    {
        LastInput = input;

        // [h_{t-1}, x_t] birleştir
        _concat = new Matrix(NeuronCount + InputSize, 1);
        for (int i = 0; i < NeuronCount; i++) _concat[i, 0] = _h_prev[i, 0];
        for (int i = 0; i < InputSize; i++) _concat[NeuronCount + i, 0] = input[i, 0];

        // Kapıları hesapla
        _ft = (_Wf * _concat + _bf).Map(_sigmoid.Activate);
        _it = (_Wi * _concat + _bi).Map(_sigmoid.Activate);
        _c_tilde = (_Wc * _concat + _bc).Map(_tanh.Activate);
        _ot = (_Wo * _concat + _bo).Map(_sigmoid.Activate);

        // Yeni Hücre Durumu: c_t = f_t * c_{t-1} + i_t * c_tilde
        _c_t = _ft.HadamardProduct(_c_prev) + _it.HadamardProduct(_c_tilde);

        // Yeni Gizli Durum: h_t = o_t * tanh(c_t)
        _tanh_c_t = _c_t.Map(_tanh.Activate);
        Matrix h_t = _ot.HadamardProduct(_tanh_c_t);

        _h_prev = h_t;
        _c_prev = _c_t;
        LastOutput = h_t;

        return LastOutput;
    }

    public Matrix Backward(Matrix error, IOptimizer optimizer)
    {
        // 1 Adımlı Truncated BPTT

        // dh_t = error
        Matrix dh_t = error;

        // do_t = dh_t * tanh(c_t)
        Matrix do_t = dh_t.HadamardProduct(_tanh_c_t);
        // sigmoid türevi: o_t * (1 - o_t)
        Matrix do_t_raw = do_t.HadamardProduct(_ot.Map(_sigmoid.Derivative));

        // dc_t = dh_t * o_t * (1 - tanh^2(c_t))
        Matrix dc_t = dh_t.HadamardProduct(_ot).HadamardProduct(_tanh_c_t.Map(_tanh.Derivative));

        // df_t = dc_t * c_{t-1}
        Matrix df_t = dc_t.HadamardProduct(_c_prev);
        Matrix df_t_raw = df_t.HadamardProduct(_ft.Map(_sigmoid.Derivative));

        // di_t = dc_t * c_tilde
        Matrix di_t = dc_t.HadamardProduct(_c_tilde);
        Matrix di_t_raw = di_t.HadamardProduct(_it.Map(_sigmoid.Derivative));

        // dc_tilde = dc_t * i_t
        Matrix dc_tilde = dc_t.HadamardProduct(_it);
        Matrix dc_tilde_raw = dc_tilde.HadamardProduct(_c_tilde.Map(_tanh.Derivative));

        // Ağırlık Gradyanları
        Matrix dWf = df_t_raw * _concat.Transpose();
        Matrix dWi = di_t_raw * _concat.Transpose();
        Matrix dWc = dc_tilde_raw * _concat.Transpose();
        Matrix dWo = do_t_raw * _concat.Transpose();

        // Bir önceki katmana iletilecek hata
        Matrix dConcat = (_Wf.Transpose() * df_t_raw) + 
                         (_Wi.Transpose() * di_t_raw) + 
                         (_Wc.Transpose() * dc_tilde_raw) + 
                         (_Wo.Transpose() * do_t_raw);

        Matrix inputError = new Matrix(InputSize, 1);
        for (int i = 0; i < InputSize; i++) inputError[i, 0] = dConcat[NeuronCount + i, 0];

        // Ağırlık ve Bias güncellemeleri
        _Wf.SubtractInPlace(optimizer.ComputeUpdate(dWf, _Wf));
        _Wi.SubtractInPlace(optimizer.ComputeUpdate(dWi, _Wi));
        _Wc.SubtractInPlace(optimizer.ComputeUpdate(dWc, _Wc));
        _Wo.SubtractInPlace(optimizer.ComputeUpdate(dWo, _Wo));

        _bf.SubtractInPlace(optimizer.ComputeUpdate(df_t_raw, _bf));
        _bi.SubtractInPlace(optimizer.ComputeUpdate(di_t_raw, _bi));
        _bc.SubtractInPlace(optimizer.ComputeUpdate(dc_tilde_raw, _bc));
        _bo.SubtractInPlace(optimizer.ComputeUpdate(do_t_raw, _bo));

        return inputError;
    }
}

