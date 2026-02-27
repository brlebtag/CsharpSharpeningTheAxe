using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ShadUI;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AppUriSingleApplication.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ToastManager _toastManager;

        public MainWindowViewModel(IServiceProvider provider)
        {
            _toastManager = provider.GetRequiredService<ToastManager>();
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

        partial void OnTokenChanged(string value)
        {
            ToastManager.CreateToast("Token Received!")
                .WithContent($"{DateTime.Now:dddd, MMMM d 'at' h:mm tt}")
                .DismissOnClick()
                .ShowSuccess();
        }
    }
}
