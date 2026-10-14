namespace HexAI.Core.Math;

using System;
using System.Collections.Generic;

/// <summary>
/// PyTorch benzeri otomatik türev (Autograd) motoru için Tensor sınıfı.
/// Matris değerlerini ve gradyanlarını tutar, geriye yayılım (backward pass) grafiğini oluşturur.
/// </summary>
public class Tensor
{
    /// <summary>Matris değeri.</summary>
    public Matrix Value { get; set; }

    /// <summary>Türev (gradyan) matrisi.</summary>
    public Matrix Gradient { get; set; }

    /// <summary>Bu tensorün türetildiği ata tensorler (hesaplama grafiği).</summary>
    public List<Tensor> Parents { get; } = new();

    /// <summary>Geriye yayılım esnasında bu işlemin gradyanını hesaplayacak fonksiyon.</summary>
    public Action? BackwardAction { get; set; }

    /// <summary>
    /// Matris değerine göre yeni bir Tensor oluşturur.
    /// </summary>
    public Tensor(Matrix value)
    {
        Value = value;
        Gradient = new Matrix(value.Rows, value.Cols);
    }

    public Tensor(double[,] value) : this(new Matrix(value)) { }
    public Tensor(double[] value) : this(Matrix.FromArray(value)) { }

    /// <summary>
    /// Tensor toplama işlemi: C = A + B
    /// </summary>
    public static Tensor operator +(Tensor a, Tensor b)
    {
        var result = new Tensor(a.Value + b.Value);
        result.Parents.Add(a);
        result.Parents.Add(b);

        result.BackwardAction = () =>
        {
            a.Gradient += result.Gradient;
            b.Gradient += result.Gradient;
        };

        return result;
    }

    /// <summary>
    /// Tensor çıkarma işlemi: C = A - B
    /// </summary>
    public static Tensor operator -(Tensor a, Tensor b)
    {
        var result = new Tensor(a.Value - b.Value);
        result.Parents.Add(a);
        result.Parents.Add(b);

        result.BackwardAction = () =>
        {
            a.Gradient += result.Gradient;
            b.Gradient -= result.Gradient;
        };

        return result;
    }

    /// <summary>
    /// Tensor çarpım işlemi: C = A * B (Matris Çarpımı)
    /// </summary>
    public static Tensor operator *(Tensor a, Tensor b)
    {
        var result = new Tensor(a.Value * b.Value);
        result.Parents.Add(a);
        result.Parents.Add(b);

        result.BackwardAction = () =>
        {
            // Matris çarpımı türevleri:
            // Y = A * B  => dA = dY * B^T,  dB = A^T * dY
            a.Gradient += result.Gradient * b.Value.Transpose();
            b.Gradient += a.Value.Transpose() * result.Gradient;
        };

        return result;
    }

    /// <summary>
    /// Eleman bazlı çarpım (Hadamard çarpımı): C = A . B
    /// </summary>
    public Tensor Hadamard(Tensor other)
    {
        var result = new Tensor(Value.HadamardProduct(other.Value));
        result.Parents.Add(this);
        result.Parents.Add(other);

        result.BackwardAction = () =>
        {
            this.Gradient += result.Gradient.HadamardProduct(other.Value);
            other.Gradient += result.Gradient.HadamardProduct(this.Value);
        };

        return result;
    }

    /// <summary>
    /// Geriye yayılım (Backpropagation) işlemini başlatır.
    /// Hesaplama grafiğini topolojik olarak sıralar ve gradyanları geriye doğru dağıtır.
    /// </summary>
    public void Backward()
    {
        var order = new List<Tensor>();
        var visited = new HashSet<Tensor>();

        void Visit(Tensor node)
        {
            if (!visited.Contains(node))
            {
                visited.Add(node);
                foreach (var parent in node.Parents)
                {
                    Visit(parent);
                }
                order.Add(node);
            }
        }

        Visit(this);

        // Başlangıç gradyanını 1.0 olarak ayarla (Loss türevi)
        for (int i = 0; i < Gradient.Data.Length; i++)
        {
            Gradient.Data[i] = 1.0;
        }

        // Topolojik sıralamaya göre tersten geriye doğru gradyanları işlet
        for (int i = order.Count - 1; i >= 0; i--)
        {
            order[i].BackwardAction?.Invoke();
        }
    }

    /// <summary>
    /// Hesaplama grafiğindeki tüm gradyanları sıfırlar.
    /// </summary>
    public void ZeroGrad()
    {
        var visited = new HashSet<Tensor>();
        void Reset(Tensor node)
        {
            if (!visited.Contains(node))
            {
                visited.Add(node);
                Array.Clear(node.Gradient.Data, 0, node.Gradient.Data.Length);
                foreach (var parent in node.Parents)
                {
                    Reset(parent);
                }
            }
        }
        Reset(this);
    }
}
