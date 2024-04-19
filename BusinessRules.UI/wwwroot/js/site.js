window.addEventListener('load', function () {
    document.querySelectorAll('button[data-bs-toggle="tab"]').forEach(function (element) {
        element.addEventListener('click', function (e) {
            sessionStorage.setItem('lastTab', e.target.getAttribute('id'));
        });
    });

    var lastTab = sessionStorage.getItem('lastTab');
    if (lastTab && document.querySelector('button[id="' + lastTab + '"]')) {
        document.querySelector('button[id="' + lastTab + '"]').click();
    }

    restoreAccordionState();

    document.addEventListener("focus", function (event) {
        
        sessionStorage.setItem('lastFocusedElementId', event.target.id);
    }, true);

    UpdateFieldSelector();

    var sidebar = document.getElementById('sidebar');
    if (sidebar) {
        document.addEventListener('click', function (event) {
            var isClickInsideSidebar = sidebar.contains(event.target);
            if (!isClickInsideSidebar) {
                sidebar.classList.remove('show');
            }
        });
    }    
});