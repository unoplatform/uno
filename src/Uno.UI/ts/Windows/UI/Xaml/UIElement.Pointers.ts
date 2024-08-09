namespace Windows.UI.Xaml {
	import WindowManager = Uno.UI.WindowManager;
	import HtmlEventDispatchResult = Uno.UI.HtmlEventDispatchResult;
	import PointerDeviceType = Windows.Devices.Input.PointerDeviceType;

	export enum NativePointerEvent {
		pointerover = 1,
		pointerout = 1 << 1,
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
		private static _isManualyDispatchingOut: boolean = false;

		private static ensurePointersInit(): void {
			if (!UIElement._dispatchPointerEventMethod) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					UIElement._dispatchPointerEventMethod = (<any>globalThis).DotnetExports.UnoUI.Windows.UI.Xaml.UIElement.OnNativePointerEvent;
				} else {
					UIElement._dispatchPointerEventMethod = (<any>Module).mono_bind_static_method("[Uno.UI] Windows.UI.Xaml.UIElement:OnNativePointerEvent");
				}

				document.getRootNode().addEventListener("pointerover", UIElement_Pointers.onRootPointerOverDispatching, { capture: true, passive: true });
			}
		}

		public static setPointerEventArgs(pArgs: number): void {
			UIElement_Pointers.ensurePointersInit();
			UIElement._dispatchPointerEventArgs = pArgs;
		}

		public static setPointerEventResult(pArgs: number): void {
			UIElement_Pointers.ensurePointersInit();
			UIElement._dispatchPointerEventResult = pArgs;
		}

		public static subscribePointerEvents(pParams: number): void {
			const params = Windows.UI.Xaml.NativePointerSubscriptionParams.unmarshal(pParams);
			const element = WindowManager.current.getView(params.HtmlId);

			if (params.Events & NativePointerEvent.pointerover) {
				element.addEventListener("pointerover", UIElement.onPointerEventReceived);
			}
			if (params.Events & NativePointerEvent.pointerout) {
				element.addEventListener("pointerout", UIElement.onPointerOutReceived);
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

			if (!element) {
				return;
			}

			if (params.Events & NativePointerEvent.pointerover) {
				element.removeEventListener("pointerover", UIElement.onPointerEventReceived);
			}
			if (params.Events & NativePointerEvent.pointerout) {
				element.removeEventListener("pointerout", UIElement.onPointerOutReceived);
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

		private static onRootPointerOverDispatching(evt: PointerEvent): void {
			const target = evt.target as HTMLElement | SVGElement;
			if (!target) {
				return;
			}

			if (target.hasPointerCapture(evt.pointerId)) {
				return;
			}

			let prevElt: Element = null;
			for (let elt of document.elementsFromPoint(evt.clientX, evt.clientY)) {
				if (!elt.contains(target) // The elt is under the target (not a parent!)
					&& !elt.contains(prevElt) // The elt is not a parent of the previous element (so it has already been processed!)
				) {
					// The element is under the target (not a parent!), with chromium browsers, if the target just popped up,
					// it happens that the pointerout event is not raised on those elemnts under.
					// Here we ensure to raise it manually before the pointerover event is being dispatched.
					try {
						UIElement._isManualyDispatchingOut = true;
						elt.dispatchEvent(new PointerEvent("pointerout", evt));
					}
					finally {
						UIElement._isManualyDispatchingOut = false;
					}
				}
				prevElt = elt;
			}
		}

		private static onPointerOutReceived(evt: PointerEvent): void {
			if (UIElement._isManualyDispatchingOut) {
				UIElement.onPointerEventReceived(evt);
				return;
			}

			const element = evt.currentTarget as HTMLElement | SVGElement;

			// When we capture the pointer, browser will raise an "out" event on nested elements
			// and then an "over" on the element that captured the pointer.
			// But those events will be raise right BEFORE the NEXT pointer event (e.g. a move).
			// Note: We don't filter out the "over" event because it's handled in managed by tracking the IsOver state.

			// Here we filter the "out" event that is being raised after capture or release
			// If the relatedTarget (the element that capture or release the pointer) is a child of (or is) the current element,
			// it means pointer might not leaving the current element!
			let elt = evt.relatedTarget as HTMLElement | SVGElement;
			if (elt
				&& element.contains(elt)
				&& (
					// on capture, we just check if it has the the capture
					elt.hasPointerCapture(evt.pointerId)
					// on release, the target is the element itself
					|| evt.target == element)
				)
			{
				evt.stopPropagation();
				return;
			}

			// Finally, here we filter out the events that are being raised when the pointer is leaving a nested element (which is bubbling in browser)
			const targetBounds = (evt.target as HTMLElement | SVGElement).getBoundingClientRect();
			elt = evt.target as HTMLElement | SVGElement;
			while (elt && elt != element) {
				if (elt.style.pointerEvents != "none") {
					const bounds = elt.getBoundingClientRect();
					if (
						(evt.clientY > bounds.top && (Math.abs(targetBounds.top - bounds.top) > 1))
						&& (evt.clientY < bounds.bottom && (Math.abs(targetBounds.bottom - bounds.bottom) > 1))
						&& (evt.clientX > bounds.left && (Math.abs(targetBounds.left - bounds.left) > 1))
						&& (evt.clientX < bounds.right && (Math.abs(targetBounds.right - bounds.right) > 1))
					) {
						// There is child that is still under the pointer (and which will raise pointer events), so we should not propagate the event.
						// Note: If the child is sharing the bounds with the target, we consider that the pointer is also leaving the intermediate child.
						evt.stopPropagation();
						return;
					}
				}
				elt = elt.parentElement;
			}

			UIElement.onPointerEventReceived(evt);
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

			let pointerId: number, pointerType: PointerDeviceType, pressure: number;
			let wheelDeltaX: number, wheelDeltaY: number;
			if (evt instanceof WheelEvent) {
				pointerId = (evt as any).mozInputSource ? 0 : 1; // Try to match the mouse pointer ID 0 for FF, 1 for others
				pointerType = PointerDeviceType.Mouse;
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
				pointerType = UIElement.toPointerDeviceType(evt.pointerType);
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
			args.hasRelatedTarget = evt.relatedTarget !== null;
			args.buttons = evt.buttons;
			args.buttonUpdate = evt.button;
			args.deviceType = pointerType;
			args.srcHandle = Number(srcHandle);
			args.timestamp = evt.timeStamp;
			args.pressure = pressure;
			args.wheelDeltaX = wheelDeltaX;
			args.wheelDeltaY = wheelDeltaY;

			return args;
		}

		private static toNativeEvent(eventName: string): NativePointerEvent {
			switch (eventName) {
				case "pointerover":
					return NativePointerEvent.pointerover;
				case "pointerout":
					return NativePointerEvent.pointerout;
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

		private static toPointerDeviceType(type: string): PointerDeviceType {
			switch (type) {
				case "touch":
					return PointerDeviceType.Touch;
				case "pen":
					// Note: As of 2019-11-28, once pen pressed events pressed/move/released are reported as TOUCH on Firefox
					//		 https://bugzilla.mozilla.org/show_bug.cgi?id=1449660
					return PointerDeviceType.Pen;
				case "mouse":
				default:
					return PointerDeviceType.Mouse;
			}
		}
		//#endregion
	}
}
