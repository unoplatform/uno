using System;
using System.Linq;
using System.Windows.Input;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

namespace SamplesApp.Windows_UI_Xaml_Controls.Models
{
	[Bindable]
	internal class RotatedListViewViewModel : ViewModelBase
	{
		private Person _selectedItem = null;
		private int _selectedIndex = -1;


		public RotatedListViewViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			SampleItems = CreatePerson();
			AddItem = GetOrCreateCommand(AddPerson);
		}

		public ICommand AddItem { get; }

		private void AddPerson()
		{
			SampleItems = SampleItems
				.Concat(CreatePerson())
				.ToArray();

			RaisePropertyChanged("SampleItems");
		}

		public Person SelectedItem
		{
			get => _selectedItem;
			set
			{
				_selectedItem = value;
				RaisePropertyChanged("SelectedItem");
			}
		}

		public int SelectedIndex
		{
			get => _selectedIndex;
			set
			{
				_selectedIndex = value;
				RaisePropertyChanged("SelectedIndex");
			}
		}

		public Person[] SampleItems { get; set; }

		private Person[] CreatePerson()
		{
			return new Person[]
			{
				Person.Random()
			};
		}

		public class Person
		{
			private static Random _random = new Random();

			public static Person Random()
			{
				var p = new Person();

				p.FirstName = new string[] { "John", "Bob", "Roger", "Arnold" }[_random.Next(0, 3)];
				p.LastName = new string[] { "Smith", "Doe", "Moore", "Sloan", "Brooks" }[_random.Next(0, 4)];
				p.Age = _random.Next(18, 75);
				p.Country = new string[] { "Canada", "USA", "Great Britain", "Germany", "Australia", "South Africa" }[_random.Next(0, 5)];
				p.Picture = new Uri("https://picsum.photos/200/200/?image=" + _random.Next(1, 500));

				return p;
			}

			public string FirstName { get; private set; }
			public string LastName { get; private set; }
			public int Age { get; private set; }
			public string Country { get; private set; }
			public Uri Picture { get; private set; }
		}
	}
}
