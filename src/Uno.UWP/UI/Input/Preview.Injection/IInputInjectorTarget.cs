#nullable enable
using System;
using System.Linq;
using Windows.UI.Core;

namespace Windows.UI.Input.Preview.Injection;

internal interface IInputInjectorTarget
{
	void InjectPointerAdded(PointerEventArgs args);

	void InjectPointerUpdated(PointerEventArgs args);

	void InjectPointerRemoved(PointerEventArgs args);
}
