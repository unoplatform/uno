using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Common;
using Uno.UI.Samples.Controls;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Uno.UI.Samples.Content.UITests.FocusTests
{
	[Sample("Focus")]
	public sealed partial class FocusManager_FocusDirection : UserControl
	{
		public FocusManager_FocusDirection()
		{
			this.InitializeComponent();

			var buttonNext = NextButton as Button;
			var buttonPrevious = PreviousButton as Button;
			var buttonUp = UpButton as Button;
			var buttonDown = DownButton as Button;
			var buttonRight = RightButton as Button;
			var buttonLeft = LeftButton as Button;
			var buttonNone = NoneButton as Button;

			var options = new FindNextElementOptions
			{
				SearchRoot = this
			};
			buttonNext.Command = new DelegateCommand(() => Microsoft.UI.Xaml.Input.FocusManager.TryMoveFocus(Microsoft.UI.Xaml.Input.FocusNavigationDirection.Next, options));
			buttonPrevious.Command = new DelegateCommand(() => Microsoft.UI.Xaml.Input.FocusManager.TryMoveFocus(Microsoft.UI.Xaml.Input.FocusNavigationDirection.Previous, options));
			buttonUp.Command = new DelegateCommand(() => Microsoft.UI.Xaml.Input.FocusManager.TryMoveFocus(Microsoft.UI.Xaml.Input.FocusNavigationDirection.Up, options));
			buttonDown.Command = new DelegateCommand(() => Microsoft.UI.Xaml.Input.FocusManager.TryMoveFocus(Microsoft.UI.Xaml.Input.FocusNavigationDirection.Down, options));
			buttonRight.Command = new DelegateCommand(() => Microsoft.UI.Xaml.Input.FocusManager.TryMoveFocus(Microsoft.UI.Xaml.Input.FocusNavigationDirection.Right, options));
			buttonLeft.Command = new DelegateCommand(() => Microsoft.UI.Xaml.Input.FocusManager.TryMoveFocus(Microsoft.UI.Xaml.Input.FocusNavigationDirection.Left, options));
			buttonNone.Command = new DelegateCommand(() => Microsoft.UI.Xaml.Input.FocusManager.TryMoveFocus(Microsoft.UI.Xaml.Input.FocusNavigationDirection.None, options));
		}

	}
}
