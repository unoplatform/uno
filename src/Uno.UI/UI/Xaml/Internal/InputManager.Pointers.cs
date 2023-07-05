using System;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.Xaml.Core;

partial class InputManager
{
	void IInputInjectorTarget.InjectPointerAdded(PointerEventArgs args) => InjectPointerAdded(args);
	partial void InjectPointerAdded(PointerEventArgs args);

	void IInputInjectorTarget.InjectPointerUpdated(PointerEventArgs args) => InjectPointerUpdated(args);
	partial void InjectPointerUpdated(PointerEventArgs args);

	void IInputInjectorTarget.InjectPointerRemoved(PointerEventArgs args) => InjectPointerRemoved(args);
	partial void InjectPointerRemoved(PointerEventArgs args);
}
