#nullable enable

using System;
using System.Collections;
using System.Numerics;
using System.Reflection;
using Microsoft.UI.Composition;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;
using Uno.UI.Composition.WebGpu;

namespace Uno.UI.Tests.CompositionTests;

// The recorder composes translations without a full 4x4 multiply; it must still match one exactly.
[UITestClass]
public class Given_WebGpuCommandRecorder
{
	private static readonly Matrix4x4[] _parents =
	{
		Matrix4x4.Identity,
		Matrix4x4.CreateTranslation(12.5f, -7, 0),
		Matrix4x4.CreateScale(2.625f, 2.625f, 1) * Matrix4x4.CreateTranslation(40, 80, 0),
		Matrix4x4.CreateRotationZ(0.7f) * Matrix4x4.CreateTranslation(-3, 9, 0),
		Matrix4x4.CreateRotationX(0.5f) * Matrix4x4.CreateTranslation(10, 20, 30),
		new Matrix4x4(1, 0, 0, 0.002f, 0, 1, 0, -0.001f, 0, 0, 1, 0, 5, 6, 0, 1),
	};

	[TestMethod]
	public void When_Concat_Translation_Then_Matches_Full_Multiply()
	{
		foreach (var parent in _parents)
		{
			var recorder = new WebGpuCommandRecorder(null!);
			recorder.SetMatrix(parent);
			var translation = Matrix4x4.CreateTranslation(31.25f, -4.5f, 0);

			recorder.Concat(translation);

			AssertClose(translation * parent, recorder.TotalMatrix);
		}
	}

	[TestMethod]
	public void When_Translate_Then_Matches_Full_Multiply()
	{
		foreach (var parent in _parents)
		{
			var recorder = new WebGpuCommandRecorder(null!);
			recorder.SetMatrix(parent);

			recorder.Translate(-17, 3.75f);

			AssertClose(Matrix4x4.CreateTranslation(-17, 3.75f, 0) * parent, recorder.TotalMatrix);
		}
	}

	[TestMethod]
	public void When_Concat_Non_Translation_Then_Matches_Full_Multiply()
	{
		var others = new[]
		{
			Matrix4x4.CreateScale(1.5f, 0.5f, 1),
			Matrix4x4.CreateRotationZ(-1.1f) * Matrix4x4.CreateTranslation(4, 5, 0),
			Matrix4x4.CreateTranslation(1, 2, 3),
			Matrix4x4.CreateRotationY(0.3f),
		};

		foreach (var parent in _parents)
		{
			foreach (var other in others)
			{
				var recorder = new WebGpuCommandRecorder(null!);
				recorder.SetMatrix(parent);

				recorder.Concat(other);

				AssertClose(other * parent, recorder.TotalMatrix);
			}
		}
	}

	[TestMethod]
	public void When_Finished_Mid_Session_Then_Saved_State_Survives()
	{
		var recorder = new WebGpuCommandRecorder(null!);
		recorder.Save();
		recorder.Translate(5, 6);

		recorder.Finish();
		recorder.Restore();

		Assert.AreEqual(Matrix4x4.Identity, recorder.TotalMatrix);
		Assert.AreEqual(0, recorder.SaveCount);
	}

	[TestMethod]
	public void When_Recorders_Share_Pooled_Stacks_Then_State_Is_Not_Shared()
	{
		var first = new WebGpuCommandRecorder(null!);
		first.Save();
		first.Restore();
		first.Finish();

		var second = new WebGpuCommandRecorder(null!);
		var third = new WebGpuCommandRecorder(null!);
		second.Translate(1, 2);
		second.Save();
		third.Save();
		third.Save();

		Assert.AreEqual(1, second.SaveCount);
		Assert.AreEqual(2, third.SaveCount);
		second.Restore();
		Assert.AreEqual(Matrix4x4.CreateTranslation(1, 2, 0), second.TotalMatrix);
		Assert.AreEqual(2, third.SaveCount);
	}

	[TestMethod]
	public void When_Saves_Balanced_Before_Finish_Then_Stack_Is_Pooled()
	{
		ClearStackPool();
		var recorder = new WebGpuCommandRecorder(null!);
		recorder.Save();
		recorder.Restore();

		recorder.Finish();

		Assert.AreEqual(1, PooledStackCount);
	}

	[TestMethod]
	public void When_Finished_With_Open_Save_Then_Stack_Is_Not_Pooled()
	{
		ClearStackPool();
		var recorder = new WebGpuCommandRecorder(null!);
		recorder.Save();

		recorder.Finish();

		Assert.AreEqual(0, PooledStackCount);
	}

	[TestMethod]
	public void When_Visual_Records_Its_Paint_Then_Stack_Is_Pooled()
	{
		var factory = DispatchProxy.Create<IDrawingFactory, RecordingFactory>();
		var root = new WebGpuCommandRecorder(factory);
		var compositor = Compositor.GetSharedCompositor();
		var visual = compositor.CreateContainerVisual();
		visual.Size = new Vector2(10, 10);
		// A childless visual that paints nothing is culled before it records.
		visual.Children.InsertAtTop(compositor.CreateContainerVisual());
		ClearStackPool();

		visual.RenderRootVisual(root, offsetOverride: null);

		Assert.IsNotNull(visual.RecordedContentForTesting);
		// The root session is balanced but not finished, so it still holds its own stack.
		Assert.AreEqual(1, PooledStackCount);
	}

	public class RecordingFactory : DispatchProxy
	{
		protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
			=> targetMethod?.Name == nameof(IDrawingFactory.CreateRecording)
				? new WebGpuCommandRecorder((IDrawingFactory)(object)this)
				: throw new NotSupportedException(targetMethod?.Name);
	}

	private static FieldInfo StackPoolField
		=> typeof(WebGpuCommandRecorder).GetField("_stackPool", BindingFlags.NonPublic | BindingFlags.Static)!;

	private static int PooledStackCount => StackPoolField.GetValue(null) is ICollection pool ? pool.Count : 0;

	private static void ClearStackPool() => StackPoolField.SetValue(null, null);

	private static void AssertClose(Matrix4x4 expected, Matrix4x4 actual)
	{
		for (var row = 0; row < 4; row++)
		{
			for (var column = 0; column < 4; column++)
			{
				Assert.AreEqual(expected[row, column], actual[row, column], 1e-4f, $"M{row + 1}{column + 1}: expected {expected}, actual {actual}");
			}
		}
	}
}
