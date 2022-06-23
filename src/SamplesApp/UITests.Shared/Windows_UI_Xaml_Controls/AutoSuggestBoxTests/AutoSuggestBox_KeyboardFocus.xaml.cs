using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml_Controls.AutoSuggestBoxTests
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("AutoSuggestBox")]
    public sealed partial class AutoSuggestBox_KeyboardFocus : Page
    {
        public AutoSuggestBox_KeyboardFocus()
        {
            this.InitializeComponent();
			this.DataContext = new AutoSuggestBoxKeyboardFocusViewModel();
		}
	}

	internal class AutoSuggestBoxKeyboardFocusViewModel : Uno.UI.Samples.UITests.Helpers.ViewModelBase
	{
		public AutoSuggestBoxKeyboardFocusViewModel()
		{
			autocompletedPredictions = new List<AutocompletedPrediction>();
		}

		private string autoCompleteInput;

		public string AutoCompleteInput
		{
			get => autoCompleteInput; set
			{
				autoCompleteInput = value;
				RaisePropertyChanged("AutoCompleteInput");
				AutoCompletedPredictions = updateAutoCompletedPredictions();
			}
		}

		private List<AutocompletedPrediction> autocompletedPredictions;

		public List<AutocompletedPrediction> AutoCompletedPredictions
		{
			get => autocompletedPredictions; set
			{
				autocompletedPredictions = value;
				RaisePropertyChanged("AutoCompletedPredictions");
			}
		}

		private List<AutocompletedPrediction> updateAutoCompletedPredictions()
		{
			return
				String.IsNullOrEmpty(autoCompleteInput)
				? new List<AutocompletedPrediction>()
				: new AutocompletedPrediction[]
					{
						new AutocompletedPrediction("I am"),
						new AutocompletedPrediction("not a"),
						new AutocompletedPrediction("Legend."),
						new AutocompletedPrediction("I'm"),
						new AutocompletedPrediction("the"),
						new AutocompletedPrediction("BATMAN!")}.ToList();
		}
	}

	[Windows.UI.Xaml.Data.Bindable]
	public class AutocompletedPrediction
	{
		public AutocompletedPrediction(string description)
		{
			Description = description;
		}
		//public string PlaceId { get; }

		public string Description { get; }

		public override string ToString() => Description;
	}
}
