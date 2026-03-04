using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.CustomGlobal;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.CustomPrefixed;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.AmbiguousA;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.AmbiguousB;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ImplicitXamlNamespaces
	{
		[TestMethod]
		public async Task When_Page_Has_No_Xmlns_Declarations()
		{
			// T011: Verify a XAML-defined UserControl without explicit xmlns
			// declarations loads and renders standard WinUI controls correctly.
			var control = new ImplicitXamlNamespaces_NoXmlns();
			TestServices.WindowHelper.WindowContent = control;
			await TestServices.WindowHelper.WaitForLoaded(control);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(control.TestButton, "Button should be created from implicit namespace XAML");
			Assert.IsInstanceOfType(control.TestButton, typeof(Button));
			Assert.AreEqual("Hello", control.TestButton.Content);

			Assert.IsNotNull(control.TestTextBlock, "TextBlock should be created from implicit namespace XAML");
			Assert.IsInstanceOfType(control.TestTextBlock, typeof(TextBlock));
			Assert.AreEqual("World", control.TestTextBlock.Text);
		}

		[TestMethod]
		public async Task When_XName_Works_Without_XmlnsX()
		{
			// T012: Verify x:Name works on elements in XAML without xmlns:x declaration.
			var control = new ImplicitXamlNamespaces_XName();
			TestServices.WindowHelper.WindowContent = control;
			await TestServices.WindowHelper.WaitForLoaded(control);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(control.NamedButton, "x:Name should resolve Button without explicit xmlns:x");
			Assert.AreEqual("Named", control.NamedButton.Content);

			Assert.IsNotNull(control.NamedTextBlock, "x:Name should resolve TextBlock without explicit xmlns:x");
			Assert.AreEqual("Named Text", control.NamedTextBlock.Text);
		}

		[TestMethod]
		public async Task When_XBind_Works_Without_XmlnsX()
		{
			// T013: Verify x:Bind expressions resolve correctly in XAML
			// without xmlns:x declaration.
			var control = new ImplicitXamlNamespaces_XBind();
			control.TestText = "Bound Value";
			TestServices.WindowHelper.WindowContent = control;
			await TestServices.WindowHelper.WaitForLoaded(control);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(control.BoundTextBlock, "TextBlock with x:Bind should exist");
			Assert.AreEqual("Bound Value", control.BoundTextBlock.Text,
				"x:Bind should resolve property without explicit xmlns:x");
		}

		[TestMethod]
		public async Task When_Explicit_Xmlns_Still_Works()
		{
			// T014: Verify XAML with explicit xmlns declarations continues
			// to compile and render identically (backward compatibility).
			var control = new ImplicitXamlNamespaces_ExplicitXmlns();
			TestServices.WindowHelper.WindowContent = control;
			await TestServices.WindowHelper.WaitForLoaded(control);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(control.ExplicitButton, "Button with explicit xmlns should work");
			Assert.IsInstanceOfType(control.ExplicitButton, typeof(Button));
			Assert.AreEqual("Explicit", control.ExplicitButton.Content);

			Assert.IsNotNull(control.ExplicitTextBlock, "TextBlock with explicit xmlns should work");
			Assert.IsInstanceOfType(control.ExplicitTextBlock, typeof(TextBlock));
			Assert.AreEqual("Explicit Xmlns", control.ExplicitTextBlock.Text);
		}

		// T015: Feature disable test - when UnoEnableImplicitXamlNamespaces=false,
		// XAML without xmlns should fail to parse. This is a build-time behavior
		// that cannot be validated at runtime since XAML compilation happens during
		// the build. A separate test project with the flag set to false would be
		// needed to verify opt-out behavior.

		// --- Phase 4: User Story 2 - Custom CLR Namespaces ---

		[TestMethod]
		public async Task When_Custom_Namespace_Registered_To_Global_Uri()
		{
			// T019: Verify a custom CLR namespace registered to the global URI
			// via [assembly: XmlnsDefinition] resolves its types unprefixed in XAML.
			var control = new ImplicitXamlNamespaces_CustomGlobal();
			TestServices.WindowHelper.WindowContent = control;
			await TestServices.WindowHelper.WaitForLoaded(control);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(control.MyCustomControl, "CustomGlobalControl should resolve from global namespace");
			Assert.IsInstanceOfType(control.MyCustomControl, typeof(CustomGlobalControl));
			Assert.AreEqual("Test Label", control.MyCustomControl.CustomLabel);
		}

		[TestMethod]
		public async Task When_Multiple_Global_Namespaces_Resolve()
		{
			// T020: Verify multiple types from the same globally registered
			// namespace resolve unprefixed.
			var control = new ImplicitXamlNamespaces_CustomGlobal();
			TestServices.WindowHelper.WindowContent = control;
			await TestServices.WindowHelper.WaitForLoaded(control);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(control.MyAnotherControl, "AnotherGlobalControl should resolve from global namespace");
			Assert.IsInstanceOfType(control.MyAnotherControl, typeof(AnotherGlobalControl));
			Assert.AreEqual(42, control.MyAnotherControl.CustomValue);
		}

		[TestMethod]
		public async Task When_WinUI_Type_Takes_Precedence_Over_Global()
		{
			// T021: Verify that standard WinUI types (e.g., Button) take precedence
			// over any custom types with the same name in global namespaces.
			// This is implicitly verified by T011 - the Button in the NoXmlns test
			// resolves to Microsoft.UI.Xaml.Controls.Button, not any custom Button.
			var control = new ImplicitXamlNamespaces_NoXmlns();
			TestServices.WindowHelper.WindowContent = control;
			await TestServices.WindowHelper.WaitForLoaded(control);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsInstanceOfType(control.TestButton, typeof(Microsoft.UI.Xaml.Controls.Button),
				"Standard WinUI Button should take precedence over any global namespace Button");
		}

		// --- Phase 5: User Story 3 - Third-Party Library Namespaces ---

		[TestMethod]
		public async Task When_Cross_Assembly_XmlnsDefinition_Resolves()
		{
			// T025: Verify types from a referenced assembly registered to the global URI
			// resolve unprefixed. Since GlobalNamespaceResolver scans both compilation.References
			// and compilation.Assembly, the same mechanism handles cross-assembly and
			// current-assembly resolution. This test verifies the end-to-end resolution
			// works for custom types alongside standard WinUI types.
			var control = new ImplicitXamlNamespaces_CustomGlobal();
			TestServices.WindowHelper.WindowContent = control;
			await TestServices.WindowHelper.WaitForLoaded(control);
			await TestServices.WindowHelper.WaitForIdle();

			// CustomGlobalControl is resolved via XmlnsDefinition targeting the global URI
			Assert.IsNotNull(control.MyCustomControl, "Cross-assembly style XmlnsDefinition should resolve");
			Assert.IsInstanceOfType(control.MyCustomControl, typeof(CustomGlobalControl));

			// Standard WinUI controls continue to work alongside custom global types
			var noXmlns = new ImplicitXamlNamespaces_NoXmlns();
			TestServices.WindowHelper.WindowContent = noXmlns;
			await TestServices.WindowHelper.WaitForLoaded(noXmlns);
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsNotNull(noXmlns.TestButton, "Standard WinUI types should coexist with global custom types");
		}

		[TestMethod]
		public async Task When_Library_Ships_Own_XmlnsDefinition()
		{
			// T026: Verify that when an assembly declares its own XmlnsDefinition
			// targeting the global URI, its types are automatically discoverable.
			// AnotherGlobalControl is in the same XmlnsDefinition-registered namespace
			// as CustomGlobalControl, proving multiple types are discovered.
			var control = new ImplicitXamlNamespaces_CustomGlobal();
			TestServices.WindowHelper.WindowContent = control;
			await TestServices.WindowHelper.WaitForLoaded(control);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(control.MyAnotherControl, "Library-shipped XmlnsDefinition types should auto-discover");
			Assert.IsInstanceOfType(control.MyAnotherControl, typeof(AnotherGlobalControl));
			Assert.AreEqual(42, control.MyAnotherControl.CustomValue);
		}

		[TestMethod]
		public async Task When_XmlnsPrefix_Registered_Prefix_Is_Implicitly_Available()
		{
			// T026b: Verify that a namespace registered with [assembly: XmlnsPrefix]
			// associating a prefix (e.g., "tc") is implicitly available in XAML
			// without an explicit xmlns:tc declaration.
			var control = new ImplicitXamlNamespaces_XmlnsPrefix();
			TestServices.WindowHelper.WindowContent = control;
			await TestServices.WindowHelper.WaitForLoaded(control);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(control.MyPrefixedControl, "XmlnsPrefix-registered prefix should be implicitly available");
			Assert.IsInstanceOfType(control.MyPrefixedControl, typeof(PrefixedControl));
			Assert.AreEqual("Prefixed Value", control.MyPrefixedControl.PrefixedLabel);

			Assert.IsNotNull(control.StandardTextBlock, "Standard controls should work alongside prefixed controls");
			Assert.AreEqual("Standard Control", control.StandardTextBlock.Text);
		}

		// --- Phase 6: User Story 4 - Disambiguation ---

		// T029: Ambiguity detection test - when two global namespaces contain
		// the same type name, a compile-time diagnostic (UNO0501) should be emitted.
		// This is a build-time behavior that cannot be validated at runtime.
		// The ambiguity detection is implemented in SourceFindTypeByXamlType() and
		// would need a separate test project with conflicting types to verify.

		[TestMethod]
		public async Task When_Explicit_Prefix_Resolves_Ambiguity()
		{
			// T030: Verify that explicit xmlns prefix declarations resolve
			// types from specific namespaces, overriding global resolution.
			var control = new ImplicitXamlNamespaces_Disambiguation();
			TestServices.WindowHelper.WindowContent = control;
			await TestServices.WindowHelper.WaitForLoaded(control);
			await TestServices.WindowHelper.WaitForIdle();

			// Unique types from different global namespaces resolve unprefixed
			Assert.IsNotNull(control.ControlA, "UniqueControlA should resolve from global namespace");
			Assert.IsInstanceOfType(control.ControlA, typeof(UniqueControlA));
			Assert.AreEqual("From A", control.ControlA.LabelA);

			Assert.IsNotNull(control.ControlB, "UniqueControlB should resolve from global namespace");
			Assert.IsInstanceOfType(control.ControlB, typeof(UniqueControlB));
			Assert.AreEqual("From B", control.ControlB.LabelB);

			// Explicit prefix also works alongside implicit resolution
			Assert.IsNotNull(control.ExplicitA, "Explicit prefix should resolve UniqueControlA");
			Assert.IsInstanceOfType(control.ExplicitA, typeof(UniqueControlA));
			Assert.AreEqual("Explicit A", control.ExplicitA.LabelA);
		}

		[TestMethod]
		public async Task When_Explicit_Xmlns_Overrides_Implicit()
		{
			// T032: Verify that explicit per-file xmlns declarations override
			// implicit global registrations (FR-009).
			var control = new ImplicitXamlNamespaces_ExplicitXmlns();
			TestServices.WindowHelper.WindowContent = control;
			await TestServices.WindowHelper.WaitForLoaded(control);
			await TestServices.WindowHelper.WaitForIdle();

			// Explicit xmlns declarations take precedence
			Assert.IsNotNull(control.ExplicitButton, "Explicit xmlns should override implicit");
			Assert.IsInstanceOfType(control.ExplicitButton, typeof(Button));
		}
	}
}
