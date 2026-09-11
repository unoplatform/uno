#nullable enable

using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace Microsoft.UI.Xaml
{
	public partial class FrameworkElement
	{
		#region DataContext DependencyProperty

		public object? DataContext
		{
			get => GetValue(DataContextProperty);
			set => SetValue(DataContextProperty, value);
		}

		[UnconditionalSuppressMessage("Trimming", "IL2111")]
		public static DependencyProperty DataContextProperty { get; } =
			DependencyProperty.Register(
				name: nameof(DataContext),
				propertyType: typeof(object),
				ownerType: typeof(FrameworkElement),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((FrameworkElement)s).OnDataContextChanged(e)
				)
		);

		public event TypedEventHandler<FrameworkElement, DataContextChangedEventArgs>? DataContextChanged;

		internal protected virtual void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
		{
			OnDataContextChangedPartial(e);
			DataContextChanged?.Invoke(this, new DataContextChangedEventArgs(DataContext));
		}

		partial void OnDataContextChangedPartial(DependencyPropertyChangedEventArgs e);

		#endregion
	}
}
