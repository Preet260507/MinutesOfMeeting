// Sidebar toggle
$(document).ready(function () {
    $("#menu-toggle").click(function (e) {
        e.preventDefault();
        $("#wrapper").toggleClass("toggled");
    });
});

// Double-click protection for forms
$(document).on('submit', 'form', function () {
    var $btn = $(this).find('button[type="submit"]');
    if ($btn.prop('disabled')) return false; // Already submitted
    $btn.prop('disabled', true);
    $btn.html('<span class="spinner-border spinner-border-sm me-2"></span>Processing...');
});

// SweetAlert notification handler
function showNotification(msgType, message) {
    if (msgType && message) {
        if (msgType === 'success') {
            Swal.fire({
                icon: 'success',
                title: 'Success!',
                text: message,
                timer: 3000,
                showConfirmButton: false
            });
        } else {
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: message
            });
        }
    }
}
