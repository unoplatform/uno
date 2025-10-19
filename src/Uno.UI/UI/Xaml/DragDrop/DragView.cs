#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using Uno.UI.Helpers.WinUI;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml
{
	internal partial class DragView : Control
	{
		#region Glyph
		public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
			"Glyph", typeof(string), typeof(DragView), new FrameworkPropertyMetadata(string.Empty));

		public string Glyph
		{
			get { return (string)GetValue(GlyphProperty); }
			set { SetValue(GlyphProperty, value); }
		}
		#endregion

		#region GlyphVisibility
		public static readonly DependencyProperty GlyphVisibilityProperty = DependencyProperty.Register(
			"GlyphVisibility", typeof(Visibility), typeof(DragView), new FrameworkPropertyMetadata(default(Visibility)));

		public Visibility GlyphVisibility
		{
			get => (Visibility)GetValue(GlyphVisibilityProperty);
			private set => SetValue(GlyphVisibilityProperty, value);
		}
		#endregion

		#region Caption
		public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(
			"Caption", typeof(string), typeof(DragView), new FrameworkPropertyMetadata(string.Empty));

		public string Caption
		{
			get => (string)GetValue(CaptionProperty);
			set => SetValue(CaptionProperty, value);
		}
		#endregion

		#region CaptionVisibility
		public static readonly DependencyProperty CaptionVisibilityProperty = DependencyProperty.Register(
			"CaptionVisibility", typeof(Visibility), typeof(DragView), new FrameworkPropertyMetadata(default(Visibility)));

		public Visibility CaptionVisibility
		{
			get => (Visibility)GetValue(CaptionVisibilityProperty);
			private set => SetValue(CaptionVisibilityProperty, value);
		}
		#endregion

		#region Content
		public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
			"Content", typeof(ImageSource), typeof(DragView), new FrameworkPropertyMetadata(default(ImageSource)));

		public ImageSource? Content
		{
			get => (ImageSource?)GetValue(ContentProperty);
			private set => SetValue(ContentProperty, value);
		}
		#endregion

		#region ContentAnchor
		public static readonly DependencyProperty ContentAnchorProperty = DependencyProperty.Register(
			"ContentAnchor", typeof(Point), typeof(DragView), new FrameworkPropertyMetadata(default(Point)));

		public Point ContentAnchor
		{
			get => (Point)GetValue(ContentAnchorProperty);
			private set => SetValue(ContentAnchorProperty, value);
		}
		#endregion

		#region ContentVisibility
		public static readonly DependencyProperty ContentVisibilityProperty = DependencyProperty.Register(
			"ContentVisibility", typeof(Visibility), typeof(DragView), new FrameworkPropertyMetadata(default(Visibility)));

		public Visibility ContentVisibility
		{
			get => (Visibility)GetValue(ContentVisibilityProperty);
			private set => SetValue(ContentVisibilityProperty, value);
		}
		#endregion

		#region TooltipVisibility
		public static readonly DependencyProperty TooltipVisibilityProperty = DependencyProperty.Register(
			"TooltipVisibility", typeof(Visibility), typeof(DragView), new FrameworkPropertyMetadata(default(Visibility)));

		public Visibility TooltipVisibility
		{
			get => (Visibility)GetValue(TooltipVisibilityProperty);
			set => SetValue(TooltipVisibilityProperty, value);
		}
		#endregion

		private readonly DragUI? _ui;
		private readonly TranslateTransform _transform;
		private FrameworkElement? _toolTipPanel = null;


		private Point _location;
		private static readonly char[] _newLineChars = new[] { '\r', '\n' };

		public DragView(DragUI? ui)
		{
			_ui = ui;
			DefaultStyleKey = typeof(DragView);
			RenderTransform = _transform = new TranslateTransform();

			Content = ui?.Content;
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			if (_toolTipPanel is not null)
			{
				_toolTipPanel.SizeChanged -= OnToolTipPanelSizeChanged;
			}

			_toolTipPanel = GetTemplateChild("ToolTipPanel") as FrameworkElement;

			if (_toolTipPanel is not null)
			{
				_toolTipPanel.SizeChanged += OnToolTipPanelSizeChanged;

				// Apply initial centering if the panel is already visible and sized
				UpdateToolTipTranslation();
			}
		}

		private void OnToolTipPanelSizeChanged(object sender, SizeChangedEventArgs args) =>
			UpdateToolTipTranslation();

		private void UpdateToolTipTranslation()
		{
			if (_toolTipPanel is { Visibility: Visibility.Visible, ActualWidth: > 0, RenderTransform: TranslateTransform translate })
			{
				translate.X = -_toolTipPanel.ActualWidth / 2;
				translate.Y = _ui?.PointerDeviceType == UI.Input.PointerDeviceType.Touch ?
					-ToolTip.DEFAULT_TOUCH_OFFSET : -ToolTip.DEFAULT_MOUSE_OFFSET;
			}
		}

		public void SetLocation(Point location)
		{
			// TODO: Make sure to not move the element out of the bounds of the window
			_location = location;

			_transform.X = location.X;
			_transform.Y = location.Y;
		}

		public void Update(DataPackageOperation acceptedOperation, CoreDragUIOverride viewOverride)
		{
			// UWP does not allow new lines (trim to the first line, even if blank) and trims the text.
			var caption = viewOverride.Caption
				?.Split(_newLineChars, StringSplitOptions.None)
				.FirstOrDefault()
				?.Trim();

			if (string.IsNullOrEmpty(caption))
			{
				caption = ToCaption(acceptedOperation);
			}

			Glyph = ToGlyph(acceptedOperation);
			GlyphVisibility = ToVisibility(viewOverride.IsGlyphVisible);
			Caption = caption!;
			CaptionVisibility = ToVisibility(viewOverride.IsCaptionVisible && !string.IsNullOrWhiteSpace(caption));
			if (viewOverride.Content is ImageSource overriddenContent)
			{
				Content = overriddenContent;
				ContentAnchor = viewOverride.ContentAnchor;
			}
			else
			{
				Content = _ui?.Content;
				ContentAnchor = _ui?.Anchor ?? default;
			}
			ContentVisibility = ToVisibility(viewOverride.IsContentVisible);
			TooltipVisibility = ToVisibility(viewOverride.IsGlyphVisible || viewOverride.IsCaptionVisible);
			Visibility = Visibility.Visible;
		}

		private static Visibility ToVisibility(bool isVisible)
			=> isVisible ? Visibility.Visible : Visibility.Collapsed;

		private static string ToGlyph(DataPackageOperation result)
		{
			// If multiple flags set (which should not!), the UWP precedence is Link > Copy > Move
			// These codepoints reference glyphs that are in the uno-fluentui-assets font
			if (result.HasFlag(DataPackageOperation.Link))
			{
				return "\uE148";
			}
			else if (result.HasFlag(DataPackageOperation.Copy))
			{
				return "\uE130";
			}
			else if (result.HasFlag(DataPackageOperation.Move))
			{
				return "\uE143";
			}
			else // None
			{
				return "\uF140";
			}
		}

		private static string ToCaption(DataPackageOperation result)
		{
			// If multiple flags set (which should not!), the UWP precedence is Link > Copy > Move
			if (result.HasFlag(DataPackageOperation.Link))
			{
				return ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_DragViewLinkCaption);
			}
			else if (result.HasFlag(DataPackageOperation.Copy))
			{
				return ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_DragViewCopyCaption);
			}
			else if (result.HasFlag(DataPackageOperation.Move))
			{
				return ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_DragViewMoveCaption);
			}
			else // None
			{
				return string.Empty;
			}
		}
	}
}
