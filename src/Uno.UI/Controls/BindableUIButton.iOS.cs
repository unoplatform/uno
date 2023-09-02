using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using ObjCRuntime;

using Uno.Foundation.Logging;

using Foundation;
using UIKit;

namespace Uno.UI.Views.Controls
{
	/// <summary>
	/// Bindable version of native UIButton
	/// </summary>
	public partial class BindableUIButton : UIButton, DependencyObject
	{

		public BindableUIButton()
		{
			InitializeBinder();
		}

		public BindableUIButton(NativeHandle handle)
			: base(handle)
		{
			InitializeBinder();
		}

		public BindableUIButton(System.Drawing.RectangleF frame)
			: base(frame)
		{
			InitializeBinder();
		}

		public BindableUIButton(NSCoder coder)
			: base(coder)
		{
			InitializeBinder();
		}

		public BindableUIButton(NSObjectFlag t)
			: base(t)
		{
			InitializeBinder();
		}

		string _text;

		public string Text
		{
			get { return _text; }
			set
			{
				if (_text != value)
				{
					_text = value;
					UpdateLabel();
				}
			}
		}

		protected virtual void UpdateLabel()
		{
			base.SetTitle(_text, UIControlState.Normal);
		}


		public Brush Foreground
		{
			get { return new SolidColorBrush(TitleColor(UIControlState.Normal)); }
			set
			{
				if (value == null)
				{
					SetTitleColor(Windows.UI.Colors.Black, UIControlState.Normal);
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
				SetTitleColor(asColorBrush.Color, UIControlState.Normal);
			}
		}
	}
}
