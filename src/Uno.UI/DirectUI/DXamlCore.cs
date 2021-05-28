using System;
using System.Collections.Generic;
using System.Text;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DirectUI
{
	internal class DXamlCore
	{
		private static DXamlCore _current;
		private ElementSoundPlayerService _elementSoundPlayerServiceNoRef;
		private BuildTreeService _buildTreeService;
		private BudgetManager _budgetManager;

		public static DXamlCore GetCurrent()
			=> _current ??= new DXamlCore();

		// UNO: This should **NOT** create the singleton!
		//		_but_ if we do return a 'null' the 'OnApplyTemplate' of the `CalendarView` will fail.
		//		As for now our implementation of the 'DXamlCore' is pretty light and stored as a basic singleton,
		//		we accept to create it even with the "NoCreate" overload.
		public static DXamlCore GetCurrentNoCreate()
			=> _current ??= new DXamlCore();

		public string GetLocalizedResourceString(string key)
		{
			var loader = ResourceLoader.GetForCurrentView();
			return loader.GetString(key);
		}

		public ElementSoundPlayerService GetElementSoundPlayerServiceNoRef()
			=> _elementSoundPlayerServiceNoRef ??= new ElementSoundPlayerService();

		public BuildTreeService GetBuildTreeService()
			=> _buildTreeService ??= new BuildTreeService();

		public BudgetManager GetBudgetManager()
			=> _budgetManager ??= new BudgetManager();
	}
}
