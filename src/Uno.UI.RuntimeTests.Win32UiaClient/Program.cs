#nullable enable

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using UIAutomationClient;

internal static class Program
{
	private const int UIA_ProcessIdPropertyId = 30002;
	private const int UIA_BoundingRectanglePropertyId = 30001;
	private const int UIA_NamePropertyId = 30005;
	private const int UIA_AutomationIdPropertyId = 30011;
	private const int UIA_ValuePatternId = 10002;
	private const int UIA_TextPatternId = 10014;
	private const int UIA_InvokePatternId = 10000;
	private const int UIA_TextPattern2Id = 10024;
	private const int UIA_AnnotationPatternId = 10023;
	private const int UIA_TextChildPatternId = 10029;
	private const int UIA_TextEditPatternId = 10032;
	private const int UIA_HyperlinkControlTypeId = 50005;
	private const int UIA_ImageControlTypeId = 50006;
	private const int UIA_BackgroundColorAttributeId = 40001;
	private const int UIA_BulletStyleAttributeId = 40002;
	private const int UIA_IndentationLeadingAttributeId = 40011;
	private const int UIA_IndentationTrailingAttributeId = 40012;
	private const int UIA_TabsAttributeId = 40027;
	private const int UIA_TextFlowDirectionsAttributeId = 40028;
	private const int UIA_AnnotationTypesAttributeId = 40031;
	private const int UIA_AnnotationObjectsAttributeId = 40032;
	private const int UIA_LinkAttributeId = 40035;
	private const int UIA_SelectionActiveEndAttributeId = 40037;
	private const int UIA_StructureChangedEventId = 20002;
	private const int UIA_Text_TextChangedEventId = 20015;
	private const int UIA_TextEdit_ConversionTargetChangedEventId = 20033;

	private static int _failures;

