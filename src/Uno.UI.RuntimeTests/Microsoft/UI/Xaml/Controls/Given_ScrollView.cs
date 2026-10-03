using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ScrollView
{
	[TestMethod]
	public async Task When_XYFocusNavigation_Then_BringIntoView_Operation_Expires_Without_Exception()
	{
		var first = new Button { Content = "1", Height = 150 };
		var second = new Button { Content = "2", Height = 150 };
		var scrollView = new ScrollView
		{
			Height = 200,
			XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled,
			Content = new StackPanel { Children = { first, second } },
		};

		// The dispatcher swallows exceptions thrown by CompositionTarget.Rendering handlers, so watch first-chance ones.
		InvalidOperationException collectionModified = null;
		void OnFirstChanceException(object sender, FirstChanceExceptionEventArgs e)
		{
			if (e.Exception is InvalidOperationException { Message: var message } ex && message.StartsWith("Collection was modified", StringComparison.Ordinal))
			{
				collectionModified ??= ex;
			}
		}

		AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
		try
		{
			await UITestHelper.Load(scrollView);
			first.Focus(FocusState.Keyboard);
			await WindowHelper.WaitForIdle();

			await KeyboardHelper.Down(first);

			// The queued bring-into-view operation expires after a few rendering ticks.
			for (var i = 0; i < 10; i++)
			{
				await WindowHelper.WaitForIdle();
				await Task.Delay(20);
			}

			Assert.AreEqual(second, FocusManager.GetFocusedElement(scrollView.XamlRoot));
			Assert.IsNull(collectionModified, collectionModified?.ToString());
		}
		finally
		{
			AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
		}
	}
}
