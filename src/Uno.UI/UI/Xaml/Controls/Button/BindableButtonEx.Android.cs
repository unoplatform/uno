using Android.Views;
using Uno.UI.DataBinding;
using Uno.UI.Controls;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Android.Graphics;
using Microsoft.UI.Xaml;
using Uno.UI;
using Microsoft.UI.Xaml.Media;


namespace Microsoft.UI.Xaml.Controls
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
				return new AColor(this.TextColors.DefaultColor);
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
