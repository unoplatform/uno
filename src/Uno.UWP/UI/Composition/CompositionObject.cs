#nullable enable

using System;
using Windows.Foundation.Metadata;
using Windows.UI.Core;

namespace Windows.UI.Composition
{
	public partial class CompositionObject : global::System.IDisposable
	{
		private object _gate = new object();

		internal CompositionObject()
		{
			ApiInformation.TryRaiseNotImplemented(GetType().FullName ?? "", "The compositor constructor is not available, as the type is not implemented");
			Compositor = new Compositor();
		}

		internal CompositionObject(Compositor compositor)
		{
			Compositor = compositor;
		}

		public Compositor Compositor { get; }

		public CoreDispatcher Dispatcher => CoreDispatcher.Main;

		public string? Comment { get; set; }

		public void StartAnimation(string propertyName, CompositionAnimation animation)
		{
			StartAnimationCore(propertyName, animation);
		}

		private protected virtual void StartAnimationCore(string propertyName, CompositionAnimation animation) { }

		public void StopAnimation(string propertyName)
		{
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private protected virtual void Dispose(bool isDispose)
		{
		}

		~CompositionObject()
		{
			Dispose(false);
		}
	}
}
