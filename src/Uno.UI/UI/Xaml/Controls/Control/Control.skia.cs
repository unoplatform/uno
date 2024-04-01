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
			if (OperatingSystem.IsBrowser())
			{
				var isFocusable = IsFocusable && !IsDelegatingFocusToTemplateChild();
				Console.WriteLine($"Calling is focusable changed callback of {Visual.Handle} with {isFocusable}");
				OnIsFocusableChangedCallback?.Invoke(this, isFocusable);
			}
		}
	}
}
