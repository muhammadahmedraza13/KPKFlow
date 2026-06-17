let timerInterval;
let REGISTRATION_TOKEN = "";

$(document).ready(function () {

    const urlParams = new URLSearchParams(window.location.search);
    REGISTRATION_TOKEN = urlParams.get('token');

    REGISTRATION_TOKEN
        ? $('#InvitationToken').val(REGISTRATION_TOKEN)
        : Swal.fire({ icon: 'error', title: 'Security Alert', text: 'Token missing!', confirmButtonColor: '#4f46e5' });

    setTimeout(() => {
        var el = $('input[name="User.UserEmail"]');
        if (el.val()) el.prop('readonly', true).valid();
    }, 200);

    $.validator.addMethod("pkMobile", function (value, element) {
        return this.optional(element) || /^03\d{9}$/.test(value);
    }, "Format: 03XXXXXXXXX (11 digits)");

    $.validator.addMethod("pkNTN", function (value, element) {
        return this.optional(element) || /^\d{7}-\d{1}$/.test(value);
    }, "NTN Format: 0000000-0");

    $.validator.addMethod("pkSTRN", function (value, element) {
        return this.optional(element) || /^\d{2}-\d{2}-\d{4}-\d{3}-\d{2}$/.test(value);
    }, "STRN Format: 00-00-0000-000-00");

    $.validator.addMethod("corporateEmail", function (value, element) {
        return this.optional(element) || /^[^@]+@[^@]+\.[a-zA-Z]{2,6}$/.test(value);
    }, "Please enter a valid official email address");

    $.validator.addMethod("extension", function (value, element, param) {
        param = typeof param === "string" ? param.replace(/,/g, '|') : "png|jpe?g|pdf|docx";
        return this.optional(element) || value.match(new RegExp(".(" + param + ")$", "i"));
    }, "Invalid file format.");

    var validator = $("#vendorForm").validate({
        ignore: ":hidden:not(.select2-multiple, .select2-single, select)",
        onkeyup: function (element) { $(element).valid(); },
        rules: {
            "BusinessName": { required: true, minlength: 3, maxlength: 150 },
            "NTN": { required: true, pkNTN: true },
            "STRN": { required: true, pkSTRN: true },
            "Address": { required: true, minlength: 10 },
            "City": { required: true },
            "PostalCode": { required: true, digits: true, minlength: 5, maxlength: 5 },
            "SubCategories[]": { required: true, minlength: 1 },
            "TaxDocument": { required: true, extension: "pdf,png,jpg,jpeg" },
            "CompanyProfile": { required: true, extension: "pdf,docx" },
            "AdditionalDocs": { required: false, extension: "pdf,png,jpg,jpeg,docx" },
            "User.UserEmail": { required: true, corporateEmail: true },
            "User.MobileNumber": { required: true, pkMobile: true },
            "terms": { required: true }
        },
        messages: {
            "BusinessName": "Enter your registered business/company name",
            "SubCategories[]": "Please select at least one sub-category",
            "TaxDocument": {
                required: "Please upload your Tax/STRN certificate",
                extension: "Only PDF, PNG, and JPG files are allowed"
            }
        },
        errorElement: 'span',
        errorPlacement: function (error, element) {
            error.addClass('invalid-feedback');
            if (element.attr("type") === "checkbox") {
                error.insertAfter(element.closest('.form-check'));
            }
            else if (element.hasClass("select2-multiple") || element.hasClass("select2-single") || element.next().hasClass("select2-container")) {
                error.insertAfter(element.next('.select2-container'));
            }
            else {
                element.closest('[class^="col-"]').append(error);
            }
        },
        highlight: function (element) {
            $(element).addClass('is-invalid').removeClass('is-valid');
            $(element).next('.select2-container').find('.select2-selection').addClass('border-danger');
        },
        unhighlight: function (element) {
            $(element).removeClass('is-invalid').addClass('is-valid');
            $(element).next('.select2-container').find('.select2-selection').removeClass('border-danger').addClass('border-success');
        }
    });

    $('.select2-single, .select2-multiple').each(function () {
        $(this).select2({
            width: '100%',
            placeholder: "Select an option",
            allowClear: true
        });
    });

    $('.select2-multiple, .select2-single').on('change', function () {
        $(this).valid();
    });

   

    $('#EmailOTP').on('input', function () {
        this.value = this.value.replace(/[^0-9]/g, '');
    });

  //  GetCategories();

    //$('#categorySelect').on('change', function () {
    //    if (this.value) {
    //        GetSubCategories(this.value);
    //    }
    //    $(this).valid(); 
    //});

    $('#vendorForm').on('submit', function (e) {
        e.preventDefault();
        RegisterVendor();
    });

    $('#VerifyAndRegister').on('click', function (e) {
        e.preventDefault();
        VerifyAndRegister();
    });

    $('#ResendOTP').on('click', function (e) {
        e.preventDefault();
        ResendOTP();
    });
   
});
$(document).on("input", "#NTN", function () {
    let val = $(this).val().replace(/\D/g, ""); // only digits

    if (val.length > 7) {
        val = val.substring(0, 7) + "-" + val.substring(7, 8);
    }

    $(this).val(val);
});
$(document).on("input", "#STRN", function () {
    let val = $(this).val().replace(/\D/g, "");

    let formatted = "";

    if (val.length > 0) formatted += val.substring(0, 2);
    if (val.length > 2) formatted += "-" + val.substring(2, 4);
    if (val.length > 4) formatted += "-" + val.substring(4, 8);
    if (val.length > 8) formatted += "-" + val.substring(8, 11);
    if (val.length > 11) formatted += "-" + val.substring(11, 13);

    $(this).val(formatted);
});
function GetCategories() {
    const $select = $('#categorySelect');

    new APICALL(GetGlobalURL('VendorRegister', 'GetCategories'), 'GET', '', true)
        .FETCH((result) => {
            $select.empty().append('<option value="" disabled selected>Select Category</option>');

            if (result.data && result.data.data) {
                result.data.data.forEach(item => {
                    $select.append(new Option(item.Text, item.Id));
                });
            }
        });
}
function GetSubCategories(CategoryId) {
    const $select = $('#subCategorySelect');

    new APICALL(GetGlobalURL('VendorRegister', 'GetSubCategories') + '?CategoryId=' + CategoryId, 'GET', '', false)
        .FETCH((result, error) => {
            $select.empty(); 

            if (result && result.data && result.data.data) {
                result.data.data.forEach(item => {
                    $select.append(new Option(item.Text, item.Id));
                });
            }
            $select.trigger('change');

            if (error) {
                Swal.fire({
                    icon: 'error',
                    title: 'Error...',
                    text: error.data ? error.data.responseText : 'Something went wrong'
                });
            }
        });
}


