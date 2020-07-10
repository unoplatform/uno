declare const config: any;

// eslint-disable-next-line @typescript-eslint/no-namespace
namespace Uno.UI {

	export class WindowManager {

		public static current: WindowManager;
		private static _isHosted: boolean = false;
		private static _isLoadEventsEnabled: boolean = false;

		/**
		 * Defines if the WindowManager is running in hosted mode, and should skip the
		 * initialization of WebAssembly, use this mode in conjunction with the Uno.UI.WpfHost
		 * to improve debuggability.
		 */
		public static get isHosted(): boolean {
			return WindowManager._isHosted;
		}

		/**
		 * Defines if the WindowManager is responsible to raise the loading, loaded and unloaded events,
		 * or if they are raised directly by the managed code to reduce interop.
		 */
		public static get isLoadEventsEnabled(): boolean {
			return WindowManager._isLoadEventsEnabled;
		}

		private static readonly unoRootClassName = "uno-root-element";
		private static readonly unoUnarrangedClassName = "uno-unarranged";
		private static readonly unoClippedToBoundsClassName = "uno-clippedToBounds";

		private static _cctor = (() => {
			WindowManager.initMethods();
			HtmlDom.initPolyfills();
		})();

		/**
			* Initialize the WindowManager
			* @param containerElementId The ID of the container element for the Xaml UI
			* @param loadingElementId The ID of the loading element to remove once ready
			*/
		public static init(isHosted: boolean, isLoadEventsEnabled: boolean, containerElementId: string = "uno-body", loadingElementId: string = "uno-loading"): string {

			WindowManager._isHosted = isHosted;
			WindowManager._isLoadEventsEnabled = isLoadEventsEnabled;

			Windows.UI.Core.CoreDispatcher.init(WindowManager.buildReadyPromise());

			this.current = new WindowManager(containerElementId, loadingElementId);
			MonoSupport.jsCallDispatcher.registerScope("Uno", this.current);
			this.current.init();

			return "ok";
		}

		/**
		 * Builds a promise that will signal the ability for the dispatcher
		 * to initiate work.
		 * */
		private static buildReadyPromise(): Promise<boolean> {
			return new Promise<boolean>(resolve => {
				Promise.all(
					[WindowManager.buildSplashScreen()]
				).then(() => resolve(true))
			});
		}

		/**
		 * Build the splashscreen image eagerly
		 * */
		private static buildSplashScreen(): Promise<boolean> {
			return new Promise<boolean>(resolve => {
				const img = new Image();
				let loaded = false;

				let loadingDone = () => {
					if (!loaded) {
						loaded = true;
						if (img.width !== 0 && img.height !== 0) {
							// Materialize the image content so it shows immediately
							// even if the dispatcher is blocked thereafter by all
							// the Uno initialization work. The resulting canvas is not used.
							//
							// If the image fails to load, setup the splashScreen anyways with the
							// proper sample.
							let canvas = document.createElement('canvas');
							canvas.width = img.width;
							canvas.height = img.height;
							let ctx = canvas.getContext("2d");
							ctx.drawImage(img, 0, 0);
						}

						if (document.readyState === "loading") {
							document.addEventListener("DOMContentLoaded", () => {
								WindowManager.setupSplashScreen(img);
								resolve(true);
							});
						} else {
							WindowManager.setupSplashScreen(img);
							resolve(true);
						}
					}
				};

				// Preload the splash screen so the image element
				// created later on 
				img.onload = loadingDone;
				img.onerror = loadingDone;
				img.src = String(UnoAppManifest.splashScreenImage);

				// If there's no response, skip the loading
				setTimeout(loadingDone, 2000);
			});
		}

		/**
			* Initialize the WindowManager
			* @param containerElementId The ID of the container element for the Xaml UI
			* @param loadingElementId The ID of the loading element to remove once ready
			*/
		public static initNative(pParams: number): boolean {

			const params = WindowManagerInitParams.unmarshal(pParams);

			WindowManager.init(params.IsHostedMode, params.IsLoadEventsEnabled);

			return true;
		}

		private containerElement: HTMLDivElement;
		private rootContent: HTMLElement;

		private cursorStyleElement: HTMLElement;

		private allActiveElementsById: { [id: string]: HTMLElement | SVGElement } = {};
		private uiElementRegistrations: {
			[id: string]: {
				typeName: string;
				isFrameworkElement: boolean;
				classNames: string[];
			};
		} = {};

		private static resizeMethod: any;
		private static dispatchEventMethod: any;
		private static focusInMethod: any;
		private static dispatchSuspendingMethod: any;
		private static getDependencyPropertyValueMethod: any;
		private static setDependencyPropertyValueMethod: any;

		private constructor(private containerElementId: string, private loadingElementId: string) {
			this.initDom();
		}

		/**
			* Creates the UWP-compatible splash screen
			* 
			*/
		static setupSplashScreen(splashImage: HTMLImageElement): void {

			if (UnoAppManifest && UnoAppManifest.splashScreenImage) {

				const loading = document.getElementById("loading");

				if (loading) {
					loading.remove();
				}

				const unoBody = document.getElementById("uno-body");

				if (unoBody) {
					const unoLoading = document.createElement("div");
					unoLoading.id = "uno-loading";

					if (UnoAppManifest.splashScreenColor) {
						const body = document.getElementsByTagName("body")[0];
						body.style.backgroundColor = UnoAppManifest.splashScreenColor;
					}

					splashImage.id = "uno-loading-splash";
					splashImage.classList.add("uno-splash");

					unoLoading.appendChild(splashImage);

					unoBody.appendChild(unoLoading);
				}
			}
		}

		/**
			* Reads the window's search parameters
			* 
			*/
		static findLaunchArguments(): string {
			if (typeof URLSearchParams === "function") {
				return new URLSearchParams(window.location.search).toString();
			}
			else {
				const queryIndex = document.location.search.indexOf('?');

				if (queryIndex !== -1) {
					return document.location.search.substring(queryIndex + 1);
				}

				return "";
			}
		}

		/**
			* Create a html DOM element representing a Xaml element.
			*
			* You need to call addView to connect it to the DOM.
			*/
		public createContent(contentDefinition: IContentDefinition): string {
			this.createContentInternal(contentDefinition);

			return "ok";
		}

		/**
			* Create a html DOM element representing a Xaml element.
			*
			* You need to call addView to connect it to the DOM.
			*/
		public createContentNative(pParams: number): boolean {

			const params = WindowManagerCreateContentParams.unmarshal(pParams);

			const def = {
				id: this.handleToString(params.HtmlId),
				handle: params.Handle,
				isFocusable: params.IsFocusable,
				isSvg: params.IsSvg,
				tagName: params.TagName,
				uiElementRegistrationId: params.UIElementRegistrationId,
			} as IContentDefinition;

			this.createContentInternal(def);

			return true;
		}