	private static int Main(string[] args)
	{
		if (args.Length is < 1 or > 2 || !int.TryParse(args[0], out var processId))
		{
			Console.Error.WriteLine("Usage: RichEditBoxUiaClient <SamplesApp process id or 0> [native]");
			return 2;
		}
		var nativeComparison = args.Length == 2 && string.Equals(args[1], "native", StringComparison.OrdinalIgnoreCase);

		var automation = (IUIAutomation)new CUIAutomation8Class();
		var editor = WaitForElement(automation, processId, "RichEditBoxUiaFixture");
		Check(editor is not null, "external client found the RichEditBox fixture");
		if (editor is null)
		{
			return 1;
		}

		Check(GetPattern<IUIAutomationValuePattern>(editor, UIA_ValuePatternId) is null, "RichEditBox exposes no Value pattern");
		var text = GetPattern<IUIAutomationTextPattern>(editor, UIA_TextPatternId);
		var text2 = GetPattern<IUIAutomationTextPattern2>(editor, UIA_TextPattern2Id);
		var textEdit = GetPattern<IUIAutomationTextEditPattern>(editor, UIA_TextEditPatternId);
		Check(text is not null, "Text pattern is exposed");
		Check(text2 is not null, "Text2 pattern is exposed");
		Check(textEdit is not null, "TextEdit pattern is exposed externally");

		var structureEditor = WaitForElement(automation, processId, "RichEditBoxUiaStructureFixture");
		Check(structureEditor is not null, "external client found the structure-event fixture");
		if (structureEditor is not null)
		{
			var structureHandler = new StructureChangedEventHandler();
			automation.AddStructureChangedEventHandler(
				structureEditor,
				TreeScope.TreeScope_Element,
				null,
				structureHandler);
			try
			{
				Check(
					InvokeByAutomationId(automation, processId, "RichEditBoxUiaAddStructureLink"),
					"structure-link button invoked before child enumeration");
				if (nativeComparison)
				{
					Console.WriteLine($"INFO: native pre-enumeration StructureChanged count={structureHandler.Count}");
				}
				else
				{
					Check(
						WaitUntil(() => structureHandler.Count > 0),
						"StructureChanged reaches the external client before prior child enumeration");
				}
			}
			finally
			{
				automation.RemoveStructureChangedEventHandler(structureEditor, structureHandler);
			}

			var structureText = GetPattern<IUIAutomationTextPattern>(structureEditor, UIA_TextPatternId);
			var originalChild = structureText?.DocumentRange.GetChildren()?.GetElement(0);
			Check(originalChild is not null, "structure fixture exposes the added hyperlink child");
			if (originalChild is not null)
			{
				var propertyHandler = new PropertyChangedEventHandler(UIA_NamePropertyId);
				automation.AddPropertyChangedEventHandler(
					structureEditor,
					TreeScope.TreeScope_Subtree,
					null,
					propertyHandler,
					[UIA_NamePropertyId]);
				try
				{
					Check(
						InvokeByAutomationId(automation, processId, "RichEditBoxUiaRenameStructureLink"),
						"structure-link rename button invoked");
					if (nativeComparison)
					{
						Console.WriteLine($"INFO: native child NamePropertyChanged count={propertyHandler.Count}");
					}
					else
					{
						Check(
							WaitUntil(() =>
								propertyHandler.Count > 0
								&& Equals(propertyHandler.LastValue, "renamed")
								&& propertyHandler.LastSender is { } sender
								&& automation.CompareElements(sender, originalChild) != 0),
							"existing hyperlink raises NamePropertyChanged");
					}
				}
				finally
				{
					automation.RemovePropertyChangedEventHandler(structureEditor, propertyHandler);
				}

				var renamedChild = TryGetFirstChild(structureText?.DocumentRange);
				if (nativeComparison && renamedChild is null)
				{
					Console.WriteLine("INFO: native renamed hyperlink child was coalesced/omitted.");
				}
				else
				{
					Check(
						renamedChild is not null
							&& automation.CompareElements(originalChild, renamedChild) != 0,
						"renamed hyperlink preserves provider identity");

					var oldBounds = originalChild.CurrentBoundingRectangle;
					var boundsHandler = new PropertyChangedEventHandler(UIA_BoundingRectanglePropertyId);
					automation.AddPropertyChangedEventHandler(
						structureEditor,
						TreeScope.TreeScope_Subtree,
						null,
						boundsHandler,
						[UIA_BoundingRectanglePropertyId]);
					try
					{
						Check(
							InvokeByAutomationId(automation, processId, "RichEditBoxUiaExpandStructurePrefix"),
							"structure-prefix expansion button invoked");
						if (nativeComparison)
						{
							Console.WriteLine($"INFO: native child BoundingRectanglePropertyChanged count={boundsHandler.Count}");
						}
						else
						{
							Check(
								WaitUntil(() =>
									boundsHandler.Count > 0
									&& boundsHandler.LastSender is { } sender
									&& automation.CompareElements(sender, originalChild) != 0),
								"existing hyperlink raises BoundingRectanglePropertyChanged");
						}
					}
					finally
					{
						automation.RemovePropertyChangedEventHandler(structureEditor, boundsHandler);
					}

					var movedChild = TryGetFirstChild(structureText?.DocumentRange);
					var newBounds = movedChild?.CurrentBoundingRectangle ?? default;
					Check(
						movedChild is not null
							&& automation.CompareElements(originalChild, movedChild) != 0,
						"moved hyperlink preserves provider identity");
					Check(
						oldBounds.left != newBounds.left || oldBounds.top != newBounds.top,
						"moved hyperlink reports updated bounds");
				}
			}
		}

		if (!nativeComparison)
		{
			WaitUntil(() => textEdit?.GetActiveComposition()?.GetText(-1) == "nihao");
			Check(
				InvokeByAutomationId(automation, processId, "RichEditBoxUiaUpdateComposition"),
				"composition fixture initialized through UIA");
			string? initialActiveComposition = null;
			string? initialConversionTarget = null;
			WaitUntil(() =>
			{
				initialActiveComposition = textEdit?.GetActiveComposition()?.GetText(-1);
				initialConversionTarget = textEdit?.GetConversionTarget()?.GetText(-1);
				return initialActiveComposition is "ni" or "nihaoma";
			});
			if (initialActiveComposition != "ni")
			{
				InvokeByAutomationId(automation, processId, "RichEditBoxUiaUpdateComposition");
				WaitUntil(() =>
				{
					initialActiveComposition = textEdit?.GetActiveComposition()?.GetText(-1);
					initialConversionTarget = textEdit?.GetConversionTarget()?.GetText(-1);
					return initialActiveComposition == "ni" && initialConversionTarget == "i";
				});
			}
			WaitUntil(() =>
			{
				initialActiveComposition = textEdit?.GetActiveComposition()?.GetText(-1);
				initialConversionTarget = textEdit?.GetConversionTarget()?.GetText(-1);
				return initialActiveComposition == "ni" && initialConversionTarget == "i";
			});
			Check(initialActiveComposition == "ni", "active composition range is current IME text");
			Check(initialConversionTarget == "i", "conversion target excludes the resolved prefix");
		}

		var documentRange = text?.DocumentRange;
		Check(documentRange is not null, "document range is available");
		if (documentRange is null)
		{
			return 1;
		}

		var firstChildren = GetTextChildren(documentRange);
		var secondChildren = GetTextChildren(documentRange);
		Check(firstChildren.FirstLink is not null, "document range exposes first hyperlink child");
		if (nativeComparison)
		{
			Console.WriteLine(
				$"INFO: native duplicate-target second hyperlink child=" +
				$"{(firstChildren.SecondLink is null ? "coalesced/omitted" : "separate")}");
		}
		else
		{
			Check(firstChildren.SecondLink is not null, "document range exposes second hyperlink child");
		}
		Check(firstChildren.Image is not null, "document range exposes image child");
		Check(firstChildren.FirstLink?.CurrentName == "first-link", "first hyperlink child has friendly-name text");
		if (!nativeComparison)
		{
			Check(firstChildren.SecondLink?.CurrentName == "second-link", "second hyperlink child has friendly-name text");
		}
		Check(firstChildren.Image?.CurrentName == "fixture image", "image child has alternate-text name");
		Check(
			firstChildren.FirstLink is not null
				&& secondChildren.FirstLink is not null
				&& automation.CompareElements(firstChildren.FirstLink, secondChildren.FirstLink) != 0,
			"first hyperlink provider identity is stable across reads");
		if (!nativeComparison)
		{
			Check(
				firstChildren.SecondLink is not null
					&& secondChildren.SecondLink is not null
					&& automation.CompareElements(firstChildren.SecondLink, secondChildren.SecondLink) != 0,
				"second hyperlink provider identity is stable across reads");
		}
		Check(
			firstChildren.Image is not null
				&& secondChildren.Image is not null
				&& automation.CompareElements(firstChildren.Image, secondChildren.Image) != 0,
			"image provider identity is stable across reads");

		var linkRange = firstChildren.FirstLink is null ? null : text?.RangeFromChild(firstChildren.FirstLink);
		var secondLinkRange = firstChildren.SecondLink is null ? null : text?.RangeFromChild(firstChildren.SecondLink);
		var imageRange = firstChildren.Image is null ? null : text?.RangeFromChild(firstChildren.Image);
		var retainedSecondLinkRange = secondLinkRange
			?? (nativeComparison ? documentRange.FindText("second-link", 0, 0) : null);
		var retainedSecondLinkClone = retainedSecondLinkRange?.Clone();
		Check(linkRange?.GetText(-1) == "first-link", "RangeFromChild maps first hyperlink to its text span");
		if (!nativeComparison)
		{
			Check(secondLinkRange?.GetText(-1) == "second-link", "RangeFromChild maps second hyperlink to its text span");
		}
		else
		{
			Check(retainedSecondLinkRange?.GetText(-1) == "second-link", "native FindText creates the retained comparison range");
		}
		var imageRangeText = imageRange?.GetText(-1);
		if (nativeComparison)
		{
			Console.WriteLine($"INFO: native image RangeFromChild text={imageRangeText ?? "(null)"}");
		}
		Check(
			imageRangeText == "fixture image",
			"RangeFromChild maps image to its object span");

		var linkTextChild = firstChildren.FirstLink is null
			? null
			: GetPattern<IUIAutomationTextChildPattern>(firstChildren.FirstLink, UIA_TextChildPatternId);
		var imageTextChild = firstChildren.Image is null
			? null
			: GetPattern<IUIAutomationTextChildPattern>(firstChildren.Image, UIA_TextChildPatternId);
		var retainedLinkTextChildRange = linkTextChild?.TextRange;
		var retainedImageTextChildRange = imageTextChild?.TextRange;
		Check(linkTextChild is not null, "hyperlink child exposes TextChild");
		Check(imageTextChild is not null, "image child exposes TextChild");
		if (linkTextChild is not null)
		{
			var textChildRange = linkTextChild.TextRange;
			Check(
				nativeComparison || automation.CompareElements(linkTextChild.TextContainer, editor) != 0,
				"hyperlink TextContainer is the editor");
			Check(
				linkTextChild.TextContainer.CurrentControlType == editor.CurrentControlType
					&& linkTextChild.TextContainer.CurrentName == editor.CurrentName,
				"hyperlink TextContainer has editor semantics");
			Check(
				linkRange is not null
					&& textChildRange is not null
					&& textChildRange.Compare(linkRange) != 0
					&& textChildRange.GetText(-1) == "first-link",
				"hyperlink TextRange exactly matches RangeFromChild");
		}
		if (imageTextChild is not null)
		{
			var textChildRange = imageTextChild.TextRange;
			Check(
				nativeComparison || automation.CompareElements(imageTextChild.TextContainer, editor) != 0,
				"image TextContainer is the editor");
			Check(
				imageTextChild.TextContainer.CurrentControlType == editor.CurrentControlType
					&& imageTextChild.TextContainer.CurrentName == editor.CurrentName,
				"image TextContainer has editor semantics");
			Check(
				imageRange is not null
					&& textChildRange is not null
					&& textChildRange.Compare(imageRange) != 0
					&& textChildRange.GetText(-1) == "fixture image",
				"image TextRange exactly matches RangeFromChild");
		}

		if (linkRange is not null && firstChildren.FirstLink is not null)
		{
			ProbeEnclosingElements(automation, editor, firstChildren.FirstLink, linkRange, "link");
		}
		if (imageRange is not null && firstChildren.Image is not null)
		{
			ProbeEnclosingElements(automation, editor, firstChildren.Image, imageRange, "image");
		}
		Check(documentRange is IUIAutomationTextRange2, "document range exposes TextRange2");
		Check(linkRange is IUIAutomationTextRange2, "hyperlink range exposes TextRange2");
		Check(imageRange is IUIAutomationTextRange2, "image range exposes TextRange2");

		var linkInvoke = firstChildren.FirstLink is null
			? null
			: GetPattern<IUIAutomationInvokePattern>(firstChildren.FirstLink, UIA_InvokePatternId);
		Check(linkInvoke is not null, "hyperlink child exposes Invoke");
		if (linkInvoke is not null && !nativeComparison)
		{
			linkInvoke.Invoke();
			Check(true, "unsafe hyperlink invocation is safely ignored");
		}
		Check(
			firstChildren.Image is null
				|| GetPattern<IUIAutomationInvokePattern>(firstChildren.Image, UIA_InvokePatternId) is null,
			"image child exposes no Invoke pattern");

		Check(
			nativeComparison
				? firstChildren.FirstLink?.CurrentIsOffscreen == 0
				: firstChildren.FirstLink?.CurrentIsOffscreen != 0,
			nativeComparison
				? "native RichEdit reports the link onscreen before range scrolling"
				: "link below the viewport is reported offscreen");
		linkRange?.ScrollIntoView(0);
		var bounds = default(tagRECT);
		WaitUntil(() =>
		{
			bounds = firstChildren.FirstLink?.CurrentBoundingRectangle ?? default;
			return firstChildren.FirstLink?.CurrentIsOffscreen == 0
				&& bounds.right > bounds.left
				&& bounds.bottom > bounds.top;
		});
		Check(firstChildren.FirstLink?.CurrentIsOffscreen == 0, "link becomes onscreen after range scrolling");
		Check(bounds.right > bounds.left && bounds.bottom > bounds.top, "onscreen link has a nonempty bounding rectangle");

		var typoRange = documentRange.FindText("typo", 0, 0);
		var annotationTypesValue = typoRange?.GetAttributeValue(UIA_AnnotationTypesAttributeId);
		var annotationObjectsValue = typoRange?.GetAttributeValue(UIA_AnnotationObjectsAttributeId);
		if (nativeComparison)
		{
			Console.WriteLine($"INFO: native spelling annotation types={DescribeAttributeValue(annotationTypesValue, automation)}");
			Console.WriteLine($"INFO: native spelling annotation objects={DescribeAttributeValue(annotationObjectsValue, automation)}");
		}
		else
		{
			Check(
				annotationTypesValue is int[] { Length: 1 } annotationTypes
					&& annotationTypes[0] == 60001,
				"spelling range exposes the SpellingError annotation type");
			Check(
				annotationObjectsValue is IUIAutomationElementArray annotationObjects
					&& annotationObjects.Length == 1,
				"spelling range exposes one annotation object");
			var annotationElement = editor.FindFirst(
				TreeScope.TreeScope_Children,
				automation.CreatePropertyCondition(UIA_NamePropertyId, "typo"));
			Check(annotationElement is not null, "spelling annotation is a child element");
			if (annotationElement is not null)
			{
				var annotation = GetPattern<IUIAutomationAnnotationPattern>(
					annotationElement,
					UIA_AnnotationPatternId);
				Check(annotation is not null, "spelling annotation exposes Annotation pattern");
				Check(annotation?.CurrentAnnotationTypeId == 60001, "annotation type id is SpellingError");
				Check(annotationElement.CurrentHelpText == "type, typo-fix", "annotation HelpText carries suggestions");
				Check(text2?.RangeFromAnnotation(annotationElement)?.GetText(-1) == "typo", "RangeFromAnnotation maps to the misspelled word");
			}
		}

		ProbeFormattingAttributes(documentRange, text, automation, nativeComparison);

		if (!nativeComparison)
		{
			var automation3 = (IUIAutomation3)automation;
			var compositionHandler = new TextEditEventHandler();
			var finalizedHandler = new TextEditEventHandler();
			var conversionHandler = new AutomationEventHandler(UIA_TextEdit_ConversionTargetChangedEventId);
			automation3.AddTextEditTextChangedEventHandler(
				editor,
				TreeScope.TreeScope_Element,
				TextEditChangeType.TextEditChangeType_Composition,
				null,
				compositionHandler);
			automation3.AddTextEditTextChangedEventHandler(
				editor,
				TreeScope.TreeScope_Element,
				TextEditChangeType.TextEditChangeType_CompositionFinalized,
				null,
				finalizedHandler);
			automation.AddAutomationEventHandler(
				UIA_TextEdit_ConversionTargetChangedEventId,
				editor,
				TreeScope.TreeScope_Element,
				null,
				conversionHandler);
			Check(true, "external TextEdit event handlers registered");

			try
			{
				Check(
					InvokeByAutomationId(automation, processId, "RichEditBoxUiaUpdateComposition"),
					"composition update button invoked through UIA");
				Check(
					WaitUntil(() => compositionHandler.Events.Count > 0 && conversionHandler.Count >= 1),
					"composition and conversion-target events reached the external client");
				Check(
					compositionHandler.Events.TryPeek(out var compositionEvent)
						&& compositionEvent.ChangedData.SequenceEqual(["nihaoma"]),
					"composition event carries changed text");

				Check(
					InvokeByAutomationId(automation, processId, "RichEditBoxUiaCompleteComposition"),
					"composition completion button invoked through UIA");
				Check(
					WaitUntil(() => finalizedHandler.Events.Count > 0 && conversionHandler.Count >= 2),
					"composition-finalized and conversion-target events reached the external client");
			}
			finally
			{
				automation3.RemoveTextEditTextChangedEventHandler(editor, compositionHandler);
				automation3.RemoveTextEditTextChangedEventHandler(editor, finalizedHandler);
				automation.RemoveAutomationEventHandler(
					UIA_TextEdit_ConversionTargetChangedEventId,
					editor,
					conversionHandler);
			}
		}

		var textChangedHandler = new AutomationEventHandler(UIA_Text_TextChangedEventId);
		automation.AddAutomationEventHandler(
			UIA_Text_TextChangedEventId,
			editor,
			TreeScope.TreeScope_Element,
			null,
			textChangedHandler);
		try
		{
			Check(
				InvokeByAutomationId(automation, processId, "RichEditBoxUiaInsertPrefix"),
				"document edit button invoked through UIA");
			Check(
				WaitUntil(() => textChangedHandler.Count >= 1),
				"Text pattern changed event reached the external client");
		}
		finally
		{
			automation.RemoveAutomationEventHandler(
				UIA_Text_TextChangedEventId,
				editor,
				textChangedHandler);
		}
		Thread.Sleep(300);

		Check(retainedSecondLinkRange?.GetText(-1) == "second-link", "retained hyperlink range rebases after a prefix edit");
		Check(retainedSecondLinkClone?.GetText(-1) == "second-link", "retained cloned range rebases after a prefix edit");
		Check(retainedLinkTextChildRange?.GetText(-1) == "first-link", "retained TextChild range rebases after a prefix edit");
		Check(retainedImageTextChildRange?.GetText(-1) == "fixture image", "retained image TextChild range rebases after a prefix edit");
		Check(
			retainedSecondLinkRange is not null
				&& retainedSecondLinkClone is not null
				&& retainedSecondLinkRange.Compare(retainedSecondLinkClone) != 0,
			"retained range and clone remain equal after a prefix edit");
		Check(
			retainedSecondLinkRange is not null
				&& retainedSecondLinkClone is not null
				&& retainedSecondLinkRange.CompareEndpoints(
					TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start,
					retainedSecondLinkClone,
					TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start) == 0,
			"retained range endpoints remain synchronized after a prefix edit");
		var retainedLinkAttribute = retainedSecondLinkRange?.GetAttributeValue(UIA_LinkAttributeId);
		if (nativeComparison)
		{
			Console.WriteLine($"INFO: native retained link attribute={retainedLinkAttribute ?? "(null)"}");
		}
		else
		{
			Check(retainedLinkAttribute is true, "retained range preserves its link attribute");
		}
		Check(
			documentRange.FindText("second-link", 1, 0)?.GetText(-1) == "second-link",
			"retained document range finds text after a prefix edit");
		if (nativeComparison)
		{
			try
			{
				var nativeBackwardLink = documentRange.FindAttribute(UIA_LinkAttributeId, true, 1);
				Console.WriteLine($"INFO: native backward LinkAttribute range={nativeBackwardLink?.GetText(-1) ?? "(null)"}");
			}
			catch (COMException error)
			{
				Console.WriteLine($"INFO: native LinkAttribute query is unsupported ({error.HResult:X8})");
			}
		}
		else
		{
			var backwardLink = documentRange.FindAttribute(UIA_LinkAttributeId, true, 1);
			Check(backwardLink?.GetText(-1) == "second-link", "retained document range finds the final link attribute span");
		}

		var rebasedChildren = GetTextChildren(documentRange);
		Check(
			firstChildren.FirstLink is not null
				&& rebasedChildren.FirstLink is not null
				&& automation.CompareElements(firstChildren.FirstLink, rebasedChildren.FirstLink) != 0,
			"first hyperlink provider identity survives a prefix edit");
		if (!nativeComparison)
		{
			Check(
				firstChildren.SecondLink is not null
					&& rebasedChildren.SecondLink is not null
					&& automation.CompareElements(firstChildren.SecondLink, rebasedChildren.SecondLink) != 0,
				"second hyperlink provider identity survives a prefix edit");
		}
		linkRange = firstChildren.FirstLink is null ? null : text?.RangeFromChild(firstChildren.FirstLink);
		Check(linkRange?.GetText(-1) == "first-link", "RangeFromChild rebases after a document edit");

		Check(
			InvokeByAutomationId(automation, processId, "RichEditBoxUiaRemoveFirstDuplicateLink"),
			"first duplicate-target hyperlink removal button invoked through UIA");
		Thread.Sleep(300);
		var afterRemoval = GetTextChildren(documentRange);
		Check(afterRemoval.FirstLink is null, "removed hyperlink no longer appears in the document children");
		if (!nativeComparison)
		{
			Check(
				firstChildren.SecondLink is not null
					&& afterRemoval.SecondLink is not null
					&& automation.CompareElements(firstChildren.SecondLink, afterRemoval.SecondLink) != 0,
				"surviving same-target hyperlink retains provider identity");
			Check(
				firstChildren.FirstLink is not null
					&& afterRemoval.SecondLink is not null
					&& automation.CompareElements(firstChildren.FirstLink, afterRemoval.SecondLink) == 0,
				"removed hyperlink provider is not reassigned to the survivor");
		}
		Check(
			firstChildren.FirstLink is null || TryRangeFromChild(text, firstChildren.FirstLink) is null,
			"removed hyperlink provider is invalid");
		if (!nativeComparison)
		{
			Check(
				firstChildren.SecondLink is not null
					&& TryRangeFromChild(text, firstChildren.SecondLink)?.GetText(-1) == "second-link",
				"surviving hyperlink provider still maps to its range");
		}
		Check(
			retainedSecondLinkRange?.GetText(-1) == "second-link",
			"retained hyperlink range survives duplicate-object removal");
		var removedTextChildRangeText = TryGetText(retainedLinkTextChildRange, out var removedTextChildRangeError);
		if (nativeComparison)
		{
			Console.WriteLine(
				$"INFO: native retained removed TextChild range=" +
				$"{removedTextChildRangeError?.HResult.ToString("X8") ?? removedTextChildRangeText ?? "(null)"}");
		}
		else
		{
			Check(removedTextChildRangeText == string.Empty, "retained TextChild range collapses after removal");
		}
		var retainedImageText = TryGetText(retainedImageTextChildRange, out var retainedImageTextError);
		if (nativeComparison)
		{
			Console.WriteLine(
				$"INFO: native retained image TextChild range=" +
				$"{retainedImageTextError?.HResult.ToString("X8") ?? retainedImageText ?? "(null)"}");
		}
		else
		{
			Check(retainedImageText == "fixture image", "retained image TextChild range survives unrelated removal");
		}
		if (linkTextChild is not null)
		{
			try
			{
				var removedRange = linkTextChild.TextRange?.GetText(-1);
				Check(nativeComparison || removedRange == string.Empty, "removed TextChild provider is not reassigned");
			}
			catch (COMException error)
			{
				Check(nativeComparison, $"native removed TextChild provider is invalid ({error.HResult:X8})");
			}
		}

		Check(
			InvokeByAutomationId(automation, processId, "RichEditBoxUiaInsertMiddle"),
			"middle insertion button invoked through UIA");
		Check(
			WaitUntil(() => retainedSecondLinkRange?.GetText(-1) == "second-link"),
			"retained hyperlink range rebases after a middle insertion");
		Check(
			InvokeByAutomationId(automation, processId, "RichEditBoxUiaDeleteMiddle"),
			"middle deletion button invoked through UIA");
		Check(
			WaitUntil(() => retainedSecondLinkRange?.GetText(-1) == "second-link"),
			"retained hyperlink range rebases after a middle deletion");
		Check(
			InvokeByAutomationId(automation, processId, "RichEditBoxUiaUndo"),
			"undo button invoked through UIA");
		Check(
			WaitUntil(() => retainedSecondLinkRange?.GetText(-1) == "second-link"),
			"retained hyperlink range rebases after undo");

		if (retainedSecondLinkRange is not null)
		{
			var moved = retainedSecondLinkRange.Clone();
			Check(
				moved.MoveEndpointByUnit(
					TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start,
					TextUnit.TextUnit_Character,
					1) == 1
					&& moved.GetText(-1) == "econd-link",
				"MoveEndpointByUnit uses the rebased retained endpoints");
			moved.MoveEndpointByRange(
				TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start,
				retainedSecondLinkRange,
				TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start);
			Check(moved.Compare(retainedSecondLinkRange) != 0, "MoveEndpointByRange restores a retained endpoint");
			Check(
				moved.Move(TextUnit.TextUnit_Character, 1) == 1
					&& moved.Compare(retainedSecondLinkRange) == 0,
				"Move uses the rebased retained range");

			var expanded = retainedSecondLinkRange.Clone();
			expanded.ExpandToEnclosingUnit(TextUnit.TextUnit_Format);
			if (nativeComparison)
			{
				Console.WriteLine($"INFO: native expanded format text={expanded.GetText(-1)}");
			}
			else
			{
				Check(expanded.GetText(-1) == "second-link", "ExpandToEnclosingUnit uses the rebased retained range");
			}
			retainedSecondLinkRange.Select();
			var selected = text?.GetSelection();
			Check(
				selected is not null
					&& selected.Length > 0
					&& selected.GetElement(0).Compare(retainedSecondLinkRange) != 0,
				"Select uses the rebased retained range");
		}

		if (!nativeComparison)
		{
			Check(textEdit?.GetActiveComposition() is null, "active composition clears after finalization");
			Check(textEdit?.GetConversionTarget() is null, "conversion target clears after finalization");
		}

		if (retainedSecondLinkRange is IUIAutomationTextRange2 range2)
		{
			try
			{
				range2.ShowContextMenu();
				Check(true, "TextRange2 ShowContextMenu completes");
				var status = WaitForElement(automation, processId, "RichEditBoxUiaStatus");
				Check(
					status is not null
						&& WaitUntil(() => status.CurrentName.StartsWith("Context menu opening at ", StringComparison.Ordinal)),
					"TextRange2 routes to the RichEditBox context menu");
			}
			catch (Exception error) when (error is COMException or NotImplementedException)
			{
				Console.WriteLine($"INFO: native TextRange2 ShowContextMenu error={error.HResult:X8}");
				Check(nativeComparison && error is NotImplementedException, "native TextRange2 ShowContextMenu reports not implemented");
			}
		}
		else
		{
			Check(false, "retained text range exposes TextRange2");
		}

		return _failures == 0 ? 0 : 1;
	}

