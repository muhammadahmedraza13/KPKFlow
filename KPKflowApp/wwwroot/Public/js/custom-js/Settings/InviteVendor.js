let emails = [];

$(document).ready(function () {
    const emailInput = document.getElementById('email-input');
    const tagsContainer = document.getElementById('email-tags-container');

    const validateEmail = (email) => /^\S+@\S+\.\S+$/.test(email);

    function addEmail() {
        const val = emailInput.value.trim().toLowerCase();

        if (!val) return Swal.fire({ icon: 'warning', title: 'Empty Email', text: 'Please enter an email address first', timer: 2000, showConfirmButton: false });

        if (!validateEmail(val)) {
            emailInput.classList.add('is-invalid');
            return Swal.fire({ icon: 'error', title: 'Invalid Format' });
        }

        if (emails.includes(val)) return Swal.fire({ icon: 'info', title: 'Duplicate', text: 'This email has already been added', timer: 2000, showConfirmButton: false });

        emails.push(val);
        emailInput.classList.remove('is-invalid');

        const tag = $(`<span class="badge bg-primary d-flex align-items-center gap-2 p-2 px-3 rounded-pill animate__animated animate__fadeIn" data-email="${val}" style="font-size:14px">
        ${val} <i class="fa-solid fa-circle-xmark remove-email-btn" style="cursor:pointer"></i></span>`);

        $(tagsContainer).append(tag);
        emailInput.value = '';
    }

    $(tagsContainer).on('click', '.remove-email-btn', function () {
        const parent = $(this).closest('span');
        emails = emails.filter(e => e !== parent.attr('data-email'));
        parent.addClass('animate__fadeOut');
        setTimeout(() => parent.remove(), 300);
    });

    document.getElementById('addEmailBtn').addEventListener('click', addEmail);
    emailInput.addEventListener('keypress', (e) => e.key === 'Enter' && (e.preventDefault(), addEmail()));

    $('#sendInvitation').on('click', function () {
        SendInvitation();
    });

    GetVendors();

    $(document).on('click', '.viewVendor', function () {
        let id = $(this).data('id');
    });

    $(document).on('click', '.btn-download-file', function (e) {
        e.preventDefault();
        const fileName = $(this).attr('data-filename');
        DownloadVendorFile(fileName);
    });


})


