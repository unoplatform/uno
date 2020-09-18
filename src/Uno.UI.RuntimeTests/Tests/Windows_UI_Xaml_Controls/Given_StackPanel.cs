using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_StackPanel
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_InsertingChildren_Then_ResultIsInRightOrder()
		{
			// This is an illustration of the bug https://github.com/unoplatform/uno/issues/3543

			var pnl = new StackPanel();
			pnl.Children.Add(new Button { Content = "abc" });
			pnl.Children.Insert(0, new TextBlock { Text = "TextBlock" });
			pnl.Children.Insert(0, new TextBox());

			TestServices.WindowHelper.WindowContent = pnl;

			using var _ = new AssertionScope();

			pnl.Children
				.Select(c => c.GetType())
				.Should()
				.Equal(typeof(TextBox), typeof(TextBlock), typeof(Button));

			await TestServices.WindowHelper.WaitForIdle();

#if __WASM__
			// Ensure children are synchronized in the DOM
			var js = $@"
				(function() {{
					var stackPanel = document.getElementById(""{pnl.HtmlId}"");
					var result = """";
					for(const elem of stackPanel.children) {{
						result = result + "";"" + elem.id;
					}}
					return result;
				}})();";
			var expectedIds = ";" + string.Join(";", pnl.Children.Select(c => c.HtmlId));

			var ids = global::Uno.Foundation.WebAssemblyRuntime.InvokeJS(js);

			ids.Should().Be(expectedIds, "Expected from DOM");
#endif
		}
	}
}
