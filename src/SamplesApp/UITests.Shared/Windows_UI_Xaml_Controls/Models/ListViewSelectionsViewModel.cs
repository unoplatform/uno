using System.Collections.Generic;
using System.Windows.Input;
using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

namespace Uno.UI.Samples.Presentation.SamplePages
{
	internal class ListViewSelectionsViewModel : ViewModelBase
	{
		private string _selectedName = "";

		public ListViewSelectionsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
		}

		public string SelectedName
		{
			get => _selectedName;
			set
			{
				_selectedName = value;
				RaisePropertyChanged();
			}
		}

		public ICommand ClearSelection => GetOrCreateCommand(() => SelectedName = "");

		private string[] GetSampleNames()
		{
			return new[]
			{
				"Keith",
				"Allison",
				"Iris",
				"Michelle",
				"Isidore",
				"Lili",
				"Fabian",
				"Isabel",
				"Juan",
				"Charley",
				"Frances",
				"Ivan",
				"Jeanne",
				"Dennis",
				"Katrina",
				"Rita",
				"Stan",
				"Wilma",
				"Dean",
				"Felix",
				"Noel",
				"Gustav",
				"Ike",
				"Paloma"
			};
		}

		public List<TwoLevelListItem> TwoLevelsItemList
		{
			get
			{
				return new List<TwoLevelListItem>
				{
					new TwoLevelListItem { SubLevelItems = new List<string> { "1a", "1b" } },
					new TwoLevelListItem { SubLevelItems = new List<string> { "2a", "2b", "2c" } }
				};
			}
		}
	}
}
