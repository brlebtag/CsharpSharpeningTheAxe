using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Router;

public static class Router
{
    private static IServiceProvider? _serviceProvider;

    public static event Action<string>? NavigationChanged;

    public static string CurrentPath { get; private set; } = "/";

    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static void Register<TViewModel, TView>(string path)
    {

    }

    public static void Go(string path)
    {
        CurrentPath = path;
        NavigationChanged?.Invoke(path);
    }

    public static void Back()
    {
    }

    public static void Forward()
    {
    }

    public static void Push(string path)
    {
    }

    public static void Pop()
    {
    }

    public static object? ViewFor(object? viewModel)
    {
        return null;
    }

    public static object? ViewModelFor(string path)
    {
        return null;
    }
}