		private createContentInternal(contentDefinition: IContentDefinition): void {
			// Create the HTML element
			const element =
				contentDefinition.isSvg
					? document.createElementNS("http://www.w3.org/2000/svg", contentDefinition.tagName)
					: document.createElement(contentDefinition.tagName);

			element.id = contentDefinition.id;

			const uiElementRegistration =
				this.uiElementRegistrations[this.handleToString(contentDefinition.uiElementRegistrationId)];
			if (!uiElementRegistration) {
				throw `UIElement registration id ${contentDefinition.uiElementRegistrationId} is unknown.`;
			}

			element.setAttribute("XamlType", uiElementRegistration.typeName);
			element.setAttribute("XamlHandle", this.handleToString(contentDefinition.handle));
			if (uiElementRegistration.isFrameworkElement) {
				this.setAsUnarranged(element);
			}
			if (element.hasOwnProperty("tabindex")) {
				(element as any)["tabindex"] = contentDefinition.isFocusable ? 0 : -1;
			} else {
				element.setAttribute("tabindex", contentDefinition.isFocusable ? "0" : "-1");
			}

			if (contentDefinition) {
				let classes = element.classList.value; 
				for (const className of uiElementRegistration.classNames) {
					classes += " uno-" + className;
				}

				element.classList.value = classes;
			}

			// Add the html element to list of elements
			this.allActiveElementsById[contentDefinition.id] = element;
		}

		public registerUIElement(typeName: string, isFrameworkElement: boolean, classNames: string[]): number {

			const registrationId = Object.keys(this.uiElementRegistrations).length;

			this.uiElementRegistrations[this.handleToString(registrationId)] = {
				classNames: classNames,
				isFrameworkElement: isFrameworkElement,
				typeName: typeName,
			};

			return registrationId;
		}

		public registerUIElementNative(pParams: number, pReturn: number): boolean {
			const params = WindowManagerRegisterUIElementParams.unmarshal(pParams);

			const registrationId = this.registerUIElement(params.TypeName, params.IsFrameworkElement, params.Classes);

			const ret = new WindowManagerRegisterUIElementReturn();
			ret.RegistrationId = registrationId;

			ret.marshal(pReturn);

			return true;
		}

		public getView(elementHandle: number): HTMLElement | SVGElement {
			const element = this.allActiveElementsById[this.handleToString(elementHandle)];
			if (!element) {
				throw `Element id ${elementHandle} not found.`;
			}
			return element;
		}

		/**
			* Set a name for an element.
			*
			* This is mostly for diagnostic purposes.
			*/
		public setName(elementId: number, name: string): string {
			this.setNameInternal(elementId, name);
			return "ok";
		}

		/**
			* Set a name for an element.
			*
			* This is mostly for diagnostic purposes.
			*/
		public setNameNative(pParam: number): boolean {
			const params = WindowManagerSetNameParams.unmarshal(pParam);
			this.setNameInternal(params.HtmlId, params.Name);
			return true;
		}

		private setNameInternal(elementId: number, name: string): void {
			this.getView(elementId).setAttribute("xamlname", name);
		}


		/**
			* Set a name for an element.
			*
			* This is mostly for diagnostic purposes.
			*/
		public setXUid(elementId: number, name: string): string {
			this.setXUidInternal(elementId, name);
			return "ok";
		}

		/**
			* Set a name for an element.
			*
			* This is mostly for diagnostic purposes.
			*/
		public setXUidNative(pParam: number): boolean {
			const params = WindowManagerSetXUidParams.unmarshal(pParam);
			this.setXUidInternal(params.HtmlId, params.Uid);
			return true;
		}

		private setXUidInternal(elementId: number, name: string): void {
			this.getView(elementId).setAttribute("xuid", name);
		}

		/**
			* Set an attribute for an element.
			*/
		public setAttributes(elementId: number, attributes: { [name: string]: string }): string {
			const element = this.getView(elementId);

			for (const name in attributes) {
				if (attributes.hasOwnProperty(name)) {
					element.setAttribute(name, attributes[name]);
				}
			}

			return "ok";
		}

		/**
			* Set an attribute for an element.
			*/
		public setAttributesNative(pParams: number): boolean {

			const params = WindowManagerSetAttributesParams.unmarshal(pParams);
			const element = this.getView(params.HtmlId);

			for (let i = 0; i < params.Pairs_Length; i += 2) {
				element.setAttribute(params.Pairs[i], params.Pairs[i + 1]);
			}

			return true;
		}

		/**
			* Set an attribute for an element.
			*/
		public setAttributeNative(pParams: number): boolean {

			const params = WindowManagerSetAttributeParams.unmarshal(pParams);
			const element = this.getView(params.HtmlId);
			element.setAttribute(params.Name, params.Value);

			return true;
		}

		/**
			* Removes an attribute for an element.
			*/
		public removeAttribute(elementId: number, name: string): string {
			const element = this.getView(elementId);
			element.removeAttribute(name);

			return "ok";
		}

		/**
			* Removes an attribute for an element.
			*/
		public removeAttributeNative(pParams: number): boolean {

			const params = WindowManagerRemoveAttributeParams.unmarshal(pParams);
			const element = this.getView(params.HtmlId);
			element.removeAttribute(params.Name);

			return true;
		}

		/**
			* Get an attribute for an element.
			*/
		public getAttribute(elementId: number, name: string): any {

			return this.getView(elementId).getAttribute(name);
		}

		/**
			* Set a property for an element.
			*/
		public setProperty(elementId: number, properties: { [name: string]: string }): string {
			const element = this.getView(elementId);

			for (const name in properties) {
				if (properties.hasOwnProperty(name)) {
					var setVal = properties[name];
					if (setVal === "true") {
						(element as any)[name] = true;
					}
					else if (setVal === "false") {
						(element as any)[name] = false;
					}
					else {
						(element as any)[name] = setVal;
					}
				}
			}

			return "ok";
		}

		/**
			* Set a property for an element.
			*/
		public setPropertyNative(pParams: number): boolean {

			const params = WindowManagerSetPropertyParams.unmarshal(pParams);
			const element = this.getView(params.HtmlId);

			for (let i = 0; i < params.Pairs_Length; i += 2) {
				var setVal = params.Pairs[i + 1];
				if (setVal === "true") {
					(element as any)[params.Pairs[i]] = true;
				}
				else if (setVal === "false") {
					(element as any)[params.Pairs[i]] = false;
				}
				else {
					(element as any)[params.Pairs[i]] = setVal;
				}
			}

			return true;
		}

		/**
			* Get a property for an element.
			*/
		public getProperty(elementId: number, name: string): any {
			const element = this.getView(elementId);

			return (element as any)[name] || "";
		}

		/**
			* Set the CSS style of a html element.
			*
			* To remove a value, set it to empty string.
			* @param styles A dictionary of styles to apply on html element.
			*/
		public setStyle(elementId: number, styles: { [name: string]: string }): string {
			const element = this.getView(elementId);

			for (const style in styles) {
				if (styles.hasOwnProperty(style)) {
					element.style.setProperty(style, styles[style]);
				}
			}

			return "ok";
		}

