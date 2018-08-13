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
		static void Main(string[] args)
		{
			Console.WriteLine("*** WARNING: Close all editor files in visual studio, otherwise VS will freeze for a few minutes ****");
			Console.WriteLine("Press any key to continue...");
			Console.ReadLine();

			new SyncGenerator().Build(@"..\..\..\..\Uno.Foundation", "Uno.Foundation", "Windows.Foundation.FoundationContract");
			new SyncGenerator().Build(@"..\..\..\..\Uno.UWP", "Uno", "Windows.Foundation.UniversalApiContract");
			new SyncGenerator().Build(@"..\..\..\..\Uno.UWP", "Uno", "Windows.Phone.PhoneContract");
			new SyncGenerator().Build(@"..\..\..\..\Uno.UI", "Uno.UI", "Windows.Foundation.UniversalApiContract");
		}
	}
}
