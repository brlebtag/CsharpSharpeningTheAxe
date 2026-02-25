using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Router;

public class Route : ContentControl
{
    public static readonly StyledProperty<string> PathProperty =
        AvaloniaProperty.Register<Route, string>(nameof(Path));

    public string Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Router.NavigationChanged += OnNavigationChanged;
        UpdateRoute(Router.CurrentPath);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Router.NavigationChanged -= OnNavigationChanged;
    }

    private void OnNavigationChanged(string newPath) => UpdateRoute(newPath);

    private void UpdateRoute(string currentPath)
    {
        if (currentPath.IndexOf(Path) != -1)
        {
            var viewModel = Router.ViewModelFor(currentPath);

            // Se o seu filho for um TransitioningContentControl, 
            // setamos o Content dele para o ViewModel.
            if (Content is ContentControl child)
            {
                child.Content = viewModel;
            }
        }
    }
}
