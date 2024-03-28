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
		private Compositor? _compositor;

		internal CompositionCapabilities(Compositor? compositor) => _compositor = compositor;

		public CompositionCapabilities() : this(Compositor.GetSharedCompositor())
		{
		}

		public static CompositionCapabilities GetForCurrentView() => new CompositionCapabilities(Compositor.GetSharedCompositor());
	}
}
