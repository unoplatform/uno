using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using Windows.UI.Xaml;
using Uno.Extensions;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Views.Controls
{
	public partial class BindableUIProgressView : UIProgressView, DependencyObject
	{
		public BindableUIProgressView()
		{
			InitializeBinder();
		}

		public Brush Foreground
		{
			get
			{
				return new SolidColorBrush(base.ProgressTintColor);
			}
			set
			{
				var scb = value as SolidColorBrush;

				if (scb != null)
				{
					base.ProgressTintColor = scb.Color;
				}
			}
		}

		public Brush Background
		{
			get
			{
				return new SolidColorBrush(base.BackgroundColor);
			}
			set
			{
				var scb = value as SolidColorBrush;

				if (scb != null)
				{
					base.TrackTintColor = scb.Color;
				}
			}
		}

		public override float Progress
		{
			get
			{
				return base.Progress;
			}
			set
			{
				base.SetProgress(value, true);
			}
		}
	}
}
