using Uno;
using Uno.Client;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading;

using View = Microsoft.UI.Xaml.UIElement;

namespace Microsoft.UI.Xaml.Controls.Primitives
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

		/// <summary>
		/// Gets the native UI Control, if any.
		/// </summary>
		private View GetUIControl()
		{
			return
				// Check for non-templated ContentControl root (ContentPresenter bypass)
				ContentTemplateRoot

				// Finally check for templated ContentControl root
				?? TemplatedRoot;
		}
	}
}
