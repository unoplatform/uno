using Uno.UI.Xaml;

namespace Windows.UI.Xaml.Input;

/// <summary>
/// Represents a keyboard shortcut (or accelerator) that lets a user perform 
/// an action using the keyboard instead of navigating the app UI (directly or through access keys).
/// Accelerators are typically assigned to buttons or menu items.
/// </summary>
public partial class KeyboardAccelerator : DependencyObject
{
#if HAS_UNO // TODO: Uno specific - workaround for the lack of support for Enter/Leave on DOs.
	private ParentVisualTreeListener _parentVisualTreeListener;
#endif
	/// <summary>
	/// Initializes a new instance of the KeyboardAccelerator class.
	/// </summary>
	public KeyboardAccelerator()
	{
#if HAS_UNO
		_parentVisualTreeListener = new ParentVisualTreeListener(this);
		_parentVisualTreeListener.ParentLoaded += (s, e) => EnterImpl(null, new EnterParams(true));
#endif
	}
}
