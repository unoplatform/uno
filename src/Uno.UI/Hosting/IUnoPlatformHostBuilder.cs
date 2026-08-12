#nullable enable

using System;
using Microsoft.UI.Xaml;

namespace Uno.UI.Hosting;

public interface IUnoPlatformHostBuilder
{
	internal Func<Application>? AppBuilder { get; set; }

	internal void SetAppType(Type appType);

	internal Action? AfterInitAction { get; set; }

	internal void AddHostBuilder(Func<IPlatformHostBuilder> hostBuilder);

	/// <summary>
	/// Captures a drawing-backend registration (render/drawing backend, font provider, image decoder) to be
	/// applied during <see cref="Build"/>, before the host runs. The builder is the single app-side entry point
	/// for these registrations; the backend-neutral extension methods (GraphicsBackend/FontProvider/ImageDecoder)
	/// funnel through here so ordering is deterministic and centralized.
	/// </summary>
	internal void AddDrawingRegistration(Action apply);

	public UnoPlatformHost Build();
}
