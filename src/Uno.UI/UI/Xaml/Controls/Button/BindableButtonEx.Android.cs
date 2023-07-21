using Android.Views;
using Uno.UI.DataBinding;
using Uno.UI.Controls;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Android.Graphics;
using Windows.UI.Xaml;
using Uno.UI;
using Windows.UI.Xaml.Media;


namespace Windows.UI.Xaml.Controls
{
	public partial class BindableButtonEx : BindableButton, DependencyObject
	{

		public BindableButtonEx()
			: base(ContextHelper.Current)
		{
		}

		public bool IsEnabled
		{
			get
			{
				return base.Focusable;
			}
			set
			{
				base.Focusable = value;
			}
		}

		public Color ForegroundColor
		{
			get
			{
				return new Android.Graphics.Color(this.TextColors.DefaultColor);
			}
			set
			{
				this.SetTextColor(value);
			}
		}

		public new Brush Foreground
		{
			get
			{
				return new SolidColorBrush(ForegroundColor);
			}
			set
			{
				if (value == null)
				{
					this.SetTextColor(Colors.White);
					return;
				}
				var asColorBrush = value as SolidColorBrush;
				if (asColorBrush == null)
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn("Only SolidColorBrush is supported as Foreground.");
					}
					return;
				}
				ForegroundColor = asColorBrush.Color;
			}
		}
	}

}
