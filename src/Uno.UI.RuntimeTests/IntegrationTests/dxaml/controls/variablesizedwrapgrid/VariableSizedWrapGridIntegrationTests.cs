using System.Threading.Tasks;
using Microsoft/* UWP don't rename */.UI.Xaml.Tests.Common;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using System.Collections.Generic;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.IntegrationTests.dxaml.controls.variablesizedwrapgrid;

[TestClass]
[RequiresFullWindow]
public class VariableSizedWrapGridIntegrationTests
{
	private const int s_itemCount = 8;

	[TestMethod]
	public async Task CanWrapItemsHorizontally()
	{
		var panel = await PanelsHelper.AddPanelWithContent<VariableSizedWrapGrid>(await PanelsHelper.CreateDefaultPanelContent(s_itemCount), Orientation.Horizontal);
		var expectedPositions = new List<Point>();

		for (int i = 0; i < s_itemCount; i++)
		{
			expectedPositions.Add(new Point(100.0f * (i % 3), 100.0f * (i / 3)));
		}

		await WindowHelper.WaitForIdle();
		await PanelsHelper.VerifyItemPositions(panel, expectedPositions);
	}

	[TestMethod]
	public async Task CanWrapItemsVertically()
	{
		var panel = await PanelsHelper.AddPanelWithContent<VariableSizedWrapGrid>(await PanelsHelper.CreateDefaultPanelContent(s_itemCount), Orientation.Vertical);
		var expectedPositions = new List<Point>();

		for (int i = 0; i < s_itemCount; i++)
		{
			expectedPositions.Add(new Point(100.0f * (i / 3), 100.0f * (i % 3)));
		}
		await WindowHelper.WaitForIdle();
		await PanelsHelper.VerifyItemPositions(panel, expectedPositions);
	}

	[TestMethod]
	public async Task CanChangeRowAndColumnSpans()
	{
		var panelContent = await PanelsHelper.CreateDefaultPanelContent(s_itemCount);

		await RunOnUIThread(() =>
		{
			VariableSizedWrapGrid.SetColumnSpan(panelContent[0], 2);
			VariableSizedWrapGrid.SetRowSpan(panelContent[2], 2);
		});

		var panel = await PanelsHelper.AddPanelWithContent<VariableSizedWrapGrid>(panelContent, Orientation.Horizontal);
		var expectedPositions = new List<Point>();

		expectedPositions.Add(new Point(50.0f, 0.0f));
		expectedPositions.Add(new Point(200.0f, 0.0f));
		expectedPositions.Add(new Point(0.0f, 150.0f));
		expectedPositions.Add(new Point(100.0f, 100.0f));
		expectedPositions.Add(new Point(200.0f, 100.0f));
		expectedPositions.Add(new Point(100.0f, 200.0f));
		expectedPositions.Add(new Point(200.0f, 200.0f));

		await WindowHelper.WaitForIdle();
		await PanelsHelper.VerifyItemPositions(panel, expectedPositions);
	}
}