	private static void ProbeFormattingAttributes(
		IUIAutomationTextRange documentRange,
		IUIAutomationTextPattern? text,
		IUIAutomation automation,
		bool nativeComparison)
	{
		var range = documentRange.FindText("format-one", 0, 0);
		Check(range is not null, "formatting probe range is available");
		if (range is null)
		{
			return;
		}

		var values = new Dictionary<string, object?>
		{
			["BackgroundColor"] = range.GetAttributeValue(UIA_BackgroundColorAttributeId),
			["BulletStyle"] = range.GetAttributeValue(UIA_BulletStyleAttributeId),
			["IndentationLeading"] = range.GetAttributeValue(UIA_IndentationLeadingAttributeId),
			["IndentationTrailing"] = range.GetAttributeValue(UIA_IndentationTrailingAttributeId),
			["Tabs"] = range.GetAttributeValue(UIA_TabsAttributeId),
			["TextFlowDirections"] = range.GetAttributeValue(UIA_TextFlowDirectionsAttributeId),
			["MixedBackground"] = documentRange.GetAttributeValue(UIA_BackgroundColorAttributeId),
			["MixedBullet"] = documentRange.GetAttributeValue(UIA_BulletStyleAttributeId),
		};

		range.Select();
		var selection = text?.GetSelection();
		values["SelectionActiveEnd"] = selection is { Length: > 0 }
			? selection.GetElement(0).GetAttributeValue(UIA_SelectionActiveEndAttributeId)
			: null;

		if (nativeComparison)
		{
			foreach (var (name, value) in values)
			{
				Console.WriteLine($"INFO: native {name}={DescribeAttributeValue(value, automation)}");
			}
			return;
		}

		Check(Convert.ToInt32(values["BackgroundColor"]) == 255, "BackgroundColor maps TOM red to COLORREF");
		Check(Convert.ToInt32(values["BulletStyle"]) == 5, "BulletStyle maps a minus marker to DashBullet");
		Check(Convert.ToDouble(values["IndentationLeading"]) == 9, "leading indent is exposed");
		Check(Convert.ToDouble(values["IndentationTrailing"]) == 12, "trailing/right indent is exposed");
		Check(
			values["Tabs"] is double[] tabs
				&& tabs.SequenceEqual([24d, 48d]),
			"tab stop positions are exposed");
		Check(Convert.ToInt32(values["TextFlowDirections"]) == 1, "right-to-left text flow is exposed");
		Check(
			IsSameComObject(values["MixedBackground"], automation.ReservedMixedAttributeValue),
			"mixed BackgroundColor returns the UIA mixed sentinel");
		Check(
			IsSameComObject(values["MixedBullet"], automation.ReservedMixedAttributeValue),
			"mixed BulletStyle returns the UIA mixed sentinel");
		Check(Convert.ToInt32(values["SelectionActiveEnd"]) == 2, "selected range exposes End as the active end");
	}

