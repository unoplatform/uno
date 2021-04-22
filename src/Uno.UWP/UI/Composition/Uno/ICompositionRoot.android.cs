using System;
using System.Linq;
using Windows.UI.Composition;
using Android.Views;

namespace Uno.UI.Composition
{
	internal interface ICompositionRoot
	{
		Window Window { get; }

		//View Content { get; }

		Compositor Compositor { get; }
	}
}
