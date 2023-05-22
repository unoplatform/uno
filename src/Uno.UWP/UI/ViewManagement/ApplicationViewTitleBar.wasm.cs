#if __WASM__
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Foundation;

#if NET7_0_OR_GREATER
using NativeMethods = __Windows.UI.ViewManagement.ApplicationViewTitleBar.NativeMethods;
#endif

namespace Windows.UI.ViewManagement
{
	public partial class ApplicationViewTitleBar
	{
#if !NET7_0_OR_GREATER
		private const string JsClassName = "Windows.UI.ViewManagement.ApplicationViewTitleBar";
#endif

		private Color? _backgroundColor;

		public Color? BackgroundColor
		{
			get => _backgroundColor;
			set
			{
				if (_backgroundColor != value)
				{
					_backgroundColor = value;
					UpdateBackgroundColor();
				}
			}
		}

		private void UpdateBackgroundColor()
		{
#if NET7_0_OR_GREATER
			NativeMethods.SetBackgroundColor(_backgroundColor?.ToHexString());
#else
			string colorString = "null";
			if (_backgroundColor != null)
			{
				colorString = $"\"{_backgroundColor.Value.ToHexString()}\"";
			}
			var command = $"{JsClassName}.setBackgroundColor({colorString})";
			WebAssemblyRuntime.InvokeJS(command);
#endif
		}
	}
}
#endif
