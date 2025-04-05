using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Views.InputMethods;
using Uno.UI;
using Windows.UI.Input;
using Windows.UI.ViewManagement;

namespace Uno.WinUI.Runtime.Skia.Android;

internal class InputPaneExtension : IInputPaneExtension
{
	public bool TryShow() => InputPaneInterop.TryShow(ContextHelper.Current as Activity);

	public bool TryHide() => InputPaneInterop.TryHide(ContextHelper.Current as Activity);
}
