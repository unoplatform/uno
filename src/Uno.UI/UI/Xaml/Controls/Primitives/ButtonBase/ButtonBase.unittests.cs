using Uno;
using Uno.Client;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading;

using View = Windows.UI.Xaml.UIElement;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class ButtonBase : ContentControl
	{
		private readonly SerialDisposable _touchSubscription = new SerialDisposable();
		private readonly SerialDisposable _isEnabledSubscription = new SerialDisposable();

		partial void OnUnloadedPartial()
		{
			_isEnabledSubscription.Disposable = null;
			_touchSubscription.Disposable = null;
		}
	}
}
