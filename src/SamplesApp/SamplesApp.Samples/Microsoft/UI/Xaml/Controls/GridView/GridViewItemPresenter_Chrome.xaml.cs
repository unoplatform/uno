#nullable enable

using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.GridView;

[Sample("GridView", Name = "GridViewItemPresenter_Chrome", Description = "GridViewItem chrome: selection, check and indicator modes, rounded vs non-rounded, disabled items, custom brushes.")]
public sealed partial class GridViewItemPresenter_Chrome : Page
{
	private object? _previousRounded;
	private bool _hadRounded;
	private bool _ready;
	private bool _disableEveryThird;

	public GridViewItemPresenter_Chrome()
	{
		this.InitializeComponent();

		SelectionModeBox.ItemsSource = Enum.GetNames(typeof(ListViewSelectionMode));
		CheckModeBox.ItemsSource = Enum.GetNames(typeof(ListViewItemPresenterCheckMode));
		IndicatorModeBox.ItemsSource = Enum.GetNames(typeof(ListViewItemPresenterSelectionIndicatorMode));
		SelectionModeBox.SelectedItem = nameof(ListViewSelectionMode.Single);
		CheckModeBox.SelectedItem = nameof(ListViewItemPresenterCheckMode.Overlay);
		IndicatorModeBox.SelectedItem = nameof(ListViewItemPresenterSelectionIndicatorMode.Overlay);

		Items.ItemsSource = Enumerable.Range(0, 30).Select(i => $"Item {i}").ToList();
		Items.ContainerContentChanging += OnContainerContentChanging;
		_ready = true;
		ApplyStyle();

		Unloaded += (_, _) => RestoreRounded();
	}

	private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
	{
		if (args.ItemContainer is Control container)
		{
			container.IsEnabled = !_disableEveryThird || args.ItemIndex % 3 != 2;
		}
	}

	private void OnModeChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_ready && SelectionModeBox.SelectedItem is string name)
		{
			Items.SelectionMode = Enum.Parse<ListViewSelectionMode>(name);
		}
	}

	private void OnPresenterChanged(object sender, object e)
	{
		if (_ready)
		{
			ApplyStyle();
		}
	}

	private void OnRoundedChanged(object sender, RoutedEventArgs e)
	{
		if (!_ready)
		{
			return;
		}

		var resources = Application.Current.Resources;
		if (!_hadRounded)
		{
			_hadRounded = true;
			_previousRounded = resources.ContainsKey("ListViewBaseItemRoundedChromeEnabled")
				? resources["ListViewBaseItemRoundedChromeEnabled"]
				: null;
		}

		resources["ListViewBaseItemRoundedChromeEnabled"] = RoundedBox.IsChecked == true;
		ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();
		ApplyStyle();
	}

	private void RestoreRounded()
	{
		if (!_hadRounded)
		{
			return;
		}

		var resources = Application.Current.Resources;
		if (_previousRounded is null)
		{
			resources.Remove("ListViewBaseItemRoundedChromeEnabled");
		}
		else
		{
			resources["ListViewBaseItemRoundedChromeEnabled"] = _previousRounded;
		}

		ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();
		_hadRounded = false;
	}

	private void ApplyStyle()
	{
		_disableEveryThird = DisabledBox.IsChecked == true;
		var brushes = BrushesBox.IsChecked == true
			? " SelectedBackground=\"Gold\" SelectedPointerOverBackground=\"Orange\" SelectedBorderBrush=\"DarkRed\" SelectedBorderThickness=\"2\" PointerOverBackground=\"LightBlue\" SelectionIndicatorBrush=\"Red\" CheckBrush=\"Green\" CheckBoxBrush=\"Purple\""
			: "";
		var xaml =
			"<Style xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" TargetType=\"GridViewItem\" BasedOn=\"{StaticResource DefaultGridViewItemStyle}\">" +
			"<Setter Property=\"Width\" Value=\"100\" /><Setter Property=\"Height\" Value=\"80\" />" +
			"<Setter Property=\"Template\"><Setter.Value><ControlTemplate TargetType=\"GridViewItem\">" +
			$"<ListViewItemPresenter CheckMode=\"{CheckModeBox.SelectedItem}\" SelectionIndicatorMode=\"{IndicatorModeBox.SelectedItem}\"{brushes}" +
			" ContentMargin=\"{TemplateBinding Padding}\" ContentTransitions=\"{TemplateBinding ContentTransitions}\"" +
			" HorizontalContentAlignment=\"{TemplateBinding HorizontalContentAlignment}\" VerticalContentAlignment=\"{TemplateBinding VerticalContentAlignment}\" />" +
			"</ControlTemplate></Setter.Value></Setter></Style>";
		Items.ItemContainerStyle = (Style)XamlReader.Load(xaml);
		Items.ItemsSource = Enumerable.Range(0, 30).Select(i => $"Item {i}").ToList();
	}
}
