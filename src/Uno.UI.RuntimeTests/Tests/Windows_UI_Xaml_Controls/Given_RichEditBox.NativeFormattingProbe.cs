#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Native_AllCaps_Preserves_Indices_And_Uses_Uppercase_Advance()
		{
			var transformed = new RichEditBox { Width = 240, TextWrapping = TextWrapping.NoWrap };
			var uppercase = new RichEditBox { Width = 240, TextWrapping = TextWrapping.NoWrap };
			var panel = new StackPanel();
			panel.Children.Add(transformed);
			panel.Children.Add(uppercase);
			try
			{
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);

				transformed.Document.SetText(TextSetOptions.None, "abc");
				transformed.Document.GetRange(0, 3).CharacterFormat.AllCaps = FormatEffect.On;
				uppercase.Document.SetText(TextSetOptions.None, "ABC");
				await WindowHelper.WaitForIdle();

				transformed.Document.GetRange(3, 3).GetRect(PointOptions.ClientCoordinates, out var transformedEnd, out _);
				uppercase.Document.GetRange(3, 3).GetRect(PointOptions.ClientCoordinates, out var uppercaseEnd, out _);
				Assert.AreEqual(uppercaseEnd.X, transformedEnd.X, 1);
				GetTextWithoutFinalEop(transformed.Document, out var source);
				Assert.AreEqual("abc", source);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

	}
}
