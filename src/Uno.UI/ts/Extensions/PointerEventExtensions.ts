interface PointerEvent {
	isOver(this: PointerEvent, element: HTMLElement | SVGElement): boolean;
	isOverDeep(this: PointerEvent, element: HTMLElement | SVGElement): boolean;

	/**
	 * Indicates if the pointer is over the given 'element' and there no other element above it (i.e. given 'element' is top most).
	 */
	isDirectlyOver(this: PointerEvent, element: HTMLElement | SVGElement): boolean;
}

PointerEvent.prototype.isOver = function(element: HTMLElement | SVGElement): boolean {
	const bounds = element.getBoundingClientRect();

	return this.pageX >= bounds.left
		&& this.pageX < bounds.right
		&& this.pageY >= bounds.top
		&& this.pageY < bounds.bottom;
}

PointerEvent.prototype.isOverDeep = function(element: HTMLElement | SVGElement): boolean {
	if (!element) {
		return false;
	} else if (element.style.pointerEvents != "none") {
		return this.isOver(element);
	} else {
		for (let elt of element.children) {
			if (this.isOverDeep(elt as HTMLElement | SVGElement)) {
				return true;
			}
		}
	}
}

PointerEvent.prototype.isDirectlyOver = function(element: HTMLElement | SVGElement): boolean {
	if (!this.isOver(element)) {
		return false;
	}

	for (let elt of document.elementsFromPoint(this.clientX, this.clientY)) {
		if (elt === element) {
			// We found our target element, so the pointer effectively over it.
			return true;
		}

		let htmlElt = elt as HTMLElement | SVGElement;
		if (htmlElt.style.pointerEvents !== "none") {
			// This 'htmlElt' is handling the pointers events, this mean that we can stop the loop.
			// However, if this 'htmlElt' is one of the children of the element it means that the pointer is over element.
			while (htmlElt.parentElement) {
				htmlElt = htmlElt.parentElement;
				if (htmlElt === element) {
					return true;
				}
			}

			// We found an element this is capable to handle the pointers but which is not a child of 'element'
			// (a sibling which is covering the element ... like a PopupRoot).
			// It means that the pointer is not ** DIRECTLY ** over the element.
			return false;
		}
	}

	return false;
}
