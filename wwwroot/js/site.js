// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(function () {
    var fabBtn = document.getElementById('feedbackFabBtn');
    var popup = document.getElementById('feedbackPopup');
    if (!fabBtn || !popup) {
        return;
    }

    var fabIcon = fabBtn.querySelector('.feedback-fab-icon');
    var closeBtn = document.getElementById('feedbackPopupClose');
    var firstField = popup.querySelector('input, textarea');

    function handleOutsideClick(event) {
        if (popup.contains(event.target) || fabBtn.contains(event.target)) return;
        closePopup();
    }

    function handleKeydown(event) {
        if (event.key === 'Escape') {
            closePopup();
        }
    }

    function openPopup() {
        popup.classList.add('is-open');
        popup.removeAttribute('inert');
        popup.setAttribute('aria-hidden', 'false');
        fabBtn.setAttribute('aria-expanded', 'true');
        if (fabIcon) fabIcon.textContent = '✕';
        if (firstField) firstField.focus();
        document.addEventListener('click', handleOutsideClick);
        document.addEventListener('keydown', handleKeydown);
    }

    function closePopup() {
        if (popup.contains(document.activeElement)) {
            fabBtn.focus();
        }
        popup.classList.remove('is-open');
        popup.setAttribute('inert', '');
        popup.setAttribute('aria-hidden', 'true');
        fabBtn.setAttribute('aria-expanded', 'false');
        if (fabIcon) fabIcon.textContent = '💬';
        document.removeEventListener('click', handleOutsideClick);
        document.removeEventListener('keydown', handleKeydown);
    }

    fabBtn.addEventListener('click', function () {
        if (popup.classList.contains('is-open')) {
            closePopup();
        } else {
            openPopup();
        }
    });

    if (closeBtn) {
        closeBtn.addEventListener('click', closePopup);
    }

    if (popup.dataset.feedbackOpen === 'true') {
        openPopup();
    }
})();
