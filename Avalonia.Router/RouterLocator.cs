using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Avalonia.Router;

public class RouterLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        return Router.ViewFor(param) as Control;
    }

    public bool Match(object? data)
    {
        return Router.ViewFor(data) is not null;
    }
}
