interface PointerEvent {
	isOver(this: PointerEvent, element: HTMLElement | SVGElement): boolean;
	isOverDeep(this: PointerEvent, element: HTMLElement | SVGElement): boolean;
}

PointerEvent.prototype.isOver = function(element: HTMLElement | SVGElement): boolean {
	const bounds = element.getBoundingClientRect();

	return this.pageX >= bounds.left &&
		this.pageX < bounds.right &&
		this.pageY >= bounds.top &&
		this.pageY < bounds.bottom;
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
