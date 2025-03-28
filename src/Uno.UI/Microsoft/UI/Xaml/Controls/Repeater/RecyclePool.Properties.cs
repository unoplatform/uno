using System;
using System.Linq;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	partial class RecyclePool
	{
		public static DependencyProperty PoolInstanceProperty { get; } = DependencyProperty.RegisterAttached(
			"PoolInstance", typeof(RecyclePool), typeof(RecyclePool), new FrameworkPropertyMetadata(default(RecyclePool)));

		public static RecyclePool GetPoolInstance(DataTemplate dataTemplate)
			=> (RecyclePool)dataTemplate.GetValue(PoolInstanceProperty);

		public static void SetPoolInstance(DataTemplate dataTemplate, RecyclePool value)
			=> dataTemplate.SetValue(PoolInstanceProperty, value);
	}
}