	private static void ProbeEnclosingElements(
		IUIAutomation automation,
		IUIAutomationElement editor,
		IUIAutomationElement child,
		IUIAutomationTextRange exact,
		string label)
	{
		CheckEnclosingElement(automation, editor, child, exact, expectChild: true, $"{label} exact");

		var interior = exact.Clone();
		interior.MoveEndpointByUnit(TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start, TextUnit.TextUnit_Character, 1);
		interior.MoveEndpointByUnit(TextPatternRangeEndpoint.TextPatternRangeEndpoint_End, TextUnit.TextUnit_Character, -1);
		CheckEnclosingElement(automation, editor, child, interior, expectChild: true, $"{label} interior");

		var partial = exact.Clone();
		partial.MoveEndpointByUnit(TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start, TextUnit.TextUnit_Character, -1);
		CheckEnclosingElement(automation, editor, child, partial, expectChild: false, $"{label} partial");

		var caret = exact.Clone();
		caret.MoveEndpointByUnit(TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start, TextUnit.TextUnit_Character, 1);
		caret.MoveEndpointByRange(
			TextPatternRangeEndpoint.TextPatternRangeEndpoint_End,
			caret,
			TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start);
		if (label == "link")
		{
			CheckEnclosingElement(automation, editor, child, caret, expectChild: true, $"{label} caret");
		}
		else
		{
			var enclosing = caret.GetEnclosingElement();
			Console.WriteLine(
				$"INFO: image caret enclosing type={enclosing.CurrentControlType}; name={enclosing.CurrentName}");
		}
	}

