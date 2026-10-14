using HexAI.Core.Math;

namespace HexAI.NeuralNetwork.Optimizers;

/// <summary>
/// Standart Stokastik Gradyan İnişi (SGD) Eniyileyicisi.
/// Sadece öğrenme hızını (Learning Rate) kullanarak gradyanı ölçekler.
/// </summary>
public class SGDOptimizer : IOptimizer
{
    public double LearningRate { get; set; }

    public SGDOptimizer(double learningRate = 0.1)
    {
        LearningRate = learningRate;
    }

    public Matrix ComputeUpdate(Matrix gradient, Matrix weightReference)
    {
        return gradient * LearningRate;
    }
}
