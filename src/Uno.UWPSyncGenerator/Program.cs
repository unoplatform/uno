using System;
using System.Threading.Tasks;

namespace Uno.UWPSyncGenerator
{
	class Program
	{
		const string SyncMode = "sync";
		const string DocMode = "doc";
		const string AllMode = "all";

		static async Task Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("No mode selected. Supported modes: doc, sync & all.");
				return;
			}

			var mode = args[0].ToLowerInvariant();

			if (mode == SyncMode || mode == AllMode)
			{
				await new SyncGenerator().Build("Uno.Foundation", "Windows.Foundation.FoundationContract");
				await new SyncGenerator().Build("Uno", "Windows.Foundation.UniversalApiContract");
				await new SyncGenerator().Build("Uno", "Windows.Phone.PhoneContract");
				await new SyncGenerator().Build("Uno", "Windows.Networking.Connectivity.WwanContract");
				await new SyncGenerator().Build("Uno", "Windows.ApplicationModel.Calls.CallsPhoneContract");
				await new SyncGenerator().Build("Uno", "Windows.Services.Store.StoreContract");

				// When adding support for a new WinRT contract here, ensure to add it to the list of supported contracts in ApiInformation.shared.cs

#if HAS_UNO_WINUI
				await new SyncGenerator().Build("Uno.Foundation", "Microsoft.Foundation");

				await new SyncGenerator().Build("Uno.UI", "Microsoft.Foundation");

				await new SyncGenerator().Build("Uno.UI", "Microsoft.Graphics");
				await new SyncGenerator().Build("Uno.UI.Dispatching", "Microsoft.UI.Dispatching");
				await new SyncGenerator().Build("Uno.UI.Composition", "Microsoft.UI.Composition");
				await new SyncGenerator().Build("Uno.UI.Dispatching", "Microsoft.UI");
				await new SyncGenerator().Build("Uno.UI.Composition", "Microsoft.UI");

				await new SyncGenerator().Build("Uno.UI", "Microsoft.UI.Text");
				await new SyncGenerator().Build("Uno.UI", "Microsoft.ApplicationModel.Resources");
				await new SyncGenerator().Build("Uno.UI", "Microsoft.Web.WebView2.Core");

				await new SyncGenerator().Build("Uno.UI", "Microsoft.UI.Input");
				await new SyncGenerator().Build("Uno.UI", "Microsoft.UI");
				await new SyncGenerator().Build("Uno.UI", "Microsoft.UI.Windowing");

				await new SyncGenerator().Build("Uno.UI", "Microsoft.UI.Xaml");

#else
				await new SyncGenerator().Build("Uno.UI.Composition", "Windows.Foundation.UniversalApiContract");
				await new SyncGenerator().Build("Uno.UI.Dispatching", "Windows.Foundation.UniversalApiContract");
				await new SyncGenerator().Build("Uno.UI", "Windows.Foundation.UniversalApiContract");
				await new SyncGenerator().Build("Uno.UI", "Windows.UI.Xaml.Hosting.HostingContract");
				await new SyncGenerator().Build("Uno.UI", "Microsoft.UI.Xaml");
				await new SyncGenerator().Build("Uno.UI", "Microsoft.Web.WebView2.Core");
#endif
			}

			if (mode == DocMode || mode == AllMode)
			{
#if HAS_UNO_WINUI
				await new DocGenerator().Build("Uno.UI", "Microsoft.ApplicationModel.Resources");
				await new DocGenerator().Build("Uno.UI", "Microsoft.Web.WebView2.Core");

				await new DocGenerator().Build("Uno.UI.Dispatching", "Microsoft.UI.Dispatching");
				await new DocGenerator().Build("Uno.UI.Composition", "Microsoft.UI.Composition");

				await new DocGenerator().Build("Uno.UI", "Microsoft.Foundation");
				await new DocGenerator().Build("Uno.UI", "Microsoft.UI.Composition");
				await new DocGenerator().Build("Uno.UI", "Microsoft.UI.Dispatching");
				await new DocGenerator().Build("Uno.UI", "Microsoft.UI.Input");
				await new DocGenerator().Build("Uno.UI", "Microsoft.Graphics");
				await new DocGenerator().Build("Uno.UI", "Microsoft.UI.Windowing");
				await new DocGenerator().Build("Uno.UI", "Microsoft.UI");

				await new DocGenerator().Build("Uno.UI", "Microsoft.UI.Xaml");
#else
				await new DocGenerator().Build("Uno.UI", "Windows.Foundation.UniversalApiContract");
#endif
			}
		}
	}
}
