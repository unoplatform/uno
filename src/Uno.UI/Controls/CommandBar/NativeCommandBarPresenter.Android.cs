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
		private protected override void OnLoaded()
		{
			base.OnLoaded();

			var commandBar = TemplatedParent as CommandBar;
			Content = commandBar?.GetRenderer(() => new CommandBarRenderer(commandBar)).Native;
		}
	}
}
