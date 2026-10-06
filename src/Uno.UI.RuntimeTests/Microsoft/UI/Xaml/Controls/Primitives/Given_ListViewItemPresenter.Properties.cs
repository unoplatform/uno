#if HAS_UNO
#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ListViewItemPresenter_Properties
{
	private const double Tolerance = 1e-6;

	[TestInitialize]
	public void Init() => ListViewBaseItemChrome.ClearIsRoundedListViewBaseItemChromeEnabledCache();

	[TestCleanup]
	public void Cleanup() => ListViewBaseItemChrome.ClearIsRoundedListViewBaseItemChromeEnabledCache();

	[TestMethod]
	public void When_Constant_Defaults()
	{
		var presenter = new ListViewItemPresenter();

		Assert.IsTrue(presenter.SelectionCheckMarkVisualEnabled);
		Assert.AreEqual(0.8, presenter.DragOpacity, Tolerance);
		Assert.AreEqual(10.0, presenter.ReorderHintOffset, Tolerance);
		Assert.AreEqual(new CornerRadius(3), presenter.CheckBoxCornerRadius);
		Assert.AreEqual(new CornerRadius(1.5), presenter.SelectionIndicatorCornerRadius);
		Assert.AreEqual(ListViewItemPresenterSelectionIndicatorMode.Overlay, presenter.SelectionIndicatorMode);
		Assert.AreEqual(ListViewItemPresenterCheckMode.Inline, presenter.CheckMode);
		Assert.IsFalse(presenter.RevealBackgroundShowsAboveContent);
		Assert.AreEqual(default(Thickness), presenter.ContentMargin);
		Assert.IsNull(presenter.SelectedBackground);
		Assert.IsNull(presenter.CheckBoxSelectedDisabledBrush);
	}

	[TestMethod]
	public void When_Rounded_Chrome_Defaults()
	{
		using (ListViewChromeHelper.UseRoundedChromeResource(true))
		{
			var presenter = new ListViewItemPresenter();

			Assert.AreEqual(0.3, presenter.DisabledOpacity, Tolerance);
			Assert.AreEqual(new Thickness(2), presenter.SelectedBorderThickness);
			Assert.IsTrue(presenter.SelectionIndicatorVisualEnabled);
		}
	}

	[TestMethod]
	public void When_Non_Rounded_Chrome_Defaults()
	{
		using (ListViewChromeHelper.UseNonRoundedChrome())
		{
			var presenter = new ListViewItemPresenter();

			Assert.AreEqual(0.55, presenter.DisabledOpacity, Tolerance);
			Assert.AreEqual(new Thickness(0), presenter.SelectedBorderThickness);
			Assert.IsFalse(presenter.SelectionIndicatorVisualEnabled);
		}
	}

	[TestMethod]
	public void When_Lazy_Default_Reevaluated_After_Scope_Ends()
	{
		var presenter = new ListViewItemPresenter();

		using (ListViewChromeHelper.UseNonRoundedChrome())
		{
			Assert.AreEqual(0.55, presenter.DisabledOpacity, Tolerance);
			Assert.IsFalse(presenter.SelectionIndicatorVisualEnabled);
		}

		using (ListViewChromeHelper.UseRoundedChromeResource(true))
		{
			Assert.AreEqual(0.3, presenter.DisabledOpacity, Tolerance);
			Assert.AreEqual(new Thickness(2), presenter.SelectedBorderThickness);
			Assert.IsTrue(presenter.SelectionIndicatorVisualEnabled);
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
			var presenter = new ListViewItemPresenter { DisabledOpacity = 0.9 };

			Assert.AreEqual(0.9, presenter.DisabledOpacity, Tolerance);

			presenter.ClearValue(ListViewItemPresenter.DisabledOpacityProperty);
			Assert.AreEqual(0.3, presenter.DisabledOpacity, Tolerance);
		}
	}

	[TestMethod]
	public void When_Deny_Skips_Resource_For_Defaults()
	{
		using (ListViewChromeHelper.UseRoundedChromeResource(true))
		using (ListViewBaseItemChromeRuntimeFeatures.Override(denyRounded: true))
		{
			var presenter = new ListViewItemPresenter();

			// Deny skips the resource lookup in the DP defaults...
			Assert.AreEqual(0.55, presenter.DisabledOpacity, Tolerance);
			Assert.AreEqual(new Thickness(0), presenter.SelectedBorderThickness);
			Assert.IsFalse(presenter.SelectionIndicatorVisualEnabled);

			// ...but not in the item reader, which still sees the resource.
			Assert.IsTrue(ListViewBaseItemChrome.IsRoundedListViewBaseItemChromeEnabledStatic());
		}
	}

	[TestMethod]
	public void When_Deny_And_Force_Defaults_Are_Non_Rounded()
	{
		using (ListViewChromeHelper.UseRoundedChromeResource(true))
		using (ListViewBaseItemChromeRuntimeFeatures.Override(denyRounded: true, forceRounded: true))
		{
			var presenter = new ListViewItemPresenter();

			Assert.AreEqual(0.55, presenter.DisabledOpacity, Tolerance);
			Assert.AreEqual(new Thickness(0), presenter.SelectedBorderThickness);
			Assert.IsFalse(presenter.SelectionIndicatorVisualEnabled);
		}
	}

	[TestMethod]
	public void When_Forced_Defaults_Are_Rounded_Without_Resource()
	{
		using (ListViewChromeHelper.UseNonRoundedChrome())
		using (ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true))
		{
			var presenter = new ListViewItemPresenter();

			Assert.AreEqual(0.3, presenter.DisabledOpacity, Tolerance);
			Assert.IsTrue(presenter.SelectionIndicatorVisualEnabled);
		}
	}

	[TestMethod]
	public void When_Alias_Forwards_To_ContentPresenter()
	{
		var presenter = new ListViewItemPresenter();

		presenter.ListViewItemPresenterPadding = new Thickness(1, 2, 3, 4);
		Assert.AreEqual(new Thickness(1, 2, 3, 4), presenter.Padding);

		presenter.Padding = new Thickness(5);
		Assert.AreEqual(new Thickness(5), presenter.ListViewItemPresenterPadding);

		presenter.ListViewItemPresenterHorizontalContentAlignment = HorizontalAlignment.Right;
		Assert.AreEqual(HorizontalAlignment.Right, presenter.HorizontalContentAlignment);

		presenter.VerticalContentAlignment = VerticalAlignment.Bottom;
		Assert.AreEqual(VerticalAlignment.Bottom, presenter.ListViewItemPresenterVerticalContentAlignment);
	}

	[TestMethod]
	public void When_Alias_Style_Setter_Removed_Binding_Is_Not_Clobbered()
	{
		var presenter = new ListViewItemPresenter();
		Style style = new(typeof(ListViewItemPresenter));
		style.Setters.Add(new Setter(ListViewItemPresenter.ListViewItemPresenterPaddingProperty, new Thickness(3)));

		presenter.Style = style;
		Assert.AreEqual(new Thickness(3), presenter.Padding);

		PaddingSource source = new() { Value = new Thickness(7) };
		presenter.SetBinding(ContentPresenter.PaddingProperty, new Binding { Source = source, Path = new PropertyPath(nameof(PaddingSource.Value)) });
		Assert.AreEqual(new Thickness(7), presenter.Padding);

		presenter.Style = null;

		Assert.AreEqual(new Thickness(7), presenter.Padding);
	}

	[TestMethod]
	public void When_Alias_Style_Setter_Does_Not_Override_Local_Target()
	{
		var presenter = new ListViewItemPresenter();
		presenter.Padding = new Thickness(5);

		Style style = new(typeof(ListViewItemPresenter));
		style.Setters.Add(new Setter(ListViewItemPresenter.ListViewItemPresenterPaddingProperty, new Thickness(3)));
		presenter.Style = style;

		Assert.AreEqual(new Thickness(5), presenter.Padding);
	}

	[TestMethod]
	public async Task When_Alias_Style_Setter_Removed_TemplateBinding_Is_Not_Clobbered()
	{
		var control = (ContentControl)XamlReader.Load(
			"""
			<ContentControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Padding="7">
				<ContentControl.Template>
					<ControlTemplate TargetType="ContentControl">
						<ListViewItemPresenter Padding="{TemplateBinding Padding}">
							<ListViewItemPresenter.Style>
								<Style TargetType="ListViewItemPresenter">
									<Setter Property="ListViewItemPresenterPadding" Value="3" />
								</Style>
							</ListViewItemPresenter.Style>
						</ListViewItemPresenter>
					</ControlTemplate>
				</ContentControl.Template>
			</ContentControl>
			""");

		await UITestHelper.Load(control);

		var presenter = (ListViewItemPresenter)VisualTreeHelper.GetChild(control, 0);
		Assert.AreEqual(new Thickness(7), presenter.Padding);

		presenter.Style = null;

		Assert.AreEqual(new Thickness(7), presenter.Padding);
	}

	[TestMethod]
	public void When_Background_Resolves_To_ContentPresenter()
	{
		Assert.AreSame(ContentPresenter.BackgroundProperty, ListViewItemPresenter.BackgroundProperty);
	}

	[TestMethod]
	public void When_Layout_Options_Match_WinUI()
	{
		static FrameworkPropertyMetadataOptions Options(DependencyProperty dp)
			=> ((FrameworkPropertyMetadata)dp.GetMetadata(typeof(ListViewItemPresenter))).Options;

		var measure = FrameworkPropertyMetadataOptions.AffectsMeasure;
		var arrange = FrameworkPropertyMetadataOptions.AffectsArrange;

		Assert.IsTrue(Options(ListViewItemPresenter.SelectionCheckMarkVisualEnabledProperty).HasFlag(measure));
		Assert.IsTrue(Options(ListViewItemPresenter.CheckModeProperty).HasFlag(measure | arrange));
		Assert.IsTrue(Options(ListViewItemPresenter.SelectionIndicatorModeProperty).HasFlag(measure | arrange));
		Assert.IsFalse(Options(ListViewItemPresenter.SelectedBackgroundProperty).HasFlag(measure));
		Assert.IsTrue(Options(ListViewItemPresenter.SelectedBackgroundProperty).HasFlag(FrameworkPropertyMetadataOptions.AffectsRender));
	}

	private class PaddingSource
	{
		public Thickness Value { get; set; }
	}
}
#endif
