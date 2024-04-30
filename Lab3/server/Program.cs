using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

public struct Numbers
{
    public double number1;
    public double number2;
    public double result;
    public int priority;
}

internal static class PipeServer
{
    private static PriorityQueue<Numbers, int> dataQueue = new PriorityQueue<Numbers, int>();
    private static Mutex mut = new Mutex();
    private static StreamWriter writer = new StreamWriter("C:\\Users\\User\\Desktop\\ЭВМ\\Lab3\\server\\output.txt");
    private static int count = 0;

    private static async Task Main()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token = source.Token;
        Console.WriteLine("Введите Ctrl+C для остановки.");
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            source.Cancel();
        };

        try
        {
            Task senderTask = SenderAsync(token);
            Task receiverTask = ReceiverAsync(token);

            await Task.WhenAll(senderTask, receiverTask);

            Console.WriteLine("Завершение работы сервера.");
        }
        catch (Exception error)
        {
            Console.WriteLine(error.Message);
        }
        finally
        {
            writer.Close();
        }
    }

    static async Task SenderAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            double _number1, _number2;
            int _priority;

            Console.Write("Enter number1: ");
            double.TryParse(Console.ReadLine(), out _number1);

            Console.Write("Enter number2: ");
            double.TryParse(Console.ReadLine(), out _number2);

            Console.Write("Enter priority: ");
            if (!int.TryParse(Console.ReadLine(), out _priority))
                _priority = 0;

            Numbers data = new Numbers
            {
                number1 = _number1,
                number2 = _number2,
                priority = _priority
            };
            mut.WaitOne();
            dataQueue.Enqueue(data, _priority);
            mut.ReleaseMutex();
            await Task.Delay(1);
        }
    }

    static async Task ReceiverAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Numbers cs;
            int pr;

            mut.WaitOne();
            bool flag = dataQueue.TryDequeue(out cs, out pr);
            mut.ReleaseMutex();

            if (flag)
            {
                await ClientProcess(cs, token);
            }
            await Task.Delay(1);
        }
    }

    static async Task ClientProcess(Numbers cs, CancellationToken token)
    {
        Process myProcess = null;

        try
        {
            byte[] dataBytes = new byte[Unsafe.SizeOf<Numbers>()];
            Unsafe.As<byte, Numbers>(ref dataBytes[0]) = cs;

            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream($"channel{count}", PipeDirection.InOut))
            {
                Console.Write("Ожидание подключения клиента...");

                myProcess = new Process
                {
                    StartInfo =
                    {
                        FileName = "C:\\Users\\User\\Desktop\\ЭВМ\\Lab3\\client\\bin\\Realease\\net8.0\\publish\\client.exe",
                        Arguments = $"channel{count}",
                        CreateNoWindow = true
                    }
                };

                myProcess.Start();
                await pipeServer.WaitForConnectionAsync();
                Console.WriteLine("Клиент подключен.");

                await pipeServer.WriteAsync(dataBytes, 0, dataBytes.Length);

                byte[] receivedBytes = new byte[Unsafe.SizeOf<Numbers>()];
                if (await pipeServer.ReadAsync(receivedBytes, 0, receivedBytes.Length) == receivedBytes.Length)
                {
                    cs = Unsafe.As<byte, Numbers>(ref receivedBytes[0]);
                }

                Console.WriteLine($"Полученные данные: a = {cs.number1}; b = {cs.number2}; priority = {cs.priority}; result = {cs.result}");
                writer.WriteLine($"a = {cs.number1}; b = {cs.number2}; priority = {cs.priority}; result = {cs.result}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            myProcess?.Close();
            count++;
        }
    }
}