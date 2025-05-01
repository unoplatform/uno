#nullable enable

using System;
using Microsoft.UI.Xaml;

namespace Uno.UI.Hosting;

public interface IUnoPlatformHostBuilder
{
	internal Func<Application>? AppBuilder { get; set; }

	internal void SetAppType<TApplication>()
		where TApplication : Microsoft.UI.Xaml.Application;

	internal Action? AfterInitAction { get; set; }

	internal void AddHostBuilder(Func<IPlatformHostBuilder> hostBuilder);

	public UnoPlatformHost Build();
}