function StartOTPTimer(durationInSeconds) {
    clearInterval(timerInterval);
    let timer = durationInSeconds;
    const display = $('#otpTimer');
    const resendBox = $('#resendContainer');
    const verifyBtn = $('#VerifyAndRegister');

    resendBox.hide();
    verifyBtn.prop('disabled', false);
    display.removeClass('text-danger animate__animated animate__pulse animate__infinite');

    timerInterval = setInterval(function () {
        let minutes = parseInt(timer / 60, 10);
        let seconds = parseInt(timer % 60, 10);

        minutes = minutes < 10 ? "0" + minutes : minutes;
        seconds = seconds < 10 ? "0" + seconds : seconds;

        display.text(minutes + ":" + seconds);

        if (timer <= 10 && timer > 0) {
            display.addClass('text-danger animate__animated animate__pulse animate__infinite');
        }

        if (--timer < 0) {
            clearInterval(timerInterval);
            display.text("00:00");
            display.removeClass('animate__pulse');
            resendBox.fadeIn();
            verifyBtn.prop('disabled', true); 
        }
    }, 1000);
}

async function RegisterVendor() {
    const $form = $('#vendorForm');
    const $btn = $('#Submit');

    if (!$('#terms').is(':checked')) {
        Swal.fire('Terms', 'Please accept the terms and conditions.', 'warning');
        return;
    }

    if ($('#TaxDocument')[0].files.length === 0) {
        Swal.fire('Missing File', 'Please upload the Tax Document (NTN).', 'warning');
        return;
    }

    let formData = new FormData($form[0]);
    $btn.prop('disabled', true).html('<i class="bx bx-loader-alt bx-spin"></i> Processing...');

    $.ajax({
        url: GetGlobalURL('VendorRegister', 'InitiateRegistration'),
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (res) {
            if (res.status === "success") {
                REGISTRATION_TOKEN = res.token;
                ShowOTPForm();
                Swal.fire('OTP Sent!', 'Verification codes sent to your mobile & email.', 'success');
            } else {
                Swal.fire('Error', res.message, 'error');
                $btn.prop('disabled', false).html('Submit <i class="fa-solid fa-paper-plane ms-2"></i>');
            }
        },
        error: function () {
            Swal.fire('System Error', 'Could not connect to server.', 'error');
            $btn.prop('disabled', false).html('Submit');
        }
    });
}
function ShowOTPForm() {
    $('#vendorForm').slideUp(400);
    $('#otpSection').delay(400).fadeIn(400);

    const $p2 = $('#step-phase2');
    $p2.removeClass('step-active');
    $p2.find('.step-icon').removeClass('bg-primary').addClass('bg-success').html('<i class="fa-solid fa-check"></i>');
    $p2.find('.text-primary').removeClass('text-primary').addClass('text-success');

    const $p3 = $('#step-phase3');
    $p3.addClass('step-active');
    $p3.find('.step-icon').removeClass('bg-secondary').addClass('bg-primary');
    $p3.find('.phase-label').removeClass('text-muted').addClass('text-primary');

    StartOTPTimer(120);
    setTimeout(() => $('#MobileOTP').focus(), 600);
}

