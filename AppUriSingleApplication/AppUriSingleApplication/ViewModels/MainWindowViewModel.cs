using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShadUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AppUriSingleApplication.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ToastManager _toastManager;
        private ILogger<MainWindowViewModel> _logger;

        public MainWindowViewModel(IServiceProvider provider)
        {
            _toastManager = provider.GetRequiredService<ToastManager>();
            _logger = provider.GetRequiredService<ILogger<MainWindowViewModel>>();
        }

        [ObservableProperty]
        private string token = "No Token Received!";


        [RelayCommand]
        private async Task GetTokenAsync()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://localhost:5003/",
                UseShellExecute = true
            });

            ToastManager.CreateToast("A request for a token was made!")
                .WithContent($"{DateTime.Now:dddd, MMMM d 'at' h:mm tt}")
                .DismissOnClick()
                .ShowInfo();
        }

        [RelayCommand]
        private async Task OpenLogFolderAsync()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logDirectory = Path.Combine(appData, "Lebtag Company", "AppUriSingleApplication", "Logs");

            Process.Start(new ProcessStartInfo
            {
                FileName = logDirectory,
                UseShellExecute = true
            });
        }

        partial void OnTokenChanged(string value)
        {
            _logger.LogInformation($"Token received as: {value}");

            ToastManager.CreateToast("Token Received!")
                .WithContent($"{DateTime.Now:dddd, MMMM d 'at' h:mm tt}")
                .DismissOnClick()
                .ShowSuccess();
        }
    }
}