	private static void CheckEnclosingElement(
		IUIAutomation automation,
		IUIAutomationElement editor,
		IUIAutomationElement child,
		IUIAutomationTextRange range,
		bool expectChild,
		string label)
	{
		var enclosing = range.GetEnclosingElement();
		Check(
			expectChild
				? automation.CompareElements(enclosing, child) != 0
				: automation.CompareElements(enclosing, editor) != 0,
			$"{label} range returns the {(expectChild ? "child" : "editor")} enclosing element");
	}

	private static string? TryGetText(IUIAutomationTextRange? range, out Exception? error)
	{
		try
		{
			error = null;
			return range?.GetText(-1);
		}
		catch (Exception caught) when (caught is COMException or NotImplementedException)
		{
			error = caught;
			return null;
		}
	}

	private static T? GetPattern<T>(IUIAutomationElement element, int patternId)
		where T : class
	{
		try
		{
			return element.GetCurrentPattern(patternId) as T;
		}
		catch (COMException)
		{
			return null;
		}
	}

	private static IUIAutomationElement? WaitForElement(
		IUIAutomation automation,
		int processId,
		string automationId)
	{
		var root = automation.GetRootElement();
		var idCondition = automation.CreatePropertyCondition(UIA_AutomationIdPropertyId, automationId);
		var condition = processId > 0
			? automation.CreateAndCondition(
				automation.CreatePropertyCondition(UIA_ProcessIdPropertyId, processId),
				idCondition)
			: idCondition;
		for (var attempt = 0; attempt < 200; attempt++)
		{
			IUIAutomationElement? element;
			try
			{
				element = root.FindFirst(TreeScope.TreeScope_Subtree, condition);
			}
			catch (COMException)
			{
				Thread.Sleep(100);
				continue;
			}
			if (element is not null)
			{
				return element;
			}

			Thread.Sleep(100);
		}

		return null;
	}

