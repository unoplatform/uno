using System.Diagnostics;
using System.IO;
using System.Text;
using Uno.UI.RemoteControl;
using Uno.UI.RemoteControl.HotReload;

namespace UnoApp50
{
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			Console.WriteLine($"HRApp.MainPage {string.Join("; ", Environment.GetCommandLineArgs())}");

			this.InitializeComponent();

			if (Environment.GetCommandLineArgs().Contains("--uitest"))
			{
				_ = RunUITests();
			}
		}

		private async Task RunUITests()
		{
			Console.WriteLine($"Starting runtime tests {string.Join("; ", Environment.GetCommandLineArgs())}");

			// Uncomment this to attach to the app when started from the runtime tests
			// Note that hotreload is forcibly disabled when a debugger is attached, and will
			// make the runtime tests of this app fail.
			//
			// Debugger.Launch();

			RemoteControlClient.Initialize(typeof(App));

			if (RemoteControlClient.Instance is not null)
			{
				Console.WriteLine($"Initializing remote control");

				await RemoteControlClient.Instance.WaitForConnection();
				await RemoteControlClient.Instance.RegisteredProcessors
					.OfType<ClientHotReloadProcessor>()
					.First()
					.HotReloadWorkspaceLoaded;

				Console.WriteLine($"Initialized remote control");
			}

			await testControl.RunTests(CancellationToken.None, new());

			// get the first command line argument after `--uitest`
			var testResultPath = Environment.GetCommandLineArgs().SkipWhile(a => a != "--uitest").Skip(1).FirstOrDefault();

			if (testResultPath is not null)
			{
				File.WriteAllText(testResultPath, testControl.NUnitTestResultsDocument, Encoding.Unicode);
			}

			Application.Current.Exit();
		}
	}
}
