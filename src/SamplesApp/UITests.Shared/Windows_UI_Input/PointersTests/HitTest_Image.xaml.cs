using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Input.PointersTests
{
	[Sample("Pointers", "Image")]
	public sealed partial class HitTest_Image : Page
	{
		public HitTest_Image()
		{
			this.InitializeComponent();

			Root.PointerPressed += (snd, e) =>
			{
				e.Handled = true;
				LastPressed.Text = Root.Name;
				LastPressedSrc.Text = (e.OriginalSource as FrameworkElement)?.Name ?? "-unknown-";
			};
			Root.PointerMoved += (snd, e) =>
			{
				e.Handled = true;
				LastHovered.Text = Root.Name;
				LastHoveredSrc.Text = (e.OriginalSource as FrameworkElement)?.Name ?? "-unknown-";
			};
			TheImage.PointerPressed += (snd, e) =>
			{
				e.Handled = true;
				LastPressed.Text = TheImage.Name;
				LastPressedSrc.Text = (e.OriginalSource as FrameworkElement)?.Name ?? "-unknown-";
			};
			TheImage.PointerMoved += (snd, e) =>
			{
				e.Handled = true;
				LastHovered.Text = TheImage.Name;
				LastHoveredSrc.Text = (e.OriginalSource as FrameworkElement)?.Name ?? "-unknown-";
			};
		}
	}
}
