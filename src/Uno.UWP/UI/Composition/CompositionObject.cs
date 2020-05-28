
using System;

namespace Windows.UI.Composition
{
	public partial class CompositionObject : global::System.IDisposable
	{
		private object _gate = new object();

		public Compositor Compositor => Compositor.Current;

		public void StartAnimation(string propertyName, CompositionAnimation animation)
		{
			StartAnimationCore(propertyName, animation);
		}

		internal virtual void StartAnimationCore(string propertyName, CompositionAnimation animation) { }

		public void StopAnimation(string propertyName)
		{

		}
	}
}
