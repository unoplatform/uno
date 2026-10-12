using System;
using System.ComponentModel;
using SamplesApp;

namespace UITests.Windows_UI_Xaml_Controls.AutoSuggestBoxTests;

public class Author
{
	public static Author[] All = new Author[]
	{
		new Author { Name = "A0" },
		new Author { Name = "A1" },
		new Author { Name = "A2" },
		new Author { Name = "A3" },
		new Author { Name = "B0" },
		new Author { Name = "B1" },
		new Author { Name = "B2" },
		new Author { Name = "B3" },
		new Author { Name = "a0" },
		new Author { Name = "a1" },
		new Author { Name = "a2" },
		new Author { Name = "a3" },
	};

	public string Name { get; set; } = string.Empty;

	public override string ToString()
	{
		return Name;
	}
}

public class Book
{
	private Author author;

	public Author Author
	{
		get => author;
		set
		{
			author = value;

			AuthorChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public event EventHandler AuthorChanged;
}
