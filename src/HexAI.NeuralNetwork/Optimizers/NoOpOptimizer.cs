using HexAI.Core.Math;

namespace HexAI.NeuralNetwork.Optimizers;

/// <summary>
/// Ağırlıkları GÜNCELLEMEYEN (No Operation) sahte bir optimizer.
/// Sadece GAN eğitiminde (Generative Adversarial Networks) 
/// Generator'ın hatalarını Discriminator üzerinden geriye yaymak (backpropagate) 
/// için kullanılır. Bu sırada Discriminator'ın kendi ağırlıkları bozulmaz.
/// </summary>
public class NoOpOptimizer : IOptimizer
{
    public double LearningRate { get; set; } = 0.0;

    public Matrix ComputeUpdate(Matrix gradients, Matrix weights)
    {
        // Hiçbir güncelleme yapma (Sıfır matrisi dön)
        return new Matrix(gradients.Rows, gradients.Cols);
    }
}
