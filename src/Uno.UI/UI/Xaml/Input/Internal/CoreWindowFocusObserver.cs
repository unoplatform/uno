using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Xaml.Input;

internal class CoreWindowFocusObserver : FocusObserver
{
	internal CoreWindowFocusObserver(ContentRoot contentRoot) : base(contentRoot)
	{
	}
}
