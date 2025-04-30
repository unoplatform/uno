#nullable enable

using System;
using Microsoft.UI.Xaml;

namespace Uno.UI.Hosting;

public interface IUnoPlatformHostBuilder
{
	internal Func<Application>? AppBuilder { get; set; }

	internal Action? AfterInitAction { get; set; }

	internal void AddHostBuilder(Func<IPlatformHostBuilder> hostBuilder);

	public UnoPlatformHost Build();
}
