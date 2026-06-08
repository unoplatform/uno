/**
 * Live Region API Contract
 * Two-tier rate-limited announcements for screen readers.
 */

/**
 * Announces content via the polite live region.
 * Subject to two-tier rate limiting:
 * - 100ms debounce: rapid bursts coalesce to final content
 * - 500ms sustained throttle: caps continuous stream rate
 *
 * @param content - Text content to announce
 */
declare function announcePolite(content: string): void;

/**
 * Announces content via the assertive live region.
 * Subject to two-tier rate limiting:
 * - 100ms debounce: rapid bursts coalesce to final content
 * - 200ms sustained throttle: caps continuous stream rate
 *
 * @param content - Text content to announce (interrupts current speech)
 */
declare function announceAssertive(content: string): void;

/**
 * Updates the live region content for a specific element.
 * Called when AutomationPeer.RaiseAutomationEvent(LiveRegionChanged) fires.
 * The liveSetting determines which region (polite/assertive) is used.
 *
 * @param handle - Handle of the element whose content changed
 * @param content - New text content
 * @param liveSetting - 0=Off, 1=Polite, 2=Assertive
 */
declare function updateLiveRegionContent(
    handle: number,
    content: string,
    liveSetting: number
): void;

/**
 * Clears any pending announcements.
 * Called when accessibility is disabled or page is unloading.
 */
declare function clearPendingAnnouncements(): void;
