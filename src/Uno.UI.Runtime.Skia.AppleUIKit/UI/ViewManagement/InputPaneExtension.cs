using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Input;
using Windows.UI.ViewManagement;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Extensions;

internal class InputPaneExtension : IInputPaneExtension
{
	public bool TryHide() => InputPaneInterop.TryHide();

	public bool TryShow() => InputPaneInterop.TryShow();
}
