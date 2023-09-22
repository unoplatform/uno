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
			try
			{
				timeout ??= TimeSpan.FromSeconds(3);

				if (element is FrameworkElement fe)
				{
					typeof(UIElementExtensions).Log().LogWarning($"$$$$$$$$$$$$$$$$ Wait for loaded - {expectedText}");
					await UnitTestsUIContentHelper.WaitForLoaded(fe);
					typeof(UIElementExtensions).Log().LogWarning($"$$$$$$$$$$$$$$$$ Wait for loaded completed - {expectedText}");


					var sw = Stopwatch.StartNew();

					TextBlock? firstText = null;

					while (sw.Elapsed < timeout)
					{
						firstText = element
							.EnumerateDescendants()
							.OfType<TextBlock>()
							.Skip(index)
							.FirstOrDefault();

						if (firstText?.Text == expectedText)
						{
							typeof(UIElementExtensions).Log().LogWarning($"$$$$$$$$$$$$$$$$ Text matches - {expectedText}");
							break;
						}

						await Task.Delay(100);
					}

					typeof(UIElementExtensions).Log().LogWarning($"$$$$$$$$$$$$$$$$ After wait - Null: {firstText is null} Match: {expectedText == firstText?.Text} - {expectedText}");
					Assert.IsNotNull(firstText);

					Assert.AreEqual(expectedText, firstText?.Text);
				}
				typeof(UIElementExtensions).Log().LogWarning($"$$$$$$$$$$$$$$$$ End-Try");
			}
			catch (Exception ex)
			{
				typeof(UIElementExtensions).Log().LogWarning($"$$$$$$$$$$$$$$$$ Catch - {ex.Message}");
				throw;
			}
			finally
			{
				typeof(UIElementExtensions).Log().LogWarning($"$$$$$$$$$$$$$$$$ Finally");
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
