using Uno.UI.Xaml;

namespace Windows.UI.Xaml.Controls
{
	public partial class Control
	{
		public Control() : this("div") { }

		internal Control(string htmlTag) : base(htmlTag)
		{
			InitializeControl();
		}

		partial void OnIsFocusableChanged()
		{
			var isFocusable = IsFocusable && !IsDelegatingFocusToTemplateChild();

			WindowManagerInterop.SetIsFocusable(HtmlId, isFocusable);
		}

		private protected virtual bool IsDelegatingFocusToTemplateChild() => false;
	}
}
