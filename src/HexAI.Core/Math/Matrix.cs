namespace HexAI.Core.Math;

using System;
using System.Threading.Tasks;

/// <summary>
/// Matris sınıfı — Yapay zeka algoritmaları için temel lineer cebir işlemleri.
/// Sinir ağlarında ağırlık matrisleri, veri dönüşümleri ve backpropagation
/// hesaplamaları için kullanılır.
/// (Faz 12: Profesyonel Hızlandırma ve Paralel İşlem ile Güçlendirildi)
/// </summary>
public class Matrix
{
    // Matris verisi tek boyutlu diziye düzleştirildi (Cache Locality için)
    public double[] Data { get; }
    public int Rows { get; }
    public int Cols { get; }

    /// <summary>
    /// Belirtilen boyutta sıfırlarla dolu matris oluşturur.
    /// </summary>
    public Matrix(int rows, int cols)
    {
        Rows = rows;
        Cols = cols;
        Data = new double[rows * cols];
    }

    /// <summary>
    /// 2D diziden matris oluşturur.
    /// </summary>
    public Matrix(double[,] data)
    {
        Rows = data.GetLength(0);
        Cols = data.GetLength(1);
        Data = new double[Rows * Cols];
        
        // Paralel olmayan hızlı kopyalama (initialization)
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Cols; j++)
                Data[i * Cols + j] = data[i, j];
    }

    /// <summary>
    /// İçsel olarak kopyalanmış veya oluşturulmuş 1D dizi üzerinden doğrudan başlatma
    /// </summary>
    private Matrix(int rows, int cols, double[] flattenedData)
    {
        Rows = rows;
        Cols = cols;
        Data = flattenedData;
    }

    /// <summary>
    /// Tek elemana erişim (indeksleyici). Tek boyutlu matematiğe çevrildi.
    /// </summary>
    public double this[int row, int col]
    {
        get => Data[row * Cols + col];
        set => Data[row * Cols + col] = value;
    }

    #region Temel İşlemler

    /// <summary>
    /// Matris toplama: C = A + B (SIMD Hızlandırmalı)
    /// </summary>
    public static Matrix operator +(Matrix a, Matrix b)
    {
        if (a.Rows != b.Rows || a.Cols != b.Cols)
            throw new ArgumentException(
                $"Matris boyutları uyuşmuyor: ({a.Rows}x{a.Cols}) + ({b.Rows}x{b.Cols})");

        var result = new Matrix(a.Rows, a.Cols);
        int length = a.Data.Length;
        int vectorSize = System.Numerics.Vector<double>.Count;
        int i = 0;

        if (System.Numerics.Vector.IsHardwareAccelerated && length >= vectorSize)
        {
            for (; i <= length - vectorSize; i += vectorSize)
            {
                var va = new System.Numerics.Vector<double>(a.Data, i);
                var vb = new System.Numerics.Vector<double>(b.Data, i);
                var vr = va + vb;
                vr.CopyTo(result.Data, i);
            }
        }

        for (; i < length; i++)
        {
            result.Data[i] = a.Data[i] + b.Data[i];
        }

        return result;
    }

    /// <summary>
    /// Matris çıkarma: C = A - B (SIMD Hızlandırmalı)
    /// </summary>
    public static Matrix operator -(Matrix a, Matrix b)
    {
        if (a.Rows != b.Rows || a.Cols != b.Cols)
            throw new ArgumentException(
                $"Matris boyutları uyuşmuyor: ({a.Rows}x{a.Cols}) - ({b.Rows}x{b.Cols})");

        var result = new Matrix(a.Rows, a.Cols);
        int length = a.Data.Length;
        int vectorSize = System.Numerics.Vector<double>.Count;
        int i = 0;

        if (System.Numerics.Vector.IsHardwareAccelerated && length >= vectorSize)
        {
            for (; i <= length - vectorSize; i += vectorSize)
            {
                var va = new System.Numerics.Vector<double>(a.Data, i);
                var vb = new System.Numerics.Vector<double>(b.Data, i);
                var vr = va - vb;
                vr.CopyTo(result.Data, i);
            }
        }

        for (; i < length; i++)
        {
            result.Data[i] = a.Data[i] - b.Data[i];
        }

        return result;
    }

    /// <summary>
    /// Verilen matrisi, bu matrisin üzerinden in-place (yerinde) çıkarır. 
    /// Yeni obje oluşturmaz. (Optimizer ve GC bellek optimizasyonu için)
    /// </summary>
    public void SubtractInPlace(Matrix other)
    {
        if (Rows != other.Rows || Cols != other.Cols)
            throw new ArgumentException("Boyutlar eşleşmiyor.");
            
        int length = Data.Length;
        int vectorSize = System.Numerics.Vector<double>.Count;
        int i = 0;

        if (System.Numerics.Vector.IsHardwareAccelerated && length >= vectorSize)
        {
            for (; i <= length - vectorSize; i += vectorSize)
            {
                var va = new System.Numerics.Vector<double>(Data, i);
                var vb = new System.Numerics.Vector<double>(other.Data, i);
                var vr = va - vb;
                vr.CopyTo(Data, i);
            }
        }

        for (; i < length; i++)
        {
            Data[i] -= other.Data[i];
        }
    }

    /// <summary>
    /// Matris çarpımı (dot product): C = A * B (Paralel + SIMD Hızlandırmalı)
    /// </summary>
    public static Matrix operator *(Matrix a, Matrix b)
    {
        if (a.Cols != b.Rows)
            throw new ArgumentException(
                $"Matris çarpımı için boyutlar uyuşmuyor: ({a.Rows}x{a.Cols}) * ({b.Rows}x{b.Cols})");

        var result = new Matrix(a.Rows, b.Cols);
        int aCols = a.Cols;
        int bCols = b.Cols;
        int vectorSize = System.Numerics.Vector<double>.Count;

        if (a.Rows >= 8)
        {
            Parallel.For(0, a.Rows, i =>
            {
                int rowOffsetA = i * aCols;
                int rowOffsetResult = i * bCols;
                for (int k = 0; k < aCols; k++)
                {
                    double aVal = a.Data[rowOffsetA + k];
                    int rowOffsetB = k * bCols;
                    int j = 0;

                    if (System.Numerics.Vector.IsHardwareAccelerated && bCols >= vectorSize)
                    {
                        var va = new System.Numerics.Vector<double>(aVal);
                        for (; j <= bCols - vectorSize; j += vectorSize)
                        {
                            var vr = new System.Numerics.Vector<double>(result.Data, rowOffsetResult + j);
                            var vb = new System.Numerics.Vector<double>(b.Data, rowOffsetB + j);
                            var vres = vr + va * vb;
                            vres.CopyTo(result.Data, rowOffsetResult + j);
                        }
                    }

                    for (; j < bCols; j++)
                    {
                        result.Data[rowOffsetResult + j] += aVal * b.Data[rowOffsetB + j];
                    }
                }
            });
        }
        else
        {
            for (int i = 0; i < a.Rows; i++)
            {
                int rowOffsetA = i * aCols;
                int rowOffsetResult = i * bCols;
                for (int k = 0; k < aCols; k++)
                {
                    double aVal = a.Data[rowOffsetA + k];
                    int rowOffsetB = k * bCols;
                    int j = 0;

                    if (System.Numerics.Vector.IsHardwareAccelerated && bCols >= vectorSize)
                    {
                        var va = new System.Numerics.Vector<double>(aVal);
                        for (; j <= bCols - vectorSize; j += vectorSize)
                        {
                            var vr = new System.Numerics.Vector<double>(result.Data, rowOffsetResult + j);
                            var vb = new System.Numerics.Vector<double>(b.Data, rowOffsetB + j);
                            var vres = vr + va * vb;
                            vres.CopyTo(result.Data, rowOffsetResult + j);
                        }
                    }

                    for (; j < bCols; j++)
                    {
                        result.Data[rowOffsetResult + j] += aVal * b.Data[rowOffsetB + j];
                    }
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Skaler çarpım: C = A * scalar (SIMD Hızlandırmalı)
    /// </summary>
    public static Matrix operator *(Matrix a, double scalar)
    {
        var result = new Matrix(a.Rows, a.Cols);
        int length = a.Data.Length;
        int vectorSize = System.Numerics.Vector<double>.Count;
        int i = 0;

        if (System.Numerics.Vector.IsHardwareAccelerated && length >= vectorSize)
        {
            var vs = new System.Numerics.Vector<double>(scalar);
            for (; i <= length - vectorSize; i += vectorSize)
            {
                var va = new System.Numerics.Vector<double>(a.Data, i);
                var vr = va * vs;
                vr.CopyTo(result.Data, i);
            }
        }

        for (; i < length; i++)
        {
            result.Data[i] = a.Data[i] * scalar;
        }

        return result;
    }

    /// <summary>
    /// Skaler çarpım (tersten): C = scalar * A
    /// </summary>
    public static Matrix operator *(double scalar, Matrix a) => a * scalar;

    #endregion

    #region Eleman Bazlı İşlemler

    /// <summary>
    /// Hadamard çarpımı — eleman bazlı çarpım (SIMD Hızlandırmalı).
    /// </summary>
    public Matrix HadamardProduct(Matrix other)
    {
        if (Rows != other.Rows || Cols != other.Cols)
            throw new ArgumentException("Hadamard çarpımı için boyutlar eşit olmalı.");

        var result = new Matrix(Rows, Cols);
        int length = Data.Length;
        int vectorSize = System.Numerics.Vector<double>.Count;
        int i = 0;

        if (System.Numerics.Vector.IsHardwareAccelerated && length >= vectorSize)
        {
            for (; i <= length - vectorSize; i += vectorSize)
            {
                var va = new System.Numerics.Vector<double>(Data, i);
                var vb = new System.Numerics.Vector<double>(other.Data, i);
                var vr = va * vb;
                vr.CopyTo(result.Data, i);
            }
        }

        for (; i < length; i++)
        {
            result.Data[i] = Data[i] * other.Data[i];
        }

        return result;
    }

    /// <summary>
    /// Her elemana verilen fonksiyonu uygular.
    /// </summary>
    public Matrix Map(Func<double, double> func)
    {
        var result = new Matrix(Rows, Cols);
        // Func mapping might not be thread safe depending on lambda captures, ama genelde güvenlidir.
        for (int i = 0; i < Data.Length; i++)
        {
            result.Data[i] = func(Data[i]);
        }
        return result;
    }

    #endregion

    #region Dönüşüm İşlemleri

    /// <summary>
    /// Transpoz: Satır ve sütunları yer değiştirir.
    /// </summary>
    public Matrix Transpose()
    {
        var result = new Matrix(Cols, Rows);
        int cols = Cols;
        int rows = Rows;
        for (int i = 0; i < rows; i++)
        {
            int rowOffset = i * cols;
            for (int j = 0; j < cols; j++)
            {
                result.Data[j * rows + i] = Data[rowOffset + j];
            }
        }
        return result;
    }

    /// <summary>
    /// 1D diziden sütun vektörü (Nx1 matris) oluşturur.
    /// </summary>
    public static Matrix FromArray(double[] array)
    {
        var result = new Matrix(array.Length, 1);
        Array.Copy(array, result.Data, array.Length);
        return result;
    }

    /// <summary>
    /// Matrisi 1D diziye dönüştürür (sütun bazlı).
    /// </summary>
    public double[] ToArray()
    {
        var result = new double[Data.Length];
        Array.Copy(Data, result, Data.Length);
        return result;
    }

    /// <summary>
    /// Matrisin derin kopyasını oluşturur.
    /// </summary>
    public Matrix Clone()
    {
        var result = new Matrix(Rows, Cols);
        Array.Copy(Data, result.Data, Data.Length);
        return result;
    }

    #endregion

    #region Fabrika Metodları

    /// <summary>
    /// Rastgele değerlerle dolu matris oluşturur.
    /// </summary>
    public static Matrix Random(int rows, int cols, Random? rng = null)
    {
        rng ??= new Random();
        var result = new Matrix(rows, cols);

        double limit = System.Math.Sqrt(6.0 / (rows + cols));
        // Random is not thread-safe, so run sequentially.
        for (int i = 0; i < result.Data.Length; i++)
        {
            result.Data[i] = (rng.NextDouble() * 2 - 1) * limit;
        }
        return result;
    }

    /// <summary>
    /// Birim (identity) matris oluşturur.
    /// </summary>
    public static Matrix Identity(int size)
    {
        var result = new Matrix(size, size);
        for (int i = 0; i < size; i++)
        {
            result.Data[i * size + i] = 1.0;
        }
        return result;
    }

    #endregion

    /// <summary>
    /// Matrisi Serialization işlemleri için Jagged Array (double[][])'e çevirir.
    /// </summary>
    public double[][] ToJaggedArray()
    {
        var result = new double[Rows][];
        for (int i = 0; i < Rows; i++)
        {
            result[i] = new double[Cols];
            int rowOffset = i * Cols;
            for (int j = 0; j < Cols; j++)
            {
                result[i][j] = Data[rowOffset + j];
            }
        }
        return result;
    }

    /// <summary>
    /// Jagged Array (double[][])'den Matrix oluşturur (Deserialization için).
    /// </summary>
    public static Matrix FromJaggedArray(double[][] jagged)
    {
        if (jagged == null || jagged.Length == 0)
            return new Matrix(0, 0);

        int rows = jagged.Length;
        int cols = jagged[0].Length;
        var matrix = new Matrix(rows, cols);

        for (int i = 0; i < rows; i++)
        {
            int rowOffset = i * cols;
            for (int j = 0; j < cols; j++)
            {
                matrix.Data[rowOffset + j] = jagged[i][j];
            }
        }
        return matrix;
    }

    /// <summary>
    /// Matrisin okunabilir gösterimi.
    /// </summary>
    public override string ToString()
    {
        var lines = new string[Rows];
        for (int i = 0; i < Rows; i++)
        {
            var values = new string[Cols];
            int rowOffset = i * Cols;
            for (int j = 0; j < Cols; j++)
                values[j] = Data[rowOffset + j].ToString("F4");
            lines[i] = $"  [{string.Join(", ", values)}]";
        }
        return $"Matrix ({Rows}x{Cols}):\n{string.Join("\n", lines)}";
    }
}
