namespace Microsoft.UI.Xaml.Data;

partial class CollectionViewSource
{
	/// <summary>
	/// Gets or sets a value that indicates whether source data is grouped.
	/// </summary>
	public bool IsSourceGrouped
	{
		get => (bool)GetValue(IsSourceGroupedProperty);
		set => SetValue(IsSourceGroupedProperty, value);
	}

	/// <summary>
	/// Identifies the IsSourceGrouped dependency property.
	/// </summary>
	public static DependencyProperty IsSourceGroupedProperty { get; } =
		DependencyProperty.Register(
			nameof(IsSourceGrouped),
			typeof(bool),
			typeof(CollectionViewSource),
			new FrameworkPropertyMetadata(defaultValue: false));

	/// <summary>
	/// Gets or sets the property path to follow from the top level item to find groups within the CollectionViewSource.
	/// </summary>
	public PropertyPath ItemsPath
	{
		get => (PropertyPath)GetValue(ItemsPathProperty);
		set => SetValue(ItemsPathProperty, value);
	}

	/// <summary>
	/// Identifies the ItemsPath dependency property.
	/// </summary>
	public static DependencyProperty ItemsPathProperty { get; } =
		DependencyProperty.Register(
			name: nameof(ItemsPath),
			propertyType: typeof(PropertyPath),
			ownerType: typeof(CollectionViewSource),
			typeMetadata: new FrameworkPropertyMetadata(defaultValue: null))
		);

	/// <summary>
	/// Gets or sets the collection object from which to create this view.
	/// </summary>
	public object Source
	{
		get => GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	/// <summary>
	/// Identifies the Source dependency property.
	/// </summary>
	public static DependencyProperty SourceProperty { get; } =
		DependencyProperty.Register(
			nameof(Source),
			typeof(object),
			typeof(CollectionViewSource),
			new FrameworkPropertyMetadata(defaultValue: null));

	/// <summary>
	/// Gets the view object that is currently associated with this instance of CollectionViewSource.
	/// </summary>
	public ICollectionView View
	{
		get => (ICollectionView)GetValue(ViewProperty);
		private set => SetValue(ViewProperty, value);
	}

	/// <summary>
	/// Identifies the View dependency property.
	/// </summary>
	public static DependencyProperty ViewProperty { get; } =
		DependencyProperty.Register(
			name: nameof(View),
			propertyType: typeof(ICollectionView),
			ownerType: typeof(CollectionViewSource),
			typeMetadata: new FrameworkPropertyMetadata(null)
		);
}
