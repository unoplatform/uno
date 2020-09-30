using System;
using System.Linq;
using Windows.UI.Core;

namespace DirectUI
{
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
}
