#nullable enable

namespace Microsoft.UI.Xaml.Controls
{
	// Functional Skia implementations of the shared "copying/cutting to clipboard" event args. The
	// generated parameterless internal constructors are compiled out for Skia (see the generated
	// files), so these partials own the constructors and the Handled state. TextBox does not raise
	// these events today, so un-stubbing them for Skia does not change TextBox behavior.

	public partial class TextControlCopyingToClipboardEventArgs
	{
		internal TextControlCopyingToClipboardEventArgs()
		{
		}

		/// <summary>
		/// Gets or sets a value that marks the routed event as handled. A true value prevents the
		/// default copy-to-clipboard behavior.
		/// </summary>
		public bool Handled { get; set; }
	}

	public partial class TextControlCuttingToClipboardEventArgs
	{
		internal TextControlCuttingToClipboardEventArgs()
		{
		}

		/// <summary>
		/// Gets or sets a value that marks the routed event as handled. A true value prevents the
		/// default cut-to-clipboard behavior.
		/// </summary>
		public bool Handled { get; set; }
	}
}
