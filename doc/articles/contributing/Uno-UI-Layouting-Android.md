---
uid: Uno.Contributing.Layouting.Android
---

# Layouting in Android

The layouting cycle (measure and arrange) in Uno on Android involves a complex interaction between Android UI framework methods and Uno
methods. These interactions are summarized in the diagram below. This information is primarily intended to help when debugging Uno, but
may be interesting to anyone curious as to how native Android methods are connected to the WinUI contract exposed by Uno.

```mermaid
flowchart TD
    %% Android layout flow

    subgraph Measure Invalidation
        uielement.invalidatearrange["UIElement.InvalidateArrange()"]
        uielement.invalidatemeasure["UIElement.InvalidateMeasure()"]
        base.requestlayout{{"View.RequestLayout()"}}
        root.requestlayout{{"ViewRootImpl.RequestLayout()"}}
        root.scheduletraversal{{"ViewRootImpl.ScheduleTraversals()"}}

        uielement.invalidatearrange -. "actually calls" .-> uielement.invalidatemeasure
        uielement.invalidatemeasure == set IS_DIRTY ==> base.requestlayout
        uielement.invalidatemeasure -. parent: set IS_DIRTY_PATH .-> uielement.invalidatemeasure

        base.requestlayout -. internally on parent .-> base.requestlayout
        base.requestlayout ==> root.requestlayout
        root.requestlayout ==> root.scheduletraversal
    end

    root.scheduletraversal -. on next animation loop .-> loop

    subgraph loop["UI Loop Scheduling"]
        root.performtraversal{{"ViewRootImpl.PerformTraversal()"}}

        root.performtraversal ==> root.performmeasure
        root.performtraversal ==> root.performlayout

        subgraph Measure Phase
            root.performmeasure{{"ViewRootImpl.PerformMeasure()"}}
            view.measure{{"View.Measure()"}}
            view.onmeasure{{"View.OnMeasure()"}}

            onmeasure["(override) FrameworkElement.OnMeasure()"]
            layouter.domeasure["ILayouterElement.DoMeasure()"]
            layouter.measureoverride["Layouter.MeasureOverride()"]
            measureoverride[["(override) Element.MeasureOverride()"]]

            measureelement["FrameworkElement.MeasureElement()"]
            layouter.measurechild["Layouter.MeasureChild()"]
            layouter.measurechildoverride["Layouter.MeasureChildOverride()"]
            view.setmeasureddimension{{"View.SetMeasuredDimension()"}}
            view.layout{{"View.Layout()"}}

            root.performmeasure == top-level element ==> view.measure
            view.measure ==> view.onmeasure

            view.onmeasure ==> onmeasure
            onmeasure ==> layouter.domeasure
            layouter.domeasure == IS_DIRTY set ==> layouter.measureoverride
            layouter.domeasure == "IS_DIRTY_PATH set:<br>call for children<br>with previous availableSize" ==> layouter.measurechild

            layouter.measureoverride ==> measureoverride
            measureoverride == "for children (generally)" ==> measureelement
            measureelement ==> layouter.measurechild

            layouter.measurechild ==> layouter.measurechildoverride
            layouter.measurechildoverride ==> view.layout
            layouter.measurechildoverride -. "(return value will set)" .-> view.setmeasureddimension
            view.layout ==> view.measure
        end

        view.layout -..-> arrange

        subgraph arrange ["Arrange Phase"]
            root.performlayout{{"View.PerformLayout()"}}
            view.onlayout{{"View.OnLayout()"}}
            unoviewgroup.onlayoutcore["UnoViewGroup.OnLayoutCore() - abstract"]
            onlayoutcore["(override) FrameworkElement.OnLayoutCore()"]
            layouter.arrange["Layouter.Arrange()"]
            layouter.arrangeoverride["Layouter.ArrangeOverride()"]
            arrangeoverride[["Element.ArrangeOverride()"]]
            arrangeelement["FrameworkElement.ArrangeElement(child)"]
            layouter.arrangechild["Layouter.ArrangeChild()"]
            layouter.arrangechildoverride["Layouter.ArrangeChildOverride()"]

            root.performlayout == x ==> view.onlayout
            view.onlayout == "through OnLayout() override" ==> unoviewgroup.onlayoutcore
            unoviewgroup.onlayoutcore ==> onlayoutcore
            onlayoutcore ==> layouter.arrange
            layouter.arrange ==> layouter.arrangeoverride
            layouter.arrangeoverride ==> arrangeoverride
            arrangeoverride == " for children (generally) " ==> arrangeelement
            arrangeelement ==> layouter.arrangechild
            layouter.arrangechild ==> layouter.arrangechildoverride
            layouter.arrangechildoverride -..-> view.onlayout
        end
    end

    subgraph legend
    direction LR
    uno-legend["Uno methods"]
    native-legend{{"Native (Android) methods"}}
    application-legend[["Application/Framework implementation"]]
    end
```