async function SendInvitation() {
    const $form = $('#inviteVendorform');

    if (!$form.valid()) {
        Swal.fire({
            icon: 'error',
            title: 'Form Invalid',
            text: 'Please fill all required fields correctly.'
        });
        return false;
    }

    if (!emails || emails.length === 0) {
        Swal.fire({
            icon: 'warning',
            title: 'No Recipients',
            text: 'Please add at least one email address before sending.'
        });
        return false;
    }

    const confirmation = await Swal.fire({
        title: 'Confirm Submission',
        html: `You are about to send invitations to <b>${emails.length}</b> recipient(s).<br>Do you want to proceed?`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Yes, Send!',
        cancelButtonText: 'Cancel',
        reverseButtons: true,
        allowOutsideClick: false
    });

    if (!confirmation.isConfirmed) return;

    Swal.fire({
        title: 'Processing...',
        text: 'Sending invitations, please wait.',
        allowOutsideClick: false,
        didOpen: () => { Swal.showLoading(); }
    });

    var formData = $form.serializeArray();
    emails.forEach((email) => {
        formData.push({ name: 'Emails', value: email });
    });

    new APICALL(GetGlobalURL('Base', 'SendInvitationtoVendors'), 'POST', $.param(formData), true, false, 'application/x-www-form-urlencoded')
        .FETCH((res, error) => {
            Swal.close(); 
            if (res && res.data) {
                const apiData = res.data;
                const d = apiData.details;

                if (apiData.success) {
                    let dynamicIcon = 'success';
                    let alertTitle = 'Invitations Processed';

                    if (d.Sent.length === 0) {
                        dynamicIcon = 'info'; 
                        alertTitle = 'Process Information';
                    }

                    const iconSuccess = `<svg style="width:16px;height:16px;vertical-align:middle;margin-right:8px" viewBox="0 0 24 24"><path fill="#2ed573" d="M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z"/></svg>`;
                    const iconWarning = `<svg style="width:16px;height:16px;vertical-align:middle;margin-right:8px" viewBox="0 0 24 24"><path fill="#ffa502" d="M12,2L1,21H23L12,2M12,6L19.53,19H4.47L12,6M11,10V14H13V10H11M11,16V18H13V16H11Z"/></svg>`;
                    const iconError = `<svg style="width:16px;height:16px;vertical-align:middle;margin-right:8px" viewBox="0 0 24 24"><path fill="#ff4757" d="M11,15H13V17H11V15M11,7H13V13H11V7M12,2C6.47,2 2,6.47 2,12C2,17.53 6.47,22 12,22C17.53,22 22,17.53 22,12C22,6.47 17.53,2 12,2Z"/></svg>`;
                    const iconInfo = `<svg style="width:16px;height:16px;vertical-align:middle;margin-right:8px" viewBox="0 0 24 24"><path fill="#57606f" d="M11,9H13V7H11V9M12,20C7.59,20 4,16.41 4,12C4,7.59 7.59,4 12,4C16.41,4 20,7.59 20,12C20,16.41 16.41,20 12,20M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M11,17H13V11H11V17Z"/></svg>`;

                    let statusHtml = `
                    <div style="text-align: left; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;">
                        <p style="font-size: 14px; color: #57606f; margin-bottom: 18px;">
                            The invitation process has finished. Summary:
                        </p>

                        <div style="background: #ffffff; border: 1px solid #e1e4e8; border-radius: 8px; overflow: hidden;">
                            <table style="width: 100%; border-collapse: collapse; font-size: 14px;">
                                ${d.Sent.length > 0 ? `
                                <tr style="background: #fdfdfd;">
                                    <td style="padding: 12px; color: #2e7d32; border-bottom: 1px solid #f0f0f0;">
                                        ${iconSuccess} <strong>Successfully Sent</strong>
                                    </td>
                                    <td style="text-align: right; padding: 12px; border-bottom: 1px solid #f0f0f0;">
                                        <span style="background: #e8f5e9; color: #2e7d32; padding: 2px 10px; border-radius: 12px; font-weight: bold;">${d.Sent.length}</span>
                                    </td>
                                </tr>` : ''}

                                ${d.AlreadyRegistered.length > 0 ? `
                                <tr>
                                    <td style="padding: 12px 12px 4px 12px; color: #ed6c02;">
                                        ${iconWarning} <strong>Already Registered</strong>
                                    </td>
                                    <td style="text-align: right; padding: 12px 12px 4px 12px;">
                                        <span style="background: #fff3e0; color: #ed6c02; padding: 2px 10px; border-radius: 12px; font-weight: bold;">${d.AlreadyRegistered.length}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td colspan="2" style="font-size: 12px; color: #888; padding: 0 12px 12px 38px; line-height: 1.4;">
                                        ${d.AlreadyRegistered.join(', ')}
                                    </td>
                                </tr>` : ''}

                                ${d.InvalidFormat.length > 0 ? `
                                <tr style="border-top: 1px solid #f0f0f0;">
                                    <td style="padding: 12px 12px 4px 12px; color: #d32f2f;">
                                        ${iconError} <strong>Invalid Format</strong>
                                    </td>
                                    <td style="text-align: right; padding: 12px 12px 4px 12px;">
                                        <span style="background: #ffebee; color: #d32f2f; padding: 2px 10px; border-radius: 12px; font-weight: bold;">${d.InvalidFormat.length}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td colspan="2" style="font-size: 12px; color: #888; padding: 0 12px 12px 38px;">
                                        ${d.InvalidFormat.join(', ')}
                                    </td>
                                </tr>` : ''}

                                ${d.Failed.length > 0 ? `
                                <tr style="border-top: 1px solid #f0f0f0;">
                                    <td style="padding: 12px; color: #57606f;">
                                        ${iconInfo} <strong>Delivery Failed</strong>
                                    </td>
                                    <td style="text-align: right; padding: 12px;">
                                        <span style="background: #f1f2f6; color: #57606f; padding: 2px 10px; border-radius: 12px; font-weight: bold;">${d.Failed.length}</span>
                                    </td>
                                </tr>` : ''}
                            </table>
                        </div>
                    </div>`;

                    Swal.fire({
                        title: `<span style="font-weight: 600; color: #2f3542;">${alertTitle}</span>`,
                        icon: dynamicIcon, 
                        html: statusHtml,
                        confirmButtonText: 'Done',
                        confirmButtonColor: '#007bff',
                        width: '460px',
                        padding: '1.5em',
                        customClass: {
                            confirmButton: 'btn-primary-style'
                        }
                    }).then(() => {
                        ClearInvitationForm();
                    });

                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'System Error',
                        text: apiData.message || 'Service unavailable.',
                        confirmButtonColor: '#007bff'
                    });
                }
            }
            else {
                Swal.fire('Error', 'Server communication failed.', 'error');
            }
        });
}
function ClearInvitationForm() {
    emails = [];

    $('#email-input').val('').removeClass('is-invalid is-valid');
    $('#email-tags-container').empty();
    const $form = $('#inviteVendorform');
    $form.trigger("reset");

    if ($.fn.validate) {
        $form.validate().resetForm();
        $form.find(".error").removeClass("error");
    }

    if (typeof DoEmptyFields === "function") {
        DoEmptyFields();
    }
}


