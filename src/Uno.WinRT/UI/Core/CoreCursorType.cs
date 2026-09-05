using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Core
{
	/// <summary>Specifies the set of cursor types.</summary>
	[WebHostHidden]
	[ContractVersion(typeof(UniversalApiContract), 65536U)]
	public enum CoreCursorType
	{
		/// <summary>The left-upward (northwest) arrow Windows cursor.</summary>
		Arrow,

		/// <summary>The "cross" Windows cursor.</summary>
		Cross,

		/// <summary>A custom cursor.</summary>
		Custom,

		/// <summary>The "hand" Windows cursor.</summary>
		Hand,

		/// <summary>The left-upward (northwest) arrow Windows cursor with a question mark.</summary>
		Help,

		/// <summary>The "I"-shaped Windows cursor used for text selection.</summary>
		IBeam,

		/// <summary>The "cross arrow" Windows cursor used for user interface (UI) element sizing.</summary>
		SizeAll,

		/// <summary>The "right-upward, left-downward" dual arrow Windows cursor often used for element sizing.</summary>
		SizeNortheastSouthwest,

		/// <summary>The up-down dual arrow Windows cursor often used for vertical (height) sizing.</summary>
		SizeNorthSouth,

		/// <summary>The "left-upward, right-downward" dual arrow Windows cursor often used for element sizing.</summary>
		SizeNorthwestSoutheast,

		/// <summary>The left-right dual arrow Windows cursor often used for horizontal (width) sizing.</summary>
		SizeWestEast,

		/// <summary>The red "circle slash" Windows cursor often used to indicate that a UI behavor cannot be performed.</summary>
		UniversalNo,

		/// <summary>The up arrow Windows cursor.</summary>
		UpArrow,

		/// <summary>The cycling Windows "wait" cursor often used to indicate that an element or behavior is in a wait state and cannot respond at the time.</summary>
		Wait,

		/// <summary>The "hand" Windows cursor with a pin symbol.</summary>
		[ContractVersion("Windows.Foundation.UniversalApiContract", 327680U)]
		Pin,

		/// <summary>The "hand" Windows cursor with a person symbol.</summary>
		[ContractVersion("Windows.Foundation.UniversalApiContract", 327680U)]
		Person,
	}
}
