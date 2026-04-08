using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

public sealed partial class When_Xaml_Collection_Add_9781 : UserControl
{
	public When_Xaml_Collection_Add_9781()
	{
		this.InitializeComponent();
	}
}

public interface ICollectionChild_9781 { }

[ContentProperty(Name = nameof(Sources))]
public class CollectionContentControl_9781 : Control, ICollectionChild_9781
{
	public static DependencyProperty SourcesProperty { get; } = DependencyProperty.Register(
		nameof(Sources),
		typeof(ObservableCollection<ICollectionChild_9781>),
		typeof(CollectionContentControl_9781),
		new PropertyMetadata(default(ObservableCollection<ICollectionChild_9781>), (s, e) => ((CollectionContentControl_9781)s).OnSourcesChanged(e)));

	public ObservableCollection<ICollectionChild_9781> Sources
	{
		get => (ObservableCollection<ICollectionChild_9781>)GetValue(SourcesProperty);
		set => SetValue(SourcesProperty, value);
	}

	/// <summary>
	/// The original collection created in the constructor. If XAML code-gen
	/// behaves correctly (calls .Add), this should remain the same instance
	/// as Sources after XAML initialization.
	/// </summary>
	public ObservableCollection<ICollectionChild_9781> OriginalCollection { get; }

	/// <summary>
	/// Tracks whether the collection was replaced (i.e., a new instance was assigned
	/// instead of items being added to the existing one).
	/// </summary>
	public bool WasCollectionReplaced { get; private set; }

	/// <summary>
	/// Count of Add notifications received via CollectionChanged on the original collection.
	/// </summary>
	public int AddNotificationCount { get; private set; }

	public CollectionContentControl_9781()
	{
		OriginalCollection = new ObservableCollection<ICollectionChild_9781>();
		OriginalCollection.CollectionChanged += OnOriginalCollectionChanged;
		Sources = OriginalCollection;
	}

	private void OnOriginalCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.Action == NotifyCollectionChangedAction.Add)
		{
			AddNotificationCount++;
		}
	}

	private void OnSourcesChanged(DependencyPropertyChangedEventArgs e)
	{
		if (e.OldValue != null && e.NewValue != null && !ReferenceEquals(e.OldValue, e.NewValue))
		{
			// The collection was replaced with a new instance
			WasCollectionReplaced = true;
		}
	}
}

public class CollectionChildControl_9781 : Control, ICollectionChild_9781 { }
