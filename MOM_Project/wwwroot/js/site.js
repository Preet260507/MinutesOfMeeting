// Sidebar toggle
$(document).ready(function () {
    $("#menu-toggle").click(function (e) {
        e.preventDefault();
        $("#wrapper").toggleClass("toggled");
    });
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
