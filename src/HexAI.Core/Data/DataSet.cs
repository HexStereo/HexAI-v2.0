namespace HexAI.Core.Data;

/// <summary>
/// Basit veri seti yapısı.
/// Eğitim ve test verilerini organize eder.
/// Her örnek bir girdi dizisi (features) ve beklenen çıktı dizisi (labels) içerir.
/// </summary>
public class DataSet
{
    /// <summary>Veri seti örnekleri.</summary>
    public List<DataSample> Samples { get; } = [];

    /// <summary>Veri setinin adı.</summary>
    public string Name { get; set; } = "Unnamed";

    /// <summary>Girdi boyutu (feature sayısı).</summary>
    public int InputSize => Samples.Count > 0 ? Samples[0].Inputs.Length : 0;

    /// <summary>Çıktı boyutu (label sayısı).</summary>
    public int OutputSize => Samples.Count > 0 ? Samples[0].ExpectedOutputs.Length : 0;

    /// <summary>Toplam örnek sayısı.</summary>
    public int Count => Samples.Count;

    /// <summary>
    /// Yeni bir örnek ekler.
    /// </summary>
    public void Add(double[] inputs, double[] expectedOutputs)
    {
        Samples.Add(new DataSample(inputs, expectedOutputs));
    }

    /// <summary>
    /// Veri setini karıştırır (shuffle).
    /// Eğitim sırasında modelin veri sırasına bağımlı olmasını önler.
    /// </summary>
    public void Shuffle(Random? rng = null)
    {
        rng ??= new Random();
        // Fisher-Yates karıştırma algoritması
        for (int i = Samples.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (Samples[i], Samples[j]) = (Samples[j], Samples[i]);
        }
    }

    /// <summary>
    /// Veri setini eğitim ve test olarak böler.
    /// </summary>
    /// <param name="trainRatio">Eğitim oranı (0-1 arası, örn: 0.8 = %80 eğitim)</param>
    public (DataSet Train, DataSet Test) Split(double trainRatio = 0.8)
    {
        int trainCount = (int)(Samples.Count * trainRatio);
        var train = new DataSet { Name = $"{Name}_Train" };
        var test = new DataSet { Name = $"{Name}_Test" };

        for (int i = 0; i < Samples.Count; i++)
        {
            if (i < trainCount)
                train.Samples.Add(Samples[i]);
            else
                test.Samples.Add(Samples[i]);
        }

        return (train, test);
    }

    /// <summary>
    /// XOR veri seti oluşturur — sinir ağı testleri için klasik problem.
    /// </summary>
    public static DataSet CreateXOR()
    {
        var ds = new DataSet { Name = "XOR" };
        ds.Add([0, 0], [0]);
        ds.Add([0, 1], [1]);
        ds.Add([1, 0], [1]);
        ds.Add([1, 1], [0]);
        return ds;
    }

    /// <summary>
    /// AND veri seti oluşturur.
    /// </summary>
    public static DataSet CreateAND()
    {
        var ds = new DataSet { Name = "AND" };
        ds.Add([0, 0], [0]);
        ds.Add([0, 1], [0]);
        ds.Add([1, 0], [0]);
        ds.Add([1, 1], [1]);
        return ds;
    }

    /// <summary>
    /// OR veri seti oluşturur.
    /// </summary>
    public static DataSet CreateOR()
    {
        var ds = new DataSet { Name = "OR" };
        ds.Add([0, 0], [0]);
        ds.Add([0, 1], [1]);
        ds.Add([1, 0], [1]);
        ds.Add([1, 1], [1]);
        return ds;
    }
}

/// <summary>
/// Tek bir veri örneği — girdi ve beklenen çıktı çifti.
/// </summary>
public class DataSample
{
    public double[] Inputs { get; }
    public double[] ExpectedOutputs { get; }

    public DataSample(double[] inputs, double[] expectedOutputs)
    {
        Inputs = inputs;
        ExpectedOutputs = expectedOutputs;
    }
}
