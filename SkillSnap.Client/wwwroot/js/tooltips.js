// Initialize all Bootstrap tooltips on the page
function initializeTooltips() {
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => 
        new bootstrap.Tooltip(tooltipTriggerEl, {
            trigger: 'hover focus',
            delay: { show: 500, hide: 100 }
        })
    );
}

// Run on page load
window.addEventListener('DOMContentLoaded', initializeTooltips);

// Export for Blazor to reinitialize after component updates
window.reinitializeTooltips = function() {
    // Dispose existing tooltips
    const tooltips = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    tooltips.forEach(el => {
        const tooltip = bootstrap.Tooltip.getInstance(el);
        if (tooltip) tooltip.dispose();
    });
    
    // Reinitialize
    initializeTooltips();
};
