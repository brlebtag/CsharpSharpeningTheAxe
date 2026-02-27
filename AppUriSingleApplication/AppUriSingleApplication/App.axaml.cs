using AppUriSingleApplication.ViewModels;
using AppUriSingleApplication.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using ShadUI;
using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;

namespace AppUriSingleApplication
{
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<ToastManager>();
            _serviceProvider = services.BuildServiceProvider();
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            StartPipeListener();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>(),
                };
            }

            base.OnFrameworkInitializationCompleted();
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
            Dispatcher.UIThread.Post(() =>
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var window = desktop.MainWindow;
                    window?.Show();
                    window?.Activate();
                }

                if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
                    return;

                if (uri.Scheme != "app-uri-single-application")
                    return;

                if (uri.AbsolutePath == "/callback")
                {
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    string? token = query["token"];
                    _serviceProvider.GetRequiredService<MainWindowViewModel>().Token = token?.ToString() ?? "Token came empty!";
                }
            });
        }

        private void StartPipeListener()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    using var server = new NamedPipeServerStream("AppUriSingleApplication", PipeDirection.In);

                    await server.WaitForConnectionAsync();

                    using var reader = new StreamReader(server);
                    var message = await reader.ReadLineAsync();

                    if (!string.IsNullOrWhiteSpace(message)) HandleUri(message);
                }
            });
        }
    }
}