		/**
		* Set the CSS style of a html element.
		*
		* To remove a value, set it to empty string.
		* @param styles A dictionary of styles to apply on html element.
		*/
		public setStyleNative(pParams: number): boolean {

			const params = WindowManagerSetStylesParams.unmarshal(pParams);
			const element = this.getView(params.HtmlId);

			const elementStyle = element.style;
			const pairs = params.Pairs;

			for (let i = 0; i < params.Pairs_Length; i += 2) {
				const key = pairs[i];
				const value = pairs[i + 1];

				elementStyle.setProperty(key, value);
			}

			return true;
		}

		/**
		* Set a single CSS style of a html element
		*
		*/
		public setStyleDoubleNative(pParams: number): boolean {

			const params = WindowManagerSetStyleDoubleParams.unmarshal(pParams);
			const element = this.getView(params.HtmlId);

			element.style.setProperty(params.Name, this.handleToString(params.Value));

			return true;
		}

		public setArrangeProperties(elementId: number, clipToBounds: boolean): string {
			const element = this.getView(elementId);

			this.setAsArranged(element);
			this.setClipToBounds(element, clipToBounds);

			return "ok";
		}

		/**
			* Remove the CSS style of a html element.
			*/
		public resetStyle(elementId: number, names: string[]): string {
			this.resetStyleInternal(elementId, names);
			return "ok";
		}

		/**
			* Remove the CSS style of a html element.
			*/
		public resetStyleNative(pParams: number): boolean {
			const params = WindowManagerResetStyleParams.unmarshal(pParams);
			this.resetStyleInternal(params.HtmlId, params.Styles);
			return true;
		}

		private resetStyleInternal(elementId: number, names: string[]): void {
			const element = this.getView(elementId);

			for (const name of names) {
				element.style.setProperty(name, "");
			}
		}
		/**
		 * Set + Unset CSS classes on an element
		 */

		public setUnsetClasses(elementId: number, cssClassesToSet: string[], cssClassesToUnset: string[]) {
			const element = this.getView(elementId);

			if (cssClassesToSet) {
				cssClassesToSet.forEach(c => {
					element.classList.add(c);
				});
			}
			if (cssClassesToUnset) {
				cssClassesToUnset.forEach(c => {
					element.classList.remove(c);
				});
			}
		}

		public setUnsetClassesNative(pParams: number): boolean {
			const params = WindowManagerSetUnsetClassesParams.unmarshal(pParams);
			this.setUnsetClasses(params.HtmlId, params.CssClassesToSet, params.CssClassesToUnset);
			return true;
		}

		/**
		 * Set CSS classes on an element from a specified list
		 */
		public setClasses(elementId: number, cssClassesList: string[], classIndex: number): string {
			const element = this.getView(elementId);

			for (let i = 0; i < cssClassesList.length; i++) {
				if (i === classIndex) {
					element.classList.add(cssClassesList[i]);
				} else {
					element.classList.remove(cssClassesList[i]);
				}
			}
			return "ok";
		}

		public setClassesNative(pParams: number): boolean {
			const params = WindowManagerSetClassesParams.unmarshal(pParams);
			this.setClasses(params.HtmlId, params.CssClasses, params.Index);
			return true;
		}

		/**
		* Arrange and clips a native elements 
		*
		*/
		public arrangeElementNative(pParams: number): boolean {

			const params = WindowManagerArrangeElementParams.unmarshal(pParams);
			const element = this.getView(params.HtmlId);

			const style = element.style;

			style.position = "absolute";
			style.top = params.Top + "px";
			style.left = params.Left + "px";
			style.width = params.Width === NaN ? "auto" : params.Width + "px";
			style.height = params.Height === NaN ? "auto" : params.Height + "px";

			if (params.Clip) {
				style.clip = `rect(${params.ClipTop}px, ${params.ClipRight}px, ${params.ClipBottom}px, ${params.ClipLeft}px)`;
			} else {
				style.clip = "";
			}

			this.setAsArranged(element);
			this.setClipToBounds(element, params.ClipToBounds);

			return true;
		}

		private setAsArranged(element: HTMLElement | SVGElement) {

			element.classList.remove(WindowManager.unoUnarrangedClassName);
		}

		private setAsUnarranged(element: HTMLElement | SVGElement) {
			element.classList.add(WindowManager.unoUnarrangedClassName);
		}

		private setClipToBounds(element: HTMLElement | SVGElement, clipToBounds: boolean) {
			if (clipToBounds) {
				element.classList.add(WindowManager.unoClippedToBoundsClassName);
			} else {
				element.classList.remove(WindowManager.unoClippedToBoundsClassName);
			}
		}

		/**
		* Sets the transform matrix of an element
		*
		*/
		public setElementTransformNative(pParams: number): boolean {

			const params = WindowManagerSetElementTransformParams.unmarshal(pParams);
			const element = this.getView(params.HtmlId);

			var style = element.style;

			const matrix = `matrix(${params.M11},${params.M12},${params.M21},${params.M22},${params.M31},${params.M32})`;
			style.transform = matrix;

			this.setAsArranged(element);
			this.setClipToBounds(element, params.ClipToBounds);

			return true;
		}

		private setPointerEvents(htmlId: number, enabled: boolean) {
			const element = this.getView(htmlId);
			element.style.pointerEvents = enabled ? "auto" : "none";
		}

		public setPointerEventsNative(pParams: number): boolean {
			const params = WindowManagerSetPointerEventsParams.unmarshal(pParams);
			this.setPointerEvents(params.HtmlId, params.Enabled);

			return true;
		}

		/**
			* Load the specified URL into a new tab or window
			* @param url URL to load
			* @returns "True" or "False", depending on whether a new window could be opened or not
			*/
		public open(url: string): string {
			const newWindow = window.open(url, "_blank");

			return newWindow != null
				? "True"
				: "False";
		}

		/**
			* Issue a browser alert to user
			* @param message message to display
			*/
		public alert(message: string): string {
			window.alert(message);

			return "ok";
		}

		/**
			* Sets the browser window title
			* @param message the new title
			*/
		public setWindowTitle(title: string): string {
			document.title = title || UnoAppManifest.displayName;
			return "ok";
		}

		/**
			* Gets the currently set browser window title
			*/
		public getWindowTitle(): string {
			return document.title || UnoAppManifest.displayName;
		}

		/**
			* Add an event handler to a html element.
			*
			* @param eventName The name of the event
			* @param onCapturePhase true means "on trickle down" (going down to target), false means "on bubble up" (bubbling back to ancestors). Default is false.
			*/
		public registerEventOnView(
			elementId: number,
			eventName: string,
			onCapturePhase: boolean = false,
			eventExtractorId: number
		): string {
			this.registerEventOnViewInternal(elementId, eventName, onCapturePhase, eventExtractorId);
			return "ok";
		}

		/**
			* Add an event handler to a html element.
			*
			* @param eventName The name of the event
			* @param onCapturePhase true means "on trickle down", false means "on bubble up". Default is false.
			*/
		public registerEventOnViewNative(pParams: number): boolean {
			const params = WindowManagerRegisterEventOnViewParams.unmarshal(pParams);

			this.registerEventOnViewInternal(
				params.HtmlId,
				params.EventName,
				params.OnCapturePhase,
				params.EventExtractorId);

			return true;
		}

