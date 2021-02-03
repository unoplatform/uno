using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Tests.ViewLibrary
{
	// This type should only be created by the Test_Dictionary_Initializer dictionary
	public class TestInitializer
	{
		public static bool IsInitialized { get; private set; }

		public TestInitializer()
		{
			IsInitialized = true;
		}
	}
}
