using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

using TwoPaneView = Microsoft.UI.Xaml.Controls.TwoPaneView;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public partial class Given_TwoPaneView
{
	private partial class MyTwoPaneView : TwoPaneView
	{
		internal bool TemplateApplied { get; private set; }
		internal Exception ExceptionThrown { get; private set; }
		internal DependencyObject Pane1ScrollViewer => GetTemplateChild("PART_Pane1ScrollViewer");
		internal DependencyObject Pane2ScrollViewer => GetTemplateChild("PART_Pane2ScrollViewer");

		protected override void OnApplyTemplate()
		{
			try
			{
				TemplateApplied = true;
				base.OnApplyTemplate();
			}
			catch (Exception e)
			{
				ExceptionThrown = e;
			}
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_ApplyTemplate_Should_Not_Throw()
	{
		var SUT = new MyTwoPaneView() { Width = 100, Height = 100 };
		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsTrue(SUT.TemplateApplied);
		Assert.IsNull(SUT.ExceptionThrown);
		Assert.IsNotNull(SUT.Template);
		Assert.IsNotNull(SUT.Pane1ScrollViewer);
		Assert.IsNotNull(SUT.Pane2ScrollViewer);
	}
}
