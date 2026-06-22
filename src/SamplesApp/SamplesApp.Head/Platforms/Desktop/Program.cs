#nullable enable
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Hosting;

namespace SamplesApp.Head;

// Minimal app used only to prove the Uno.Sdk x Directory.Build layering and Win32 boot.
// Replaced by the real SamplesApp.App once shared content is wired (P1 Task 1.2).
internal sealed class SpikeApp : Application
{
	private Window? _window;

	protected internal override void OnLaunched(LaunchActivatedEventArgs args)
	{
		_window = new Window
		{
			Content = new TextBlock { Text = "Uno Platform SamplesApp head — Uno.Sdk spike" }
		};
		_window.Activate();
	}
}

internal static class Program
{
	[System.STAThread]
	public static void Main(string[] args)
	{
		var host = UnoPlatformHostBuilder.Create()
			.App(() => new SpikeApp())
			.UseWin32()
			.Build();

		host.Run();
	}
}
