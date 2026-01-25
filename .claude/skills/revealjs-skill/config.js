/**
 * Reveal.js Configuration for Swedish Tech Template
 * Optimized settings for 1920x1080 presentations
 */

Reveal.initialize({
    // Navigation
    hash: true,
    history: true,
    controls: true,
    controlsLayout: 'bottom-right',
    progress: true,
    slideNumber: 'c/t',

    // Layout
    center: false,
    width: 1920,
    height: 1080,
    margin: 0.04,
    minScale: 0.2,
    maxScale: 1.0,

    // Transitions
    transition: 'slide',
    transitionSpeed: 'default',
    backgroundTransition: 'fade',

    // Behavior
    keyboard: true,
    overview: true,
    touch: true,
    loop: false,
    fragments: true,
    fragmentInURL: true,
    embedded: false,
    help: true,

    // Media
    autoPlayMedia: null,
    preloadIframes: null,
    autoSlide: 0,
    autoSlideStoppable: true,

    // Display
    display: 'block',
    hideInactiveCursor: true,
    hideCursorTime: 3000,

    // Timing (for speaker view)
    defaultTiming: 120,

    // View distance
    viewDistance: 3
});

// Log when ready
Reveal.on('ready', () => {
    console.log('Swedish Tech Presentation Ready');
});
