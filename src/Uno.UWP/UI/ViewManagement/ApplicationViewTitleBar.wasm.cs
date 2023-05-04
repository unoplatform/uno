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
		private const string JsClassName = "Windows.UI.ViewManagement.ApplicationViewTitleBar";
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
			string colorString = "null";
			if (_backgroundColor != null)
			{
				colorString = $"\"{_backgroundColor.Value.ToHexString()}\"";
			}
			var command = $"{JsClassName}.setBackgroundColor({colorString})";
			WebAssemblyRuntime.InvokeJS(command);
		}
	}
}
#endif
