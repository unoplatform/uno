#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Uno.UI.Helpers.WinUI;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml
{
	internal partial class DragView : Control
	{
		#region Glyph
		public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
			"Glyph", typeof(string), typeof(DragView), new PropertyMetadata(default(string)));

		public string Glyph
		{
			get { return (string)GetValue(GlyphProperty); }
			set { SetValue(GlyphProperty, value); }
		} 
		#endregion

		#region GlyphVisibility
		public static readonly DependencyProperty GlyphVisibilityProperty = DependencyProperty.Register(
			"GlyphVisibility", typeof(Visibility), typeof(DragView), new PropertyMetadata(default(Visibility)));

		public Visibility GlyphVisibility
		{
			get => (Visibility)GetValue(GlyphVisibilityProperty);
			private set => SetValue(GlyphVisibilityProperty, value);
		}
		#endregion

		#region Caption
		public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(
			"Caption", typeof(string), typeof(DragView), new PropertyMetadata(default(string)));

		public string Caption
		{
			get => (string)GetValue(CaptionProperty);
			set => SetValue(CaptionProperty, value);
		}
		#endregion

		#region CaptionVisibility
		public static readonly DependencyProperty CaptionVisibilityProperty = DependencyProperty.Register(
			"CaptionVisibility", typeof(Visibility), typeof(DragView), new PropertyMetadata(default(Visibility)));

		public Visibility CaptionVisibility
		{
			get => (Visibility)GetValue(CaptionVisibilityProperty);
			private set => SetValue(CaptionVisibilityProperty, value);
		}
		#endregion

		#region Content
		public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
			"Content", typeof(ImageSource), typeof(DragView), new PropertyMetadata(default(ImageSource)));

		public ImageSource? Content
		{
			get => (ImageSource?)GetValue(ContentProperty);
			private set => SetValue(ContentProperty, value);
		}
		#endregion

		#region ContentVisibility
		public static readonly DependencyProperty ContentVisibilityProperty = DependencyProperty.Register(
			"ContentVisibility", typeof(Visibility), typeof(DragView), new PropertyMetadata(default(Visibility)));

		public Visibility ContentVisibility
		{
			get => (Visibility)GetValue(ContentVisibilityProperty);
			private set => SetValue(ContentVisibilityProperty, value);
		}
		#endregion

		#region TooltipVisibility
		public static readonly DependencyProperty TooltipVisibilityProperty = DependencyProperty.Register(
			"TooltipVisibility", typeof(Visibility), typeof(DragView), new PropertyMetadata(default(Visibility)));

		public Visibility TooltipVisibility
		{
			get => (Visibility)GetValue(TooltipVisibilityProperty);
			set => SetValue(TooltipVisibilityProperty, value);
		} 
		#endregion

		private readonly DragUI? _ui;
		private readonly TranslateTransform _transform;

		private Point _location;

		public DragView(DragUI? ui)
		{
			_ui = ui;
			DefaultStyleKey = typeof(DragView);
			RenderTransform = _transform = new TranslateTransform();

			Content = ui?.Content;
		}

		public void SetLocation(Point location)
		{
			// TODO: Make sure to not move the element out of the bounds of the window
			_location = location;

			_transform.X = location.X - (ActualWidth / 2);
			_transform.Y = location.Y - 40; // The caption is above the pointer
		}

		public void Update(DataPackageOperation acceptedOperation, CoreDragUIOverride viewOverride)
		{
			// UWP does not allow new lines (trim to the first line, even if blank) and trims the text.
			var caption = viewOverride
				.Caption
				?.Split(new[] {'\r', '\n'}, StringSplitOptions.None)
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
			Content = viewOverride.Content as ImageSource ?? _ui?.Content;
			ContentVisibility = ToVisibility(viewOverride.IsContentVisible);
			TooltipVisibility = ToVisibility(viewOverride.IsGlyphVisible || viewOverride.IsCaptionVisible);
			Visibility = Visibility.Visible;
		}

		public void Hide()
		{
			Visibility = Visibility.Collapsed;
		}

		private static Visibility ToVisibility(bool isVisible)
			=> isVisible ? Visibility.Visible : Visibility.Collapsed;

		private static string ToGlyph(DataPackageOperation result)
		{
			// If multiple flags set (which should not!), the UWP precedence is Link > Copy > Move
			// TODO: Real glyph
			if (result.HasFlag(DataPackageOperation.Link))
			{
				return "🔗";
			}
			else if (result.HasFlag(DataPackageOperation.Copy))
			{
				return "⎘";
			}
			else if (result.HasFlag(DataPackageOperation.Move))
			{
				return "🡕";
			}
			else // None
			{
				return "🚫";
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
