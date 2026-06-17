var instanceid = 0;
var UserDetails;

$(document).ready(function () {
    instanceid = GetParameterValues('instanceid') ?? 0;
    LoadViewMyRequest(instanceid);

    $("#intiateform").on("click", ".DownloadPRFile", function (e) {
        e.preventDefault();
        const fileName = $(this).attr('data-filename');
        DownloadFile(fileName);
    });

    $('#intiateform').on('blur change input', 'input, select, textarea', function () {
        const $el = $(this);
        if ($el.hasClass('select2-setup') || $el.hasClass('select2-multiple') || $el.attr('type') === 'file') {
            setTimeout(() => validateSingleField($el), 100);
        } else {
            validateSingleField($el);
        }
    });

    document.addEventListener('click', function (e) {
        const card = e.target.closest('.flip-card');
        if (card && document.body.contains(card)) {
            card.classList.toggle('hover');
        }
    });

});

async function LoadViewMyRequest(instanceid) {
    await GetFormsByInstanceId($('#workflow').val(), instanceid, "appendForm", "View");
    getWorkflowLog(instanceid);
    GetUserDetails();
    await GetCategories();
    await GetPurchaseRequestItems();
    if (instanceid > 0) {
        GetPurchaseRequestDetailByInstanceId(instanceid);
    }
}
function formatDecimal(value) {
    if (!value) return "0.00";
    value = value.toString().replace(/%/g, "").trim();
    let num = parseFloat(value);
    if (isNaN(num)) return "0.00";
    return num.toFixed(2);
}


function GetPurchaseRequestItems() {
    return new Promise((resolve, reject) => {
        new APICALL(GetGlobalURL('Base', 'GetPurchaseRequestItems'), 'GET', '', true).FETCH((result, error) => {
            if (error) {
                console.error("Error/No Data:", error);
                reject(error);
                return;
            }

            const data = result?.data?.data;
            if (data) {
                const options = data.map(item =>
                    `<option value="${item.Id}">${item.ItemCode} | ${item.ItemName}</option>`
                );
                $('#itemDropdown').html('<option value="" selected disabled>Select Item</option>' + options.join(''));
                resolve();
            } else {
                resolve();
            }
        });
    });
}

async function GetCategories() {
    const $select = $('#ddlCategory').empty().append('<option value="" disabled selected>Select Category</option>');

    return new Promise((resolve) => {
        new APICALL(GetGlobalURL('VendorRegister', 'GetCategories'), 'GET', '', true)
            .FETCH((result) => {
                if (result.data?.data) {
                    result.data.data.forEach(item => $select.append(new Option(item.Text, item.Id)));
                }
                resolve(); 
            });
    });
}

const GetUserDetails = () =>
    new APICALL(GetGlobalURL('Base', 'GetUserDetails'), 'GET', '', false)
        .FETCH((res, err) => res && (UserDetails = res));



