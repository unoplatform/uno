#if !SILVERLIGHT
using System;
using System.Collections.Generic;
using System.Text;

#if NETFX_CORE
using Microsoft.UI.Xaml.Input;
using NativeInputScope = Microsoft.UI.Xaml.Input.InputScopeNameValue;
#else
using Microsoft.UI.Xaml.Input;
using NativeInputScope = Microsoft.UI.Xaml.Input.InputScopeNameValue;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public class InputScopes
	{
		public static InputScope Default { get { return Convert(NativeInputScope.Default); } }

		public static InputScope Number { get { return Convert(NativeInputScope.Number); } }
		public static InputScope NumericPin { get { return Convert(NativeInputScope.NumericPin); } }
		public static InputScope NumberFullWidth { get { return Convert(NativeInputScope.NumberFullWidth); } }
		public static InputScope Url { get { return Convert(NativeInputScope.Url); } }
		public static InputScope TelephoneNumber { get { return Convert(NativeInputScope.TelephoneNumber); } }
		public static InputScope Search { get { return Convert(NativeInputScope.Search); } }
		public static InputScope EmailSmtpAddress { get { return Convert(NativeInputScope.EmailSmtpAddress); } }

		private static InputScope Convert(NativeInputScope inputScopeNameValue)
		{
			var inputScope = new InputScope();
			inputScope.Names.Add(new InputScopeName(inputScopeNameValue));
			return inputScope;
		}
	}
}
#endif
