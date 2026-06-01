using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_SplitView
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task Update_OpenPaneLength()
	{
		// This test asserts changes to the OpenPaneLength property are reflected in to the column definition's width.
		// We assume here that the control-template is setup with: SplitView\Grid\@ColumnDefinition[0].Width bound to TemplateSettings.OpenPaneLength
		// Should the template change, this test will need to be updated accordingly or voided.

		var sut = new SplitView()
		{
			OpenPaneLength = 100,
			CompactPaneLength = 50
		};
		await UITestHelper.Load(sut, x => x.IsLoaded);

		var rootGrid = sut.FindFirstDescendant<Grid>() ?? throw new InvalidOperationException("failed to find root grid.");
		var columnDefinition = rootGrid.ColumnDefinitions.ElementAtOrDefault(0) ?? throw new InvalidOperationException("root grid doesnt contains any column definition");

		Assert.AreEqual(sut.OpenPaneLength, columnDefinition.Width.Value, "ColumnDefinition Width should be equal to OpenPaneLength");

		sut.OpenPaneLength = 105;
		await UITestHelper.WaitForIdle();

		Assert.AreEqual(sut.OpenPaneLength, columnDefinition.Width.Value, "ColumnDefinition Width should be equal to OpenPaneLength after update");
	}
}
