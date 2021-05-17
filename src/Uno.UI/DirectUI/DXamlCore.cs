using System;
using System.Collections.Generic;
using System.Text;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
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

	internal class BuildTreeService
	{
		public void RegisterWork(ITreeBuilder treeBuilder)
		{
			treeBuilder.IsRegisteredForCallbacks = true;
			CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.High, () =>
			{
				if (treeBuilder.IsBuildTreeSuspended)
				{
					RegisterWork(treeBuilder);
					return;
				}

				treeBuilder.IsRegisteredForCallbacks = false;

				var workerHasWorkLeft = treeBuilder.BuildTree();

				if (workerHasWorkLeft)
				{
					var workerReRegistered = treeBuilder.IsRegisteredForCallbacks;
					if (!workerReRegistered)
					{
						RegisterWork(treeBuilder);
					}
				}
			});
		}
	}

	internal interface ITreeBuilder
	{
		public bool IsBuildTreeSuspended { get; }

		public bool IsRegisteredForCallbacks { get; set; }

		public bool BuildTree();
	}
}
