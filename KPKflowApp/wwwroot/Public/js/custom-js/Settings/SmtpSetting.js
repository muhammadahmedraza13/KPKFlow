$(document).ready(function () {

    SmtpSettingGet();

    $('#Smtptogglepassword').on('click', function (e) {
        var type = $('#SmtpPassword').attr('type') === 'password' ? 'text' : 'password';
        $('#SmtpPassword').attr('type', type);
        $('#Smtptogglepassword i').toggleClass('bx-show');
        $('#Smtptogglepassword i').toggleClass('bx-hide');
    });

    $('#UpdateBtn').on('click', function () {
        UpdateSmpt();
    });

});

function SmtpSettingGet() {

    new APICALL(GetGlobalURL('Base', 'SmtpSettingGet'), 'GET', '', true).FETCH((result, error) => {

        if (result) {
            if (result.data != null) {
                var SSLvalue = result.data[0].EnableSSL;
                $('#Smtp').val(result.data[0].Smtp);
                $('#SenderEmailID').val(result.data[0].SenderEmailID);
                $('#SmtpPassword').val('');
                $('#SmtpPort').val(result.data[0].SmtpPort);
                $('#DisplayName').val(result.data[0].DisplayName);

                if (SSLvalue == true) {
                    $('#EnableSSL').prop('checked', true);
                }
                else {
                    $('#EnableSSL').val('').prop('checked', false);
                }

            }
        }
        if (error) {

            Swal.fire({
                icon: 'error',
                title: 'Error...',
                text: error.data.responseText,
                footer: ''
            });
        }
    });
}

function UpdateSmpt() {
    if (!$('#smtpform').valid()) {
        return false;
    }
    var Smtp = $("#Smtp").val();
    var SenderEmailID = $("#SenderEmailID").val();

    var SmtpPassword = $("#SmtpPassword").val();
    var SmtpPort = $("#SmtpPort").val();
    var DisplayName = $("#DisplayName").val();
    var EnableSSL = $("#EnableSSL").prop("checked");

    var model = new Object();
    model.Smtp = Smtp;
    model.SenderEmailID = SenderEmailID;
    model.SmtpPassword = SmtpPassword;
    model.SmtpPort = SmtpPort;
    model.DisplayName = DisplayName;
    model.EnableSSL = EnableSSL;
    model.UserID = window.UserID;

    Swal.fire({
        title: 'Do you want to save the changes?',
        showDenyButton: true,
        showCancelButton: false,
        confirmButtonText: 'Ok',
        denyButtonText: 'Cancel',
    }).then((result) => {
        if (result.isConfirmed) {

            new APICALL(GetGlobalURL('Base', 'SmtpSettingUpdate'), 'POST', JSON.stringify(model), true).FETCH((result, error) => {

                if (result) {

                    Swal.fire({
                        icon: 'success',
                        title: 'Success...',
                        text: 'SMTP setting Saved Successfully!',
                        footer: ''
                    });
                }
                if (error) {

                    Swal.fire({
                        icon: 'error',
                        title: 'Error...',
                        text: error.data.responseText,
                        footer: ''
                    });
                }
            });
        }
    });
}