using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_TeachingTip
{
	[TestMethod]
	[DataRow(true, "open:True/True;enabled:True/True")]
	[DataRow(false, "open:True/True;disabled:True/True;enabled:True/True")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26613")]
	public async Task When_Container_IsEnabled_Changes_Then_Content_Follows(bool openWhileDisabled, string expectedStates)
	{
		var target = new TextBlock { Text = "Target" };
		var contentButton = new Button { Content = "Tip content" };
		var tip = new TeachingTip
		{
			Title = "Title",
			Target = target,
			Content = contentButton,
		};
		var container = new ContentControl
		{
			IsEnabled = !openWhileDisabled,
			Content = new StackPanel { Children = { target, tip } },
		};

		try
		{
			TestServices.WindowHelper.WindowContent = container;
			await TestServices.WindowHelper.WaitForLoaded(container);

			tip.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(contentButton);
			var closeButton = FindTipCloseButton();

			// Records "<step>:<content IsEnabled>/<close button IsEnabled>" for each step.
			var states = $"open:{contentButton.IsEnabled}/{closeButton.IsEnabled}";

			if (!openWhileDisabled)
			{
				container.IsEnabled = false;
				await TestServices.WindowHelper.WaitForIdle();
				states += $";disabled:{contentButton.IsEnabled}/{closeButton.IsEnabled}";
			}

			container.IsEnabled = true;
			await TestServices.WindowHelper.WaitForIdle();
			states += $";enabled:{contentButton.IsEnabled}/{closeButton.IsEnabled}";

			Assert.AreEqual(expectedStates, states);

			new ButtonAutomationPeer(closeButton).Invoke();
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsFalse(tip.IsOpen, "The tip should close through its own close button.");
		}
		finally
		{
			tip.IsOpen = false;
#if HAS_UNO
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	private static Button FindTipCloseButton()
	{
		var closeButton = VisualTreeHelper
			.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot)
			.Select(popup => FindByName(popup.Child, "AlternateCloseButton"))
			.OfType<Button>()
			.FirstOrDefault();

		Assert.IsNotNull(closeButton, "The tip's close button should be in an open popup.");
		return closeButton;
	}

	private static FrameworkElement FindByName(DependencyObject element, string name)
	{
		if (element is FrameworkElement { Name: var elementName } fe && elementName == name)
		{
			return fe;
		}

		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
		{
			if (FindByName(VisualTreeHelper.GetChild(element, i), name) is { } found)
			{
				return found;
			}
		}

		return null;
	}
}
