using System;
using System.Linq;
using Android.Views;

namespace Uno.UI.Composition
{
	internal interface ICompositionRoot
	{
		Window Window { get; }

		View Content { get; }
	}
}
