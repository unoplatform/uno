using Uno;
using Uno.Client;
using Uno.Extensions;
using Uno.Logging;
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

		protected override void OnLoaded()
		{
			base.OnLoaded();
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();
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
