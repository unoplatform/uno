using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia
{
	public class TizenApplicationExtension : IApplicationExtension
	{
		private readonly Windows.UI.Xaml.Application _owner;
		private readonly Windows.UI.Xaml.IApplicationEvents _ownerEvents;

		public TizenApplicationExtension(object owner)
		{
			_owner = (Application)owner;
			_ownerEvents = (IApplicationEvents)owner;
		}

		public ApplicationTheme GetDefaultSystemTheme() => ApplicationTheme.Light;
	}
}
