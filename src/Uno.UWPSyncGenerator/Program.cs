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
				Console.WriteLine("*** WARNING: Close all editor files in visual studio, otherwise VS will freeze for a few minutes ****");
				Console.WriteLine("Press enter to continue...");
				Console.ReadLine();

				new SyncGenerator().Build(@"..\..\..\..\Uno.Foundation", "Uno.Foundation", "Windows.Foundation.FoundationContract");
				new SyncGenerator().Build(@"..\..\..\..\Uno.UWP", "Uno", "Windows.Foundation.UniversalApiContract");
				new SyncGenerator().Build(@"..\..\..\..\Uno.UWP", "Uno", "Windows.Phone.PhoneContract");
				new SyncGenerator().Build(@"..\..\..\..\Uno.UWP", "Uno", "Windows.ApplicationModel.Calls.CallsPhoneContract");
				new SyncGenerator().Build(@"..\..\..\..\Uno.UI", "Uno.UI", "Windows.Foundation.UniversalApiContract");
			}

			if (mode == DocMode || mode == AllMode)
			{
				new DocGenerator().Build(@"..\..\..\..\Uno.UI", "Uno.UI", "Windows.Foundation.UniversalApiContract");
			}
		}
	}
}
