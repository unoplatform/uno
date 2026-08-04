#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// Host for an Uno Skia Android application.
/// </summary>
/// <remarks>
/// Unlike desktop hosts, this host does not own a run loop: Android's loop is
/// <c>Looper.MainLooper</c>, and <see cref="UnoPlatformHost.Run"/> is invoked from
/// <c>Activity.OnStart</c> on that very looper. Every stage must therefore complete
/// synchronously — introducing a real <c>await</c> in <see cref="Initialize"/>,
/// <c>InitializeAsync</c> or <see cref="RunLoop"/> would make <see cref="UnoPlatformHost.Run"/>
/// throw, and app authors cannot switch to <c>RunAsync</c> because the call site is
/// owned by <see cref="NativeApplication"/>.
/// </remarks>
internal sealed class AndroidHost : SkiaHost, ISkiaApplicationHost
{
	private readonly Func<Application> _appBuilder;

	public AndroidHost(Func<Application> appBuilder)
	{
		_appBuilder = appBuilder ?? throw new ArgumentNullException(nameof(appBuilder));
	}

	protected override void Initialize() => ExtensionsRegistrar.Register();

	protected override Task RunLoop()
	{
		void CreateApp(ApplicationInitializationCallbackParams _)
		{
			var app = _appBuilder();
			app.Host = this;
		}

		Application.Start(CreateApp);

		return Task.CompletedTask;
	}
}
