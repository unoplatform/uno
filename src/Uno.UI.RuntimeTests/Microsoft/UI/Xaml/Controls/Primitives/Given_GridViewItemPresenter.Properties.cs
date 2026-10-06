#if HAS_UNO
#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_GridViewItemPresenter_Properties
{
	private const double Tolerance = 1e-6;

	[TestInitialize]
	public void Init() => ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();

	[TestCleanup]
	public void Cleanup() => ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();

	[TestMethod]
	public void When_Constant_Defaults()
	{
		var presenter = new GridViewItemPresenter();

		Assert.IsTrue(presenter.SelectionCheckMarkVisualEnabled);
		Assert.AreEqual(0.8, presenter.DragOpacity, Tolerance);
		Assert.AreEqual(16.0, presenter.ReorderHintOffset, Tolerance);
		Assert.AreEqual(default(Thickness), presenter.ContentMargin);
		Assert.AreEqual(default(Thickness), presenter.PointerOverBackgroundMargin);
		Assert.IsNull(presenter.SelectedBackground);
		Assert.IsNull(presenter.CheckBrush);
	}

	[TestMethod]
	public void When_Rounded_Chrome_Defaults()
	{
		using (ListViewChromeHelper.UseRoundedChromeResource(true))
		{
			var presenter = new GridViewItemPresenter();

			Assert.AreEqual(0.3, presenter.DisabledOpacity, Tolerance);
			Assert.AreEqual(new Thickness(2), presenter.SelectedBorderThickness);
		}
	}

	[TestMethod]
	public void When_Non_Rounded_Chrome_Defaults()
	{
		using (ListViewChromeHelper.UseNonRoundedChrome())
		{
			var presenter = new GridViewItemPresenter();

			Assert.AreEqual(0.55, presenter.DisabledOpacity, Tolerance);
			Assert.AreEqual(new Thickness(0), presenter.SelectedBorderThickness);
		}
	}

	[TestMethod]
	public void When_Lazy_Default_Reevaluated_After_Scope_Ends()
	{
		var presenter = new GridViewItemPresenter();

		using (ListViewChromeHelper.UseNonRoundedChrome())
		{
			Assert.AreEqual(0.55, presenter.DisabledOpacity, Tolerance);
		}

		using (ListViewChromeHelper.UseRoundedChromeResource(true))
		{
			Assert.AreEqual(0.3, presenter.DisabledOpacity, Tolerance);
			Assert.AreEqual(new Thickness(2), presenter.SelectedBorderThickness);
		}

		using (ListViewChromeHelper.UseNonRoundedChrome())
		{
			Assert.AreEqual(0.55, presenter.DisabledOpacity, Tolerance);
			Assert.AreEqual(new Thickness(0), presenter.SelectedBorderThickness);
		}
	}

	[TestMethod]
	public void When_Local_Value_Set_It_Wins_Over_Lazy_Default()
	{
		using (ListViewChromeHelper.UseRoundedChromeResource(true))
		{
			var presenter = new GridViewItemPresenter { DisabledOpacity = 0.9 };

			Assert.AreEqual(0.9, presenter.DisabledOpacity, Tolerance);

			presenter.ClearValue(GridViewItemPresenter.DisabledOpacityProperty);
			Assert.AreEqual(0.3, presenter.DisabledOpacity, Tolerance);
		}
	}

	[TestMethod]
	public void When_Deny_Skips_Resource_For_Defaults()
	{
		using (ListViewChromeHelper.UseRoundedChromeResource(true))
		using (ListViewBaseItemChromeRuntimeFeatures.Override(denyRounded: true))
		{
			var presenter = new GridViewItemPresenter();

			Assert.AreEqual(0.55, presenter.DisabledOpacity, Tolerance);
			Assert.AreEqual(new Thickness(0), presenter.SelectedBorderThickness);
		}
	}

	[TestMethod]
	public void When_Forced_Defaults_Are_Rounded_Without_Resource()
	{
		using (ListViewChromeHelper.UseNonRoundedChrome())
		using (ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true))
		{
			var presenter = new GridViewItemPresenter();

			Assert.AreEqual(0.3, presenter.DisabledOpacity, Tolerance);
			Assert.AreEqual(new Thickness(2), presenter.SelectedBorderThickness);
		}
	}

	[TestMethod]
	public void When_Alias_Forwards_To_ContentPresenter()
	{
		var presenter = new GridViewItemPresenter();

		presenter.GridViewItemPresenterPadding = new Thickness(1, 2, 3, 4);
		Assert.AreEqual(new Thickness(1, 2, 3, 4), presenter.Padding);

		presenter.Padding = new Thickness(5);
		Assert.AreEqual(new Thickness(5), presenter.GridViewItemPresenterPadding);

		presenter.GridViewItemPresenterHorizontalContentAlignment = HorizontalAlignment.Right;
		Assert.AreEqual(HorizontalAlignment.Right, presenter.HorizontalContentAlignment);

		presenter.VerticalContentAlignment = VerticalAlignment.Bottom;
		Assert.AreEqual(VerticalAlignment.Bottom, presenter.GridViewItemPresenterVerticalContentAlignment);
	}

	[TestMethod]
	public void When_Alias_Style_Setter_Does_Not_Override_Local_Target()
	{
		var presenter = new GridViewItemPresenter();
		presenter.Padding = new Thickness(5);

		Style style = new(typeof(GridViewItemPresenter));
		style.Setters.Add(new Setter(GridViewItemPresenter.GridViewItemPresenterPaddingProperty, new Thickness(3)));
		presenter.Style = style;

		Assert.AreEqual(new Thickness(5), presenter.Padding);
	}

	[TestMethod]
	public void When_Background_Resolves_To_ContentPresenter()
	{
		Assert.AreSame(ContentPresenter.BackgroundProperty, GridViewItemPresenter.BackgroundProperty);
	}

	[TestMethod]
	public void When_Dps_Are_Distinct_From_ListViewItemPresenter()
	{
		Assert.AreNotSame(ListViewItemPresenter.DisabledOpacityProperty, GridViewItemPresenter.DisabledOpacityProperty);
		Assert.AreNotSame(ListViewItemPresenter.ReorderHintOffsetProperty, GridViewItemPresenter.ReorderHintOffsetProperty);
	}

	[TestMethod]
	public void When_Layout_Options_Match_WinUI()
	{
		static FrameworkPropertyMetadataOptions Options(DependencyProperty dp)
			=> ((FrameworkPropertyMetadata)dp.GetMetadata(typeof(GridViewItemPresenter))).Options;

		var measure = FrameworkPropertyMetadataOptions.AffectsMeasure;

		Assert.IsTrue(Options(GridViewItemPresenter.SelectionCheckMarkVisualEnabledProperty).HasFlag(measure));
		Assert.IsTrue(Options(GridViewItemPresenter.ContentMarginProperty).HasFlag(measure));
		Assert.IsTrue(Options(GridViewItemPresenter.PointerOverBackgroundMarginProperty).HasFlag(measure));
		Assert.IsFalse(Options(GridViewItemPresenter.SelectedBackgroundProperty).HasFlag(measure));
		Assert.IsTrue(Options(GridViewItemPresenter.SelectedBackgroundProperty).HasFlag(FrameworkPropertyMetadataOptions.AffectsRender));
	}
}
#endif