async function ResendOTP() {
    if (!REGISTRATION_TOKEN) return;

    const $resendBtn = $('#ResendOTP');
    const $loader = $('<i class="fas fa-spinner fa-spin me-2"></i>');

    $resendBtn.prop('disabled', true).prepend($loader);

    $.ajax({
        url: GetGlobalURL('VendorRegister', 'ResendOTP'),
        type: 'POST',
        data: { RegistrationToken: REGISTRATION_TOKEN },
        success: function (res) {
            if (res.status === "success") {
                Swal.fire('OTP Resent', 'New codes have been sent.', 'success');
                $('#MobileOTP').val('').removeClass('is-invalid');
                $('#EmailOTP').val('').removeClass('is-invalid');
                StartOTPTimer(120);
            } else {
                Swal.fire('Error', res.message, 'error');
            }
        },
        complete: function () {
            $resendBtn.prop('disabled', false);
            $resendBtn.find('.fa-spinner').remove();
        }
    });
}
function VerifyAndRegister() {
    const emailVal = $('#EmailOTP').val().trim();
    const mobileVal = '1234';
    const $btn = $('#VerifyAndRegister');

    $('.otp-input-modern').removeClass('is-invalid');

    if (mobileVal.length !== 4) {
        $('#MobileOTP').addClass('is-invalid').focus();
        Swal.fire('Invalid Mobile OTP', 'Please enter 4-digit code.', 'warning');
        return;
    }
    if (emailVal.length !== 6) {
        $('#EmailOTP').addClass('is-invalid').focus();
        Swal.fire('Invalid Email OTP', 'Please enter 6-digit code.', 'warning');
        return;
    }

    $btn.prop('disabled', true).html('<span><i class="bx bx-loader-alt bx-spin me-2"></i>Verifying...</span>');

    $.ajax({
        url: GetGlobalURL('VendorRegister', 'FinalRegister'),
        type: 'POST',
        data: {
            RegistrationToken: REGISTRATION_TOKEN,
            EmailOTP: emailVal,
            MobileOTP: mobileVal
        },
        success: function (res) {
            if (res.status === "success") {
                clearInterval(timerInterval);
                Swal.fire({
                    title: 'Verified!',
                    text: 'Your application has been submitted successfully.',
                    icon: 'success',
                    timer: 2000,
                    showConfirmButton: false
                }).then(() => ShowFinalSuccess());
            } else {
                Swal.fire('Error', res.message, 'error');
                if (res.message && res.message.includes("Maximum attempts reached")) {
                    ForceStopTimerAndShowResend();
                } else {
                    $btn.prop('disabled', false).html('<span>Verify & Submit</span><i class="fas fa-arrow-right ms-2 small"></i>');
                }
            }
        },
        error: function () {
            Swal.fire('System Error', 'Connection failed.', 'error');
            $btn.prop('disabled', false).html('<span>Verify & Submit</span><i class="fas fa-arrow-right ms-2 small"></i>');
        }
    });
}
function ShowFinalSuccess() {
    $('#otpSection').fadeOut(400, function () {
        const $p3 = $('#step-phase3');

        $p3.removeClass('step-active');

        $p3.find('.step-icon')
            .removeClass('bg-primary bg-secondary') 
            .addClass('bg-success')
            .html('<i class="fa-solid fa-check"></i>');

        $p3.find('.phase-label')
            .removeClass('text-primary text-muted')
            .addClass('text-success');

        const successHtml = `
            <div class="text-center py-5 animate__animated animate__zoomIn">
                <div class="icon-circle-custom bg-light-success mb-4" style="width:100px; height:100px; margin: 0 auto; display: flex; align-items: center; justify-content: center; border-radius: 50%;">
                    <i class="fas fa-check-circle fa-4x text-success"></i>
                </div>
                <h2 class="fw-bold text-dark">Registration Received!</h2>
                <p class="text-muted fs-6 px-lg-5">Thank you for applying. Our Bid team will review your tax documents and credentials. You will receive an update via email.</p>
            </div>
        `;

        $(this).parent().append(successHtml);
    });
}
function ForceStopTimerAndShowResend() {
    clearInterval(timerInterval);
    $('#otpTimer').text("00:00").addClass('text-danger');
    $('#resendContainer').fadeIn();
    $('#VerifyAndRegister').prop('disabled', true).html('<span>Verify & Submit</span><i class="fas fa-arrow-right ms-2 small"></i>');
}