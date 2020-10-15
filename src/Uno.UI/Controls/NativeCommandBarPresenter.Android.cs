#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using Windows.UI.Xaml;
using System.IO;
using Uno.Collections;

namespace Uno.UI.Controls
{
	public partial class NativeCommandBarPresenter : ContentPresenter
	{
		private CommandBar? _commandBar;

		private protected override void OnLoaded()
		{
			base.OnLoaded();
			
			_commandBar = TemplatedParent as CommandBar;
			Content = _commandBar?.GetRenderer(() => new CommandBarRenderer(_commandBar)).Native;
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			var renderer = _commandBar?.GetRenderer((Func<CommandBarRenderer>?)null);
			if (renderer != null)
			{
				renderer.Native = null!;
			}

			_commandBar = null;
		}
	}
}
