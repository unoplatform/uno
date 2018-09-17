declare namespace Uno.Utils {
    class Clipboard {
        static setText(text: string): string;
    }
}
declare namespace Windows.UI.Core {
    /**
     * Support file for the Windows.UI.Core
     * */
    class CoreDispatcher {
        static _coreDispatcherCallback: any;
        static _isIOS: boolean;
        static _isFirstCall: boolean;
        static init(): void;
        /**
         * Enqueues a core dispatcher callback on the javascript's event loop
         *
         * */
        static WakeUp(): boolean;
        private static initMethods();
    }
}
declare namespace Uno.UI {
    class HtmlDom {
        /**
         * Initialize various polyfills used by Uno
         */
        static initPolyfills(): void;
        private static isConnectedPolyfill();
    }
}
declare namespace Uno.Http {
    interface IHttpClientConfig {
        id: string;
        method: string;
        url: string;
        headers?: string[][];
        payload?: string;
        payloadType?: string;
        cacheMode?: RequestCache;
    }
    class HttpClient {
        static send(config: IHttpClientConfig): Promise<void>;
        private static blobFromBase64(base64, contentType);
        private static base64FromBlob(blob);
        private static dispatchResponse(requestId, status, headers, payload);
        private static dispatchError(requestId, error);
        private static dispatchResponseMethod;
        private static dispatchErrorMethod;
        private static initMethods();
    }
}
declare module Uno.UI {
    interface IContentDefinition {
        id: number;
        tagName: string;
        handle: number;
        type: string;
        isSvg: boolean;
        isFrameworkElement: boolean;
        isFocusable: boolean;
        classes?: string[];
    }
}
declare namespace MonoSupport {
    /**
     * This class is used by https://github.com/mono/mono/blob/fa726d3ac7153d87ed187abd422faa4877f85bb5/sdks/wasm/dotnet_support.js#L88 to perform
     * unmarshaled invocation of javascript from .NET code.
     * */
    class jsCallDispatcher {
        static registrations: Map<string, any>;
        static methodMap: Map<string, any>;
        static _isUnoRegistered: boolean;
        /**
         * Registers a instance for a specified identier
         * @param identifier the scope name
         * @param instance the instance to use for the scope
         */
        static registerScope(identifier: string, instance: any): void;
        static findJSFunction(identifier: string): any;
        /**
         * Parses the method identifier
         * @param identifier
         */
        private static parseIdentifier(identifier);
        /**
         * Adds the a resolved method for a given identifier
         * @param identifier the findJSFunction identifier
         * @param boundMethod the method to call
         */
        private static cacheMethod(identifier, boundMethod);
    }
}
declare namespace Uno.UI {
    class WindowManager {
        private containerElementId;
        private loadingElementId;
        static current: WindowManager;
        private static _isHosted;
        private static _isLoadEventsEnabled;
        /**
         * Defines if the WindowManager is running in hosted mode, and should skip the
         * initialization of WebAssembly, use this mode in conjunction with the Uno.UI.WpfHost
         * to improve debuggability.
         */
        static readonly isHosted: boolean;
        /**
         * Defines if the WindowManager is responsible to raise the loading, loaded and unloaded events,
         * or if they are raised directly by the managed code to reduce interop.
         */
        static readonly isLoadEventsEnabled: boolean;
        private static readonly unoRootClassName;
        private static readonly unoUnarrangedClassName;
        private static _cctor;
        /**
            * Initialize the WindowManager
            * @param containerElementId The ID of the container element for the Xaml UI
            * @param loadingElementId The ID of the loading element to remove once ready
            */
        static init(localStoragePath: string, isHosted: boolean, isLoadEventsEnabled: boolean, containerElementId?: string, loadingElementId?: string): string;
        /**
            * Initialize the WindowManager
            * @param containerElementId The ID of the container element for the Xaml UI
            * @param loadingElementId The ID of the loading element to remove once ready
            */
        static initNative(pParams: number): boolean;
        private containerElement;
        private rootContent;
        private allActiveElementsById;
        private static resizeMethod;
        private static dispatchEventMethod;
        private constructor();
        /**
         * Setup the storage persistence
         *
         * */
        static setupStorage(localStoragePath: string): void;
        /**
         * Determine if IndexDB is available, some browsers and modes disable it.
         * */
        static isIndexDBAvailable(): boolean;
        /**
         * Synchronize the IDBFS memory cache back to IndexDB
         * */
        static synchronizeFileSystem(): void;
        /**
            * Creates the UWP-compatible splash screen
            *
            */
        static setupSplashScreen(): void;
        /**
            * Reads the window's search parameters
            *
            */
        static findLaunchArguments(): string;
        /**
            * Create a html DOM element representing a Xaml element.
            *
            * You need to call addView to connect it to the DOM.
            */
        createContent(contentDefinition: IContentDefinition): string;
        /**
            * Create a html DOM element representing a Xaml element.
            *
            * You need to call addView to connect it to the DOM.
            */
        createContentNative(pParams: number): boolean;
        private createContentInternal(contentDefinition);
        /**
            * Set a name for an element.
            *
            * This is mostly for diagnostic purposes.
            */
        setName(elementId: number, name: string): string;
        /**
            * Set a name for an element.
            *
            * This is mostly for diagnostic purposes.
            */
        setNameNative(pParam: number): boolean;
        private setNameInternal(elementId, name);
        /**
            * Set an attribute for an element.
            */
        setAttribute(elementId: string, attributes: {
            [name: string]: string;
        }): string;
        /**
            * Set an attribute for an element.
            */
        setAttributeNative(pParams: number): boolean;
        /**
            * Get an attribute for an element.
            */
        getAttribute(elementId: string, name: string): any;
        /**
            * Set a property for an element.
            */
        setProperty(elementId: string, properties: {
            [name: string]: string;
        }): string;
        /**
            * Set a property for an element.
            */
        setPropertyNative(pParams: number): boolean;
        /**
            * Get a property for an element.
            */
        getProperty(elementId: string, name: string): any;
        /**
            * Set the CSS style of a html element.
            *
            * To remove a value, set it to empty string.
            * @param styles A dictionary of styles to apply on html element.
            */
        setStyle(elementId: string, styles: {
            [name: string]: string;
        }, setAsArranged?: boolean): string;
        /**
        * Set the CSS style of a html element.
        *
        * To remove a value, set it to empty string.
        * @param styles A dictionary of styles to apply on html element.
        */
        setStyleNative(pParams: number): boolean;
        /**
            * Set the CSS style of a html element.
            *
            * To remove a value, set it to empty string.
            * @param styles A dictionary of styles to apply on html element.
            */
        resetStyle(elementId: number, names: string[]): string;
        /**
            * Set the CSS style of a html element.
            *
            * To remove a value, set it to empty string.
            * @param styles A dictionary of styles to apply on html element.
            */
        resetStyleNative(pParams: number): boolean;
        private resetStyleInternal(elementId, names);
        /**
            * Load the specified URL into a new tab or window
            * @param url URL to load
            * @returns "True" or "False", depending on whether a new window could be opened or not
            */
        open(url: string): string;
        /**
            * Issue a browser alert to user
            * @param message message to display
            */
        alert(message: string): string;
        /**
            * Sets the browser window title
            * @param message the new title
            */
        setWindowTitle(title: string): string;
        /**
            * Gets the currently set browser window title
            */
        getWindowTitle(): string;
        /**
            * Add an event handler to a html element.
            *
            * @param eventName The name of the event
            * @param onCapturePhase true means "on trickle down", false means "on bubble up". Default is false.
            */
        registerEventOnView(elementId: number, eventName: string, onCapturePhase?: boolean, eventFilterName?: string, eventExtractorName?: string): string;
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
        private registerEventOnViewInternal(elementId, eventName, onCapturePhase?, eventFilterName?, eventExtractorName?);
        /**
         * left pointer event filter to be used with registerEventOnView
         * @param evt
         */
        private leftPointerEventFilter(evt);
        /**
         * default event filter to be used with registerEventOnView to
         * use for most routed events
         * @param evt
         */
        private defaultEventFilter(evt);
        /**
         * Gets the event filter function. See UIElement.HtmlEventFilter
         * @param eventFilterName an event filter name.
         */
        private getEventFilter(eventFilterName);
        /**
         * pointer event extractor to be used with registerEventOnView
         * @param evt
         */
        private pointerEventExtractor(evt);
        /**
         * keyboard event extractor to be used with registerEventOnView
         * @param evt
         */
        private keyboardEventExtractor(evt);
        /**
         * Gets the event extractor function. See UIElement.HtmlEventExtractor
         * @param eventExtractorName an event extractor name.
         */
        private getEventExtractor(eventExtractorName);
        /**
            * Set or replace the root content element.
            */
        setRootContent(elementId?: string): string;
        /**
            * Set a view as a child of another one.
            *
            * "Loading" & "Loaded" events will be raised if necessary.
            *
            * @param index Position in children list. Appended at end if not specified.
            */
        addView(parentId: number, childId: number, index?: number): string;
        /**
            * Set a view as a child of another one.
            *
            * "Loading" & "Loaded" events will be raised if necessary.
            *
            * @param pParams Pointer to a WindowManagerAddViewParams native structure.
            */
        addViewNative(pParams: number): boolean;
        addViewInternal(parentId: number, childId: number, index?: number): void;
        /**
            * Remove a child from a parent element.
            *
            * "Unloading" & "Unloaded" events will be raised if necessary.
            */
        removeView(parentId: number, childId: number): string;
        /**
            * Remove a child from a parent element.
            *
            * "Unloading" & "Unloaded" events will be raised if necessary.
            */
        removeViewNative(pParams: number): boolean;
        private removeViewInternal(parentId, childId);
        /**
            * Destroy a html element.
            *
            * The element won't be available anymore. Usually indicate the managed
            * version has been scavenged by the GC.
            */
        destroyView(viewId: number): string;
        /**
            * Destroy a html element.
            *
            * The element won't be available anymore. Usually indicate the managed
            * version has been scavenged by the GC.
            */
        destroyViewNative(pParams: number): boolean;
        private destroyViewInternal(viewId);
        getBoundingClientRect(elementId: string): string;
        getBBox(elementId: number): string;
        getBBoxNative(pParams: number, pReturn: number): boolean;
        private getBBoxInternal(elementId);
        /**
            * Use the Html engine to measure the element using specified constraints.
            *
            * @param maxWidth string containing width in pixels. Empty string means infinite.
            * @param maxHeight string containing height in pixels. Empty string means infinite.
            */
        measureView(viewId: string, maxWidth: string, maxHeight: string): string;
        /**
            * Use the Html engine to measure the element using specified constraints.
            *
            * @param maxWidth string containing width in pixels. Empty string means infinite.
            * @param maxHeight string containing height in pixels. Empty string means infinite.
            */
        measureViewNative(pParams: number, pReturn: number): boolean;
        private measureViewInternal(viewId, maxWidth, maxHeight);
        setImageRawData(viewId: string, dataPtr: number, width: number, height: number): string;
        /**
         * Sets the provided image with a mono-chrome version of the provided url.
         * @param viewId the image to manipulate
         * @param url the source image
         * @param color the color to apply to the monochrome pixels
         */
        setImageAsMonochrome(viewId: string, url: string, color: string): string;
        setPointerCapture(viewId: string, pointerId: number): string;
        releasePointerCapture(viewId: string, pointerId: number): string;
        focusView(elementId: string): string;
        /**
            * Set the Html content for an element.
            *
            * Those html elements won't be available as XamlElement in managed code.
            * WARNING: you should avoid mixing this and `addView` for the same element.
            */
        setHtmlContent(viewId: number, html: string): string;
        /**
            * Set the Html content for an element.
            *
            * Those html elements won't be available as XamlElement in managed code.
            * WARNING: you should avoid mixing this and `addView` for the same element.
            */
        setHtmlContentNative(pParams: number): boolean;
        private setHtmlContentInternal(viewId, html);
        /**
            * Remove the loading indicator.
            *
            * In a future version it will also handle the splashscreen.
            */
        activate(): string;
        private init();
        private static initMethods();
        private initDom();
        private removeLoading();
        private resize();
        private dispatchEvent(element, eventName, eventPayload?);
        private getIsConnectedToRootElement(element);
    }
}
declare class WindowManagerAddViewParams {
    HtmlId: number;
    ChildView: number;
    Index: number;
    static unmarshal(pData: number): WindowManagerAddViewParams;
}
declare class WindowManagerCreateContentParams {
    HtmlId: number;
    TagName: string;
    Handle: number;
    Type: string;
    IsSvg: boolean;
    IsFrameworkElement: boolean;
    IsFocusable: boolean;
    Classes_Length: number;
    Classes: Array<string>;
    static unmarshal(pData: number): WindowManagerCreateContentParams;
}
declare class WindowManagerDestroyViewParams {
    HtmlId: number;
    static unmarshal(pData: number): WindowManagerDestroyViewParams;
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
declare class WindowManagerInitParams {
    LocalFolderPath: string;
    IsHostedMode: boolean;
    IsLoadEventsEnabled: boolean;
    static unmarshal(pData: number): WindowManagerInitParams;
}
declare class WindowManagerMeasureViewParams {
    HtmlId: number;
    AvailableWidth: number;
    AvailableHeight: number;
    static unmarshal(pData: number): WindowManagerMeasureViewParams;
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
    EventFilterName: string;
    EventExtractorName: string;
    static unmarshal(pData: number): WindowManagerRegisterEventOnViewParams;
}
declare class WindowManagerRemoveViewParams {
    HtmlId: number;
    ChildView: number;
    static unmarshal(pData: number): WindowManagerRemoveViewParams;
}
declare class WindowManagerResetStyleParams {
    HtmlId: number;
    Styles_Length: number;
    Styles: Array<string>;
    static unmarshal(pData: number): WindowManagerResetStyleParams;
}
declare class WindowManagerSetAttributeParams {
    HtmlId: number;
    Pairs_Length: number;
    Pairs: Array<string>;
    static unmarshal(pData: number): WindowManagerSetAttributeParams;
}
declare class WindowManagerSetContentHtmlParams {
    HtmlId: number;
    Html: string;
    static unmarshal(pData: number): WindowManagerSetContentHtmlParams;
}
declare class WindowManagerSetNameParams {
    HtmlId: number;
    Name: string;
    static unmarshal(pData: number): WindowManagerSetNameParams;
}
declare class WindowManagerSetPropertyParams {
    HtmlId: number;
    Pairs_Length: number;
    Pairs: Array<string>;
    static unmarshal(pData: number): WindowManagerSetPropertyParams;
}
declare class WindowManagerSetStylesParams {
    HtmlId: number;
    SetAsArranged: boolean;
    Pairs_Length: number;
    Pairs: Array<string>;
    static unmarshal(pData: number): WindowManagerSetStylesParams;
}
declare module Uno.UI {
    interface IAppManifest {
        splashScreenImage: URL;
        splashScreenColor: string;
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
    interface IUnoDispatch {
        resize(size: string): void;
        dispatch(htmlIdStr: string, eventNameStr: string, eventPayloadStr: string): string;
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
        private static init();
        static dispatch(handle: string, method: string, parameters: string): void;
    }
}
declare namespace Uno.UI.Interop {
    class Runtime {
        static readonly engine: any;
        private static init();
    }
}
declare namespace Uno.UI.Interop {
    class Xaml {
    }
}
declare const MonoRuntime: Uno.UI.Interop.IMonoRuntime;
declare const WebAssemblyApp: Uno.UI.Interop.IWebAssemblyApp;
declare const UnoAppManifest: Uno.UI.IAppManifest;
declare const UnoDispatch: Uno.UI.Interop.IUnoDispatch;
