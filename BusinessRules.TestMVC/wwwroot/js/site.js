// Function to save scroll position and focused field to localStorage
function saveScrollAndFocus() {
    var main = document.getElementById("main");
    if (localStorage.getItem('scrollPosition') !== 'X') {
        localStorage.setItem('scrollPosition', main.scrollTop);
    }
    const focusedElement = document.activeElement;
    if (focusedElement) {
        localStorage.setItem('focusedField', focusedElement.id);
    }
}

function clearScrollPosition() {
    localStorage.setItem('scrollPosition', 'X');
}

// Function to restore scroll position and focused field from localStorage
function restoreScrollAndFocus() {
    const scrollPosition = parseInt(localStorage.getItem('scrollPosition')) || 0;
    const focusedFieldId = localStorage.getItem('focusedField');

    var main = document.getElementById("main");
    main.scrollTop = scrollPosition;

    if (focusedFieldId) {
        const focusedElement = document.getElementById(focusedFieldId);
        if (focusedElement) {
            focusedElement.focus();
        }
    }
}

// Save scroll position and focused field when the page is unloaded
window.addEventListener('beforeunload', saveScrollAndFocus);

// Restore scroll position and focused field on page load
window.addEventListener('load', restoreScrollAndFocus);