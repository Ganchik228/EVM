using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Client
{
    public struct Tt
    {
        public int x;
        public bool y;
    }

    class Program
    {
        static void Main()
        {
            Console.WriteLine("Соединение с сервером...");
            var pipeStream = new NamedPipeClientStream(".", "p", PipeDirection.InOut);
            pipeStream.Connect();
            Console.WriteLine("Соединение установлено");
            Console.WriteLine("Ожидание данных...");
            while (true)
            {
                byte[] buffer = new byte[Unsafe.SizeOf<Tt>()];
                pipeStream.Read(buffer);
                var data = MemoryMarshal.Read<Tt>(buffer);
                Console.WriteLine($"Получено: {data.x}, {data.y}");
                data.y = true;
                Console.WriteLine($"Отправлено: {data.x}, {data.y}");
                byte[] resBuffer = new byte[Unsafe.SizeOf<Tt>()];
                MemoryMarshal.Write(resBuffer, in data);
                pipeStream.Write(resBuffer);
            }
        }
    }
}
