using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Microsoft.UI.Input;
public partial class InputNonClientPointerSource
{
	public static InputNonClientPointerSource GetForWindowId(WindowId windowId)
	{
		// UNO TODO: Port InputNonClientPointerSource properly from WinUI
		return new InputNonClientPointerSource();
	}

	public event TypedEventHandler<InputNonClientPointerSource, NonClientPointerEventArgs> PointerEntered;
	public event TypedEventHandler<InputNonClientPointerSource, NonClientPointerEventArgs> PointerExited;
	public event TypedEventHandler<InputNonClientPointerSource, NonClientPointerEventArgs> PointerMoved;
	public event TypedEventHandler<InputNonClientPointerSource, NonClientPointerEventArgs> PointerPressed;
	public event TypedEventHandler<InputNonClientPointerSource, NonClientPointerEventArgs> PointerReleased;
	public event TypedEventHandler<InputNonClientPointerSource, NonClientRegionsChangedEventArgs> RegionsChanged;
	public event TypedEventHandler<InputNonClientPointerSource, EnteredMoveSizeEventArgs> EnteredMoveSize;
	public event TypedEventHandler<InputNonClientPointerSource, EnteringMoveSizeEventArgs> EnteringMoveSize;
	public event TypedEventHandler<InputNonClientPointerSource, ExitedMoveSizeEventArgs> ExitedMoveSize;
}
