using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;
using static Private.Infrastructure.TestServices;

namespace Windows.UI.Xaml.Tests.Common;

internal static class SelectorHelper
{
	public static async Task VerifySelectedIndex(Selector selector, int expected)
	{
		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Selected Index = %d", selector.SelectedIndex);
			VERIFY_ARE_EQUAL(selector.SelectedIndex, expected);
		});
	}
}
