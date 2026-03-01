using AppUriSingleApplication.ViewModels;
using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using ShadUI;
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

        private static IHost? _host;

        private static ILogger<Program>? _logger;

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            _host = BuildHost(args);
            _logger = _host.Services.GetRequiredService<ILogger<Program>>();
            App.SetServiceProvider(_host.Services);

            const string mutexName = "Global\\AppUriSingleApplication";
            _mutex = new Mutex(true, mutexName, out bool isFirstInstance);
            if (!isFirstInstance)
            {
                _logger?.LogInformation($"App is NOT first instance!");
                // Já existe uma instância → repassa args e sai
                SendArgsToRunningInstance(args);
                return;
            }

            _logger?.LogInformation($"App is first instance!");

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

                using var writer = new StreamWriter(client) { AutoFlush = true }; // AutoFlush garante o envio
                _logger?.LogInformation($"Sending args...");

                writer.WriteLine(args.FirstOrDefault());
                _logger?.LogInformation($"App sent args: {args.FirstOrDefault()}");

                // Opcional: WaitForPipeDrain garante que o servidor leu tudo antes de prosseguir
                _logger?.LogInformation($"WaitForPipeDrain...");
                client.WaitForPipeDrain();
                _logger?.LogInformation($"WaitForPipeDrain!");
                _logger?.LogInformation($"Sent args!");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to send args: {ex.Message}");
            }
        }

        private static IHost BuildHost(string[] args)
        {
            // 1. Cria o HostApplicationBuilder
            var builder = Host.CreateApplicationBuilder(args);

            SetUpSerilog(builder);

            // 2. Registra seus serviços e ViewModels
            builder.Services.AddSingleton<MainWindowViewModel>();
            builder.Services.AddSingleton<ToastManager>();

            // 3. Constrói o Host
            return builder.Build();
        }

        private static void SetUpSerilog(HostApplicationBuilder builder)
        {
            // 1. Preparar o caminho dinâmico
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logDirectory = Path.Combine(appData, "Lebtag Company", "AppUriSingleApplication", "Logs");

            // Garante que a pasta existe (MSIX às vezes precisa desse empurrãozinho)
            if (!Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory);

            var logPath = Path.Combine(logDirectory, "log-.txt");

            // 2. Configuração Híbrida
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration) // Pega níveis e propriedades do JSON
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day) // Adiciona o arquivo via código
                .WriteTo.Console() // Opcional: mantém o console para debug
                .CreateLogger();

            // 3. Registrar no Pipeline do .NET
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(Log.Logger);
        }
    }
}
