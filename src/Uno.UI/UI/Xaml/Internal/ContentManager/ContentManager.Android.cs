#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	partial void SetupCoreWindowRootVisualPlatform(RootVisual rootVisual)
	{
		ApplicationActivity.Instance?.SetContentView(_rootVisual);
	}
}
