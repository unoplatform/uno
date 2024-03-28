#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Core;

partial class PointerCapture
{
	private IList<TouchesManager>? _currentManagers;

	partial void AddOptions(UIElement target, PointerCaptureOptions options)
	{
		if (options.HasFlag(PointerCaptureOptions.PreventOSSteal))
		{
			RemoveOptions(target, PointerCaptureOptions.PreventOSSteal);
			_currentManagers = TouchesManager.GetAllParents(target).ToList();

			foreach (var manager in _currentManagers)
			{
				manager.RegisterChildListener();
				manager.ManipulationStarted();
			}
		}
	}

	partial void RemoveOptions(UIElement target, PointerCaptureOptions options)
	{
		if (options.HasFlag(PointerCaptureOptions.PreventOSSteal))
		{
			// The 'target' will not change between Add and Remove, so we can use the same _currentManagers list.

			if (_currentManagers is null)
			{
				return;
			}

			foreach (var manager in _currentManagers)
			{
				manager.UnRegisterChildListener();
				manager.ManipulationEnded();
			}

			_currentManagers = null;
		}
	}
}
