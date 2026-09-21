#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	[DataRow(false, false)]
	[DataRow(true, false)]
	[DataRow(false, true)]
	[DataRow(true, true)]
	public async Task When_AndroidAccessibility_Text_Object_Bounds_Use_The_Virtual_Parent(bool image, bool transformedAndScrolled)
	{
		var editor = new RichEditBox
		{
			Width = 190,
			Height = 100,
			FontSize = 16,
			Margin = new Thickness(29, 37, 0, 0),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
		};
		var container = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Children = { editor },
		};
		if (transformedAndScrolled)
		{
			container.RenderTransform = new CompositeTransform
			{
				ScaleX = 1.15,
				ScaleY = 1.15,
				TranslateX = 13,
				TranslateY = 9,
			};
		}

		object? renderView = null;
		float originalTranslationX = 0;
		float originalTranslationY = 0;
		try
		{
			await UITestHelper.Load(container);
			editor.Document.SetText(TextSetOptions.None, "first\rsecond\rlink\rfourth\rfifth\rsixth\rseventh");
			if (image)
			{
				using var stream = CreateImageStream(SKColors.Red);
				editor.Document.GetRange(13, 17).InsertImage(20, 14, 10, VerticalCharacterAlignment.Baseline, "logo", stream);
			}
			else
			{
				editor.Document.GetRange(13, 17).Link = "\"https://example.com\"";
			}
			editor.Document.Selection.SetRange(0, 0);
			editor.UpdateLayout();
			await WindowHelper.WaitForIdle();
			var scrollViewer = ((ITextBoxViewHost)editor).ContentElement as ScrollViewer;
			Assert.IsNotNull(scrollViewer);
			scrollViewer.ChangeView(null, transformedAndScrolled ? 24 : 0, null, disableAnimation: true);
			await WindowHelper.WaitForIdle();
			if (transformedAndScrolled)
			{
				Assert.IsTrue(scrollViewer.VerticalOffset > 0);
			}

			renderView = GetAndroidRenderView();
			originalTranslationX = (float)ReadAndroidProperty(renderView, "TranslationX");
			originalTranslationY = (float)ReadAndroidProperty(renderView, "TranslationY");
			renderView.GetType().GetProperty("TranslationX")!.SetValue(renderView, originalTranslationX + 7);
			renderView.GetType().GetProperty("TranslationY")!.SetValue(renderView, originalTranslationY + 11);
			await WindowHelper.WaitForIdle();

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(editor);
			Assert.IsNotNull(peer);
			var child = peer.GetChildren()!.Single(candidate =>
				candidate.GetAutomationControlType() == (image ? AutomationControlType.Image : AutomationControlType.Hyperlink));
			var logicalBounds = child.GetBoundingRectangle();
			Assert.IsTrue(logicalBounds.Width > 0 && logicalBounds.Height > 0);
			var xamlRoot = editor.XamlRoot ?? throw new InvalidOperationException("The editor is not attached.");
			var scale = xamlRoot.RasterizationScale;
			var rootBounds = new Rect(
				ToAndroidPhysicalPixels(logicalBounds.X, scale),
				ToAndroidPhysicalPixels(logicalBounds.Y, scale),
				ToAndroidPhysicalPixels(logicalBounds.Width, scale),
				ToAndroidPhysicalPixels(logicalBounds.Height, scale));

			var helper = ReadAndroidProperty(renderView, "ExploreByTouchHelper");
			var childId = InvokeAndroidMethod(helper, "GetOrCreateVirtualId", child)!;
			var parentId = InvokeAndroidMethod(helper, "GetOrCreateVirtualId", editor)!;
			var provider = InvokeAndroidMethod(helper, "GetAccessibilityNodeProvider", renderView)
				?? throw new InvalidOperationException("AndroidX did not return an accessibility node provider.");
			using var childNode = (IDisposable)(InvokeAndroidMethod(provider, "CreateAccessibilityNodeInfo", childId)
				?? throw new InvalidOperationException("AndroidX did not create the text-object node."));
			using var parentNode = (IDisposable)(InvokeAndroidMethod(provider, "CreateAccessibilityNodeInfo", parentId)
				?? throw new InvalidOperationException("AndroidX did not create the editor node."));
			var parentBounds = GetAndroidNodeBounds(parentNode, "GetBoundsInParent");
			var boundsInParent = GetAndroidNodeBounds(childNode, "GetBoundsInParent");
			Assert.AreEqual(new Rect(
				rootBounds.X - parentBounds.X,
				rootBounds.Y - parentBounds.Y,
				rootBounds.Width,
				rootBounds.Height), boundsInParent);

			var screenOffset = new int[2];
			InvokeAndroidMethod(renderView, "GetLocationOnScreen", screenOffset);
			var hostScrollX = (int)ReadAndroidProperty(renderView, "ScrollX");
			var hostScrollY = (int)ReadAndroidProperty(renderView, "ScrollY");
			var expectedScreenBounds = new Rect(
				rootBounds.X + screenOffset[0] - hostScrollX,
				rootBounds.Y + screenOffset[1] - hostScrollY,
				rootBounds.Width,
				rootBounds.Height);
			Assert.AreEqual(expectedScreenBounds, GetAndroidNodeBounds(childNode, "GetBoundsInScreen"),
				"Inspect the AndroidX-produced screen bounds, not only our node-population callback.");
		}
		finally
		{
			if (renderView is not null)
			{
				renderView.GetType().GetProperty("TranslationX")!.SetValue(renderView, originalTranslationX);
				renderView.GetType().GetProperty("TranslationY")!.SetValue(renderView, originalTranslationY);
			}
			WindowHelper.WindowContent = null;
		}
	}

	private static int ToAndroidPhysicalPixels(double value, double scale)
		=> value < 0 ? (int)Math.Ceiling(value * scale) : (int)(value * scale + 0.5);

	private static Rect GetAndroidNodeBounds(object node, string methodName)
	{
		var method = node.GetType().GetMethod(methodName)!;
		using var bounds = (IDisposable)(Activator.CreateInstance(method.GetParameters()[0].ParameterType)
			?? throw new InvalidOperationException("Android rectangle construction failed."));
		method.Invoke(node, [bounds]);
		var left = (int)ReadAndroidProperty(bounds, "Left");
		var top = (int)ReadAndroidProperty(bounds, "Top");
		var right = (int)ReadAndroidProperty(bounds, "Right");
		var bottom = (int)ReadAndroidProperty(bounds, "Bottom");
		return new Rect(left, top, right - left, bottom - top);
	}
}
