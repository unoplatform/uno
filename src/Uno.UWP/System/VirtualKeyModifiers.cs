using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.System
{
	/// <summary>Specifies the virtual key used to modify another keypress. For example, the Ctrl key when pressed in conjunction with another key, as in Ctrl+C.</summary>
	[Flags]
	[ContractVersion(typeof(UniversalApiContract), 65536U)]
	public enum VirtualKeyModifiers : uint
	{
		/// <summary>No virtual key modifier.</summary>
		None = 0U,
		/// <summary>The Ctrl (control) virtual key.</summary>
		Control = 1U,
		/// <summary>The Menu virtual key.</summary>
		Menu = 2U,
		/// <summary>The Shift virtual key.</summary>
		Shift = 4U,
		/// <summary>The Windows virtual key.</summary>
		Windows = 8U,
	}
}