		public registerPointerEventsOnView(pParams: number): void {
			const params = WindowManagerRegisterEventOnViewParams.unmarshal(pParams);
			const element = this.getView(params.HtmlId);

			element.addEventListener("pointerenter", WindowManager.onPointerEnterReceived);
			element.addEventListener("pointerleave", WindowManager.onPointerLeaveReceived);
			element.addEventListener("pointerdown", WindowManager.onPointerEventReceived);
			element.addEventListener("pointerup", WindowManager.onPointerEventReceived);
			element.addEventListener("pointercancel", WindowManager.onPointerEventReceived);
		}

		public static onPointerEventReceived(evt: PointerEvent): void {
			const element = evt.currentTarget as HTMLElement | SVGElement;
			const payload = WindowManager.pointerEventExtractor(evt);
			const handled = WindowManager.current.dispatchEvent(element, evt.type, payload);
			if (handled) {
				evt.stopPropagation();
			}
		}

		public static onPointerEnterReceived(evt: PointerEvent): void {
			const element = evt.target as HTMLElement | SVGElement;
			const e = evt as any;

			if (e.explicitOriginalTarget) { // FF only

				// It happens on FF that when another control which is over the 'element' has been updated, like text or visibility changed,
				// we receive a pointer enter/leave of an element which is under an element that is capable to handle pointers,
				// which is unexpected as the "pointerenter" should not bubble.
				// So we have to validate that this event is effectively due to the pointer entering the control.
				// We achieve this by browsing up the elements under the pointer (** not the visual tree**) 

				for (let elt of document.elementsFromPoint(evt.pageX, evt.pageY)) {
					if (elt == element) {
						// We found our target element, we can raise the event and stop the loop
						WindowManager.onPointerEventReceived(evt);
						return;
					}

					let htmlElt = elt as HTMLElement;
					if (htmlElt.style.pointerEvents != "none") {
						// This 'htmlElt' is handling the pointers events, this mean that we can stop the loop.
						// However, if this 'htmlElt' is one of our child it means that the event was legitimate
						// and we have to raise it for the 'element'.
						while (htmlElt.parentElement) {
							htmlElt = htmlElt.parentElement;
							if (htmlElt == element) {
								WindowManager.onPointerEventReceived(evt);
								return;
							}
						}

						// We found an element this is capable to handle the pointers but which is not one of our child
						// (probably a sibling which is covering the element). It means that the pointerEnter/Leave should
						// not have bubble to the element, and we can mute it.
						return;
					}
				}
			} else {
				WindowManager.onPointerEventReceived(evt);
			}
		}

		public static onPointerLeaveReceived(evt: PointerEvent): void {
			const element = evt.target as HTMLElement | SVGElement;
			const e = evt as any;

			if (e.explicitOriginalTarget // FF only
				&& e.explicitOriginalTarget !== event.currentTarget
				&& (event as PointerEvent).isOver(element)) {

				// If the event was re-targeted, it's suspicious as the leave event should not bubble
				// This happens on FF when another control which is over the 'element' has been updated, like text or visibility changed.
				// So we have to validate that this event is effectively due to the pointer leaving the element.
				// We achieve that by buffering it until the next few 'pointermove' on document for which we validate the new pointer location.

				// It's common to get a move right after the leave with the same pointer's location,
				// so we wait up to 3 pointer move before dropping the leave event.
				var attempt = 3;

				WindowManager.current.ensurePendingLeaveEventProcessing();
				WindowManager.current.processPendingLeaveEvent = (move: PointerEvent) => {
					if (!move.isOverDeep(element)) {
						// Raising deferred pointerleave on element " + element.id);
						WindowManager.onPointerEventReceived(evt);

						WindowManager.current.processPendingLeaveEvent = null;
					} else if (--attempt <= 0) {
						// Drop deferred pointerleave on element " + element.id);

						WindowManager.current.processPendingLeaveEvent = null;
					} else {
						// Requeue deferred pointerleave on element " + element.id);
					}
				};

			} else {
				WindowManager.onPointerEventReceived(evt);
			}
		}

		private processPendingLeaveEvent: (evt: PointerEvent) => void;

		private _isPendingLeaveProcessingEnabled: boolean;

		/**
		 * Ensure that any pending leave event are going to be processed (cf @see processPendingLeaveEvent )
		 */
		private ensurePendingLeaveEventProcessing() {
			if (this._isPendingLeaveProcessingEnabled) {
				return;
			}

			// Register an event listener on move in order to process any pending event (leave).
			document.addEventListener(
				"pointermove",
				evt => {
					if (this.processPendingLeaveEvent) {
						this.processPendingLeaveEvent(evt as PointerEvent);
					}
				},
				true); // in the capture phase to get it as soon as possible, and to make sure to respect the events ordering
			this._isPendingLeaveProcessingEnabled = true;
		}

		/**
			* Add an event handler to a html element.
			*
			* @param eventName The name of the event
			* @param onCapturePhase true means "on trickle down", false means "on bubble up". Default is false.
			*/
		private registerEventOnViewInternal(
			elementId: number,
			eventName: string,
			onCapturePhase: boolean = false,
			eventExtractorId: number
		): void {
			const element = this.getView(elementId);
			const eventExtractor = this.getEventExtractor(eventExtractorId);
			const eventHandler = (event: Event) => {
				const eventPayload = eventExtractor
					? `${eventExtractor(event)}`
					: "";

				var handled = this.dispatchEvent(element, eventName, eventPayload);
				if (handled) {
					event.stopPropagation();
				}
			};

			element.addEventListener(eventName, eventHandler, onCapturePhase);
		}

		/**
		 * pointer event extractor to be used with registerEventOnView
		 * @param evt
		 */
		private static pointerEventExtractor(evt: PointerEvent|WheelEvent): string {
			if (!evt) {
				return "";
			}

			let src = evt.target as HTMLElement | SVGElement;
			if (src as SVGElement) {
				// The XAML SvgElement are UIElement in Uno (so they have a XamlHandle),
				// but as on WinUI they are not part of the visual tree, they should not be used as OriginalElement.
				// Instead we should use the actual parent <svg /> which is the XAML Shape.
				const shape = (src as any).ownerSVGElement;
				if (shape) {
					src = shape;
				}
			}
			let srcHandle = "0";
			while (src) {
				let handle = src.getAttribute("XamlHandle");
				if (handle) {
					srcHandle = handle;
					break;
				}

				src = src.parentElement;
			}

			let pointerId: number, pointerType: string, pressure: number;
			let wheelDeltaX: number, wheelDeltaY: number;
			if (evt instanceof WheelEvent) {
				pointerId = (evt as any).mozInputSource ? 0 : 1; // Try to match the mouse pointer ID 0 for FF, 1 for others
				pointerType = "mouse";
				pressure = 0.5; // like WinUI
				wheelDeltaX = evt.deltaX;
				wheelDeltaY = evt.deltaY;

				switch (evt.deltaMode) {
					case WheelEvent.DOM_DELTA_LINE: // Actually this is supported only by FF
						const lineSize = WindowManager.wheelLineSize;
						wheelDeltaX *= lineSize;
						wheelDeltaY *= lineSize;
						break;
					case WheelEvent.DOM_DELTA_PAGE:
						wheelDeltaX *= document.documentElement.clientWidth;
						wheelDeltaY *= document.documentElement.clientHeight;
						break;
				}
			} else {
				pointerId = evt.pointerId;
				pointerType = evt.pointerType;
				pressure = evt.pressure;
				wheelDeltaX = 0;
				wheelDeltaY = 0;
			}

			return `${pointerId};${evt.clientX};${evt.clientY};${(evt.ctrlKey ? "1" : "0")};${(evt.shiftKey ? "1" : "0")};${evt.buttons};${evt.button};${pointerType};${srcHandle};${evt.timeStamp};${pressure};${wheelDeltaX};${wheelDeltaY}`;
		}

