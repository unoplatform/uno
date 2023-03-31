using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.Xaml.Media;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Core;

[TestClass]
[RequiresFullWindow]
public class Given_RootVisual
{
#if HAS_UNO
	[TestMethod]
	public async Task When_Theme_Changes()
	{
		var rootVisual = WinUICoreServices.Instance.MainRootVisual;
		if (rootVisual is null)
		{
			// Ignore on Uno Islands
			return;
		}

		Assert.AreEqual(Colors.White, (rootVisual.Background as SolidColorBrush).Color);

		using (ThemeHelper.UseDarkTheme())
		{
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(Colors.Black, (rootVisual.Background as SolidColorBrush).Color);
		}

		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(Colors.White, (rootVisual.Background as SolidColorBrush).Color);
	}
#endif
}
