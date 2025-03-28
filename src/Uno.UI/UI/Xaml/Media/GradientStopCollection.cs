using System.Collections.Generic;

namespace Windows.UI.Xaml.Media
{
	internal delegate void GradientStopAddedOrRemovedEventHandler(GradientStop addedOrRemovedGradientStop);

	public sealed partial class GradientStopCollection : DependencyObjectCollection<GradientStop>, IList<GradientStop>, IEnumerable<GradientStop>
	{
		internal event GradientStopAddedOrRemovedEventHandler Removed;
		internal event GradientStopAddedOrRemovedEventHandler Added;

		private protected override void OnCollectionChanged()
		{
			base.OnCollectionChanged();
			this.InvalidateArrange();
		}

		private protected override void OnRemoved(GradientStop d)
		{
			base.OnRemoved(d);
			Removed?.Invoke(d);
		}

		private protected override void OnAdded(GradientStop d)
		{
			base.OnAdded(d);
			Added?.Invoke(d);
		}
	}
}
