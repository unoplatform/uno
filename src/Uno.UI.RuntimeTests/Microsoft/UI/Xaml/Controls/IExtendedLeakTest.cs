using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	internal interface IExtendedLeakTest
	{
		Task WaitForTestToComplete();
	}
}
