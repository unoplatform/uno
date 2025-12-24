/**
 * Smooth Scroll Service
 * Provides smooth scrolling functionality for TOC navigation
 * and prevents flickering when selecting new documentation pages
 */

// Configuration
const SCROLL_CONFIG = {
  duration: 250,
  easing: 'cubic-bezier(0.4, 0, 0.2, 1)' // Material Design easing
};

// Debounce helper
function debounce(func, wait) {
  let timeout;
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
}

/**
 * Smoothly scroll an element to a target position using native smooth behavior
 * @param {HTMLElement} element - The element to scroll
 * @param {number} targetScroll - The target scroll position
 */
function smoothScrollTo(element, targetScroll) {
  if (!element) return;

  // Use native smooth scrolling with immediate fallback
  try {
    element.scrollTo({
      top: targetScroll,
      behavior: 'smooth'
    });
  } catch (e) {
    // Fallback for older browsers
    element.scrollTop = targetScroll;
  }
}

/**
 * Prevent reflow by batching DOM operations
 */
let rafId = null;
let pendingUpdates = [];

function batchUpdate(callback) {
  pendingUpdates.push(callback);
  
  if (!rafId) {
    rafId = requestAnimationFrame(() => {
      const updates = pendingUpdates.slice();
      pendingUpdates = [];
      rafId = null;
      
      // Execute all batched updates
      updates.forEach(cb => cb());
    });
  }
}

/**
 * Override the default scrollTop setter to use smooth scrolling
 */
function patchSidetocScrolling() {
  const sidetoc = document.querySelector('.sidetoc');
  if (!sidetoc) return;

  // Store original scrollTop setter
  const originalScrollTop = Object.getOwnPropertyDescriptor(Element.prototype, 'scrollTop');
  
  if (originalScrollTop && originalScrollTop.set) {
    Object.defineProperty(sidetoc, 'scrollTop', {
      get: function() {
        return originalScrollTop.get.call(this);
      },
      set: function(value) {
        // Use smooth scrolling instead of instant
        smoothScrollTo(this, value);
      },
      configurable: true
    });
  }
}

/**
 * Optimize active state changes to prevent flickering
 */
function optimizeActiveStateChanges() {
  // Use MutationObserver to batch class changes
  const observer = new MutationObserver(debounce((mutations) => {
    batchUpdate(() => {
      // Process mutations in batches
      mutations.forEach(mutation => {
        if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
          const target = mutation.target;
          // Force GPU acceleration for smooth transitions
          if (target.classList.contains('active') || target.classList.contains('expanded')) {
            target.style.transform = 'translateZ(0)';
          }
        }
      });
    });
  }, 16)); // Debounce to next frame

  const tocNav = document.querySelector('#toc');
  if (tocNav) {
    observer.observe(tocNav, {
      attributes: true,
      attributeFilter: ['class'],
      subtree: true
    });
  }
}

/**
 * Initialize smooth scroll immediately
 */
(function initImmediately() {
  // Wait for DOM to be ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  function init() {
    // Use a slight delay to ensure other scripts have loaded
    setTimeout(() => {
      patchSidetocScrolling();
      optimizeActiveStateChanges();
    }, 50);
  }
})();

// Re-init on AJAX updates
if (typeof $ !== 'undefined') {
  $(document).on('wordpressMenuHasLoaded', function() {
    setTimeout(() => {
      patchSidetocScrolling();
      optimizeActiveStateChanges();
    }, 50);
  });
}

// Export for debugging
window.SmoothScroll = {
  smoothScrollTo,
  patchSidetocScrolling,
  optimizeActiveStateChanges
};