function GetPurchaseRequestDetailByInstanceId(instanceid) {
    new APICALL(GetGlobalURL('Base', 'GetPurchaseRequestDetailByInstanceId') + '?instanceid=' + instanceid, 'GET', '', false).FETCH((result, error) => {

        if (result && result.data) {
            const master = (result.data.Master && result.data.Master.length > 0) ? result.data.Master[0] : null;
            const items = result.data.Items || [];
            const vendorQuote = (result.data.VendorQuote && result.data.VendorQuote.length > 0) ? result.data.VendorQuote : [];
            const vendorQuotes = (result.data.VendorQuotes && result.data.VendorQuotes.length > 0) ? result.data.VendorQuotes : [];
            const PurchaseOrder = result.data?.PurchaseOrder || null;
            const gateEntries = result.data?.GateEntry || [];
            const QAQC = result.data?.QAQC || [];
            const GRNRecord = result.data?.GRNRecord || [];
            const PaymentRecord = result.data?.PaymentRecord || [];

            // --- Helper functions for consistency ---
            const getFileIcon = (fileName) => {
                if (!fileName) return 'bx-file text-secondary';
                const ext = fileName.split('.').pop().toLowerCase();
                const icons = {
                    pdf: 'bxs-file-pdf text-danger',
                    doc: 'bxs-file-doc text-primary',
                    docx: 'bxs-file-doc text-primary',
                    jpg: 'bxs-file-image text-warning',
                    jpeg: 'bxs-file-image text-warning',
                    png: 'bxs-file-image text-warning',
                    gif: 'bxs-file-image text-warning'
                };
                return icons[ext] || 'bx-file text-secondary';
            };

            const lockAndTrigger = ($el, val, isReadonly = false) => {
                if (val !== undefined) $el.val(val);
                $el.prop(isReadonly ? 'readonly' : 'disabled', true).addClass('bg-light').trigger('change');
                if ($el.is('textarea')) $el.css('height', ($el[0].scrollHeight || 40) + 'px');
            };

            const applyVerifiedBadge = ($s, text, icon) => {
                if (!$s.find('.header-title .badge').length) {
                    $s.find('.header-title').append(`<span class="badge bg-light-success text-success border border-success-subtle px-3 py-2 rounded-pill ms-2" style="font-size: 0.75rem;"><i class="bx ${icon} me-1"></i> ${text}</span>`);
                }
            };

            // --- Section 1: Requestor Section ---
            if (master?.justification) {
                const $s = $('#requestorSection'), opts = $('#itemDropdown').html();
                $s.addClass('section-filled border-start border-success border-5 rounded-3');
                applyVerifiedBadge($s, 'VERIFIED', 'bxs-check-shield');
                $('#addItemRow, .policy-banner').closest('.row').addClass('d-none');

                if (master.requestDate) lockAndTrigger($s.find('#requestDate'), master.requestDate.split('T')[0]);

                const labels = $s.find('.item-row:first label').map((i, el) => $(el).text()).get();
                const rows = items.map((item, i) => {
                    const fileHtml = item.fileName ? `
                        <div class="d-flex align-items-center p-2 border rounded bg-white w-100">
                            <i class="bx ${getFileIcon(item.fileName)} fs-4 me-2"></i>
                            <div class="flex-grow-1 overflow-hidden">
                                <p class="mb-0 fw-bold extra-small text-truncate">${item.fileName.split('_').pop()}</p>
                                <button type="button" class="DownloadPRFile btn btn-link p-0 extra-small fw-bold text-decoration-none" data-filename="${item.fileName}">DOWNLOAD</button>
                            </div>
                        </div>` : `<div class="form-control py-3 bg-light text-muted extra-small text-center rounded-3 border-dashed">No document</div>`;

                    return `<div class="row g-3 ${i ? 'mb-2' : 'mb-4'} item-row align-items-end animate__animated animate__fadeIn">
                        <div class="col-md-5 d-none">${i ? '' : `<label class="form-label small fw-bold text-secondary text-uppercase">${labels[0] || 'Item'}</label>`}
                            <select class="form-select py-3 bg-light item-select" disabled data-val="${item.ProductId}">${opts}</select></div>
                        <div class="col-md-2 d-none">${i ? '' : `<label class="form-label small fw-bold text-secondary text-uppercase">${labels[1] || 'Qty'}</label>`}
                            <input type="number" class="form-control py-3 bg-light item-qty" value="${item.qty}" disabled></div>
                       <div class="col-md-12">${i ? '' : `<label class="form-label small fw-bold text-secondary text-uppercase">${labels[2] || 'Reason'}</label> <textarea
                          id="justification"
                          name="justification"
                          class="form-control form-control-custom"
                          rows="3" disabled "
                       >${master.justification ?? ''}</textarea>`}
                     </div><div class="col-md-12"><div class="upload-wrapper">${fileHtml}</div></div></div>`;
                }).join('');

                $s.find('#itemsContainer').html(rows).find('.item-select, .item-qty').each(function () {
                    const $el = $(this);
                    if ($el.is('select')) $el.val($el.data('val'));
                    $el.trigger('change');
                });
                $s.show();
            } else {
                $('#requestorSection').addClass('d-none');
            }

            // --- Section 2: RFQ Generation Section ---
            if (master?.RFQDescription) {
                const $s = $('#RFQGenerationSection');
                $s.addClass('section-filled border-start border-success border-5 rounded-3');
                applyVerifiedBadge($s, 'ISSUED', 'bxs-check-circle');
                $s.find('.text-muted.small').first().text('Bid record locked after issuance');

                lockAndTrigger($s.find('#ddlCategory'), master.CategoryId);
                $s.find('#ddlVendorsSelect').closest('.col-md-6').hide();

                const fieldMap = { '#dtStart': master.RFQStartDate, '#dtEnd': master.RFQEndDate, '#txtDescription': master.RFQDescription };
                Object.entries(fieldMap).forEach(([id, val]) => {
                    if (val) {
                        const finalVal = id.includes('dt') ? val.replace(' ', 'T').substring(0, 16) : val;
                        lockAndTrigger($s.find(id), finalVal, true);
                    }
                });

                if (master.RFQAttachmentPath) {
                    $s.find('.upload-wrapper').html(`<div class="mt-3 p-3 border rounded-3 bg-light d-flex align-items-center justify-content-between animate__animated animate__fadeIn">
                    <div class="d-flex align-items-center"><i class="bx ${getFileIcon(master.RFQAttachmentPath)} fs-2 me-3"></i>
                    <div><p class="mb-0 fw-bold small">Bid Document</p><small class="text-success fw-bold">Uploaded</small></div></div>
                    <button type="button" class="DownloadPRFile btn btn-primary btn-sm px-4" data-filename="${master.RFQAttachmentPath}">DOWNLOAD</button></div>`).show();
                }
                $s.find('#txtReIssuanceDays').addClass('is-valid').val(master.ReIssuanceDuration).trigger('change');

                $s.show();
            } else {
                $('#RFQGenerationSection').addClass('d-none');
            }

            // --- Section 3: RFQ Submission Section ---

            if (vendorQuote.length > 0) {

                const $s = $('#RFQSubmissionSection');

                const isSel = vendorQuote.some(x => x.IsSelected == 1);

                $s.addClass(`
                    section-filled
                    border-start
                    border-5
                    rounded-3
                    ${isSel ? 'border-primary shadow-sm' : 'border-success'}
                `);

                applyVerifiedBadge(
                    $s,
                    isSel ? 'SELECTED' : 'SUBMITTED',
                    isSel ? 'bxs-trophy' : 'bxs-check-circle'
                );

                $s.find('.text-muted.small')
                    .first()
                    .html(`${isSel ? '<b>Selected</b>' : 'Quotation'} details (Locked)`);

                // remarks (first row)
                lockAndTrigger(
                    $s.find('#txtRFQRemarks'),
                    vendorQuote[0].VendorDescription,
                    true
                );

                // clear table
                $('#investmentTableBody').html('');

                // LOOP ALL ROWS
                vendorQuote.forEach(vd => {

                    let month3 = '';
                    let month6 = '';
                    let month12 = '';

                    if (vd.TenorDetails) {

                        const details = vd.TenorDetails.split('|');

                        details.forEach(item => {

                            item = item.trim();

                            const tenorMatch = item.match(/TenorId:(\d+)/);
                            const valueMatch = item.match(/Value:(\d+(\.\d+)?)/);

                            if (tenorMatch && valueMatch) {

                                const tenorId = parseInt(tenorMatch[1]);
                                const value = valueMatch[1];

                                if (tenorId === 2)
                                    month3 = value;

                                if (tenorId === 3)
                                    month6 = value;

                                if (tenorId === 4)
                                    month12 = value;
                            }
                        });
                    }

                    // append row
                    $('#investmentTableBody').append(`
                        <tr>

                            <td>
                                <input type="text"
                                       class="form-control"
                                       value="${vd.VendorQuotedPrice || ''}"
                                       readonly>
                            </td>

                            <td>
                                <input type="text"
                                       class="form-control"
                                       value="${month3}"
                                       readonly>
                            </td>

                            <td>
                                <input type="text"
                                       class="form-control"
                                       value="${month6}"
                                       readonly>
                            </td>

                            <td>
                                <input type="text"
                                       class="form-control"
                                       value="${month12}"
                                       readonly>
                            </td>
                             <td>
                               -
                            </td>
                        </tr>
                    `);
                });

                // FILE
                const firstAttachment = vendorQuote[0]?.VendorAttachmentPath;

                if (firstAttachment) {

                    $s.find('.upload-wrapper').html(`
                        <div class="mt-3 p-3 border rounded-3 bg-light d-flex align-items-center justify-content-between">

                            <div class="d-flex align-items-center">

                                <i class="bx ${getFileIcon(firstAttachment)} fs-2 me-3"></i>

                                <div>
                                    <p class="mb-0 fw-bold small">
                                        Bid Document
                                    </p>

                                    <small class="${isSel ? 'text-primary' : 'text-success'} fw-bold">
                                        ${isSel ? '🏆 Selected' : 'Verified'}
                                    </small>
                                </div>

                            </div>

                            <button type="button"
                                    class="DownloadPRFile btn btn-primary btn-sm px-4"
                                    data-filename="${firstAttachment}">
                                DOWNLOAD
                            </button>

                        </div>
                    `).show();
                }

                $s.show();

            }
            else {

                $('#RFQSubmissionSection').addClass('d-none');
            }

            // --- Section 4: Vendor Evaluation Section ---
            if (vendorQuotes?.length > 0) {

                const $s = $('#VendorEvaluationSection'),
                    $tableBody = $('#vendorTableBody').empty();

                // =========================
                // GROUP BY VENDOR
                // =========================
                const groupedVendors = {};

                vendorQuotes.forEach(q => {

                    if (!groupedVendors[q.VendorID]) {

                        groupedVendors[q.VendorID] = {
                            VendorID: q.VendorID,
                            BusinessName: q.BusinessName,
                            VendorName: q.VendorName,
                            VendorEmail: q.VendorEmail,
                            VendorAttachmentPath: q.VendorAttachmentPath,
                            IsAwarded: q.IsAwarded,
                            AwardedBy: q.AwardedBy,
                            AwardedDate: q.AwardedDate,
                            EvaluationRemarks: q.EvaluationRemarks,
                            quotations: []
                        };
                    }

                    groupedVendors[q.VendorID].quotations.push({
                        VendorQuotedPrice: q.VendorQuotedPrice,
                        TenorDetails: q.TenorDetails
                    });
                });

                const vendors = Object.values(groupedVendors);

                // =========================
                // AWARDED CHECK
                // =========================
                const awarded = vendors.find(q => q.IsAwarded == 1);
                const isAwarded = !!awarded;

                const hasPO = typeof PurchaseOrder !== 'undefined'
                    && PurchaseOrder?.length > 0;

                const displayStatus = (isAwarded && hasPO)
                    ? 'AWARDED'
                    : 'SELECTED';

                const statusIcon = (isAwarded && hasPO)
                    ? 'bxs-award'
                    : 'bx-check-double';

                $s.toggleClass(
                    'section-filled border-start border-success border-5 rounded-3',
                    isAwarded
                );

                applyVerifiedBadge(
                    $s,
                    isAwarded ? displayStatus : 'COMPLETED',
                    isAwarded ? statusIcon : 'bx-list-check'
                );

                $('#evalHeading').text(
                    isAwarded
                        ? `${displayStatus} Bank Details`
                        : 'Comparative Statement / Evaluation'
                );

                $('#evalSubText').text(
                    isAwarded
                        ? `The following Bank has been ${displayStatus.toLowerCase()} for this procurement.`
                        : 'Compare Bank financial bids to select the most viable partner.'
                );

                const displayList = isAwarded
                    ? [awarded]
                    : vendors;

                // =========================
                // CREATE TENOR TABLE
                // =========================
                const createTenorTable = (quotations) => {

                    let tenorHeaders = [];

                    // Extract unique tenor names
                    quotations.forEach(q => {

                        if (!q.TenorDetails) return;

                        q.TenorDetails.split('|').forEach(t => {

                            const tenorMatch = t.match(/Tenorname:(.*?),/i);

                            if (tenorMatch) {

                                const tenorName = tenorMatch[1].trim();

                                if (!tenorHeaders.includes(tenorName)) {
                                    tenorHeaders.push(tenorName);
                                }
                            }
                        });
                    });

                    // =========================
                    // TABLE HEADER
                    // =========================
                    const thead = `
                        <thead>

                            <tr class="table-light">

                                <th rowspan="2"
                                    class="align-middle text-center fw-bold">
                                    Amount
                                </th>

                                <th colspan="${tenorHeaders.length}"
                                    class="text-center fw-bold">
                                    Tenure Of Investment
                                </th>

                            </tr>

                            <tr class="table-light">

                                ${tenorHeaders.map(h => `
                                    <th class="text-center fw-bold">
                                        ${h}
                                    </th>
                                `).join('')}

                            </tr>

                        </thead>
                    `;

                    // =========================
                    // TABLE BODY
                    // =========================
                    const tbodyRows = quotations.map(q => {

                        const tenorMap = {};

                        if (q.TenorDetails) {

                            q.TenorDetails.split('|').forEach(t => {

                                const tenorMatch = t.match(/Tenorname:(.*?),/i);
                                const valueMatch = t.match(/Value:(.*)/i);

                                if (tenorMatch && valueMatch) {

                                    const tenorName = tenorMatch[1].trim();
                                    const value = valueMatch[1].trim();

                                    tenorMap[tenorName] = value;
                                }
                            });
                        }

                        return `
                            <tr>

                                <td class="w-50">
                                    ${q.VendorQuotedPrice || '-'}
                                </td>

                                ${tenorHeaders.map(h => `
                                    <td class="text-center">
                                        ${tenorMap[h] || '-'}
                                    </td>
                                `).join('')}

                            </tr>
                        `;

                    }).join('');

                    return `
                        <table class="table table-bordered align-middle mb-0">

                            ${thead}

                            <tbody>
                                ${tbodyRows}
                            </tbody>

                        </table>
                    `;
                };

                // =========================
                // MAIN ROWS
                // =========================
                const rows = displayList.map((vendor, i) => {

                    const actionHtml = isAwarded

                        ? `<div class="text-center">

                                <span class="badge bg-success p-2 px-3 rounded-pill">
                                    <i class="bx ${statusIcon} me-1"></i>
                                    ${displayStatus}
                                </span>

                                <div class="extra-small text-muted mt-1">

                                    By: ${vendor.AwardedBy || ''}

                                    |

                                    On: ${vendor.AwardedDate?.split('T')[0] || ''}

                                </div>

                           </div>`

                        : `<input type="radio"
                                  name="vendorWinner"
                                  class="btn-check"
                                  id="v_${i}"
                                  value="${vendor.VendorID}">

                           <label class="btn btn-outline-success btn-sm px-3 rounded-pill fw-bold"
                                  for="v_${i}">

                                <i class="bx bx-check-circle"></i>
                                Select

                           </label>`;

                    return `

                        <!-- PARENT ROW -->
                        <tr class="align-middle ${isAwarded ? 'table-success' : ''}">

                            <td class="text-center">

                                <button type="button" class="btn btn-sm btn-light border toggleChild"
                                        data-target="child_${vendor.VendorID}">

                                    <i class="fa fa-minus"></i>

                                </button>

                            </td>

                            <td class="text-center fw-bold text-muted">

                                ${i + 1}

                            </td>

                            <td hidden>
                                ${vendor.VendorID}
                            </td>
                            <td>
                                <div class="fw-bold text-dark text-nowrap">
                                    ${vendor.BusinessName || 'Unknown'}
                                </div>
                            </td>
                            <td>
                                <div class="fw-bold text-dark text-nowrap">
                                    ${vendor.VendorName || 'Unknown'}
                                </div>
                            </td>
                            <td>
                                <div class="text-nowrap">
                                    ${vendor.VendorEmail || 'Unknown'}
                                </div>
                            </td>
                            <td>
                                <div class="text-nowrap">
                                    ${vendor.VendorDescription || '-'}
                                </div>
                            </td>
                            <td>

                                ${vendor.VendorAttachmentPath

                            ? `<button type="button"
                                               class="DownloadPRFile btn btn-sm btn-light border text-primary fw-bold px-3"
                                               data-filename="${vendor.VendorAttachmentPath}">

                                            <i class="bx ${getFileIcon(vendor.VendorAttachmentPath)} me-1"></i>

                                            DOWNLOAD

                                       </button>`

                            : `<span class="text-muted small">
                                            No Doc
                                       </span>`
                        }

                            </td>
                            <td class="text-center">
                                ${actionHtml}
                            </td>
                        </tr>
                        <!-- CHILD ROW -->
                        <tr id="child_${vendor.VendorID}"
                            class="child-table-row">

                            <td colspan="8"
                                class="p-3 bg-light">

                                ${createTenorTable(vendor.quotations)}

                            </td>

                        </tr>

                    `;

                }).join('');

                $tableBody.append(rows);

                $(document).off('click', '.toggleChild').on('click', '.toggleChild', function () {

                    const target = $(this).data('target');

                    $('#' + target).toggleClass('d-none');

                    const icon = $(this).find('i');

                    icon.toggleClass('fa-plus fa-minus');
                });

                const remarksField = $('#txtEvaluationRemarks');

                if (isAwarded) {

                    lockAndTrigger(
                        remarksField,
                        awarded.EvaluationRemarks || ''
                    );

                    remarksField.prop('disabled', true);

                } else {

                    remarksField.val('')
                        .prop('disabled', false);
                }

                $('#btnSubmitEvaluation').toggle(!isAwarded);

                $s.fadeIn();
            }
            else {

                $('#VendorEvaluationSection').addClass('d-none');
            }
         


            const isVendor = UserDetails.data.RoleID === 1017;

            $('#VendorEvaluationSection, #purchaseOrderSection, #gateEntrySection, #grnConfirmationSection, #historySection')
                .toggle(!isVendor);

            $('#RFQSubmissionSection').toggle(isVendor);

        }

        if (error) {
            Swal.fire({
                icon: 'error',
                title: 'Error...',
                text: error.data ? error.data.responseText : 'Something went wrong',
                footer: ''
            });
        }
    });
}
function DownloadFile(fileName) {
    if (!fileName || ["null", "undefined", ""].includes(String(fileName))) {
        return Swal.fire('Info', 'Document not uploaded', 'info');
    }
    window.location.href = GetGlobalURL('Base', 'DownloadFile') + "?fileName=" + fileName;
}