	private static bool InvokeByAutomationId(
		IUIAutomation automation,
		int processId,
		string automationId)
	{
		var element = WaitForElement(automation, processId, automationId);
		var invoke = element is null ? null : GetPattern<IUIAutomationInvokePattern>(element, UIA_InvokePatternId);
		if (invoke is null)
		{
			return false;
		}

		invoke.Invoke();
		return true;
	}

	private static (
		IUIAutomationElement? FirstLink,
		IUIAutomationElement? SecondLink,
		IUIAutomationElement? Image) GetTextChildren(
		IUIAutomationTextRange range)
	{
		IUIAutomationElement? firstLink = null;
		IUIAutomationElement? secondLink = null;
		IUIAutomationElement? image = null;
		var children = range.GetChildren();
		if (children is null)
		{
			return (null, null, null);
		}

		for (var index = 0; index < children.Length; index++)
		{
			var child = children.GetElement(index);
			switch (child.CurrentControlType)
			{
				case UIA_HyperlinkControlTypeId:
					if (child.CurrentName == "first-link")
					{
						firstLink = child;
					}
					else if (child.CurrentName == "second-link")
					{
						secondLink = child;
					}
					break;
				case UIA_ImageControlTypeId:
					image = child;
					break;
			}
		}

		return (firstLink, secondLink, image);
	}