function GetVendors() {
    ShowLoader('VendorMasterDiv');
    UTILITY.CheckSession((isLoggedIn) => {
        if (!isLoggedIn) return;

        new APICALL(GetGlobalURL('Base', 'GetVendors'), 'GET', '', true).FETCH((result, error) => {
            if (result?.data?.data) {
                if ($.fn.DataTable.isDataTable('#Vendor-master')) {
                    $('#Vendor-master').DataTable().destroy();
                }

                const $tbody = $('#Vendor-master tbody').empty();

                $.each(result.data.data, function (i, row) {
                    let statusBadge = '';
                    let actionButtons = '-'; 

                    if (row.Status === 'Approved') {
                        statusBadge = '<span class="st-badge st-approved">Approved</span>';
                    } else if (row.Status === 'Rejected') {
                        statusBadge = '<span class="st-badge st-rejected">Rejected</span>';
                    } else {
                        statusBadge = '<span class="st-badge st-pending">Pending</span>';

                        actionButtons = `
                            <div class="d-flex gap-2">
                                <button class="btn btn-sm btn-outline-success" id="btn-approve-${i}" title="Approve">
                                    <i class="fas fa-check"></i>
                                </button>
                                <button class="btn btn-sm btn-outline-danger" id="btn-reject-${i}" title="Reject">
                                    <i class="fas fa-times"></i>
                                </button>
                            </div>`;
                    }

                    const rowHtml = `
                        <tr>
                            <td class="fw-bold">${row.BusinessName || '-'}</td>
                            <td class="fw-bold">${row.UserName || '-'}</td>
                            <td class="text-muted">${row.UserEmail || '-'}</td>
                            <td class="fw-semibold">${row.LoginName || '-'}</td>
                            <td>${row.MobileNumber || '-'}</td>
                            <td>${row.City || '-'}</td>
                            <td>${row.CategoryName || '-'}</td>
                            <td>${statusBadge}</td>
                            <td>
                                <button class="btn-view-profile" id="btn-view-${i}">
                                    View Full Details
                                </button>
                            </td>
                            <td>${actionButtons}</td>
                        </tr>`;

                    const $row = $(rowHtml);

                    $row.find(`#btn-view-${i}`).on('click', () => ViewVendorRow(row));

                    if (row.Status === 'Pending') {
                        $row.find(`#btn-approve-${i}`).on('click', () => ApproveVendor(row.VendorID));
                        $row.find(`#btn-reject-${i}`).on('click', () => RejectVendor(row.VendorID));
                    }

                    $tbody.append($row);
                });

                const table = $('#Vendor-master').DataTable({
                    "responsive": true,
                    "dom": 'rtip',
                    "pageLength": 10
                });

                $('#tableSearch').keyup(function () {
                    table.search($(this).val()).draw();
                });
            }
            HideLoader('VendorMasterDiv');
        });
    });
}
function ViewVendorRow(data) {
    let content = `
        <div class="row g-3">
            <div class="col-12 mb-2">
                <div class="d-flex align-items-center">
                    <div style="width:3px; height:20px; background:#6366f1; margin-right:10px; border-radius:10px;"></div>
                    <h6 class="fw-bold m-0" style="color: #334155;">Business Identification</h6>
                </div>
            </div>
            
            <div class="col-md-6">
                <div class="info-box">
                    <p class="info-label text-muted mb-1 small">Full Business Name</p>
                    <p class="info-data fw-bold">${data.BusinessName || 'Not Provided'}</p>
                </div>
            </div>
            <div class="col-md-6 d-none">
                <div class="info-box">
                    <p class="info-label text-muted mb-1 small">Category / Sub-Category</p>
                    <p class="info-data">${data.CategoryName} <span class="text-muted mx-1">/</span> ${data.SubCategoryName || '-'}</p>
                </div>
            </div>
            <div class="col-md-6"><div class="info-box"><p class="info-label text-muted mb-1 small">NTN Number</p><p class="info-data">${data.NTN || '-'}</p></div></div>
            <div class="col-md-6"><div class="info-box"><p class="info-label text-muted mb-1 small">STRN</p><p class="info-data">${data.STRN || '-'}</p></div></div>
            <div class="col-md-6"><div class="info-box"><p class="info-label text-muted mb-1 small">Postal Code</p><p class="info-data">${data.PostalCode || '-'}</p></div></div>

            <div class="col-12 mt-4 mb-2">
                <div class="d-flex align-items-center">
                    <div style="width:3px; height:20px; background:#6366f1; margin-right:10px; border-radius:10px;"></div>
                    <h6 class="fw-bold m-0" style="color: #334155;">Contact & Operations</h6>
                </div>
            </div>
            <div class="col-md-6">
                <div class="info-box">
                    <p class="info-label text-muted mb-1 small">Department / Designation</p>
                    <p class="info-data">${data.Department || '-'} <span class="text-muted mx-1">/</span> ${data.Designation || '-'}</p>
                </div>
            </div>
            <div class="col-md-6">
                <div class="info-box">
                    <p class="info-label text-muted mb-1 small">Location Address</p>
                    <p class="info-data"><i class="fa fa-map-marker-alt me-2 text-muted small"></i>${data.Address || '-'}</p>
                </div>
            </div>

            <div class="col-12 mt-4 mb-2">
                <div class="d-flex align-items-center">
                    <div style="width:3px; height:20px; background:#6366f1; margin-right:10px; border-radius:10px;"></div>
                    <h6 class="fw-bold m-0" style="color: #334155;">Verification Documents</h6>
                </div>
            </div>

            ${data.TaxDocumentPath ? `
            <div class="col-md-4">
                <button type="button" class="btn-download-file attachment-card shadow-sm w-100 text-start border-0 bg-white p-3 mb-2" 
                        data-filename="${data.TaxDocumentPath}" title="Click to download">
                    <i class="fas fa-file-pdf text-danger fa-lg me-2"></i>
                    <span class="attachment-name fw-medium">Tax Document</span>
                </button>
            </div>` : ''}

            ${data.CompanyProfilePath ? `
            <div class="col-md-4">
                <button type="button" class="btn-download-file attachment-card shadow-sm w-100 text-start border-0 bg-white p-3 mb-2" 
                        data-filename="${data.CompanyProfilePath}" title="Click to download">
                    <i class="fas fa-file-invoice text-primary fa-lg me-2"></i>
                    <span class="attachment-name fw-medium">Company Profile</span>
                </button>
            </div>` : ''}

            ${data.AdditionalDocsPath ? `
            <div class="col-md-4">
                <button type="button" class="btn-download-file attachment-card shadow-sm w-100 text-start border-0 bg-white p-3 mb-2" 
                        data-filename="${data.AdditionalDocsPath}" title="Click to download">
                    <i class="fas fa-folder-open text-warning fa-lg me-2"></i>
                    <span class="attachment-name fw-medium">Additional Docs</span>
                </button>
            </div>` : ''}
        </div>
    `;
    $('#detailsContent').html(content);
    $('#vendorDetailsModal').modal('show');
}
function DownloadVendorFile(fileName) {
    if (!fileName || ["null", "undefined", ""].includes(String(fileName))) {
        return Swal.fire('Info', 'Document not uploaded', 'info');
    }
    window.location.href = GetGlobalURL('Base', 'DownloadFile') + "?fileName=" + fileName;
}

