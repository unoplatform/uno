#nullable enable

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DirectUI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Text;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_Win32_RichEdit_Preserves_Upstream_Properties_And_Text_Patterns()
	{
		var editor = new RichEditBox { Width = 300, Height = 100 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "one\rtwo");
			editor.Document.GetRange(0, 3).Link = "\"https://example.com\"";
			editor.Document.GetRange(0, 3).CharacterFormat.BackgroundColor = Microsoft.UI.Colors.Red;
			editor.Document.GetRange(0, 7).ParagraphFormat.SetIndents(3, 9, 12);
			AutomationProperties.SetCulture(editor, 1036);
			AutomationProperties.SetIsPeripheral(editor, true);
			await WindowHelper.WaitForIdle();

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(editor);
			Assert.IsNotNull(peer);
			var accessibility = ResolveWin32Accessibility(editor);
			var provider = GetWin32Provider(accessibility, peer);
			Assert.AreEqual("XAML", InvokeBridge(provider, "GetPropertyValue", 30024));
			Assert.AreEqual(1036, InvokeBridge(provider, "GetPropertyValue", 30015));
			Assert.AreEqual(true, InvokeBridge(provider, "GetPropertyValue", 30150));
			Assert.IsNull(InvokeBridge(provider, "GetPatternProvider", 10002), "Rich content must not expose the lossy Value pattern.");
			Assert.IsNotNull(InvokeBridge(provider, "GetPatternProvider", 10024));
			Assert.IsNotNull(InvokeBridge(provider, "GetPatternProvider", 10032));

			var textProvider = InvokeBridge(provider, "GetPatternProvider", 10014);
			Assert.IsNotNull(textProvider);
			var range = textProvider.GetType().GetProperty("DocumentRange")!.GetValue(textProvider);
			Assert.IsNotNull(range);
			var interop = GetWin32BridgeType("Win32UIAutomationInterop");
			Assert.AreSame(
				interop.GetProperty("ReservedMixedAttributeValue", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null),
				InvokeBridge(range, "GetAttributeValue", (int)AutomationTextAttributesEnum.BackgroundColorAttribute));
			Assert.AreSame(
				interop.GetMethod("GetReservedNotSupportedValue", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null),
				InvokeBridge(range, "GetAttributeValue", -1));
			Assert.AreEqual(9d, InvokeBridge(range, "GetAttributeValue", (int)AutomationTextAttributesEnum.IndentationLeadingAttribute));

			var children = (object[])InvokeBridge(range, "GetChildren")!;
			Assert.HasCount(1, children);
			Assert.AreSame(children[0], ((object[])InvokeBridge(range, "GetChildren")!)[0]);
			var childRange = InvokeBridge(textProvider, "RangeFromChild", children[0]);
			Assert.IsNotNull(childRange);
			Assert.AreEqual("one", InvokeBridge(childRange, "GetText", -1));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_Win32_Text_Range_Marshals_Null_And_Rich_Attribute_Values()
	{
		var editor = new RichEditBox { Width = 300, Height = 100 };
		try
		{
			await UITestHelper.Load(editor);
			var accessibility = ResolveWin32Accessibility(editor);
			var inner = new BridgeAttributeRange();
			var wrapper = Activator.CreateInstance(
				GetWin32BridgeType("UiaTextRangeProviderWrapper"),
				BindingFlags.Instance | BindingFlags.NonPublic,
				binder: null,
				args: new object[] { inner, accessibility },
				culture: null)!;
			var interop = GetWin32BridgeType("Win32UIAutomationInterop");
			var unsupported = interop.GetMethod("GetReservedNotSupportedValue", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null);
			Assert.IsNotNull(unsupported);
			Assert.AreSame(unsupported, InvokeBridge(wrapper, "GetAttributeValue", -1));
			inner.AttributeValue = TextAttributeValueSentinel.NotSupported;
			Assert.AreSame(unsupported, InvokeBridge(wrapper, "GetAttributeValue", -1));
			inner.AttributeValue = Array.Empty<IRawElementProviderSimple>();
			Assert.AreSame(unsupported, InvokeBridge(wrapper, "GetAttributeValue", -1));
			inner.AttributeValue = new[] { AnnotationType.SpellingError };
			CollectionAssert.AreEqual(new[] { (int)AnnotationType.SpellingError }, (int[])InvokeBridge(wrapper, "GetAttributeValue", -1)!);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(editor);
			Assert.IsNotNull(peer);
			inner.Children = new[] { null!, new IRawElementProviderSimple(peer) };
			inner.AttributeValue = inner.Children;
			var converted = (Array)InvokeBridge(wrapper, "GetAttributeValue", -1)!;
			Assert.AreEqual(1, converted.Length);
			Assert.AreSame(GetWin32Provider(accessibility, peer), converted.GetValue(0));
			Assert.HasCount(1, (object[])InvokeBridge(wrapper, "GetChildren")!);
			InvokeBridge(wrapper, "ShowContextMenu");
			Assert.AreEqual(1, inner.ContextMenuCount);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_Win32_Virtual_Text_Child_Does_Not_Rebind_To_Editor_After_Peer_Expires()
	{
		var editor = new RichEditBox { Width = 300, Height = 100 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "link");
			editor.Document.GetRange(0, 4).Link = "\"https://example.com\"";
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(editor);
			var text = peer?.GetPattern(PatternInterface.Text) as ITextProvider;
			Assert.IsNotNull(text);
			var child = text.DocumentRange.GetChildren()[0].AutomationPeer;
			Assert.IsNotNull(child);
			var provider = GetWin32Provider(ResolveWin32Accessibility(editor), child);
			var reference = (WeakReference<AutomationPeer>)provider.GetType()
				.GetField("_representedPeer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(provider)!;
			reference.SetTarget(null!);
			Assert.IsNull(InvokeBridge(provider, "GetPatternProvider", 10014));

			provider.GetType().GetMethod("Invalidate", BindingFlags.Instance | BindingFlags.NonPublic)!
				.Invoke(provider, new object?[] { null, false });
			Assert.IsNull(provider.GetType().GetProperty("Owner", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(provider));
			var error = Assert.ThrowsExactly<TargetInvocationException>(() => InvokeBridge(provider, "GetPropertyValue", 30005));
			Assert.IsInstanceOfType<COMException>(error.InnerException);
			Assert.AreEqual(unchecked((int)0x80040201), error.InnerException.HResult);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static Type GetWin32BridgeType(string name)
		=> Type.GetType($"Uno.UI.Runtime.Skia.Win32.{name}, Uno.UI.Runtime.Skia.Win32", throwOnError: true)!;

	private static object ResolveWin32Accessibility(UIElement element)
	{
		var router = Type.GetType("Uno.UI.Runtime.Skia.AccessibilityRouter, Uno.UI.Runtime.Skia", throwOnError: true)!;
		var resolve = router.GetMethod("Resolve", new[] { typeof(UIElement) })!;
		return resolve.Invoke(null, new object[] { element }) ?? throw new InvalidOperationException("No window accessibility instance.");
	}

	private static object GetWin32Provider(object accessibility, AutomationPeer peer)
		=> accessibility.GetType().GetMethod("GetProviderForPeer", BindingFlags.Instance | BindingFlags.NonPublic)!
			.Invoke(accessibility, new object[] { peer, false }) ?? throw new InvalidOperationException("No peer provider.");

	private static object? InvokeBridge(object instance, string method, params object[] args)
		=> instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public)!.Invoke(instance, args);

	private sealed class BridgeAttributeRange : ITextRangeProvider, ITextRangeProvider2
	{
		internal object? AttributeValue { get; set; }
		internal IRawElementProviderSimple[] Children { get; set; } = Array.Empty<IRawElementProviderSimple>();
		internal int ContextMenuCount { get; private set; }

		public object GetAttributeValue(int attributeId) => AttributeValue!;
		public IRawElementProviderSimple[] GetChildren() => Children;
		public void ShowContextMenu() => ContextMenuCount++;
		public ITextRangeProvider Clone() => throw new NotSupportedException();
		public bool Compare(ITextRangeProvider textRangeProvider) => throw new NotSupportedException();
		public int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider textRangeProvider, TextPatternRangeEndpoint targetEndpoint) => throw new NotSupportedException();
		public void ExpandToEnclosingUnit(TextUnit unit) => throw new NotSupportedException();
		public ITextRangeProvider FindAttribute(int attributeId, object value, bool backward) => throw new NotSupportedException();
		public ITextRangeProvider FindText(string text, bool backward, bool ignoreCase) => throw new NotSupportedException();
		public void GetBoundingRectangles(out double[] returnValue) => throw new NotSupportedException();
		public IRawElementProviderSimple GetEnclosingElement() => throw new NotSupportedException();
		public string GetText(int maxLength) => throw new NotSupportedException();
		public int Move(TextUnit unit, int count) => throw new NotSupportedException();
		public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count) => throw new NotSupportedException();
		public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider textRangeProvider, TextPatternRangeEndpoint targetEndpoint) => throw new NotSupportedException();
		public void Select() => throw new NotSupportedException();
		public void AddToSelection() => throw new NotSupportedException();
		public void RemoveFromSelection() => throw new NotSupportedException();
		public void ScrollIntoView(bool alignToTop) => throw new NotSupportedException();
	}
}
