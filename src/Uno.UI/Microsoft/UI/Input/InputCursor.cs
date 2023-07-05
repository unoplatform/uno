#nullable disable

namespace Microsoft.UI.Input;

#if HAS_UNO_WINUI
public partial class InputCursor
#else
internal partial class InputCursor
#endif
{
	protected InputCursor()
	{
	}
}
