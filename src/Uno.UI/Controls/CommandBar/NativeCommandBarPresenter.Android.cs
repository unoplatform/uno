#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Uno.Collections;
using Uno.UI;
using Uno.UI.Extensions;

namespace Uno.UI.Controls
{
	public partial class NativeCommandBarPresenter : ContentPresenter
	{
		private protected override void OnLoaded()
		{
			base.OnLoaded();

			var commandBar = this.GetTemplatedParent() as CommandBar;
			Content = commandBar?.GetRenderer(() => new CommandBarRenderer(commandBar)).Native;
		}
	}
}
