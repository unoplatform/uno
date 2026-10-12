#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Samples.Controls;

/// <summary>
/// A settings row: glyph, header and description, with its control (the content) on the right.
/// Below <see cref="StackWidth"/> the control moves under the text, except for toggles, which always fit beside it.
/// </summary>
public sealed partial class ShellSettingsCard : ContentControl
{
	/// <summary>The settings view's breakpoint (cards span the whole view).</summary>
	public const double StackWidth = 360;

	private FrameworkElement? _contentPart;
	private FrameworkElement? _descriptionPart;

	public ShellSettingsCard()
	{
		SizeChanged += (_, e) => UpdateStacking(e.NewSize.Width);
	}

	public string? Header
	{
		get => (string?)GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	public static DependencyProperty HeaderProperty { get; } =
		DependencyProperty.Register(nameof(Header), typeof(string), typeof(ShellSettingsCard), new PropertyMetadata(null, OnHeaderChanged));

	public string? Description
	{
		get => (string?)GetValue(DescriptionProperty);
		set => SetValue(DescriptionProperty, value);
	}

	public static DependencyProperty DescriptionProperty { get; } =
		DependencyProperty.Register(nameof(Description), typeof(string), typeof(ShellSettingsCard), new PropertyMetadata(null, OnDescriptionChanged));

	private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((ShellSettingsCard)d).UpdateDescription();

	public string? Glyph
	{
		get => (string?)GetValue(GlyphProperty);
		set => SetValue(GlyphProperty, value);
	}

	public static DependencyProperty GlyphProperty { get; } =
		DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(ShellSettingsCard), new PropertyMetadata(null));

	/// <summary>True while the control sits under the text.</summary>
	public bool IsStacked { get; private set; }

	internal static bool ShouldStack(double width, object? content) => width < StackWidth && content is not ToggleSwitch;

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		_contentPart = GetTemplateChild("ContentPart") as FrameworkElement;
		_descriptionPart = GetTemplateChild("DescriptionPart") as FrameworkElement;

		UpdateDescription();
		ApplyStacking();
	}

	protected override void OnContentChanged(object oldContent, object newContent)
	{
		base.OnContentChanged(oldContent, newContent);

		// The header labels the switch, so it needs none of the stock 154 DIP minimum.
		if (newContent is ToggleSwitch toggle)
		{
			toggle.MinWidth = 0;

			// SizeChanged also catches a re-applied template (Fluent styles toggled), which brings the minimum back.
			toggle.SizeChanged -= ClearTemplateMinWidth;
			toggle.SizeChanged += ClearTemplateMinWidth;
		}

		NameContent(previousHeader: null);
		UpdateStacking(ActualWidth);
	}

	// WinUI's template puts the minimum on the grid around SwitchAreaGrid, from a StaticResource no override reaches.
	private static void ClearTemplateMinWidth(object sender, SizeChangedEventArgs e)
	{
		if (FindDescendant((DependencyObject)sender, "SwitchAreaGrid") is { } switchArea
			&& VisualTreeHelper.GetParent(switchArea) is FrameworkElement { MinWidth: > 0 } switchHost)
		{
			switchHost.MinWidth = 0;
		}
	}

	private static FrameworkElement? FindDescendant(DependencyObject parent, string name)
	{
		var count = VisualTreeHelper.GetChildrenCount(parent);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			if (child is FrameworkElement element && element.Name == name)
			{
				return element;
			}

			if (FindDescendant(child, name) is { } match)
			{
				return match;
			}
		}

		return null;
	}

	private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		=> ((ShellSettingsCard)d).NameContent(e.OldValue as string);

	// A screen reader announces the control by the card's header, unless the control names itself.
	private void NameContent(string? previousHeader)
	{
		if (Content is DependencyObject content and not Button)
		{
			var name = AutomationProperties.GetName(content);
			if (string.IsNullOrEmpty(name) || name == previousHeader)
			{
				AutomationProperties.SetName(content, Header ?? "");
			}
		}
	}

	private void UpdateDescription()
	{
		if (_descriptionPart is not null)
		{
			_descriptionPart.Visibility = string.IsNullOrEmpty(Description) ? Visibility.Collapsed : Visibility.Visible;
		}
	}

	private void UpdateStacking(double width)
	{
		// Not laid out yet: keep the current arrangement.
		if (width <= 0)
		{
			return;
		}

		var isStacked = ShouldStack(width, Content);
		if (isStacked != IsStacked)
		{
			IsStacked = isStacked;
			ApplyStacking();
		}
	}

	private void ApplyStacking()
	{
		if (_contentPart is null)
		{
			return;
		}

		Grid.SetRow(_contentPart, IsStacked ? 1 : 0);
		Grid.SetColumn(_contentPart, IsStacked ? 1 : 2);
		Grid.SetColumnSpan(_contentPart, IsStacked ? 2 : 1);
		_contentPart.HorizontalAlignment = IsStacked ? HorizontalAlignment.Left : HorizontalAlignment.Right;
	}
}
