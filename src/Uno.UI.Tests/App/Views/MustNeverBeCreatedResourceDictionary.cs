using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Uno.UI.Tests.App.Views
{
	public class MustNeverBeCreatedResourceDictionary : ResourceDictionary
	{
		public MustNeverBeCreatedResourceDictionary() => throw new InvalidOperationException("This type exists to validate the lazy initialization of theme dictionaries in resource dictionaries. It must not be materialized.");
	}
}