		private static _wheelLineSize : number = undefined;
		private static get wheelLineSize(): number {
			// In web browsers, scroll might happen by pixels, line or page.
			// But WinUI works only with pixels, so we have to convert it before send the value to the managed code.
			// The issue is that there is no easy way get the "size of a line", instead we have to determine the CSS "line-height"
			// defined in the browser settings. 
			// https://stackoverflow.com/questions/20110224/what-is-the-height-of-a-line-in-a-wheel-event-deltamode-dom-delta-line
			if (this._wheelLineSize == undefined) {
				const el = document.createElement('div');
				el.style.fontSize = 'initial';
				el.style.display = 'none';
				document.body.appendChild(el);
				const fontSize = window.getComputedStyle(el).fontSize;
				document.body.removeChild(el);

				this._wheelLineSize = fontSize ? parseInt(fontSize) : 16; /* 16 = The current common default font size */

				// Based on observations, even if the event reports 3 lines (the settings of windows),
				// the browser will actually scroll of about 6 lines of text.
				this._wheelLineSize *= 2.0;
			}

			return this._wheelLineSize;
		}

		/**
		 * keyboard event extractor to be used with registerEventOnView
		 * @param evt
		 */
		private keyboardEventExtractor(evt: Event): string {
			return (evt instanceof KeyboardEvent) ? evt.key : "0";
		}

		/**
		 * tapped (mouse clicked / double clicked) event extractor to be used with registerEventOnView
		 * @param evt
		 */
		private tappedEventExtractor(evt: MouseEvent): string {
			return evt
				? `0;${evt.clientX};${evt.clientY};${(evt.ctrlKey ? "1" : "0")};${(evt.shiftKey ? "1" : "0")};${evt.button};mouse`
				: "";
		}

		/**
		 * focus event extractor to be used with registerEventOnView
		 * @param evt
		 */
		private focusEventExtractor(evt: FocusEvent): string {

			if (evt) {
				const targetElement: HTMLElement | SVGElement = <any>evt.target;

				if (targetElement) {
					const targetXamlHandle = targetElement.getAttribute("XamlHandle");

					if (targetXamlHandle) {
						return `${targetXamlHandle}`;
					}
				}
			}

			return "";
		}

		private customEventDetailExtractor(evt: CustomEvent): string {
			if (evt) {
				const detail = evt.detail;
				if (detail) {
					return JSON.stringify(detail);
				}
			}

			return "";
		}

		private customEventDetailStringExtractor(evt: CustomEvent): string {
			return evt ? `${evt.detail}` : "";
		}

		/**
		 * Gets the event extractor function. See UIElement.HtmlEventExtractor
		 * @param eventExtractorName an event extractor name.
		 */
		private getEventExtractor(eventExtractorId: number): (evt: Event) => string {

			if (eventExtractorId) {
				//
				// NOTE TO MAINTAINERS: Keep in sync with Windows.UI.Xaml.UIElement.HtmlEventExtractor
				//

				switch (eventExtractorId) {
					case 1:
						return WindowManager.pointerEventExtractor;
					case 3:
						return this.keyboardEventExtractor;

					case 2:
						return this.tappedEventExtractor;

					case 4:
						return this.focusEventExtractor;

					case 6:
						return this.customEventDetailExtractor;

					case 5:
						return this.customEventDetailStringExtractor;
				}

				throw `Event extractor ${eventExtractorId} is not supported`;
			}

			return null;
		}

		/**
			* Set or replace the root content element.
			*/
		public setRootContent(elementId?: number): string {
			if (this.rootContent && Number(this.rootContent.id) === elementId) {
				return null; // nothing to do
			}

			if (this.rootContent) {
				// Remove existing
				this.containerElement.removeChild(this.rootContent);

				if (WindowManager.isLoadEventsEnabled) {
					this.dispatchEvent(this.rootContent, "unloaded");
				}
				this.rootContent.classList.remove(WindowManager.unoRootClassName);
			}

			if (!elementId) {
				return null;
			}

			// set new root
			const newRootElement = this.getView(elementId) as HTMLElement;
			newRootElement.classList.add(WindowManager.unoRootClassName);

			this.rootContent = newRootElement;

			if (WindowManager.isLoadEventsEnabled) {
				this.dispatchEvent(this.rootContent, "loading");
			}

			this.containerElement.appendChild(this.rootContent);

			if (WindowManager.isLoadEventsEnabled) {
				this.dispatchEvent(this.rootContent, "loaded");
			}
			this.setAsArranged(newRootElement); // patch because root is not measured/arranged

			this.resize();

			return "ok";
		}

		/**
			* Set a view as a child of another one.
			*
			* "Loading" & "Loaded" events will be raised if necessary.
			*
			* @param index Position in children list. Appended at end if not specified.
			*/
		public addView(parentId: number, childId: number, index?: number): string {
			this.addViewInternal(parentId, childId, index);
			return "ok";
		}

		/**
			* Set a view as a child of another one.
			*
			* "Loading" & "Loaded" events will be raised if necessary.
			*
			* @param pParams Pointer to a WindowManagerAddViewParams native structure.
			*/
		public addViewNative(pParams: number): boolean {
			const params = WindowManagerAddViewParams.unmarshal(pParams);

			this.addViewInternal(
				params.HtmlId,
				params.ChildView,
				params.Index != -1 ? params.Index : null
			);

			return true;
		}

		public addViewInternal(parentId: number, childId: number, index?: number): void {
			const parentElement = this.getView(parentId);
			const childElement = this.getView(childId);

			let shouldRaiseLoadEvents = false;
			if (WindowManager.isLoadEventsEnabled) {
				const alreadyLoaded = this.getIsConnectedToRootElement(childElement);
				shouldRaiseLoadEvents = !alreadyLoaded && this.getIsConnectedToRootElement(parentElement);

				if (shouldRaiseLoadEvents) {
					this.dispatchEvent(childElement, "loading");
				}
			}

			if (index != null && index < parentElement.childElementCount) {
				const insertBeforeElement = parentElement.children[index];
				parentElement.insertBefore(childElement, insertBeforeElement);

			} else {
				parentElement.appendChild(childElement);
			}

			if (shouldRaiseLoadEvents) {
				this.dispatchEvent(childElement, "loaded");
			}
		}

