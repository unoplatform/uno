namespace Windows.UI.Xaml {
	import WindowManager = Uno.UI.WindowManager;
	import HtmlEventDispatchResult = Uno.UI.HtmlEventDispatchResult;

	export enum NativePointerEvent {
		pointerenter = 1,
		pointerleave = 1 << 1,
		pointerdown = 1 << 2,
		pointerup = 1 << 3,
		pointercancel = 1 << 4,

		// Optional pointer events
		pointermove = 1 << 5,
		wheel = 1 << 6,
	}

	export class UIElement_Pointers {

		private static _dispatchPointerEventMethod: any;
		private static _dispatchPointerEventArgs: number;
		private static _dispatchPointerEventResult: number;

		private static _isPendingLeaveProcessingEnabled: boolean;

		public static setPointerEventArgs(pArgs: number): void {
			if (!UIElement._dispatchPointerEventMethod) {
				UIElement._dispatchPointerEventMethod = (<any>Module).mono_bind_static_method("[Uno.UI] Windows.UI.Xaml.UIElement:OnNativePointerEvent");
			}
			UIElement._dispatchPointerEventArgs = pArgs;
		}

		public static setPointerEventResult(pArgs: number): void {
			if (!UIElement._dispatchPointerEventMethod) {
				UIElement._dispatchPointerEventMethod = (<any>Module).mono_bind_static_method("[Uno.UI] Windows.UI.Xaml.UIElement:OnNativePointerEvent");
			}
			UIElement._dispatchPointerEventResult = pArgs;
		}

		public static subscribePointerEvents(pParams: number): void {
			const params = Windows.UI.Xaml.NativePointerSubscriptionParams.unmarshal(pParams);
			const element = WindowManager.current.getView(params.HtmlId);

			if (params.Events & NativePointerEvent.pointerenter) {
				element.addEventListener("pointerenter", UIElement.onPointerEnterReceived);
			}
			if (params.Events & NativePointerEvent.pointerleave) {
				element.addEventListener("pointerleave", UIElement.onPointerLeaveReceived);
			}
			if (params.Events & NativePointerEvent.pointerdown) {
				element.addEventListener("pointerdown", UIElement.onPointerEventReceived);
			}
			if (params.Events & NativePointerEvent.pointerup) {
				element.addEventListener("pointerup", UIElement.onPointerEventReceived);
			}
			if (params.Events & NativePointerEvent.pointercancel) {
				element.addEventListener("pointercancel", UIElement.onPointerEventReceived);
			}
			if (params.Events & NativePointerEvent.pointermove) {
				element.addEventListener("pointermove", UIElement.onPointerEventReceived);
			}
			if (params.Events & NativePointerEvent.wheel) {
				element.addEventListener("wheel", UIElement.onPointerEventReceived);
			}
		}

		public static unSubscribePointerEvents(pParams: number): void {
			const params = Windows.UI.Xaml.NativePointerSubscriptionParams.unmarshal(pParams);
			const element = WindowManager.current.getView(params.HtmlId);

			if (params.Events & NativePointerEvent.pointerenter) {
				element.removeEventListener("pointerenter", UIElement.onPointerEnterReceived);
			}
			if (params.Events & NativePointerEvent.pointerleave) {
				element.removeEventListener("pointerleave", UIElement.onPointerLeaveReceived);
			}
			if (params.Events & NativePointerEvent.pointerdown) {
				element.removeEventListener("pointerdown", UIElement.onPointerEventReceived);
			}
			if (params.Events & NativePointerEvent.pointerup) {
				element.removeEventListener("pointerup", UIElement.onPointerEventReceived);
			}
			if (params.Events & NativePointerEvent.pointercancel) {
				element.removeEventListener("pointercancel", UIElement.onPointerEventReceived);
			}
			if (params.Events & NativePointerEvent.pointermove) {
				element.removeEventListener("pointermove", UIElement.onPointerEventReceived);
			}
			if (params.Events & NativePointerEvent.wheel) {
				element.removeEventListener("wheel", UIElement.onPointerEventReceived);
			}
		}

		private static onPointerEventReceived(evt: PointerEvent): void {
			UIElement.dispatchPointerEvent(evt.currentTarget as HTMLElement | SVGElement, evt);
		}

		private static onPointerEnterReceived(evt: PointerEvent): void {
			const element = evt.currentTarget as HTMLElement | SVGElement;
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
						UIElement.onPointerEventReceived(evt);
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
								UIElement.onPointerEventReceived(evt);
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
				UIElement.onPointerEventReceived(evt);
			}
		}

		private static onPointerLeaveReceived(evt: PointerEvent): void {
			const element = evt.currentTarget as HTMLElement | SVGElement;
			const e = evt as any;

			if (e.explicitOriginalTarget // FF only
				&& e.explicitOriginalTarget !== element
				&& (event as PointerEvent).isOver(element)) {

				// If the event was re-targeted, it's suspicious as the leave event should not bubble
				// This happens on FF when another control which is over the 'element' has been updated, like text or visibility changed.
				// So we have to validate that this event is effectively due to the pointer leaving the element.
				// We achieve that by buffering it until the next few 'pointermove' on document for which we validate the new pointer location.

				// It's common to get a move right after the leave with the same pointer's location,
				// so we wait up to 3 pointer move before dropping the leave event.
				let attempt = 3;

				UIElement.ensurePendingLeaveEventProcessing();
				UIElement.processPendingLeaveEvent = (move: PointerEvent) => {
					if (!move.isOverDeep(element)) {
						// Raising deferred pointerleave on element " + element.id);
						// Note The 'evt.currentTarget' is available only while in the event handler.
						//		So we manually keep a reference ('element') and explicit dispatch event to it.
						//		https://developer.mozilla.org/en-US/docs/Web/API/Event/currentTarget
						UIElement.dispatchPointerEvent(element, evt);

						UIElement.processPendingLeaveEvent = null;
					} else if (--attempt <= 0) {
						// Drop deferred pointerleave on element " + element.id);

						UIElement.processPendingLeaveEvent = null;
					} else {
						// Requeue deferred pointerleave on element " + element.id);
					}
				};

			} else {
				UIElement.onPointerEventReceived(evt);
			}
		}

		private static processPendingLeaveEvent: (evt: PointerEvent) => void;

		/**
		 * Ensure that any pending leave event are going to be processed (cf @see processPendingLeaveEvent )
		 */
		private static ensurePendingLeaveEventProcessing() {
			if (UIElement._isPendingLeaveProcessingEnabled) {
				return;
			}

			// Register an event listener on move in order to process any pending event (leave).
			document.addEventListener(
				"pointermove",
				evt => {
					if (UIElement.processPendingLeaveEvent) {
						UIElement.processPendingLeaveEvent(evt as PointerEvent);
					}
				},
				true); // in the capture phase to get it as soon as possible, and to make sure to respect the events ordering
			UIElement._isPendingLeaveProcessingEnabled = true;
		}

		private static dispatchPointerEvent(element: HTMLElement | SVGElement, evt: PointerEvent): void {
			if (!evt) {
				return;
			}

			const args = UIElement.toNativePointerEventArgs(evt);
			args.HtmlId = Number(element.getAttribute("XamlHandle"));

			args.marshal(UIElement._dispatchPointerEventArgs);
			UIElement._dispatchPointerEventMethod();
			const response = Windows.UI.Xaml.NativePointerEventResult.unmarshal(UIElement._dispatchPointerEventResult);

			if (response.Result & HtmlEventDispatchResult.StopPropagation) {
				evt.stopPropagation();
			}
			if (response.Result & HtmlEventDispatchResult.PreventDefault) {
				evt.preventDefault();
			}
		}

		//#region WheelLineSize
		private static _wheelLineSize: number = undefined;
		private static get wheelLineSize(): number {
			// In web browsers, scroll might happen by pixels, line or page.
			// But WinUI works only with pixels, so we have to convert it before send the value to the managed code.
			// The issue is that there is no easy way get the "size of a line", instead we have to determine the CSS "line-height"
			// defined in the browser settings. 
			// https://stackoverflow.com/questions/20110224/what-is-the-height-of-a-line-in-a-wheel-event-deltamode-dom-delta-line
			if (this._wheelLineSize == undefined) {
				const el = document.createElement("div");
				el.style.fontSize = "initial";
				el.style.display = "none";
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
		//#endregion

		//#region Helpers
		/**
		 * pointer event extractor to be used with registerEventOnView
		 * @param evt
		 */
		private static toNativePointerEventArgs(evt: PointerEvent | WheelEvent): Windows.UI.Xaml.NativePointerEventArgs {
			let src = evt.target as HTMLElement | SVGElement;
			if (src instanceof SVGElement) {
				// The XAML SvgElement are UIElement in Uno (so they have a XamlHandle),
				// but as on WinUI they are not part of the visual tree, they should not be used as OriginalElement.
				// Instead we should use the actual parent <svg /> which is the XAML Shape.
				const shape = (src as any).ownerSVGElement;
				if (shape) {
					src = shape;
				}
			} else if (src instanceof HTMLImageElement) {
				// Same as above for images (<img /> == HtmlImage, we use the parent <div /> which is the XAML Image).
				src = src.parentElement;
			}

			let srcHandle = "0";
			while (src) {
				const handle = src.getAttribute("XamlHandle");
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
						const lineSize = UIElement.wheelLineSize;
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

			const args = new Windows.UI.Xaml.NativePointerEventArgs();
			args.Event = UIElement.toNativeEvent(evt.type);
			args.pointerId = pointerId;
			args.x = evt.clientX;
			args.y = evt.clientY;
			args.ctrl = evt.ctrlKey;
			args.shift = evt.shiftKey;
			args.buttons = evt.buttons;
			args.buttonUpdate = evt.button;
			args.typeStr = pointerType;
			args.srcHandle = Number(srcHandle);
			args.timestamp = evt.timeStamp;
			args.pressure = pressure;
			args.wheelDeltaX = wheelDeltaX;
			args.wheelDeltaY = wheelDeltaY;

			return args;
		}

		private static toNativeEvent(eventName: string): NativePointerEvent {
			switch (eventName) {
				case "pointerenter":
					return NativePointerEvent.pointerenter;
				case "pointerleave"  :
					return NativePointerEvent.pointerleave;
				case "pointerdown"	 :
					return NativePointerEvent.pointerdown;
				case "pointerup"	 :
					return NativePointerEvent.pointerup;
				case "pointercancel" :
					return NativePointerEvent.pointercancel;
				case "pointermove"	 :
					return NativePointerEvent.pointermove;
				case "wheel":
					return NativePointerEvent.wheel;
				default:
					return undefined;
			}
		}
		//#endregion
	}
}
