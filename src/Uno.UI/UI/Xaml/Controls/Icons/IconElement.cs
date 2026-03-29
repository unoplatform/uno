#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents the base class for an icon UI element.
/// </summary>
public partial class IconElement : FrameworkElement
{
	private Grid? _rootGrid;

	//This field is never accessed. It just exists to create a reference, because the DP causes is	sues with ImageBrush of the backing bitmap being prematurely garbage-collected. (Bug with ConditionalWeakTable? https://bugzilla.xamarin.com/show_bug.cgi?id=21620)
	private Brush? _foregroundStrongref;

	public IconElement()
	{
		UpdateLastUsedTheme();
	}

	/// <summary>
	/// Gets or sets a brush that describes the foreground color.
	/// </summary>
	public
#if __ANDROID__
	new
#endif
	Brush Foreground
	{
		get
		{
			// MUX ref: In WinUI, Foreground is a TextFormatting storage group property.
			// CIconElement::PullInheritedTextFormatting checks IsPropertyDefault(IconElement_Foreground)
			// before pulling. When not locally set, the inherited value is returned.
			// WinUI ref: icon.cpp:39-84 (CIconElement::PullInheritedTextFormatting).
			if (IsPropertyDefault(ForegroundProperty))
			{
				EnsureTextFormatting(ForegroundProperty, forGetValue: true);
				if (_textFormatting?.Foreground is { } inherited)
				{
					return inherited;
				}
			}
			return (Brush)GetValue(ForegroundProperty);
		}
		set
		{
			SetValue(ForegroundProperty, value);
			_foregroundStrongref = value;
		}
	}

	/// <summary>
	/// Pulls inherited text formatting from parent, checking IsPropertyDefault for Foreground.
	/// MUX ref: CIconElement::PullInheritedTextFormatting (icon.cpp:39-84).
	/// </summary>
	internal override void PullInheritedTextFormatting()
	{
		var parent = GetParentTextFormatting();

		_textFormatting.FontFamily = parent.FontFamily;

		if (IsPropertyDefault(ForegroundProperty) && !_textFormatting.FreezeForeground)
		{
			_textFormatting.Foreground = parent.Foreground;
		}

		_textFormatting.Language = parent.Language;
		_textFormatting.FontSize = parent.FontSize;
		_textFormatting.FontWeight = parent.FontWeight;
		_textFormatting.FontStyle = parent.FontStyle;
		_textFormatting.FontStretch = parent.FontStretch;
		_textFormatting.CharacterSpacing = parent.CharacterSpacing;
		_textFormatting.TextDecorations = parent.TextDecorations;

		if (IsPropertyDefault(FrameworkElement.FlowDirectionProperty))
		{
			_textFormatting.FlowDirection = parent.FlowDirection;
		}

		_textFormatting.IsTextScaleFactorEnabled = parent.IsTextScaleFactorEnabled;
	}

	/// <summary>
	/// Identifies the Foreground dependency property.
	/// </summary>
	public static DependencyProperty ForegroundProperty { get; } =
		DependencyProperty.Register(
			nameof(Foreground),
			typeof(Brush),
			typeof(IconElement),
			new FrameworkPropertyMetadata(
				SolidColorBrushHelper.White,
				// MUX ref: IconElement_Foreground uses IsInherited in WinUI's DP system.
				// Restoring Inherits ensures icon foreground propagates from parent Controls.
				FrameworkPropertyMetadataOptions.Inherits,
				propertyChangedCallback: (s, e) =>
				{
					var icon = (IconElement)s;
					icon.OnForegroundChanged(e);
					if (!TextFormattingHelper.IsProcessingInheritedNotification)
					{
						var tf = icon._textFormatting ??= TextFormatting.CreateDefault();
						tf.SetFieldValue("Foreground", e.NewValue);
						GlobalTextFormattingCounter.Invalidate();
						icon.MarkInheritedPropertyDirty("Foreground", e.NewValue);
					}
				}
			)
		);


	public static implicit operator IconElement(string symbol) =>
		new SymbolIcon()
		{
			Symbol = Enum.Parse<Symbol>(symbol, true)
		};


	protected override Size MeasureOverride(Size availableSize)
	{
		if (_rootGrid is not null)
		{
			// Measure the child
			_rootGrid.Measure(availableSize);
			_rootGrid.EnsureLayoutStorage();
			return _rootGrid.DesiredSize;
		}

		return default;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		if (_rootGrid is not null)
		{
			Rect arrangeRect = new Rect(0, 0, finalSize.Width, finalSize.Height);
			_rootGrid.Arrange(arrangeRect);
		}

		return finalSize;
	}

	private protected virtual void OnForegroundChanged(DependencyPropertyChangedEventArgs e) { }

	internal override void UpdateThemeBindings(ResourceUpdateReason updateReason)
	{
		base.UpdateThemeBindings(updateReason);

		UpdateLastUsedTheme();
	}

	[MemberNotNull(nameof(_rootGrid))]
	private protected void InitializeRootGrid()
	{
		if (_rootGrid is not null)
		{
			return;
		}

		var backgroundBrush = new SolidColorBrush()
		{
			Color = Color.FromArgb(0, 0, 0, 0)
		};

		_rootGrid = new Grid()
		{
			Background = backgroundBrush
		};

		VisualTreeHelper.AddChild(this, _rootGrid);
	}

	internal void AddIconChild(UIElement child)
	{
		InitializeRootGrid();

		_rootGrid.Children.Add(child);
	}

	internal void RemoveIconChild()
	{
		InitializeRootGrid();

		_rootGrid.Children.Clear();
	}

	internal override bool CanHaveChildren() => true;
}