	private static IUIAutomationTextRange? TryRangeFromChild(
		IUIAutomationTextPattern? text,
		IUIAutomationElement child)
	{
		try
		{
			return text?.RangeFromChild(child);
		}
		catch (COMException)
		{
			return null;
		}
	}

	private static IUIAutomationElement? TryGetFirstChild(IUIAutomationTextRange? range)
	{
		try
		{
			var children = range?.GetChildren();
			return children is { Length: > 0 } ? children.GetElement(0) : null;
		}
		catch (ArgumentException)
		{
			return null;
		}
	}

	private static bool IsEmptyOrNotSupported(object? value, IUIAutomation automation)
	{
		var isEmptyOrNotSupported = value is Array { Length: 0 }
			|| IsSameComObject(value, automation.ReservedNotSupportedValue);
		if (!isEmptyOrNotSupported)
		{
			Console.Error.WriteLine(
				$"INFO: unexpected annotation value type={value?.GetType().FullName ?? "(null)"}, " +
				$"length={(value as Array)?.Length.ToString() ?? "n/a"}");
		}

		return isEmptyOrNotSupported;
	}

	private static string DescribeAttributeValue(object? value, IUIAutomation automation)
	{
		if (IsSameComObject(value, automation.ReservedMixedAttributeValue))
		{
			return "Mixed";
		}
		if (IsSameComObject(value, automation.ReservedNotSupportedValue))
		{
			return "NotSupported";
		}
		if (value is Array array)
		{
			return $"[{string.Join(", ", array.Cast<object?>().Select(item => item?.ToString() ?? "null"))}]";
		}
		return value?.ToString() ?? "null";
	}

