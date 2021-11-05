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

#if HAS_UNO_WINUI
				new SyncGenerator().Build(@"..\..\..\Uno.Foundation", "Uno.Foundation", "Microsoft.Foundation");

				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Xaml");
				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Text");
				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.ApplicationModel.Resources");
				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.Web.WebView2.Core");

				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.Foundation");
				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Composition");
				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Dispatching");
				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Input");
				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.Graphics");
				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Windowing");
				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI");
#else
				new SyncGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Windows.Foundation.UniversalApiContract");
#endif
			}

			if (mode == DocMode || mode == AllMode)
			{
#if HAS_UNO_WINUI
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Xaml");
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.ApplicationModel.Resources");
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.Web.WebView2.Core");

				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.Foundation");
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Composition");
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Dispatching");
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Input");
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.Graphics");
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI.Windowing");
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Microsoft.UI");
#else
				new DocGenerator().Build(@"..\..\..\Uno.UI", "Uno.UI", "Windows.Foundation.UniversalApiContract");
#endif
			}
		}
	}
}