		/**
			* Remove a child from a parent element.
			*
			* "Unloading" & "Unloaded" events will be raised if necessary.
			*/
		public removeView(parentId: number, childId: number): string {
			this.removeViewInternal(parentId, childId);
			return "ok";
		}

		/**
			* Remove a child from a parent element.
			*
			* "Unloading" & "Unloaded" events will be raised if necessary.
			*/
		public removeViewNative(pParams: number): boolean {
			const params = WindowManagerRemoveViewParams.unmarshal(pParams);
			this.removeViewInternal(params.HtmlId, params.ChildView);
			return true;
		}

		private removeViewInternal(parentId: number, childId: number): void {
			const parentElement = this.getView(parentId);
			const childElement = this.getView(childId);

			const shouldRaiseLoadEvents = WindowManager.isLoadEventsEnabled
				&& this.getIsConnectedToRootElement(childElement);

			parentElement.removeChild(childElement);

			// Mark the element as unarranged, so if it gets measured while being
			// disconnected from the root element, it won't be visible.
			this.setAsUnarranged(childElement);

			if (shouldRaiseLoadEvents) {
				this.dispatchEvent(childElement, "unloaded");
			}
		}

		/**
			* Destroy a html element.
			*
			* The element won't be available anymore. Usually indicate the managed
			* version has been scavenged by the GC.
			*/
		public destroyView(elementId: number): string {
			this.destroyViewInternal(elementId);
			return "ok";
		}

		/**
			* Destroy a html element.
			*
			* The element won't be available anymore. Usually indicate the managed
			* version has been scavenged by the GC.
			*/
		public destroyViewNative(pParams: number): boolean {
			const params = WindowManagerDestroyViewParams.unmarshal(pParams);
			this.destroyViewInternal(params.HtmlId);
			return true;
		}

		private destroyViewInternal(elementId: number): void {
			const element = this.getView(elementId);

			if (element.parentElement) {
				element.parentElement.removeChild(element);
			}

			delete this.allActiveElementsById[elementId];
		}

		public getBoundingClientRect(elementId: number): string {

			const bounds = (<any>this.getView(elementId)).getBoundingClientRect();
			return `${bounds.left};${bounds.top};${bounds.right - bounds.left};${bounds.bottom - bounds.top}`;
		}

		public getBBox(elementId: number): string {
			const bbox = this.getBBoxInternal(elementId);

			return `${bbox.x};${bbox.y};${bbox.width};${bbox.height}`;
		}

		public getBBoxNative(pParams: number, pReturn: number): boolean {

			const params = WindowManagerGetBBoxParams.unmarshal(pParams);

			const bbox = this.getBBoxInternal(params.HtmlId);

			const ret = new WindowManagerGetBBoxReturn();
			ret.X = bbox.x;
			ret.Y = bbox.y;
			ret.Width = bbox.width;
			ret.Height = bbox.height;

			ret.marshal(pReturn);

			return true;
		}

		private getBBoxInternal(elementId: number): any {
			return (<any>this.getView(elementId)).getBBox();
		}

		public setSvgElementRect(pParams: number): boolean {
			const params = WindowManagerSetSvgElementRectParams.unmarshal(pParams);

			const element = this.getView(params.HtmlId) as any;

			element.x.baseVal.value = params.X;
			element.y.baseVal.value = params.Y;
			element.width.baseVal.value = params.Width;
			element.height.baseVal.value = params.Height;

			return true;
		}

		/**
			* Use the Html engine to measure the element using specified constraints.
			*
			* @param maxWidth string containing width in pixels. Empty string means infinite.
			* @param maxHeight string containing height in pixels. Empty string means infinite.
			*/
		public measureView(viewId: string, maxWidth: string, maxHeight: string): string {

			const ret = this.measureViewInternal(Number(viewId), maxWidth ? Number(maxWidth) : NaN, maxHeight ? Number(maxHeight) : NaN);

			return `${ret[0]};${ret[1]}`;
		}

		/**
			* Use the Html engine to measure the element using specified constraints.
			*
			* @param maxWidth string containing width in pixels. Empty string means infinite.
			* @param maxHeight string containing height in pixels. Empty string means infinite.
			*/
		public measureViewNative(pParams: number, pReturn: number): boolean {

			const params = WindowManagerMeasureViewParams.unmarshal(pParams);

			const ret = this.measureViewInternal(params.HtmlId, params.AvailableWidth, params.AvailableHeight);

			const ret2 = new WindowManagerMeasureViewReturn();
			ret2.DesiredWidth = ret[0];
			ret2.DesiredHeight = ret[1];

			ret2.marshal(pReturn);

			return true;
		}

		private static MAX_WIDTH = `${Number.MAX_SAFE_INTEGER}vw`;
		private static MAX_HEIGHT = `${Number.MAX_SAFE_INTEGER}vh`;

		private measureElement(element: HTMLElement): [number, number] {

			const offsetWidth = element.offsetWidth;
			const offsetHeight = element.offsetHeight;

			const resultWidth = offsetWidth ? offsetWidth : element.clientWidth;
			const resultHeight = offsetHeight ? offsetHeight : element.clientHeight;

			// +1 is added to take rounding/flooring into account
			return [resultWidth + 1, resultHeight];
		}