function ApproveVendor(vendorId) {
    Swal.fire({
        title: 'Confirm Approval?',
        text: "This will activate the bank account and grant portal access.",
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: '<i class="fas fa-check"></i> Approve',
        cancelButtonText: 'Cancel',
        reverseButtons: true,
        customClass: {
            container: 'swal2-container-custom',
            popup: 'swal2-popup-custom',
            title: 'swal2-title-custom',
            confirmButton: 'btn-swal btn-swal-approve',
            cancelButton: 'btn-swal btn-swal-cancel'
        },
        buttonsStyling: false 
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire({
                title: 'Approving Application...',
                text: 'Updating status and generating portal credentials.',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            const params = `?VendorId=${vendorId}&Status=Approved`;

            new APICALL(GetGlobalURL('Base', 'UpdateVendorStatus') + params, 'POST', '', true).FETCH((result, error) => {
                Swal.close();

                if (result?.data?.status === "Success") {
                    Swal.fire({
                        icon: 'success',
                        title: 'Approved!',
                        text: 'Bank registration has been approved successfully.',
                        timer: 2000,
                        showConfirmButton: false
                    });
                    GetVendors();
                } else {
                    Swal.fire(
                        'Error',
                        result?.data?.message || 'Failed to approve bank.',
                        'error'
                    );
                }
            });
        }
    });
}
function RejectVendor(vendorId) {
    Swal.fire({
        title: 'Reject Registration?',
        text: "Are you sure you want to decline this application? This action will notify the bank.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: '<i class="fas fa-times"></i>Reject',
        cancelButtonText: 'Cancel',
        reverseButtons: true,
        customClass: {
            popup: 'swal2-popup-custom',
            title: 'swal2-title-custom',
            confirmButton: 'btn-swal btn-swal-reject', 
            cancelButton: 'btn-swal btn-swal-cancel'
        },
        buttonsStyling: false
    }).then((result) => {
        if (result.isConfirmed) {
            ShowLoader('VendorMasterDiv');

            const params = `?VendorId=${vendorId}&Status=Rejected`;
            new APICALL(GetGlobalURL('Base', 'UpdateVendorStatus') + params, 'POST', '', true).FETCH((result, error) => {
                HideLoader('VendorMasterDiv');
                if (result?.data?.status === "Success") {
                    Swal.fire({
                        icon: 'info',
                        title: 'Rejected',
                        text: 'The application has been declined.',
                        timer: 2000,
                        showConfirmButton: false
                    });
                    GetVendors();
                } else {
                    Swal.fire('Error', result?.data?.message || 'Failed to reject bank.', 'error');
                }
            });
        }
    });
}
