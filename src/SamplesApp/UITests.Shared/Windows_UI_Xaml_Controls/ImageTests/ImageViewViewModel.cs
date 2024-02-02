using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests;

internal class ImageViewViewModel : ViewModelBase
{
	public IEnumerable<Item> Items { get; private set; }

	public ImageViewViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
	{
		Items = new List<Item>()
		{
			new() { Title = "Item 1", Subtitle = "Subtitle 1", ImageUrl = "ms-appx:///Assets/Formats/couch.svg" },
			new() { Title = "Item 2", Subtitle = "Subtitle 2", ImageUrl = "ms-appx:///Assets/Formats/home.svg" },
			new() { Title = "Item 3", Subtitle = "Subtitle 3", ImageUrl = "ms-appx:///Assets/Formats/couch.svg" },
			new() { Title = "Item 4", Subtitle = "Subtitle 4", ImageUrl = "ms-appx:///Assets/Formats/home.svg" },
			new() { Title = "Item 5", Subtitle = "Subtitle 5", ImageUrl = "ms-appx:///Assets/Formats/couch.svg" },
			new() { Title = "Item 6", Subtitle = "Subtitle 6", ImageUrl = null },
			new() { Title = "Item 7", Subtitle = "Subtitle 7", ImageUrl = "ms-appx:///Assets/Formats/couch.svg" },
			new() { Title = "Item 8", Subtitle = "Subtitle 8", ImageUrl = "ms-appx:///Assets/Formats/home.svg" },
			new() { Title = "Item 9", Subtitle = "Subtitle 9", ImageUrl = "ms-appx:///Assets/Formats/couch.svg" },
			new() { Title = "Item 10", Subtitle = "Subtitle 10", ImageUrl = "ms-appx:///Assets/Formats/home.svg" },
			new() { Title = "Item 11", Subtitle = "Subtitle 11", ImageUrl = "ms-appx:///Assets/Formats/couch.svg" },
			new() { Title = "Item 12", Subtitle = "Subtitle 12", ImageUrl = "ms-appx:///Assets/Formats/home.svg" },
			new() { Title = "Item 13", Subtitle = "Subtitle 13", ImageUrl = null },
			new() { Title = "Item 14", Subtitle = "Subtitle 14", ImageUrl = "ms-appx:///Assets/Formats/home.svg" },
			new() { Title = "Item 15", Subtitle = "Subtitle 15", ImageUrl = "ms-appx:///Assets/Formats/couch.svg" },
			new() { Title = "Item 16", Subtitle = "Subtitle 16", ImageUrl = "ms-appx:///Assets/Formats/home.svg" },
			new() { Title = "Item 17", Subtitle = "Subtitle 17", ImageUrl = "ms-appx:///Assets/Formats/couch.svg" },
			new() { Title = "Item 18", Subtitle = "Subtitle 18", ImageUrl = "ms-appx:///Assets/Formats/home.svg" },
			new() { Title = "Item 19", Subtitle = "Subtitle 19", ImageUrl = "ms-appx:///Assets/Formats/couch.svg" },
			new() { Title = "Item 20", Subtitle = "Subtitle 20", ImageUrl = null },
			new() { Title = "Item 21", Subtitle = "Subtitle 21", ImageUrl = "ms-appx:///Assets/Formats/couch.svg" },
			new() { Title = "Item 22", Subtitle = "Subtitle 22", ImageUrl = "ms-appx:///Assets/Formats/home.svg" },

		};
	}
}

internal class Item
{
	public string Title { get; set; }
	public string Subtitle { get; set; }
	public string ImageUrl { get; set; }
}