function validateSingleField($el) {
    const val = $el.val();
    const type = $el.attr('type');

    let label = $el.closest('.form-group, .mb-3, .col-md, .col-md-5, .col-md-2').find('label').first().text().replace('*', '').trim();
    if (!label || label === "") {
        label = "This field";
    }

    let isFieldValid = true;
    let errorMsg = "";

    if (type === 'file') {
        isFieldValid = $el[0].files && $el[0].files.length > 0;
        errorMsg = `Please upload the required ${label}.`;
    }
    else if ($el.is(':checkbox') || $el.is(':radio')) {
        isFieldValid = $el.is(':checked');
        errorMsg = `Please select ${label}.`;
    }
    else if (!val || (typeof val === 'string' && val.trim() === "") || (Array.isArray(val) && val.length === 0)) {
        isFieldValid = false;
        errorMsg = `${label} is required and cannot be empty.`;
    }
    else if (type === 'email' && !/^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$/.test(val)) {
        isFieldValid = false;
        errorMsg = `Please enter a valid email address for ${label}.`;
    }
    else if (type === 'number' && parseFloat(val) <= 0) {
        isFieldValid = false;
        errorMsg = `${label} must be a value greater than zero.`;
    }

    $el.toggleClass('is-invalid', !isFieldValid).toggleClass('is-valid', !!isFieldValid);

    if (type === 'file') {
        const $customArea = $el.next('.upload-area');
        if (!isFieldValid) {
            $customArea.css({ 'border-color': '#dc3545', 'background-color': '#fff8f8' });
        } else {
            $customArea.css({ 'border-color': '#198754', 'background-color': '#f8fff9' });
        }
    }

    if ($el.hasClass('select2-hidden-accessible')) {
        const $selection = $el.next('.select2-container').find('.select2-selection');
        if (!isFieldValid) {
            $selection.addClass('select2-invalid').removeClass('select2-valid');
        } else {
            $selection.addClass('select2-valid').removeClass('select2-invalid');
        }
    }

    let $feedback = $el.siblings('.invalid-feedback');
    if ($feedback.length === 0) {
        $feedback = $el.parent().find('.invalid-feedback');
    }
    $feedback.text(isFieldValid ? "" : errorMsg);

    return { isValid: isFieldValid, message: errorMsg };
}
function validateSection(selector) {
    let sectionValid = true;
    let firstErrorMsg = "";

    $(selector).find('input, select, textarea').not(":disabled, .select2-search__field").each(function () {
        const $el = $(this);

        if (!$el.is(':visible') && !$el.hasClass('select2-hidden-accessible') && $el.attr('type') !== 'file') {
            return;
        }

        const result = validateSingleField($el);

        if (!result.isValid) {
            if (!firstErrorMsg) firstErrorMsg = result.message;
            sectionValid = false;
        }
    });

    if (!sectionValid) {
        showValidationErrorAlert(firstErrorMsg);
    }
    return sectionValid;
}
