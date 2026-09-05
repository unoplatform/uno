#nullable enable

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TextBox
	{
		// Set only by this control's pointer/visibility overrides and read only for visual state, so it
		// stays control-owned; the core asks for it through ITextBoxHost.
		private bool _isPointerOver;

		bool ITextBoxHost.IsPointerOver => _isPointerOver;

		internal override void UpdateVisualState(bool useTransitions = true)
			=> _core.UpdateVisualStateCore(useTransitions);

		internal override string GetPlainText()
		{
			if (Header is not null)
			{
				var plainText = FrameworkElement.GetStringFromObject(Header);
				if (!string.IsNullOrEmpty(plainText))
				{
					return plainText;
				}
			}

			return PlaceholderText ?? string.Empty;
		}
	}
}
