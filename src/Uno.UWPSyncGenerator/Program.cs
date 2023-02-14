using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Uno.UWPSyncGenerator
{
	class Program
	{
		const string SyncMode = "sync";
		const string DocMode = "doc";
		const string AllMode = "all";

		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("No mode selected. Supported modes: doc, sync & all.");
				return;
			}

			var mode = args[0].ToLowerInvariant();

			if (mode == SyncMode || mode == AllMode)
			{
				new SyncGenerator().Build(@"..\..\..\Uno.Foundation", "Uno.Foundation", "Windows.Foundation.FoundationContract");
				new SyncGenerator().Build(@"..\..\..\Uno.UWP", "Uno", "Windows.Foundation.UniversalApiContract");
				new SyncGenerator().Build(@"..\..\..\Uno.UWP", "Uno", "Windows.Phone.PhoneContract");
				new SyncGenerator().Build(@"..\..\..\Uno.UWP", "Uno", "Windows.Networking.Connectivity.WwanContract");
				new SyncGenerator().Build(@"..\..\..\Uno.UWP", "Uno", "Windows.ApplicationModel.Calls.CallsPhoneContract");

				// When adding support for a new WinRT contract here, ensure to add it to the list of origins in Generator.cs
				// and to the list of supported contracts in ApiInformation.shared.cs

#if HAS_UNO_WINUI
				new SyncGenerator().Build(@"..\..\..\Uno.Foundation", "Uno.Foundation", "Microsoft.Foundation");

				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.Foundation");

				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.Graphics");
				new SyncGenerator().Build(@"..\..\..\Uno.UI.Dispatching", "Uno.UI.Dispatching", "Microsoft.UI.Dispatching");
				new SyncGenerator().Build(@"..\..\..\Uno.UI.Composition", "Uno.UI.Composition", "Microsoft.UI.Composition");
				new SyncGenerator().Build(@"..\..\..\Uno.UI.Dispatching", "Uno.UI.Dispatching", "Microsoft.UI");
				new SyncGenerator().Build(@"..\..\..\Uno.UI.Composition", "Uno.UI.Composition", "Microsoft.UI");

				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Text");
				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.ApplicationModel.Resources");
				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.Web.WebView2.Core");

				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Input");
				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI");
				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Windowing");

				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Xaml");

#else
				new SyncGenerator().Build(@"..\..\..\Uno.UI.Composition", "Uno.UI.Composition", "Windows.Foundation.UniversalApiContract");
				new SyncGenerator().Build(@"..\..\..\Uno.UI.Dispatching", "Uno.UI.Dispatching", "Windows.Foundation.UniversalApiContract");
				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Windows.Foundation.UniversalApiContract");
				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Xaml.Hosting.HostingContract");
#endif
			}

			if (mode == DocMode || mode == AllMode)
			{
#if HAS_UNO_WINUI
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.ApplicationModel.Resources");
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.Web.WebView2.Core");

				new DocGenerator().Build(@"..\..\..\Uno.UI.Dispatching", "Uno.UI.Dispatching", "Microsoft.UI.Dispatching");
				new DocGenerator().Build(@"..\..\..\Uno.UI.Composition", "Uno.UI.Composition", "Microsoft.UI.Composition");

				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.Foundation");
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Composition");
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Dispatching");
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Input");
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.Graphics");
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Windowing");
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI");

				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Xaml");
#else
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Windows.Foundation.UniversalApiContract");
#endif
			}
		}
	}
}
