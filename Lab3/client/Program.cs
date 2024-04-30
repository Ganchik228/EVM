using System.IO.Pipes;
using System.Runtime.CompilerServices;

public struct Numbers
{
    public double number1;
    public double number2;
    public double result;
    public int priority;
}

class PipeClient
{
    static void Main(string[] args)
    {
        Console.Title = "Client";
        if (args.Length > 0)
        {
            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", args[0], PipeDirection.InOut))
            {
                Console.Write("Attempting to connect to pipe...");
                pipeClient.Connect();
                Console.WriteLine("Connected to pipe.");
                try
                {
                    int n = 100000;
                    byte[] bytes = new byte[Unsafe.SizeOf<Numbers>()];
                    pipeClient.Read(bytes, 0, bytes.Length);
                    Numbers receivedData = Unsafe.As<byte, Numbers>(ref bytes[0]);
                    Console.WriteLine("Number 1: " + receivedData.number1);
                    Console.WriteLine("Number 2: " + receivedData.number2);

                    receivedData.result = intg(receivedData.number1, receivedData.number2, n);
                    Console.WriteLine(receivedData.result);
                    byte[] modifiedBytes = new byte[Unsafe.SizeOf<Numbers>()];
                    Unsafe.As<byte, Numbers>(ref modifiedBytes[0]) = receivedData;
                    pipeClient.Write(modifiedBytes, 0, modifiedBytes.Length);
                }
                catch (Exception) { }
            }
            Console.Write("Press Enter to continue...");
            Console.ReadLine();
        }
    }
    static double Function(double x)
    {
        return 2 * Math.Cos(x);
    }
    static double intg(double a, double b, int n)
    {
        double h = (b - a) / Convert.ToDouble(n);
        double result = 0.5 * (Function(a) + Function(b));



        for (int i = 1; i < n; i++)
        {
            double x = a + i * h;
            result += Function(x);
        }

        result *= h;

        Console.WriteLine(result);
        return result;
    }

}