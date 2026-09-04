declare namespace Microsoft.UI.Xaml.Media {
    class CompositionTarget {
        static requestRender: any;
        static buildImports(): Promise<void>;
        static requestFrame(): void;
    }
}
declare namespace Uno.UI {
    class ExportManager {
        static initialize(): Promise<void>;
    }
}
declare namespace Uno.Utils {
    class Guid {
        private static newGuidMethod;
        static NewGuid(): string;
    }
}
declare namespace Uno.UI {
    class HtmlDom {
        /**
         * Initialize various polyfills used by Uno
         */
        static initPolyfills(): void;
        private static isConnectedPolyfill;
    }
}
declare module Uno.UI {
    enum HtmlEventDispatchResult {
        Ok = 0,
        StopPropagation = 1,
        PreventDefault = 2,
        NotDispatched = 128
    }
}
declare module Uno.UI {
    interface IContentDefinition {
        id: string;
        tagName: string;
        handle: number;
        uiElementRegistrationId: number;
        isSvg: boolean;
        isFocusable: boolean;
    }
}
declare namespace MonoSupport {
    /**
     * This class is used by https://github.com/mono/mono/blob/fa726d3ac7153d87ed187abd422faa4877f85bb5/sdks/wasm/dotnet_support.js#L88 to perform
     * unmarshaled invocation of javascript from .NET code.
     * */
    class jsCallDispatcher {
        private static registrations;
        private static methodMap;
        private static _isUnoRegistered;
        private static dispatcherCallback;
        /**
         * Registers a instance for a specified identier
         * @param identifier the scope name
         * @param instance the instance to use for the scope
         */
        static registerScope(identifier: string, instance: any): void;
        static invokeJSUnmarshalled(funcName: string, arg0: any, arg1: any, arg2: any): void | number;
        static findJSFunction(identifier: string): any;
        /**
         * Internal dispatcher for methods invoked through TSInteropMarshaller
         * @param id The method ID obtained when invoking WebAssemblyRuntime.InvokeJSUnmarshalled with a method name
         * @param pParams The parameters structure ID
         * @param pRet The pointer to the return value structure
         */
        private static dispatch;
        /**
         * Parses the method identifier
         * @param identifier
         */
        private static parseIdentifier;
        /**
         * Adds the a resolved method for a given identifier
         * @param identifier the findJSFunction identifier
         * @param boundMethod the method to call
         */
        private static cacheMethod;
        private static getMethodMapId;
        static invokeOnMainThread(): void;
    }
}
declare const config: any;
declare namespace Uno.UI {
    class WindowManager {
        private containerElementId;
        private loadingElementId;
        static current: WindowManager;
        private static readonly unoRootClassName;
        private static readonly unoUnarrangedClassName;
        private static readonly unoCollapsedClassName;
        private static readonly unoPersistentLoaderClassName;
        private static readonly unoKeepLoaderClassName;
        /**
            * Initialize the WindowManager
            * @param containerElementId The ID of the container element for the Xaml UI
            * @param loadingElementId The ID of the loading element to remove once ready
            */
        static init(containerElementId?: string, loadingElementId?: string): Promise<void>;
        /**
         * Builds a promise that will signal the ability for the dispatcher
         * to initiate work.
         * */
        private static buildReadyPromise;
        /**
         * Build the splashscreen image eagerly
         * */
        private static buildSplashScreen;
        private containerElement;
        private rootElement;
        private cursorStyleRule;
        private allActiveElementsById;
        /** Native elements created with the BrowserHtmlElement class */
        private nativeHandlersMap;
        private uiElementRegistrations;
        private static resizeMethod;
        private static dispatchEventMethod;
        private static dispatchEventNativeElementMethod;
        private static focusInMethod;
        private static dispatchSuspendingMethod;
        private static getDependencyPropertyValueMethod;
        private static setDependencyPropertyValueMethod;
        private static keyTrackingMethod;
        private constructor();
        /**
            * Creates the UWP-compatible splash screen
            *
            */
        static setupSplashScreen(splashImage: HTMLImageElement): void;
        static setBodyCursor(value: string): void;
        static setSingleLine(htmlId: number): void;
        /**
            * Reads the window's search parameters
            *
            */
        static beforeLaunch(): string;
        /**
            * Estimated application startup time
            */
        static getBootTime(): number;
        containsPoint(htmlId: number, x: number, y: number, considerFill: boolean, considerStroke: boolean): boolean;
        /**
            * Create a html DOM element representing a Xaml element.
            *
            * You need to call addView to connect it to the DOM.
            */
        createContentNativeFast(htmlId: number, tagName: string, uiElementRegistrationId: number, isFocusable: boolean, isSvg: boolean): void;
        private createContentInternal;
        registerUIElement(typeName: string, isFrameworkElement: boolean, classNames: string[]): number;
        getView(elementHandle: number): HTMLElement | SVGElement;
        /**
            * Set a name for an element.
            *
            * This is mostly for diagnostic purposes.
            */
        setNameNative(pParam: number): boolean;
        private setNameInternal;
        /**
            * Set a name for an element.
            *
            * This is mostly for diagnostic purposes.
            */
        setXUidNative(pParam: number): boolean;
        private setXUidInternal;
        setVisibilityNativeFast(htmlId: number, visible: boolean): void;
        private setVisibilityInternal;
        /**
            * Set an attribute for an element.
            */
        setAttributesNativeFast(htmlId: number, pairs: string[]): void;
        /**
            * Set an attribute for an element.
            */
        setAttribute(htmlId: number, name: string, value: string): void;
        /**
            * Removes an attribute for an element.
            */
        removeAttributeNative(pParams: number): boolean;
        /**
            * Get an attribute for an element.
            */
        getAttribute(elementId: number, name: string): string;
        /**
            * Set a property for an element.
            */
        setPropertyNativeFast(htmlId: number, pairs: string[]): void;
        setSinglePropertyNativeFast(htmlId: number, name: string, value: string): void;
        /**
            * Get a property for an element.
            */
        getProperty(elementId: number, name: string): string;
        /**
        * Set the CSS style of a html element.
        *
        * To remove a value, set it to empty string.
        * @param styles A dictionary of styles to apply on html element.
        */
        setStyleNativeFast(htmlId: number, styles: string[]): void;
        /**
        * Set a single CSS style of a html element
        *
        */
        setStyleDoubleNative(pParams: number): boolean;
        setStyleStringNativeFast(htmlId: number, name: string, value: string): void;
        /**
            * Remove the CSS style of a html element.
            */
        resetStyle(elementId: number, names: string[]): void;
        isCssConditionSupported(supportCondition: string): boolean;
        /**
         * Set + Unset CSS classes on an element
         */
        setUnsetCssClasses(elementId: number, cssClassesToSet: string[], cssClassesToUnset: string[]): void;
        /**
         * Set CSS classes on an element from a specified list
         */
        setClasses(elementId: number, cssClassesList: string[], classIndex: number): void;
        /**
        * Arrange and clips a native elements
        *
        */
        arrangeElementNativeFast(htmlId: number, top: number, left: number, width: number, height: number, clip: boolean, clipTop: number, clipLeft: number, clipBottom: number, clipRight: number): void;
        private setAsArranged;
        private setAsUnarranged;
        /**
        * Sets the color property of the specified element
        */
        setElementColorNative(pParam: number): boolean;
        private setElementColorInternal;
        /**
         * Sets the element's selection highlight.
        **/
        setSelectionHighlight(elementId: number, backgroundColor: number, foregroundColor: number): boolean;
        setSelectionHighlightNative(pParam: number): boolean;
        /**
        * Sets the background color property of the specified element
        */
        setElementBackgroundColor(pParam: number): boolean;
        /**
        * Sets the background image property of the specified element
        */
        setElementBackgroundGradient(pParam: number): boolean;
        /**
        * Clears the background property of the specified element
        */
        resetElementBackground(pParam: number): boolean;
        /**
        * Sets the transform matrix of an element
        *
        */
        setElementTransformNativeFast(htmlId: number, m11: number, m12: number, m21: number, m22: number, m31: number, m32: number): void;
        setPointerEvents(htmlId: number, enabled: boolean): void;
        /**
            * Issue a browser alert to user
            * @param message message to display
            */
        alert(message: string): string;
        /**
            * Add an event handler to a html element.
            *
            * @param eventName The name of the event
            * @param onCapturePhase true means "on trickle down", false means "on bubble up". Default is false.
            */
        registerEventOnViewNative(pParams: number): boolean;
        /**
            * Add an event handler to a html element.
            *
            * @param eventName The name of the event
            * @param onCapturePhase true means "on trickle down", false means "on bubble up". Default is false.
            */
        private registerEventOnViewInternal;
        /**
         * keyboard event extractor to be used with registerEventOnView
         * @param evt
         */
        private keyboardEventExtractor;
        /**
         * tapped (mouse clicked / double clicked) event extractor to be used with registerEventOnView
         * @param evt
         */
        private tappedEventExtractor;
        /**
         * focus event extractor to be used with registerEventOnView
         * @param evt
         */
        private focusEventExtractor;
        private customEventDetailExtractor;
        private customEventDetailStringExtractor;
        /**
         * Gets the event extractor function. See UIElement.HtmlEventExtractor
         * @param eventExtractorName an event extractor name.
         */
        private getEventExtractor;
        /**
            * Set or replace the root element.
            */
        setRootElement(elementId?: number): void;
        /**
            * Set a view as a child of another one.
            * @param pParams Pointer to a WindowManagerAddViewParams native structure.
            */
        addViewNative(pParams: number): boolean;
        addViewInternal(parentId: number, childId: number, index?: number): void;
        /**
            * Remove a child from a parent element.
            */
        removeViewNative(pParams: number): boolean;
        private removeViewInternal;
        destroyViewNativeFast(htmlId: number): void;
        private destroyViewInternal;
        getBBox(elementId: number): any;
        /**
            * Use the Html engine to measure the element using specified constraints.
            *
            * @param maxWidth string containing width in pixels. Empty string means infinite.
            * @param maxHeight string containing height in pixels. Empty string means infinite.
            */
        measureViewNativeFast(htmlId: number, availableWidth: number, availableHeight: number, measureContent: boolean, pReturn: number): void;
        private static MAX_WIDTH;
        private static MAX_HEIGHT;
        private measureElement;
        private measureViewInternal;
        private createUnconstrainedStyle;
        scrollTo(pParams: number): boolean;
        rawPixelsToBase64EncodeImage(dataPtr: number, width: number, height: number): string;
        /**
         * Sets the provided image with a mono-chrome version of the provided url.
         * @param viewId the image to manipulate
         * @param url the source image
         * @param color the color to apply to the monochrome pixels
         */
        setImageAsMonochrome(viewId: number, url: string, color: string): void;
        setCornerRadius(viewId: number, topLeftX: number, topLeftY: number, topRightX: number, topRightY: number, bottomRightX: number, bottomRightY: number, bottomLeftX: number, bottomLeftY: number): void;
        focusView(elementId: number): void;
        /**
            * Set the Html content for an element.
            *
            * Those html elements won't be available as XamlElement in managed code.
            * WARNING: you should avoid mixing this and `addView` for the same element.
            */
        setHtmlContentNative(pParams: number): boolean;
        private setHtmlContentInternal;
        /**
         * Gets the Client and Offset size of the specified element
         *
         * This method is used to determine the size of the scroll bars, to
         * mask the events coming from that zone.
         */
        getClientViewSizeNative(pParams: number, pReturn: number): boolean;
        /**
         * Gets a dependency property value.
         *
         * Note that the casing of this method is intentionally Pascal for platform alignment.
         */
        GetDependencyPropertyValue(elementId: number, propertyName: string): string;
        /**
         * Sets a dependency property value.
         *
         * Note that the casing of this method is intentionally Pascal for platform alignment.
         */
        SetDependencyPropertyValue(elementId: number, propertyNameAndValue: string): string;
        /**
            * Remove the loading indicator.
            *
            * In a future version it will also handle the splashscreen.
            */
        activate(): void;
        /**
         * Creates a native element from BrowserHttpElement.
         */
        createNativeElement(elementId: string, unoElementId: number, tagname: string): void;
        /**
         * Dispose a native element
         */
        disposeNativeElement(unoElementId: number): void;
        /**
         * Attaches a native element to a known UIElement-backed element.
         */
        attachNativeElement(ownerId: number, unoElementId: number): void;
        /**
         * Detaches a native element to a known UIElement-backed element.
         */
        detachNativeElement(unoElementId: number): void;
        /**
         * Registers a managed event handler
         */
        registerNativeHtmlEvent(owner: any, unoElementId: any, eventName: string, managedHandler: any): void;
        /**
         * Unregisters a managed handler from its element
         */
        unregisterNativeHtmlEvent(unoElementId: any, eventName: string, managedHandler: any): void;
        private init;
        private static initMethods;
        private initDom;
        private removeLoading;
        private static resize;
        private onfocusin;
        private onWindowBlur;
        private dispatchEvent;
        private getIsConnectedToRootElement;
        private handleToString;
        private numberToCssColor;
        getElementInCoordinate(x: number, y: number): number;
        setCursor(cssCursor: string): string;
        getNaturalImageSize(imageUrl: string): Promise<string>;
        selectInputRange(elementId: number, start: number, length: number): void;
        getIsOverflowing(elementId: number): boolean;
        setIsFocusable(elementId: number, isFocusable: boolean): void;
        resizeWindow(width: number, height: number): void;
        moveWindow(x: number, y: number): void;
        private onBodyKeyDown;
        private onBodyKeyUp;
        private getCssColorOrUrlRef;
        setShapeFillStyle(elementId: number, color: number, paintRef: number): void;
        setShapeStrokeStyle(elementId: number, color: number, paintRef: number): void;
        setShapeStrokeWidthStyle(elementId: number, strokeWidth: number): void;
        setShapeStrokeDashArrayStyle(elementId: number, strokeDashArray: number[]): void;
        setShapeStylesFast1(elementId: number, fillColor: number, fillPaintRef: number, strokeColor: number, strokePaintRef: number): void;
        setShapeStylesFast2(elementId: number, fillColor: number, fillPaintRef: number, strokeColor: number, strokePaintRef: number, strokeWidth: number, strokeDashArray: any[]): void;
        setSvgFillRule(htmlId: number, nonzero: boolean): void;
        setSvgEllipseAttributes(htmlId: number, cx: number, cy: number, rx: number, ry: number): void;
        setSvgLineAttributes(htmlId: number, x1: number, x2: number, y1: number, y2: number): void;
        setSvgPathAttributes(htmlId: number, nonzero: boolean, data: string): void;
        setSvgPolyPoints(htmlId: number, points: number[]): void;
        setSvgRectangleAttributes(htmlId: number, x: number, y: number, width: number, height: number, rx: number, ry: number): void;
    }
}
declare namespace Uno.UI.Interop {
    class AsyncInteropHelper {
        private static dispatchResultMethod;
        private static dispatchErrorMethod;
        private static init;
        static Invoke(handle: number, promiseFunction: () => Promise<string>): void;
    }
}
declare namespace Uno.UI.Interop {
    class Emscripten {
        static assert(x: any, message: any): void;
        static warnOnce(a: any, msg?: any): void;
        static stringToUTF8Array(str: any, heap: any, outIdx: any, maxBytesToWrite: any): number;
        static stringToUTF8(str: any, outPtr: any, maxBytesToWrite: any): number;
    }
}
declare module Uno.UI {
    interface IAppManifest {
        splashScreenImage: URL;
        splashScreenColor: string;
        lightThemeBackgroundColor: string;
        darkThemeBackgroundColor: string;
        displayName: string;
    }
}
declare module Uno.UI.Interop {
    interface IMonoAssemblyHandle {
    }
}
declare module Uno.UI.Interop {
    interface IMonoClassHandle {
    }
}
declare module Uno.UI.Interop {
    interface IMonoMethodHandle {
    }
}
declare module Uno.UI.Interop {
    interface IMonoRuntime {
        assembly_load(assemblyName: string): Interop.IMonoAssemblyHandle;
        find_class(moduleHandle: Interop.IMonoAssemblyHandle, namespace: string, typeName: string): Interop.IMonoClassHandle;
        find_method(classHandle: Interop.IMonoClassHandle, methodName: string, _: number): Interop.IMonoMethodHandle;
        call_method(methodHandle: Interop.IMonoMethodHandle, object: any, params?: any[]): any;
        mono_string(str: string): Interop.IMonoStringHandle;
        conv_string(strHandle: Interop.IMonoStringHandle): string;
    }
}
declare module Uno.UI.Interop {
    interface IMonoStringHandle {
    }
}
declare module Uno.UI.Interop {
    interface IWebAssemblyApp {
        main_module: Interop.IMonoAssemblyHandle;
        main_class: Interop.IMonoClassHandle;
    }
}
declare namespace Uno.Foundation.Interop {
    class ManagedObject {
        private static assembly;
        private static dispatchMethod;
        private static init;
        static dispatch(handle: number, method: string, parameters: string): void;
    }
}
declare namespace Uno.UI.Interop {
    class Runtime {
        static readonly engine: any;
        private static init;
        static InvokeJS(command: string): string;
    }
}
declare namespace Uno.UI.Interop {
    class Xaml {
    }
}
declare const MonoRuntime: Uno.UI.Interop.IMonoRuntime;
declare const WebAssemblyApp: Uno.UI.Interop.IWebAssemblyApp;
declare const UnoAppManifest: Uno.UI.IAppManifest;
declare namespace Uno.UI.Runtime.Skia {
    enum PointerDeviceType {
        Touch = 0,
        Pen = 1,
        Mouse = 2
    }
    enum HtmlPointerEvent {
        pointerover = 1,
        pointerleave = 2,
        pointerdown = 4,
        pointerup = 8,
        pointercancel = 16,
        pointermove = 32,
        lostpointercapture = 64,
        wheel = 128
    }
    class BrowserPointerInputSource {
        private static _exports;
        static initialize(inputSource: any): Promise<any>;
        static setPointerCapture(pointerId: number): void;
        static releasePointerCapture(pointerId: number): void;
        private _source;
        private _bootTime;
        private constructor();
        private subscribePointerEvents;
        private onPointerEventReceived;
        private static _wheelLineSize;
        private static get wheelLineSize();
        private static toHtmlPointerEvent;
        private static toPointerDeviceType;
    }
}
declare namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core {
    class DragDropExtension {
        private static _dispatchDropEventMethod;
        private static _dispatchDragDropArgs;
        private static _current;
        private static _nextDropId;
        private _dropHandler;
        private _pendingDropId;
        private _pendingDropData;
        static enable(pArgs: number): void;
        static disable(pArgs: number): void;
        constructor();
        dispose(): void;
        static registerNoOp(): void;
        private dispatchDropEvent;
        static retrieveText(itemId: number): Promise<string>;
        static retrieveFiles(itemIds: number[]): Promise<string>;
        private static getAsFile;
    }
}
declare namespace Microsoft.UI.Xaml {
    class Application {
        private static dispatchVisibilityChange;
        static observeVisibility(): void;
    }
}
declare namespace Microsoft.UI.Xaml.Media.Animation {
    class RenderingLoopAnimator {
        private static dispatchFrame;
        private static init;
        static setEnabled(enabled: boolean): void;
        private static scheduleAnimationFrame;
        private static onAnimationFrame;
        private static _frameRequestId?;
        private static _isEnabled;
    }
}
declare namespace Microsoft.UI.Xaml.Controls {
    class WebView {
        private static unoExports;
        private static cachedPackageBase;
        static buildImports(assembly: string): void;
        static reload(htmlId: string): void;
        static stop(htmlId: string): void;
        static goBack(htmlId: string): void;
        static goForward(htmlId: string): void;
        static executeScript(htmlId: string, script: string): string;
        static getDocumentTitle(htmlId: string): string;
        static setAttribute(htmlId: string, name: string, value: string): void;
        static getAttribute(htmlId: string, name: string): string;
        static navigate(htmlId: string, url: string): void;
        static initializeStyling(htmlId: string): void;
        static getPackageBase(): string;
        static setupEvents(htmlId: string): void;
        static cleanupEvents(htmlId: string): void;
        private static onLoad;
    }
}
declare namespace Microsoft.UI.Xaml.Input {
    class FocusVisual {
        private static focusVisualId;
        private static focusVisual;
        private static focusedElement;
        private static currentDispatchTimeout?;
        private static dispatchPositionChange;
        static attachVisual(focusVisualId: number, focusedElementId: number): void;
        static detachVisual(): void;
        private static onDocumentScroll;
        static updatePosition(): void;
    }
}
declare namespace Microsoft.UI.Xaml.Media {
    class FontFamily {
        private static managedNotifyFontLoaded?;
        private static managedNotifyFontLoadFailed?;
        static loadFont(fontFamilyName: string, fontSource: string): Promise<void>;
        static forceFontUsage(fontFamilyName: string): Promise<void>;
        private static notifyFontLoaded;
        private static notifyFontLoadFailed;
    }
}
declare class WindowManagerAddViewParams {
    HtmlId: number;
    ChildView: number;
    Index: number;
    static unmarshal(pData: number): WindowManagerAddViewParams;
}
declare class WindowManagerGetBBoxParams {
    HtmlId: number;
    static unmarshal(pData: number): WindowManagerGetBBoxParams;
}
declare class WindowManagerGetBBoxReturn {
    X: number;
    Y: number;
    Width: number;
    Height: number;
    marshal(pData: number): void;
}
declare class WindowManagerGetClientViewSizeParams {
    HtmlId: number;
    static unmarshal(pData: number): WindowManagerGetClientViewSizeParams;
}
declare class WindowManagerGetClientViewSizeReturn {
    OffsetWidth: number;
    OffsetHeight: number;
    ClientWidth: number;
    ClientHeight: number;
    marshal(pData: number): void;
}
declare class WindowManagerMeasureViewReturn {
    DesiredWidth: number;
    DesiredHeight: number;
    marshal(pData: number): void;
}
declare class WindowManagerRegisterEventOnViewParams {
    HtmlId: number;
    EventName: string;
    OnCapturePhase: boolean;
    EventExtractorId: number;
    static unmarshal(pData: number): WindowManagerRegisterEventOnViewParams;
}
declare class WindowManagerRemoveAttributeParams {
    HtmlId: number;
    Name: string;
    static unmarshal(pData: number): WindowManagerRemoveAttributeParams;
}
declare class WindowManagerRemoveViewParams {
    HtmlId: number;
    ChildView: number;
    static unmarshal(pData: number): WindowManagerRemoveViewParams;
}
declare class WindowManagerResetElementBackgroundParams {
    HtmlId: number;
    static unmarshal(pData: number): WindowManagerResetElementBackgroundParams;
}
declare class WindowManagerScrollToOptionsParams {
    Left: number;
    Top: number;
    HasLeft: boolean;
    HasTop: boolean;
    DisableAnimation: boolean;
    HtmlId: number;
    static unmarshal(pData: number): WindowManagerScrollToOptionsParams;
}
declare class WindowManagerSetContentHtmlParams {
    HtmlId: number;
    Html: string;
    static unmarshal(pData: number): WindowManagerSetContentHtmlParams;
}
declare class WindowManagerSetElementBackgroundColorParams {
    HtmlId: number;
    Color: number;
    static unmarshal(pData: number): WindowManagerSetElementBackgroundColorParams;
}
declare class WindowManagerSetElementBackgroundGradientParams {
    HtmlId: number;
    CssGradient: string;
    static unmarshal(pData: number): WindowManagerSetElementBackgroundGradientParams;
}
declare class WindowManagerSetElementColorParams {
    HtmlId: number;
    Color: number;
    static unmarshal(pData: number): WindowManagerSetElementColorParams;
}
declare class WindowManagerSetElementFillParams {
    HtmlId: number;
    Color: number;
    static unmarshal(pData: number): WindowManagerSetElementFillParams;
}
declare class WindowManagerSetNameParams {
    HtmlId: number;
    Name: string;
    static unmarshal(pData: number): WindowManagerSetNameParams;
}
declare class WindowManagerSetSelectionHighlightParams {
    HtmlId: number;
    BackgroundColor: number;
    ForegroundColor: number;
    static unmarshal(pData: number): WindowManagerSetSelectionHighlightParams;
}
declare class WindowManagerSetStyleDoubleParams {
    HtmlId: number;
    Name: string;
    Value: number;
    static unmarshal(pData: number): WindowManagerSetStyleDoubleParams;
}
declare class WindowManagerSetSvgElementRectParams {
    X: number;
    Y: number;
    Width: number;
    Height: number;
    HtmlId: number;
    static unmarshal(pData: number): WindowManagerSetSvgElementRectParams;
}
declare class WindowManagerSetXUidParams {
    HtmlId: number;
    Uid: string;
    static unmarshal(pData: number): WindowManagerSetXUidParams;
}
declare namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core {
    class DragDropExtensionEventArgs {
        eventName: string;
        allowedOperations: string;
        acceptedOperation: string;
        dataItems: string;
        timestamp: number;
        x: number;
        y: number;
        id: number;
        buttons: number;
        shift: boolean;
        ctrl: boolean;
        alt: boolean;
        static unmarshal(pData: number): DragDropExtensionEventArgs;
        marshal(pData: number): void;
    }
}
