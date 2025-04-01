---
uid: Uno.Contributing.Layouting.iOS
---

# Layouting in iOS

The layouting cycle (measure and arrange) in Uno on iOS is a mingling of native layouting logic and logic in managed code. These interactions are summarized in the diagram below. This information is primarily intended to help when debugging Uno, but may also be useful when attempting to incorporate non-Uno views into the visual tree.

```mermaid
flowchart TD
  %% ios layout flow

  basesetneedslayout{{"base.SetNeedsLayout()"}}
  layoutsublayers{{"CALayer.LayoutSublayers"}}
  layoutsubviews["(override) FrameworkElement.LayoutSubviews()"]

  subgraph Measure Invalidation
    invalidate-arrange["UIElement.InvalidateArrange()"]
    invalidate-measure["IFrameworkElementHelper.InvalidateMeasure()"]
    setneedslayout["(override) FrameworkElement.SetNeedsLayout()"]
    setsuperviewneedslayout["FrameworkElement.SetSuperviewNeedsLayout()"]

    native.sizethatfits{{".SizeThatFits() called from parent element"}}

    invalidate-arrange ==> invalidate-measure
    invalidate-measure ==> setneedslayout

    setneedslayout --> setsuperviewneedslayout
    setsuperviewneedslayout -. on parent element.-> setneedslayout

    setneedslayout ==> basesetneedslayout
  end

  subgraph Measure Phase
    sizethatfits["(override) FrameworkElement.SizeThatFits(availableSize)"]
    xamlmeasure["FrameworkElement.XamlMeasure(availableSize)"]
    layouter.measure["Layouter.Measure(availableSize)"]
    layouter.measureoverride["Layouter.MeasureOverride(availableSize)"]
    measureoverride["FrameworkELement.MeasureOverride(availableSize)<br><em>overridden by element</em>"]
    appmeasureoverrride[["local implementation of MeasureOverride(availableSize)<br>"]]

    subgraph Subelement measuring
      measureelement["FrameworkElement.MeasureElement(child)"]
      layouter.measureelement["Layouter.MeasureElement(child)"]
      measurechildoverride["Layouter.MeasureChildOverride"]

      measureelement ==> layouter.measureelement
      layouter.measureelement ==> measurechildoverride
    end

    sizethatfits ==> xamlmeasure
    xamlmeasure ==> layouter.measure
    layouter.measure ==> layouter.measureoverride
    layoutsubviews ==> xamlmeasure
    layouter.measureoverride ==> measureoverride
    measureoverride ==> appmeasureoverrride
    appmeasureoverrride -- for child elements --> measureelement
  end

  basesetneedslayout -. "schedule for next loop (native)" .-> layoutsublayers
  layoutsublayers ==> layoutsubviews
  native.sizethatfits ==> sizethatfits
  measurechildoverride ==> sizethatfits

  frame -. "internal scheduling for next loop (native)" .-> layoutsublayers

  subgraph Arrange Phase
    layouter.arrange["Layouter.Arrange(finalRect)"]
    arrangeoverride["FrameworkElement.ArrangeOverride(finalSize)"]
    frame{{".Frame property set"}}

    subgraph for child elements
      arrangeelement["Framework.ArrangeElement(child)"]
      layouter.arrangeelement
      app.arrangeoverride[["(override) ArrangeOverride(finalSize)"]]

      layouter.arrangechild
      layouter.arrangechildoverride
    end

    layouter.arrange ==> arrangeoverride
    arrangeoverride ==> app.arrangeoverride
    app.arrangeoverride ==> arrangeelement

    arrangeelement ==> layouter.arrangeelement
    layouter.arrangeelement ==> layouter.arrangechild
    layouter.arrangechild ==> layouter.arrangechildoverride

    layouter.arrangechildoverride ==> frame

    layoutsubviews ==> layouter.arrange
  end

  subgraph legend
    direction LR
    uno-legend["Uno methods"]
    native-legend{{"Native (iOS) methods"}}
    application-legend[["Application/Framework implementation"]]
  end
```
