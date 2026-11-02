/* =====================================================================
   ecommercesite shared client-side helpers
   ===================================================================== */

(function () {
    'use strict';

    // ---- Auto-dismiss Bootstrap toasts after 4 seconds -----------------
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('.toast.show').forEach(function (toast) {
            setTimeout(function () {
                var instance = bootstrap.Toast.getInstance(toast)
                    || new bootstrap.Toast(toast);
                instance.hide();
            }, 4000);
        });
    });

    // ---- Confirm any element with [data-confirm="..."] -----------------
    document.addEventListener('click', function (e) {
        var target = e.target.closest('[data-confirm]');
        if (!target) return;

        var message = target.getAttribute('data-confirm') || 'Are you sure?';
        if (!window.confirm(message)) {
            e.preventDefault();
            e.stopPropagation();
        }
    });

    // ---- Auto-trim text inputs on blur ---------------------------------
    document.addEventListener('blur', function (e) {
        if (e.target && e.target.matches('input[type="text"], input[type="email"], input[type="search"]')) {
            e.target.value = e.target.value.trim();
        }
    }, true);
})();