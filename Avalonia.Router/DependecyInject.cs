using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Router;

public static class DependecyInject
{
    public static ServiceCollection RegisterDI(this ServiceCollection service)
    {
        service.AddSingleton<IRouting, Routing>();
        return service;
    }
}
