$(document).ready(function() {
    $('.form-box').on('submit', function(e) {
        e.preventDefault();
        
        var email = $('#email').val();
        var password = $('#password').val();
        var rememberMe = $('#rememberMe').is(':checked');

        // Hiển thị loading
        $('#login-text').addClass('d-none');
        $('#loading-icon').removeClass('d-none');
        $('#login-btn').prop('disabled', true);

        $.ajax({
            url: '/Admin/Login',
            type: 'POST',
            data: {
                email: email,
                password: password,
                rememberMe: rememberMe
            },
            success: function(response) {
                if (response.redirectUrl) {
                    window.location.href = response.redirectUrl;
                } else {
                    toastr.error(response.error || 'Email hoặc mật khẩu không đúng');
                }
            },
            error: function() {
                toastr.error('Có lỗi xảy ra, vui lòng thử lại');
            },
            complete: function() {
                // Ẩn loading
                $('#login-text').removeClass('d-none');
                $('#loading-icon').addClass('d-none');
                $('#login-btn').prop('disabled', false);
            }
        });
    });
}); 