namespace HexAI.Core.Activation;

public class Linear : IActivation
{
    public string Name => "Linear";

    public double Activate(double x)
    {
        return x;
    }

    public double Derivative(double activatedOutput)
    {
        return 1.0;
    }
}