		private measureViewInternal(viewId: number, maxWidth: number, maxHeight: number): [number, number] {
			const element = this.getView(viewId) as HTMLElement;

			const elementStyle = element.style;
			const originalStyleCssText = elementStyle.cssText;
			let parentElement: HTMLElement = null;
			let parentElementWidthHeight: { width: string, height: string } = null;
			let unconnectedRoot: HTMLElement = null;

			let cleanupUnconnectedRoot = function (owner: HTMLDivElement) {
				if (unconnectedRoot !== null) {
					owner.removeChild(unconnectedRoot);
				}
			}

			try {
				if (!element.isConnected) {
					// If the element is not connected to the DOM, we need it
					// to be connected for the measure to provide a meaningful value.

					unconnectedRoot = element;
					while (unconnectedRoot.parentElement) {
						// Need to find the top most "unconnected" parent
						// of this element
						unconnectedRoot = unconnectedRoot.parentElement as HTMLElement;
					}

					this.containerElement.appendChild(unconnectedRoot);
				}

				// As per W3C css-transform spec:
				// https://www.w3.org/TR/css-transforms-1/#propdef-transform
				//
				// > For elements whose layout is governed by the CSS box model, any value other than none
				// > for the transform property also causes the element to establish a containing block for
				// > all descendants.Its padding box will be used to layout for all of its
				// > absolute - position descendants, fixed - position descendants, and descendant fixed
				// > background attachments.
				//
				// We use this feature to allow an measure of text without being influenced by the bounds
				// of the viewport. We just need to temporary set both the parent width & height to a very big value.

				parentElement = element.parentElement;
				parentElementWidthHeight = { width: parentElement.style.width, height: parentElement.style.height };
				parentElement.style.width = WindowManager.MAX_WIDTH;
				parentElement.style.height = WindowManager.MAX_HEIGHT;

				const updatedStyles = <any>{};

				for (let i = 0; i < elementStyle.length; i++) {
					const key = elementStyle[i];
					updatedStyles[key] = elementStyle.getPropertyValue(key);
				}

				if (updatedStyles.hasOwnProperty("width")) {
					delete updatedStyles.width;
				}
				if (updatedStyles.hasOwnProperty("height")) {
					delete updatedStyles.height;
				}

				// This is required for an unconstrained measure (otherwise the parents size is taken into account)
				updatedStyles.position = "fixed";
				updatedStyles["max-width"] = Number.isFinite(maxWidth) ? maxWidth + "px" : "none";
				updatedStyles["max-height"] = Number.isFinite(maxHeight) ? maxHeight + "px" : "none";

				let updatedStyleString = "";

				for (let key in updatedStyles) {
					if (updatedStyles.hasOwnProperty(key)) {
						updatedStyleString += key + ": " + updatedStyles[key] + "; ";
					}
				}

				// We use a string to prevent the browser to update the element between
				// each style assignation. This way, the browser will update the element only once.
				elementStyle.cssText = updatedStyleString;

				if (element instanceof HTMLImageElement) {
					const imgElement = element as HTMLImageElement;
					return [imgElement.naturalWidth, imgElement.naturalHeight];
				}
				else if (element instanceof HTMLInputElement) {
					const inputElement = element as HTMLInputElement;

					cleanupUnconnectedRoot(this.containerElement);

					// Create a temporary element that will contain the input's content
					var textOnlyElement = document.createElement("p") as HTMLParagraphElement;
					textOnlyElement.style.cssText = updatedStyleString;
					textOnlyElement.innerText = inputElement.value;

					unconnectedRoot = textOnlyElement;
					this.containerElement.appendChild(unconnectedRoot);

					var textSize = this.measureElement(textOnlyElement);
					var inputSize = this.measureElement(element);

					// Take the width of the inner text, but keep the height of the input element.
					return [textSize[0], inputSize[1]];
				}
				else {
					return this.measureElement(element);
				}
			}
			finally {
				elementStyle.cssText = originalStyleCssText;

				if (parentElement && parentElementWidthHeight) {
					parentElement.style.width = parentElementWidthHeight.width;
					parentElement.style.height = parentElementWidthHeight.height;
				}

				cleanupUnconnectedRoot(this.containerElement);
			}
		}

		public scrollTo(pParams: number): boolean {

			const params = WindowManagerScrollToOptionsParams.unmarshal(pParams);
			const elt = this.getView(params.HtmlId);
			const opts = <ScrollToOptions>({
				left: params.HasLeft ? params.Left : undefined,
				top: params.HasTop ? params.Top : undefined,
				behavior: <ScrollBehavior>(params.DisableAnimation ? "auto" : "smooth")
			});

			elt.scrollTo(opts);

			return true;
		}

		public rawPixelsToBase64EncodeImage(dataPtr: number, width: number, height: number): string {
			const rawCanvas = document.createElement("canvas");
			rawCanvas.width = width;
			rawCanvas.height = height;

			const ctx = rawCanvas.getContext("2d");
			const imgData = ctx.createImageData(width, height);

			const bufferSize = width * height * 4;

			for (let i = 0; i < bufferSize; i += 4) {
				imgData.data[i + 0] = Module.HEAPU8[dataPtr + i + 2];
				imgData.data[i + 1] = Module.HEAPU8[dataPtr + i + 1];
				imgData.data[i + 2] = Module.HEAPU8[dataPtr + i + 0];
				imgData.data[i + 3] = Module.HEAPU8[dataPtr + i + 3];
			}
			ctx.putImageData(imgData, 0, 0);

			return rawCanvas.toDataURL();
		}


		/**
		 * Sets the provided image with a mono-chrome version of the provided url.
		 * @param viewId the image to manipulate
		 * @param url the source image
		 * @param color the color to apply to the monochrome pixels
		 */
		public setImageAsMonochrome(viewId: number, url: string, color: string): string {
			const element = this.getView(viewId);

			if (element.tagName.toUpperCase() === "IMG") {

				const imgElement = element as HTMLImageElement;
				var img = new Image();
				img.onload = buildMonochromeImage;
				img.src = url;

				function buildMonochromeImage() {

					// create a colored version of img
					const c = document.createElement('canvas');
					const ctx = c.getContext('2d');

					c.width = img.width;
					c.height = img.height;

					ctx.drawImage(img, 0, 0);
					ctx.globalCompositeOperation = 'source-atop';
					ctx.fillStyle = color;
					ctx.fillRect(0, 0, img.width, img.height);
					ctx.globalCompositeOperation = 'source-over';

					imgElement.src = c.toDataURL();
				}

				return "ok";
			}
			else {
				throw `setImageAsMonochrome: Element id ${viewId} is not an Img.`;
			}
		}

		public setPointerCapture(viewId: number, pointerId: number): string {
			this.getView(viewId).setPointerCapture(pointerId);

			return "ok";
		}

		public releasePointerCapture(viewId: number, pointerId: number): string {
			this.getView(viewId).releasePointerCapture(pointerId);

			return "ok";
		}

		public focusView(elementId: number): string {
			const element = this.getView(elementId);

			if (!(element instanceof HTMLElement)) {
				throw `Element id ${elementId} is not focusable.`;
			}

			element.focus();

			return "ok";
		}

		/**
			* Set the Html content for an element.
			*
			* Those html elements won't be available as XamlElement in managed code.
			* WARNING: you should avoid mixing this and `addView` for the same element.
			*/
		public setHtmlContent(viewId: number, html: string): string {
			this.setHtmlContentInternal(viewId, html);
			return "ok";
		}

		/**
			* Set the Html content for an element.
			*
			* Those html elements won't be available as XamlElement in managed code.
			* WARNING: you should avoid mixing this and `addView` for the same element.
			*/
		public setHtmlContentNative(pParams: number): boolean {
			const params = WindowManagerSetContentHtmlParams.unmarshal(pParams);

			this.setHtmlContentInternal(params.HtmlId, params.Html);
			return true;
		}

		private setHtmlContentInternal(viewId: number, html: string): void {

			this.getView(viewId).innerHTML = html;
		}

		/**
		 * Gets the Client and Offset size of the specified element
		 *
		 * This method is used to determine the size of the scroll bars, to
		 * mask the events coming from that zone.
		 */
		public getClientViewSize(elementId: number): string {
			const element = this.getView(elementId) as HTMLElement;

			return `${element.clientWidth};${element.clientHeight};${element.offsetWidth};${element.offsetHeight}`;
		}

		/**
		 * Gets the Client and Offset size of the specified element
		 *
		 * This method is used to determine the size of the scroll bars, to
		 * mask the events coming from that zone.
		 */
		public getClientViewSizeNative(pParams: number, pReturn: number): boolean {
			const params = WindowManagerGetClientViewSizeParams.unmarshal(pParams);

			const element = this.getView(params.HtmlId) as HTMLElement;

			const ret2 = new WindowManagerGetClientViewSizeReturn();
			ret2.ClientWidth = element.clientWidth;
			ret2.ClientHeight = element.clientHeight;
			ret2.OffsetWidth = element.offsetWidth;
			ret2.OffsetHeight = element.offsetHeight;

			ret2.marshal(pReturn);

			return true;
		}

