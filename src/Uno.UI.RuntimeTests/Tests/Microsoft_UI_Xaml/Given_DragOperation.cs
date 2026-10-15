#if HAS_UNO
#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml;

[TestClass]
public class Given_DragOperation
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Host_Takes_Drag_Then_Entered_Target_Gets_Leave_Before_Completion()
	{
		var (operation, target, host, completions) = Create();
		Assert.IsNotNull(host.OnCompleted, "the host must be offered the drag when it starts");

		// The pointer still reports in-window positions until the platform converts the grab.
		operation.Moved(new Source(2, new Point(10, 10)));
		await WaitFor(() => target.Enters == 1);

		// The platform drag ends elsewhere: the entered target leaves exactly once, then the result is reported.
		host.OnCompleted!(DataPackageOperation.Copy);
		await WaitFor(() => completions.Count == 1);

		Assert.AreEqual(1, target.Leaves);
		Assert.AreEqual(0, target.Drops);
		Assert.AreEqual(DataPackageOperation.Copy, completions[0]);
		Assert.IsTrue(target.LeaveBeforeCompletion);

		// Nothing happens again for a late duplicate.
		host.OnCompleted!(DataPackageOperation.None);
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(1, target.Leaves);
		Assert.AreEqual(1, completions.Count);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Host_Takes_Drag_Without_Entered_Target_Then_Completion_Has_No_Leave()
	{
		var (_, target, host, completions) = Create();

		host.OnCompleted!(DataPackageOperation.Move);
		await WaitFor(() => completions.Count == 1);

		Assert.AreEqual(0, target.Enters);
		Assert.AreEqual(0, target.Leaves);
		Assert.AreEqual(DataPackageOperation.Move, completions[0]);
	}

	private static (DragOperation operation, Target target, Host host, System.Collections.Generic.List<DataPackageOperation> completions) Create()
	{
		var xamlRoot = TestServices.WindowHelper.XamlRoot;
		var inputManager = xamlRoot.VisualTree.ContentRoot.InputManager;
		var target = new Target();
		var host = new Host();
		var package = new DataPackage();
		package.SetText("dragged");
		var info = new CoreDragInfo(new Source(1, new Point(10, 10)), package.GetView(), DataPackageOperation.Copy | DataPackageOperation.Move);
		var completions = new System.Collections.Generic.List<DataPackageOperation>();
		info.RegisterCompletedCallback(result => completions.Add(result));
		var operation = new DragOperation(inputManager, host, info, target);
		return (operation, target, host, completions);
	}

	private static async Task WaitFor(Func<bool> condition)
	{
		var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
		while (!condition())
		{
			if (DateTime.UtcNow > deadline)
			{
				Assert.Fail("Condition not met in time.");
			}
			await TestServices.WindowHelper.WaitForIdle();
			await Task.Delay(20);
		}
	}

	private sealed class Host : IImmediateDragDropExtension
	{
		public Action<DataPackageOperation>? OnCompleted { get; private set; }

		public bool TryStartNativeDrag(CoreDragInfo info, Action<DataPackageOperation> onCompleted)
		{
			OnCompleted = onCompleted;
			return true;
		}

		public void StartNativeDrag(CoreDragInfo info, Action<DataPackageOperation> onCompleted) => Assert.Fail("The drag was already taken.");
	}

	private sealed class Target : ICoreDropOperationTarget
	{
		public int Enters { get; private set; }
		public int Leaves { get; private set; }
		public int Drops { get; private set; }
		public bool LeaveBeforeCompletion { get; private set; }

		public IAsyncOperation<DataPackageOperation> EnterAsync(CoreDragInfo dragInfo, CoreDragUIOverride dragUIOverride)
		{
			Enters++;
			return Task.FromResult(DataPackageOperation.Copy).AsAsyncOperation();
		}

		public IAsyncOperation<DataPackageOperation> OverAsync(CoreDragInfo dragInfo, CoreDragUIOverride dragUIOverride)
			=> Task.FromResult(DataPackageOperation.Copy).AsAsyncOperation();

		public IAsyncAction LeaveAsync(CoreDragInfo dragInfo)
		{
			Leaves++;
			LeaveBeforeCompletion = true;
			dragInfo.RegisterCompletedCallback(_ => LeaveBeforeCompletion &= Leaves == 1);
			return Task.CompletedTask.AsAsyncAction();
		}

		public IAsyncOperation<DataPackageOperation> DropAsync(CoreDragInfo dragInfo)
		{
			Drops++;
			return Task.FromResult(DataPackageOperation.Copy).AsAsyncOperation();
		}
	}

	private sealed class Source(uint frameId, Point location) : IDragEventSource
	{
		public long Id => 4242;
		public uint FrameId => frameId;
		public (Point location, DragDropModifiers modifier) GetState() => (location, DragDropModifiers.None);
		public Point GetPosition(object? relativeTo) => location;
	}
}
#endif
