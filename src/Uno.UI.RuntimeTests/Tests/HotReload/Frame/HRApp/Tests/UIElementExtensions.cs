using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI;
using Uno.UI.Extensions;
using System.Diagnostics;
using DirectUI;
using Windows.Devices.AllJoyn;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;
using Uno.Extensions;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame
{
	public static class UIElementExtensions
	{
		public static async Task ValidateFirstTextBlockOnCurrentPageText(this Windows.UI.Xaml.UIElement element, string expectedText)
			=> await element.ValidateTextBlockOnCurrentPageText(expectedText);

		public static async Task ValidateTextBlockOnCurrentPageText(this Windows.UI.Xaml.UIElement element, string expectedText, int index = 0, TimeSpan? timeout = null)
		{
			timeout ??= TimeSpan.FromSeconds(3);

			if (element is FrameworkElement fe)
			{
				await UnitTestsUIContentHelper.WaitForLoaded(fe);

				var sw = Stopwatch.StartNew();

				TextBlock? firstText = null;

				firstText = element
					.EnumerateDescendants()
					.OfType<TextBlock>()
					.Skip(index)
					.FirstOrDefault();

				Assert.IsNotNull(firstText);

				Assert.AreEqual(expectedText, firstText?.Text);
			}
		}

		public static async Task ValidateElementOnCurrentPageText<TElement>(this Windows.UI.Xaml.UIElement element, Func<TElement, Task> validation, int index = 0)
			where TElement : FrameworkElement
		{
			if (element is FrameworkElement fe)
			{
				await UnitTestsUIContentHelper.WaitForLoaded(fe);

				var firstText = element
					.EnumerateDescendants()
					.OfType<TElement>()
					.Skip(index)
					.FirstOrDefault();

				Assert.IsNotNull(firstText);
				await validation(firstText);
			}
		}
	}
}
