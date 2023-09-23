#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.UI.Composition
{
	public partial class CompositionCapabilities
	{
		public bool AreEffectsSupported() => true;

		public bool AreEffectsFast() => _compositor?.IsSoftwareRenderer is not true;
	}
}
