#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

public sealed partial class TreeViewSelectionTestPage : Page
{
	public List<string> SelectionChangedLogs { get; private set; } = new();

	public TreeViewSelectionTestPage()
	{
		this.InitializeComponent();
		DataContext = new TreeViewSelectionTestPageVM();
	}

	private void TreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
	{
		SelectionChangedLogs.Add($"Added: {args.AddedItems.Count}, Removed: {args.RemovedItems.Count}");
	}
}

public partial class TreeViewSelectionTestPageVM : INotifyPropertyChanged
{
	private List<ElementItem>? _elements;
	private ElementItem? _selectedElement;
	private string? _name;

	public List<ElementItem>? Elements
	{
		get => _elements;
		set
		{
			if (!EqualityComparer<List<ElementItem>?>.Default.Equals(_elements, value))
			{
				_elements = value;

				PropertyChanged?.Invoke(this, new(nameof(Elements)));
			}
		}
	}

	public ElementItem? SelectedElement
	{
		get => _selectedElement;
		set
		{
			if (_selectedElement != value)
			{
				_selectedElement = value;

				PropertyChanged?.Invoke(this, new(nameof(SelectedElement)));
			}
		}
	}

	public string? Name
	{
		get => _name;
		set
		{
			if (_name != value)
			{
				_name = value;

				PropertyChanged?.Invoke(this, new(nameof(Name)));
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public TreeViewSelectionTestPageVM()
	{
		Elements = new List<ElementItem>
		{
			new ElementItem
			{
				Name = "Element 1",
				Children = new List<ElementItem>
				{
					new ElementItem
					{
						Name = "SubElement 1.1",
						Children = new List<ElementItem>
						{
							new ElementItem
							{
								Name = "SubElement 1.1.1 Selected",
								IsSelected = true,
								Children = new List<ElementItem>
								{
									new ElementItem
									{
										Name = "SubElement 1.1.1.1",
										Children = new List<ElementItem>
										{
											new ElementItem
											{
												Name = "SubElement 1.1.1.1.1"
											},
											new ElementItem
											{
												Name = "SubElement 1.1.1.1.2"
											}
										}
									},
									new ElementItem
									{
										Name = "SubElement 1.1.1.2"
									}
								}
							},
							new ElementItem
							{
								Name = "SubElement 1.1.2"
							}
						}
					},
					new ElementItem
					{
						Name = "SubElement 1.2"
					}
				}
			},
			new ElementItem
			{
				Name = "Element 2",
				Children = new List<ElementItem>
				{
					// ...
				}
			}
		};

		SelectedElement = Elements[0].Children![0].Children![0];
	}
}

public partial class ElementItem : INotifyPropertyChanged
{
	private string? _name;
	private bool _isSelected;
	private List<ElementItem>? _children;

	public string? Name
	{
		get => _name;
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_name, value))
			{
				_name = value;
				PropertyChanged?.Invoke(this, new(nameof(Name)));
			}
		}
	}

	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isSelected, value))
			{
				_isSelected = value;
				PropertyChanged?.Invoke(this, new(nameof(IsSelected)));
			}
		}
	}

	public List<ElementItem>? Children
	{
		get => _children;
		set
		{
			if (!EqualityComparer<List<ElementItem>?>.Default.Equals(_children, value))
			{
				_children = value;
				PropertyChanged?.Invoke(this, new(nameof(Children)));
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;
}