	private static bool IsSameComObject(object? left, object? right)
	{
		if (left is null || right is null)
		{
			return false;
		}

		nint leftIdentity = 0;
		nint rightIdentity = 0;
		try
		{
			leftIdentity = Marshal.GetIUnknownForObject(left);
			rightIdentity = Marshal.GetIUnknownForObject(right);
			return leftIdentity == rightIdentity;
		}
		finally
		{
			if (leftIdentity != 0)
			{
				Marshal.Release(leftIdentity);
			}
			if (rightIdentity != 0)
			{
				Marshal.Release(rightIdentity);
			}
		}
	}

	private static bool WaitUntil(Func<bool> condition)
	{
		for (var attempt = 0; attempt < 50; attempt++)
		{
			if (condition())
			{
				return true;
			}

			Thread.Sleep(100);
		}

		return false;
	}

	private static void Check(bool condition, string message)
	{
		if (condition)
		{
			Console.WriteLine($"PASS: {message}");
		}
		else
		{
			Console.Error.WriteLine($"FAIL: {message}");
			_failures++;
		}
	}

	[ComVisible(true)]
	[ClassInterface(ClassInterfaceType.None)]
	private sealed class StructureChangedEventHandler : IUIAutomationStructureChangedEventHandler
	{
		internal int Count => Volatile.Read(ref _count);

		private int _count;

		public void HandleStructureChangedEvent(
			IUIAutomationElement sender,
			StructureChangeType changeType,
			int[] runtimeId)
			=> Interlocked.Increment(ref _count);
	}

	[ComVisible(true)]
	[ClassInterface(ClassInterfaceType.None)]
	private sealed class PropertyChangedEventHandler : IUIAutomationPropertyChangedEventHandler
	{
		private readonly int _propertyId;
		internal int Count => Volatile.Read(ref _count);
		internal object? LastValue { get; private set; }
		internal IUIAutomationElement? LastSender { get; private set; }

		private int _count;

		internal PropertyChangedEventHandler(int propertyId)
		{
			_propertyId = propertyId;
		}

		public void HandlePropertyChangedEvent(
			IUIAutomationElement sender,
			int propertyId,
			object newValue)
		{
			if (propertyId == _propertyId)
			{
				LastSender = sender;
				LastValue = newValue;
				Interlocked.Increment(ref _count);
			}
		}
	}

	[ComVisible(true)]
	[ClassInterface(ClassInterfaceType.None)]
	private sealed class TextEditEventHandler : IUIAutomationTextEditTextChangedEventHandler
	{
		internal ConcurrentQueue<(TextEditChangeType ChangeType, string[] ChangedData)> Events { get; } = new();

		public void HandleTextEditTextChangedEvent(
			IUIAutomationElement sender,
			TextEditChangeType textEditChangeType,
			string[] eventStrings)
			=> Events.Enqueue((textEditChangeType, eventStrings));
	}

	[ComVisible(true)]
	[ClassInterface(ClassInterfaceType.None)]
	private sealed class AutomationEventHandler : IUIAutomationEventHandler
	{
		private readonly int _eventId;
		internal int Count => Volatile.Read(ref _count);

		private int _count;

		internal AutomationEventHandler(int eventId)
		{
			_eventId = eventId;
		}

		public void HandleAutomationEvent(IUIAutomationElement sender, int eventId)
		{
			if (eventId == _eventId)
			{
				Interlocked.Increment(ref _count);
			}
		}
	}
}