		/**
		 * Gets a dependency property value.
		 *
		 * Note that the casing of this method is intentionally Pascal for platform alignment.
		 */
		public GetDependencyPropertyValue(elementId: number, propertyName: string): string {
			if (!WindowManager.getDependencyPropertyValueMethod) {
				WindowManager.getDependencyPropertyValueMethod = (<any>Module).mono_bind_static_method("[Uno.UI] Uno.UI.Helpers.Automation:GetDependencyPropertyValue");
			}

			const element = this.getView(elementId) as HTMLElement;
			const htmlId = Number(element.getAttribute("XamlHandle"));

			return WindowManager.getDependencyPropertyValueMethod(htmlId, propertyName);
		}

		/**
		 * Sets a dependency property value.
		 *
		 * Note that the casing of this method is intentionally Pascal for platform alignment.
		 */
		public SetDependencyPropertyValue(elementId: number, propertyNameAndValue: string): string {
			if (!WindowManager.setDependencyPropertyValueMethod) {
				WindowManager.setDependencyPropertyValueMethod = (<any>Module).mono_bind_static_method("[Uno.UI] Uno.UI.Helpers.Automation:SetDependencyPropertyValue");
			}

			const element = this.getView(elementId) as HTMLElement;
			const htmlId = Number(element.getAttribute("XamlHandle"));

			return WindowManager.setDependencyPropertyValueMethod(htmlId, propertyNameAndValue);
		}

		/**
			* Remove the loading indicator.
			*
			* In a future version it will also handle the splashscreen.
			*/
		public activate(): string {
			this.removeLoading();
			return "ok";
		}

		private init() {

			if (UnoAppManifest.displayName) {
				document.title = UnoAppManifest.displayName;
			}

			window.addEventListener(
				"beforeunload",
				() => WindowManager.dispatchSuspendingMethod()
			);
		}

		private static initMethods() {
			if (WindowManager.isHosted) {
				console.debug("Hosted Mode: Skipping MonoRuntime initialization ");
			}
			else {
				if (!WindowManager.resizeMethod) {
					WindowManager.resizeMethod = (<any>Module).mono_bind_static_method("[Uno.UI] Windows.UI.Xaml.Window:Resize");
				}

				if (!WindowManager.dispatchEventMethod) {
					WindowManager.dispatchEventMethod = (<any>Module).mono_bind_static_method("[Uno.UI] Windows.UI.Xaml.UIElement:DispatchEvent");
				}

				if (!WindowManager.focusInMethod) {
					WindowManager.focusInMethod = (<any>Module).mono_bind_static_method("[Uno.UI] Windows.UI.Xaml.Input.FocusManager:ReceiveFocusNative");
				}

				if (!WindowManager.dispatchSuspendingMethod) {
					WindowManager.dispatchSuspendingMethod = (<any>Module).mono_bind_static_method("[Uno.UI] Windows.UI.Xaml.Application:DispatchSuspending");
				}
			}
		}

		private initDom() {
			this.containerElement = (document.getElementById(this.containerElementId) as HTMLDivElement);
			if (!this.containerElement) {
				// If not found, we simply create a new one.
				this.containerElement = document.createElement("div");
			}
			document.body.addEventListener("focusin", this.onfocusin);
			document.body.appendChild(this.containerElement);

			window.addEventListener("resize", x => this.resize());
			window.addEventListener("contextmenu", x => {
				if (!(x.target instanceof HTMLInputElement)) {
					x.preventDefault();
				}
			})
			window.addEventListener("blur", this.onWindowBlur);
		}

		private removeLoading() {

			if (!this.loadingElementId) {
				return;
			}

			const element = document.getElementById(this.loadingElementId);
			if (element) {
				element.parentElement.removeChild(element);
			}

			// UWP Window's default background is white.
			const body = document.getElementsByTagName("body")[0];
			body.style.backgroundColor = "#fff";
		}

		private resize() {

			if (WindowManager.isHosted) {
				UnoDispatch.resize(`${document.documentElement.clientWidth};${document.documentElement.clientHeight}`);
			}
			else {
				WindowManager.resizeMethod(document.documentElement.clientWidth, document.documentElement.clientHeight);
			}
		}

		private onfocusin(event: Event) {
			if (WindowManager.isHosted) {
				console.warn("Focus not supported in hosted mode");
			}
			else {
				const newFocus = event.target;
				const handle = (newFocus as HTMLElement).getAttribute("XamlHandle");
				const htmlId = handle ? Number(handle) : -1; // newFocus may not be an Uno element
				WindowManager.focusInMethod(htmlId);
			}
		}

		private onWindowBlur() {
			if (WindowManager.isHosted) {
				console.warn("Focus not supported in hosted mode");
			}
			else {
				// Unset managed focus when Window loses focus
				WindowManager.focusInMethod(-1);
			}
		}

		private dispatchEvent(element: HTMLElement | SVGElement, eventName: string, eventPayload: string = null): boolean {
			const htmlId = Number(element.getAttribute("XamlHandle"));

			// console.debug(`${element.getAttribute("id")}: Raising event ${eventName}.`);

			if (!htmlId) {
				throw `No attribute XamlHandle on element ${element}. Can't raise event.`;
			}

			if (WindowManager.isHosted) {
				// Dispatch to the C# backed UnoDispatch class. Events propagated
				// this way always succeed because synchronous calls are not possible
				// between the host and the browser, unlike wasm.
				UnoDispatch.dispatch(this.handleToString(htmlId), eventName, eventPayload);
				return true;
			}
			else {
				return WindowManager.dispatchEventMethod(htmlId, eventName, eventPayload || "");
			}
		}

		private getIsConnectedToRootElement(element: HTMLElement | SVGElement): boolean {
			const rootElement = this.rootContent;

			if (!rootElement) {
				return false;
			}
			return rootElement === element || rootElement.contains(element);
		}

		private handleToString(handle: number): string {

			// Fastest conversion as of 2020-03-25 (when compared to String(handle) or handle.toString())
			return handle + "";
		}

		public setCursor(cssCursor: string): string {
			const unoBody = document.getElementById(this.containerElementId);

			if (unoBody) {

				//always cleanup
				if (this.cursorStyleElement != undefined) {
					this.cursorStyleElement.remove();
					this.cursorStyleElement = undefined
				}

				//only add custom overriding style if not auto 
				if (cssCursor != "auto") {

					// this part is only to override default css:  .uno-buttonbase {cursor: pointer;}

					this.cursorStyleElement = document.createElement("style");
					this.cursorStyleElement.innerHTML = ".uno-buttonbase { cursor: " + cssCursor + "; }";
					document.body.appendChild(this.cursorStyleElement);
				}

				unoBody.style.cursor = cssCursor;
			}
			return "ok";
		}
	}

	if (typeof define === "function") {
		define(
			[`${config.uno_app_base}/AppManifest`],
			() => {
			}
		);
	}
	else {
		throw `The Uno.Wasm.Boostrap is not up to date, please upgrade to a later version`;
	}
}
