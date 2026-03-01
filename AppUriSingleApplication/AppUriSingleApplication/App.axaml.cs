using AppUriSingleApplication.ViewModels;
using AppUriSingleApplication.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShadUI;
using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace AppUriSingleApplication
{
    public partial class App : Application
    {
        public static IServiceProvider? Services { get; private set; }

        public static void SetServiceProvider(IServiceProvider provider) => Services = provider;

        public App()
        {
            _logger = Services!.GetRequiredService<ILogger<App>>();
        }

        private ILogger<App>? _logger;

        public override void Initialize()
        {
            _logger?.LogInformation($"Initialize...");
            AvaloniaXamlLoader.Load(this);
            _logger?.LogInformation($"Initialized!");
        }

        public override void OnFrameworkInitializationCompleted()
        {
            _logger?.LogInformation($"OnFrameworkInitializationCompleted...");

            StartPipeListener();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Services!.GetRequiredService<MainWindowViewModel>(),
                };
            }

            base.OnFrameworkInitializationCompleted();

            _logger?.LogInformation($"OnFrameworkInitializationCompleted!");
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }

        private void HandleUri(string uriString)
        {
            _logger?.LogInformation("Args received from other instance!");

            Dispatcher.UIThread.Post(() =>
            {
                _logger?.LogInformation("In Main UI's thread!");

                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var window = desktop.MainWindow;
                    window?.Show();
                    window?.Activate();
                }

                if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
                {
                    _logger?.LogInformation($"Failed to parse uri! {uriString}");
                    return;
                }
                    

                if (uri.Scheme != "app-uri-single-application")
                {
                    _logger?.LogInformation($"{uri.Scheme} != app-uri-single-application");
                    return;
                }
                    

                if (uri.Host == "callback")
                {
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    string? token = query["token"];

                    _logger?.LogInformation($"Notify from with token: {token?.ToString() ?? "Token came empty!"}");
                    Services!.GetRequiredService<MainWindowViewModel>().Token = token?.ToString() ?? "Token came empty!";
                }
                else
                {
                    _logger?.LogInformation($"{uri.AbsolutePath} != /callback");
                }
            });
        }

        private void StartPipeListener()
        {
            _logger?.LogInformation($"StartPipeListener...");

            Task.Run(async () =>
            {
                _logger?.LogInformation($"Task.Run ...");

                while (true)
                {
                    // Criamos o server. O parâmetro 'maxNumberOfServerInstances: 1' é o padrão.
                    using var server = new NamedPipeServerStream("AppUriSingleApplication", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                    _logger?.LogInformation($"Listening ...");

                    while (true)
                    {
                        try
                        {
                            _logger?.LogInformation($"WaitForConnectionAsync...");
                            await server.WaitForConnectionAsync();
                            _logger?.LogInformation($"WaitForConnectionAsync!");

                            using var reader = new StreamReader(server);

                            _logger?.LogInformation($"ReadLineAsync...");
                            var message = await reader.ReadLineAsync();
                            _logger?.LogInformation($"ReadLineAsync!");

                            _logger?.LogInformation($"Message received: {message ?? "Empty!"}");

                            if (!string.IsNullOrWhiteSpace(message))
                            {
                                // Importante: Processe a URI sem bloquear o loop do Pipe se for algo pesado
                                _ = Task.Run(() => HandleUri(message));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError($"Pipe Server Error: {ex.Message}");
                            await Task.Delay(1000); // Evita loop infinito de erro se algo quebrar
                            break;
                        }
                    }
                }

                _logger?.LogInformation($"Task.Run!");
            });

            _logger?.LogInformation($"StartPipeListener!");
        }
    }
}