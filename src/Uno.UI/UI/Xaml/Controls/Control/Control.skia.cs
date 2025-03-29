using System;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Control
	{
		internal static Action<Control, bool> OnIsFocusableChangedCallback { get; set; }

		public Control()
		{
			InitializeControl();
		}

		partial void OnIsFocusableChanged()
		{
			if (OnIsFocusableChangedCallback is { } callback)
			{
				var isFocusable = IsFocusable && !IsDelegatingFocusToTemplateChild();
				callback.Invoke(this, isFocusable);
			}
		}
	}
}
