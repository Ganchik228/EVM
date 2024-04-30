using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server
{
    public struct Tt
    {
        public int x;
        public bool y;
        public override string ToString()
        {
            return $"Введенные данные = {x}, Изменения = {y}";
        }
    }

    internal class Program
    {
        private static CancellationTokenSource cancellationTokenSource = new();
        private static CancellationToken cancellationToken = cancellationTokenSource.Token;
        private static PriorityQueue<Tt, int> adQueue = new();
        private static Mutex mutex = new();

        private static async Task ClientTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Console.WriteLine("Значение: ");
                var value = Console.ReadLine();

                Console.WriteLine("Приоритет: ");
                var priority = Console.ReadLine();

                try
                {
                    var data = new Tt { x = Convert.ToInt32(value), y = false };
                    mutex.WaitOne();
                    adQueue.Enqueue(data, Convert.ToInt32(priority));
                    mutex.ReleaseMutex();
                }
                catch (FormatException)
                {
                    await Console.Error.WriteLineAsync("Введено не число!");
                }
            }
        }
        private static async Task ServerTask(NamedPipeServerStream pipeStream, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (adQueue.Count < 1)
                    {
                        await Task.Delay(1, token);
                        continue;
                    }
                    mutex.WaitOne();
                    var data = adQueue.Dequeue();
                    mutex.ReleaseMutex();
                    byte[] buffer = new byte[Unsafe.SizeOf<Tt>()];
                    MemoryMarshal.Write(buffer, in data);
                    await pipeStream.WriteAsync(buffer);
                    byte[] bytesRead = new byte[Unsafe.SizeOf<Tt>()];
                    int numBytesRead = await pipeStream.ReadAsync(bytesRead);
                    if (numBytesRead > 0)
                    {
                        await using var fileStream = new FileStream("output.txt", FileMode.Append, FileAccess.Write);
                        await using var streamWriter = new StreamWriter(fileStream);
                        await streamWriter.WriteLineAsync($"{data.x}, {data.y}");
                    }
                }
                catch (IOException)
                {
                    await Console.Error.WriteLineAsync("Проблема");
                }
            }
        }

        static async Task Main()
        {
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
            };

            Console.WriteLine("Ожидание клиента...");
            var pipeStream = new NamedPipeServerStream("p", PipeDirection.InOut);
            await pipeStream.WaitForConnectionAsync();
            Console.WriteLine("Клиент подключен");
            var server = ServerTask(pipeStream, cancellationToken);
            var client = ClientTask(cancellationToken);
            await Task.WhenAll(server, client);
        }
    }
}