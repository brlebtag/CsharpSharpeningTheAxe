using Avalonia;
using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;

namespace AppUriSingleApplication
{
    internal sealed class Program
    {
        private static Mutex? _mutex;

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            const string mutexName = "Global\\AppUriSingleApplication";

            _mutex = new Mutex(true, mutexName, out bool isFirstInstance);

            if (!isFirstInstance)
            {
                // Já existe uma instância → repassa args e sai
                SendArgsToRunningInstance(args);
                return;
            }

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        private static void SendArgsToRunningInstance(string[] args)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", "AppUriSingleApplication", PipeDirection.Out);
                client.Connect(500);
                using var writer = new StreamWriter(client);
                writer.WriteLine(args.FirstOrDefault());
            }
            catch
            {
                // Instância não respondeu (pode estar fechando)
            }
        }
    }
}
