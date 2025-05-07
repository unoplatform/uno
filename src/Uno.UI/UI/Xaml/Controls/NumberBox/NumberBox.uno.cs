using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class NumberBox
{
	private readonly SerialDisposable _eventSubscriptions = new();

	private void ReApplyTemplate()
	{
		// No need to reapply template on initial load.
		if (_eventSubscriptions.Disposable is null)
		{
			InitializeTemplate();
		}
	}

	private void DisposeRegistrations() => UnhookEvents();
}
