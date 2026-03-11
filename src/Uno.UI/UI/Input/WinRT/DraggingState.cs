#if IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input;
#else
namespace Windows.UI.Input;
#endif

public enum DraggingState
{
	Started,
	Continuing,
	Completed,
}
