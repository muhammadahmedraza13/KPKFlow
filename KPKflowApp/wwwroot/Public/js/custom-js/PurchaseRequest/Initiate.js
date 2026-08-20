var instanceid = 0;
var UserDetails;
let itemCount = 1;
let isFormCompleted = false;

$(document).ready(function () {
    instanceid = GetParameterValues('instanceid') ?? 0;
    LoadViewMyRequest(instanceid);
   
    $("#intiateform").on("change", "input[type='file']", function () {
        const file = this.files[0];
        const $area = $(this).closest('.upload-wrapper').find('.upload-area');
        const $text = $area.find('.file-name-text, span.fw-bold').first();
        const $icon = $area.find('i');

        if (file) {
            $area.addClass('has-file').css({
                'border-color': '#05611a', 
                'background-color': '#dcfcde', 
                'box-shadow': '0 4px 12px rgba(30, 126, 52, 0.15)',
                'transform': 'scale(1.01)'
            });
            $text.text(file.name).addClass('text-success'); 
            $icon.attr('class', 'bx bx-check-double text-success');
        } else {
            $area.removeClass('has-file').attr('style', ''); 
            $text.text($text.hasClass('file-name-text') ? "Attach file" : "Click to upload or drag documents").removeClass('text-success');
            $icon.attr('class', 'bx bx-cloud-upload');
        }
    });

    $("#intiateform").on("input keydown", "input[id*='[qty]']", function (e) {
        if (e.type === "keydown" && ["e", "E", "+", "-", "."].includes(e.key)) {
            e.preventDefault();
        }
        if (e.type === "input") {
            this.value = this.value.replace(/\D/g, '');
        }
    });

    $("#intiateform").on("click", "#addItemRow", function () {
        addItemRow();
    });

    $("#intiateform").on("click", ".remove-row-btn", function (e) {
        e.preventDefault();
        $(this).closest('.item-row').remove();
        updateTrashButtons();
    });

    $("#intiateform").on("change", "#ddlCategory", function (e) {
        if (this.value) {
            GetVendorsbyCategories(1);
        }
        $(this).valid(); 
    });

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

    $("#intiateform").on("click", ".savebutton", function () {
        mynextsetp(this);
    });

    document.addEventListener('click', function (e) {
        const card = e.target.closest('.flip-card');
        if (card && document.body.contains(card)) {
            card.classList.toggle('hover');
        }
    });
    
    $("#intiateform").on("click", "#addRowBtn", function () {

        let newRow = `
            <tr>
                <td>
                    <input type="text" class="form-control">
                </td>
                <td>
                    <input type="text" class="form-control inputnum">
                </td>
                <td>
                    <input type="text" class="form-control inputnum">
                </td>
                <td>
                    <input type="text" class="form-control inputnum">
                </td>
                <td>
                    <button type="button" class="btn btn-danger removeRowBtn">
                        Remove
                    </button>
                </td>
            </tr>
        `;
        $("#investmentTableBody").append(newRow);
    });
    
});
$(document).on("click", ".removeRowBtn", function () {
    $(this).closest("tr").remove();
});
$(document).on('input', 'input.number', function () {
    formatNumberInput($(this));
});
async function LoadViewMyRequest(instanceid) {
    await GetFormsByInstanceId($('#workflow').val(), instanceid, "appendForm", "intiate");
    getWorkflowLog(instanceid);
    GetWorkflowAction($('#workflow').val(), instanceid, "intiateform");

    updateTrashButtons();

    GetUserDetails();
    refreshSelect2();
    $('#requestDate').val(new Date().toISOString().split('T')[0]).prop('disabled', true).trigger('change');

    await GetCategories();
    await GetPurchaseRequestItems();
    GetVendorsbyCategories(1);

    if (instanceid > 0) {
        GetPurchaseRequestDetailByInstanceId(instanceid);
    }

    if (!isFormCompleted) {
        setInterval(updateLiveTimestamp, 1000);
    }
    if (UserDetails.data.RoleID == 1019) {
        $(".savebutton").prop('disabled', true);
        startOrResumeTimer();
    }
}


function refreshSelect2() {
    const $selects = $('#intiateform').find('.select2-multiple');
    $selects.filter('.select2-hidden-accessible').select2('destroy');

    $selects.select2({
        width: '100%',
        placeholder: "Select Banks",
        minimumResultsForSearch: Infinity,
        allowClear: true
    });
}
function updateTrashButtons() {
    const allRows = $(".item-row");
    const trashContainers = $(".trash-column");

    allRows.each(function (index) {
        const container = $(trashContainers[index]);
        if (allRows.length > 1) {
            container.html(`
                <button type="button" class="btn btn-link text-danger p-0 mb-1 shadow-none remove-row-btn" 
                        style="text-decoration: none; transition: 0.2s ease;" 
                        title="Remove Item">
                    <i class="bx bx-trash fs-4"></i>
                </button>`);
        } else {
            container.html('');
        }
    });
}
function addItemRow() {
    const container = document.getElementById('itemsContainer');

    const firstDropdown = document.querySelector('select[name^="items[0][id]"]');
    const optionsHTML = firstDropdown ? firstDropdown.innerHTML : '<option value="" selected disabled>Select Item</option>';

    const newRow = document.createElement('div');
    newRow.className = 'row g-3 mb-4 item-row align-items-end'; 

    newRow.innerHTML = `
        <div class="col-md-5">
            <label class="form-label-custom">Requirement</label>
            <select name="items[${itemCount}][id]" class="form-select form-control-custom">
                ${optionsHTML}
            </select>
        </div>

        <div class="col-md-2">
            <label class="form-label-custom">QTY</label>
            <input type="number" name="items[${itemCount}][qty]" 
                   class="form-control form-control-custom" 
                   placeholder="0">
        </div>

        <div class="col-md"> 
            <label class="form-label-custom">Attach Documents</label>
            <div class="upload-wrapper">
                <input type="file" name="items[${itemCount}][file]" id="fileUpload_${itemCount}" class="d-none" accept=".docx, .pdf, image/png, image/jpeg">
                <label for="fileUpload_${itemCount}" class="upload-area py-2" style="min-height: 52px; flex-direction: row; justify-content: center; padding: 0;">
                    <i class="bx bx-cloud-upload text-primary fs-5 me-2"></i> 
                    <span class="small file-name-text text-muted">Attach file</span>
                </label>
            </div>
        </div>

        <div class="col-auto text-center trash-column px-1" style="min-width: 45px;">
            <button type="button" class="btn btn-link text-danger p-0 mb-1 shadow-none remove-row-btn" 
                    onclick="this.closest('.item-row').remove(); updateTrashButtons();">
                <i class="bx bx-trash fs-4"></i>
            </button>
        </div>
    `;

    container.appendChild(newRow);
    itemCount++;

    if (typeof updateTrashButtons === "function") updateTrashButtons();
}
function updateLiveTimestamp() {
    const el = document.getElementById('liveTimestamp');
    if (!el) return;

    el.textContent = new Date().toLocaleString('en-US', {
        month: 'short', day: '2-digit', year: 'numeric',
        hour: '2-digit', minute: '2-digit', second: '2-digit',
        hour12: false
    }).toUpperCase().replace(',', ' |');
}




const GetUserDetails = () =>
    new APICALL(GetGlobalURL('Base', 'GetUserDetails'), 'GET', '', false)
        .FETCH((res, err) => res && (UserDetails = res));
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
                //const options = data.map(item =>
                //    `<option value="${item.Id}">${item.ItemCode} | ${item.ItemName}</option>`
                //);
                //$('#itemDropdown').html('<option value="" selected disabled>Select Item</option>' + options.join(''));
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
function GetVendorsbyCategories(CategoryId) {
    const $select = $('#ddlVendorsSelect');
    var Data = {
        CategoryId: CategoryId,
        InstanceId: instanceid
    };

    new APICALL(GetGlobalURL('Base', 'GetVendorsbyCategories'), 'POST', Data, false, false, 'application/x-www-form-urlencoded')
        .FETCH((result, error) => {

            $select.empty();

            if (error) {
                Swal.fire({ icon: 'error', title: 'Error', text: 'Server connection failed' });
                return;
            }

            const response = result.data;

            if (response && response.success === true) {

                const tables = response.data || [];

                const table1 = tables[0]?.Rows || []; 
                const table2 = tables[1]?.Rows || []; 

                if (table1.length > 0) {

                    table1.forEach(item => {
                        let option = new Option(item.BusinessName+' (User: ' + item.VendorName + ')', item.VendorId);

                        $(option).attr('data-email', item.Email);
                        $(option).attr('data-mobile', item.MobileNumber);

                        $select.append(option);
                    });

                    const selectedIds = table2.map(x => x.VendorId.toString());

                    $select.val(selectedIds);

                } else {
                    Swal.fire({
                        icon: 'warning',
                        title: 'Notice',
                        text: response.message || 'No Banks found for this category.'
                    });
                    $('#ddlCategory').val('').trigger('change');
                }

            } else {
                Swal.fire({
                    icon: 'warning',
                    title: 'Notice',
                    text: response?.message || 'No Banks found.'
                });
            }

            $select.trigger('change');
        });
}



function GetPurchaseRequestDetailByInstanceId(instanceid) {
    new APICALL(GetGlobalURL('Base', 'GetPurchaseRequestDetailByInstanceId') + '?instanceid=' + instanceid, 'GET', '', false).FETCH((result, error) => {

        if (result && result.data) {
            const master = (result.data.Master && result.data.Master.length > 0) ? result.data.Master[0] : null;
            const items = result.data.Items || [];
            const vendorQuote = (result.data.VendorQuote && result.data.VendorQuote.length > 0) ? result.data.VendorQuote[0] : null;
            const vendorQuotes = (result.data.VendorQuotes && result.data.VendorQuotes.length > 0) ? result.data.VendorQuotes: [];
            const PurchaseOrder = result.data?.PurchaseOrder || null;
            const gateEntries = result.data?.GateEntry || [];
            const QAQC = result.data?.QAQC || [];
            const GRNRecord = result.data?.GRNRecord || [];
            const PaymentRecord = result.data?.PaymentRecord || [];
            const isVendor = UserDetails.data.RoleID === 1017;
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

                lockAndTrigger($s.find('#justification'), master.justification);
                lockAndTrigger($s.find('#targetAmount'), master.targetAmount);
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
                    <div><p class="mb-0 fw-bold small">BID Document</p><small class="text-success fw-bold">Uploaded</small></div></div>
                    <button type="button" class="DownloadPRFile btn btn-primary btn-sm px-4" data-filename="${master.RFQAttachmentPath}">DOWNLOAD</button></div>`).show();
                }
                $s.find('#txtReIssuanceDays').addClass('is-valid').val(master.ReIssuanceDuration).trigger('change');

                $s.show();
            }

            // --- Section 3: RFQ Submission Section ---
            if (vendorQuote?.VendorDescription) {
                const $s = $('#RFQSubmissionSection');
                const isSel = vendorQuote.IsSelected == 1;

                $s.addClass(`section-filled border-start border-5 rounded-3 ${isSel ? 'border-primary shadow-sm' : 'border-success'}`);

                applyVerifiedBadge($s, isSel ? 'SELECTED' : 'SUBMITTED', isSel ? 'bxs-trophy' : 'bxs-check-circle');

                $s.find('.text-muted.small').first().html(`${isSel ? '<b>Selected</b>' : 'Quotation'} details (Locked)`);

                lockAndTrigger($s.find('#txtQuotedPrice'), vendorQuote.VendorQuotedPrice, true);
                lockAndTrigger($s.find('#txtRFQRemarks'), vendorQuote.VendorDescription, true);

                if (vendorQuote.VendorAttachmentPath) {
                    $s.find('.upload-wrapper').html(`
                    <div class="mt-3 p-3 border rounded-3 bg-light d-flex align-items-center justify-content-between">
                        <div class="d-flex align-items-center">
                            <i class="bx ${getFileIcon(vendorQuote.VendorAttachmentPath)} fs-2 me-3"></i>
                            <div>
                                <p class="mb-0 fw-bold small">Bid Document</p>
                                <small class="${isSel ? 'text-primary' : 'text-success'} fw-bold">${isSel ? '🏆 Selected' : 'Verified'}</small>
                            </div>
                        </div>
                        <button type="button" class="DownloadPRFile btn btn-primary btn-sm px-4" data-filename="${vendorQuote.VendorAttachmentPath}">DOWNLOAD</button>
                    </div>`).show();
                }
                $s.show();
            }

            // --- Section 4: Vendor Evaluation Section ---
            if (vendorQuotes?.length > 0) {

                
                const $s = $('#VendorEvaluationSection'),
                    $tableBody = $('#vendorTableBody').empty();
                    let $investmentTableBody = $('#investmentTableBody');

                // Filter me check karein ke SubmittedVendorId match ho, TenorDetails null/undefined na ho, 
                // aur usme waqai 'Tenorname' ka word mojood ho (yani actual data ho)
                const validQuotes = vendorQuotes ? vendorQuotes.filter(x =>
                    x.SubmittedVendorId == UserDetails.data.UserID &&
                    x.TenorDetails &&
                    x.TenorDetails.trim() !== "" &&
                    x.TenorDetails.toLowerCase().includes("tenorname")
                ) : [];

               
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

                const displayStatus = (isAwarded)
                    ? 'AWARDED'
                    : 'SELECTED';

                const statusIcon = (isAwarded)
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
                        ? `The following Bank has been ${displayStatus.toLowerCase()} for this bid.`
                        : 'Compare Bank financial bids to select the most viable partner.'
                );

                const displayList = isAwarded
                    ? [awarded]
                    : vendors;
                if (validQuotes.length > 0) {

                    $investmentTableBody.empty();
                    if (isVendor) {
                            $("#intiateform").find('#actionId').empty();
                            $("#intiateform").find('#actionId').append(
                                '<button class="mt-2 btn btn-primary btn-sm savebutton me-2 mb-2" type="button" data-assignmenttype="static" data-dynamicfunction="undefind" data-save="true" data-move="false" data-id="56">' +
                                '<i class="feather-save me-2"></i> Submit Bids' +
                                '</button>'
                            );
                        
                    }
                    validQuotes.forEach((quote, index) => {
                        const tenorMap = { '3m': '', '6m': '', '12m': '' };

                        quote.TenorDetails.split('|').forEach(t => {
                            const tenorMatch = t.match(/Tenorname\s*:\s*([^,]+)/i);
                            const valueMatch = t.match(/Value\s*:\s*(.*)/i);

                            if (tenorMatch && valueMatch) {
                                const tenorName = tenorMatch[1].trim().toLowerCase();
                                const value = valueMatch[1].trim();

                                if (tenorName.includes('3m') || tenorName.includes('3')) {
                                    tenorMap['3m'] = value;
                                } else if (tenorName.includes('6m') || tenorName.includes('6')) {
                                    tenorMap['6m'] = value;
                                } else if (tenorName.includes('1y') || tenorName.includes('12')) {
                                    tenorMap['12m'] = value;
                                }
                            }
                        });

                        const investmentRow = `
                            <tr>
                                <td>
                                    <input type="text" class="form-control number" value="${quote.VendorQuotedPrice || ''}" >
                                </td>
                                <td>
                                    <input type="text" class="form-control inputnum" value="${tenorMap['3m'] || '-'}" >
                                </td>
                                <td>
                                    <input type="text" class="form-control inputnum" value="${tenorMap['6m'] || '-'}" >
                                </td>
                                <td>
                                    <input type="text" class="form-control inputnum" value="${tenorMap['12m'] || '-'}" >
                                </td>
                                <td class="text-center"></td>
                            </tr>
                        `;
                        $investmentTableBody.append(investmentRow);
                    });
                }
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
                            value="${vendor.VendorID}"
                            disabled>
                            <label class="btn btn-outline-success btn-sm px-3 rounded-pill fw-bold disabled"
                                   for="v_${i}">
                                <i class="bx bx-check-circle"></i>
                                Select
                            </label>`;

                                return `

                        <!-- PARENT ROW -->
                        <tr class="align-middle ${isAwarded ? 'table-success' : ''}">

                            <td class="text-center">

                                <button disabled type="button" class="btn btn-sm btn-light border toggleChild"
                                        data-target="child_${vendor.VendorID}">

                                    <i class="fa fa-plus"></i>

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
                                            data-filename="${vendor.VendorAttachmentPath}"
                                            disabled>

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
                            class="child-table-row d-none">

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
                        .prop('disabled', true);
                }

                $('#btnSubmitEvaluation').toggle(!isAwarded);

                $s.fadeIn();
            }
            else {
                $('#VendorEvaluationSection').addClass('d-none');
            }
            // --- Section 5: Purchase Order Section ---
            if (PurchaseOrder?.length > 0) {
                const po = PurchaseOrder[0], $s = $('#purchaseOrderSection');
                $s.addClass('section-filled border-start border-success border-5 rounded-3');
                applyVerifiedBadge($s, 'COMPLETED', 'bxs-check-shield');
                $s.find('.text-muted.small').first().text('Locked: PO already uploaded.');

                lockAndTrigger($('#poNumber'), po.PONumber);
                lockAndTrigger($('#poDescription'), po.PODescription);
                $('#purchaseOrderForm input, #purchaseOrderForm textarea').prop('disabled', true);

                if (po.NewFileName) {
                    $('#uploadLabel').hide().after(`<div class="d-flex align-items-center p-3 border rounded bg-light mt-2">
                    <i class="bx ${getFileIcon(po.NewFileName)} fs-2 me-3"></i>
                    <div class="flex-grow-1 text-truncate"><div class="fw-bold">${po.OriginalFileName}</div><small class="text-success"><i class="bx bx-check"></i> Uploaded</small></div>
                    <button type="button" class="btn btn-sm btn-outline-primary DownloadPRFile" data-filename="${po.NewFileName}">DOWNLOAD</button></div>`);
                }
                $s.fadeIn();
            }

            // --- Section 6: Gate Entry Section ---
            if (gateEntries.length > 0) {
                const latestEntry = gateEntries[0];
                const $s = $('#gateEntrySection');
                const entryTime = latestEntry.liveTimestamp || latestEntry.EntryDateTime || "APR 29 | 2026, 11:01:37";
                isFormCompleted = true;

                $s.find('#autoBadge, #btnSaveGateEntry').addClass('d-none');
                $s.addClass('section-filled border-start border-success border-5 rounded-3');

                $s.find('#liveTimestamp').html(`<i class="bx bxs-lock-alt me-1"></i> ${entryTime}`)
                    .closest('.bg-light').removeClass('border-info').addClass('border-success bg-white shadow-sm');

                lockAndTrigger($s.find('#vehicleNumber'), latestEntry.vehicleNumber);
                lockAndTrigger($s.find('#driverIdentity'), latestEntry.driverIdentity);

                if (!$s.find('.status-check').length) {
                    $s.find('form .row').append(`
                    <div class="status-check col-12 mt-3 text-center">
                        <div class="p-2 bg-light-success text-success rounded border border-success-subtle shadow-sm animate__animated animate__pulse">
                            <i class="bx bxs-check-shield me-1"></i> <strong>Vehicle Entered:</strong> Verified at ${entryTime}
                        </div>
                    </div>`);
                }
                $s.fadeIn();
            }

            // --- Section 7: QA QC Confirmation Section ---
            if (QAQC?.length > 0) {
                const q = QAQC[0], $s = $('#QAQCApprovalSection');
                const displayTime = q.EntryDateTime ? new Date(q.EntryDateTime).toLocaleString('en-GB').toUpperCase() : "N/A";

                $s.addClass('section-filled border-start border-success border-5 rounded-3');
                applyVerifiedBadge($s, 'QUALITY CLEARED', 'bxs-check-shield');

                lockAndTrigger($('#qaqcRemarks'), q.Remarks);
                $s.find('input, textarea, select').prop('disabled', true);
                $s.find('#btnSaveQAQC, .auto-badge').addClass('d-none');

                const fileInfo = q.NewFileName || q.AttachmentPath;
                const fileHtml = fileInfo ? `
                <div class="d-flex align-items-center p-3 border rounded bg-light mt-2">
                    <i class="bx bxs-file-pdf fs-2 text-danger me-3"></i>
                    <div class="flex-grow-1 text-truncate">
                        <div class="fw-bold">${q.OriginalFileName || 'QAQC_Doc.pdf'}</div>
                        <small class="text-success"><i class="bx bx-check"></i> Verified</small>
                    </div>
                    <button type="button" class="btn btn-sm btn-outline-primary DownloadPRFile" data-filename="${fileInfo}">DOWNLOAD</button>
                </div>` : '<div class="p-2 bg-light text-muted small text-center border rounded">No attachment</div>';

                $s.find('.upload-wrapper').html(fileHtml);

                if (!$s.find('.status-check').length) {
                    $s.find('form .row').append(`
                    <div class="status-check col-12 mt-3 text-center">
                        <div class="p-2 bg-light-success text-success rounded border border-success-subtle shadow-sm">
                            <i class="bx bxs-check-shield me-1"></i> 
                            <strong>Verified:</strong> User ${q.EntryByUserId} | ${displayTime}
                        </div>
                    </div>`);
                }

                $s.fadeIn();
            }

            // --- Section 8: GRN Entry Section ---
            if (GRNRecord?.length > 0) {
                const grn = GRNRecord[0], $gs = $('#grnConfirmationSection');

                $gs.removeClass('d-none').addClass('section-filled border-start border-success border-5 rounded-3');
                $gs.find('.text-muted.small').first().html('<i class="bx bx-lock-alt"></i> Locked: GRN recorded.');

                if (typeof applyVerifiedBadge === "function") applyVerifiedBadge($gs, 'VERIFIED', 'bxs-check-shield');

                lockAndTrigger($('#grnNumber'), grn.grnNumber);
                lockAndTrigger($('#paymentDueDate'), grn.paymentDueDate?.split('T')[0]);
                lockAndTrigger($('#receivedQuantity'), grn.receivedQuantity);
                lockAndTrigger($('#grnRemarks'), grn.Remarks);

                $('#grnConfirmationForm input, #grnConfirmationForm textarea').prop('disabled', true);
                $gs.fadeIn();
            }

            // --- Section 9:Payment Section ---
            if (PaymentRecord?.length > 0) {
                const p = PaymentRecord[0], $s = $('#PaymentSchedulingSection');
                const displayTime = p.created_at ? new Date(p.created_at).toLocaleString('en-GB').toUpperCase() : "N/A";

                $s.addClass('section-filled border-start border-success border-5 rounded-3');
                applyVerifiedBadge($s, 'PAYMENT VERIFIED', 'bxs-badge-check');

                lockAndTrigger($('#amountReceived'), p.AmountReceived);
                lockAndTrigger($('#paymentRemarks'), p.PaymentRemarks);

                $s.find('input, textarea, select').prop('disabled', true);
                $s.find('#btnSavePayment, .auto-badge').addClass('d-none');

                const fileInfo = p.AttachmentPath;
                const fileHtml = fileInfo ? `
                <div class="d-flex align-items-center p-3 border rounded bg-light mt-2">
                    <i class="bx bxs-file-image fs-2 text-primary me-3"></i>
                    <div class="flex-grow-1 text-truncate">
                        <div class="fw-bold">${fileInfo.split('/').pop() || 'Payment_Proof.png'}</div>
                        <small class="text-success"><i class="bx bx-check"></i> Proof Attached</small>
                    </div>
                    <button type="button" class="btn btn-sm btn-outline-primary DownloadPRFile" data-filename="${fileInfo}">DOWNLOAD</button>
                </div>` : '<div class="p-2 bg-light text-muted small text-center border rounded">No payment proof uploaded</div>';

                $s.find('.upload-wrapper').html(fileHtml);

                if (!$s.find('.status-check').length) {
                    $s.find('form .row').append(`
                    <div class="status-check col-12 mt-3 text-center">
                        <div class="p-2 bg-light-success text-success rounded border border-success-subtle shadow-sm">
                            <i class="bx bxs-check-circle me-1"></i> 
                            <strong>Payment Logged:</strong> User ${p.createdby} | ${displayTime}
                        </div>
                    </div>`);
                }

                $s.fadeIn();
            }


 
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

    let label = $el.closest('.form-group, .mb-3, .col-md, .col-md-5, .col-md-2, td').find('label').first().text().replace('*', '').trim();
    if (!label || label === "") {
        const colIndex = $el.closest('td').index();
        label = $el.closest('table').find('thead th').eq(colIndex).text().trim() || "This field";
    }

    let isFieldValid = true;
    let errorMsg = "";

    if (type === 'file') {
        isFieldValid = $el[0].files && $el[0].files.length > 0;
        errorMsg = `Please upload the required ${label}.`;
    }
    else if ($el.is(':checkbox')) {
        isFieldValid = $el.is(':checked');
        errorMsg = `Please check ${label}.`;
    }
    else if ($el.is(':radio')) {
        const name = $el.attr('name');
        isFieldValid = $(`input[name="${name}"]:checked`).length > 0;
        errorMsg = `Please select ${label}.`;
    }
    else if (!val || (typeof val === 'string' && val.trim() === "") || (Array.isArray(val) && val.length === 0)) {
        isFieldValid = false;
        errorMsg = `${label} is required.`;
    }
    else if (type === 'email' && !/^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$/.test(val)) {
        isFieldValid = false;
        errorMsg = `Please enter a valid email address for ${label}.`;
    }
    else if (type === 'number' && parseFloat(val) <= 0) {
        isFieldValid = false;
        errorMsg = `${label} must be greater than zero.`;
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
    let validatedRadioNames = []; 

    $(selector).find('input, select, textarea').not(":disabled, .select2-search__field").each(function () {
        const $el = $(this);

        if (!$el.is(':visible') && !$el.hasClass('select2-hidden-accessible') && $el.attr('type') !== 'file') {
            return;
        }

        if ($el.is(':radio')) {
            const name = $el.attr('name');

            if (validatedRadioNames.indexOf(name) > -1) return;

            const $group = $(`input[name="${name}"]`);
            const isAnyChecked = $group.is(':checked');

            if (!isAnyChecked) {
                sectionValid = false;
                if (!firstErrorMsg) firstErrorMsg = "Please select at least one Bank as a winner.";
                $group.addClass('is-invalid').removeClass('is-valid');
            } else {
                $group.addClass('is-valid').removeClass('is-invalid');
            }

            validatedRadioNames.push(name); 
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
function validateProcurementRequest() {
        return validateSection('#requestorSection');
}
function validateRFQIssuance() {
        return validateSection('#RFQGenerationSection');
}
function validateRFQSubmission() {
    return validateSection('#RFQSubmissionSection');
}
function validateVendorEvaluation() {
    return validateSection('#VendorEvaluationSection');
}
function validatepurchaseOrder() {
    return validateSection('#purchaseOrderSection');
}
function validateGateEntry() {
    return validateSection('#gateEntrySection');
}
function validateQAQCSection() {
    return validateSection('#QAQCApprovalSection');
}
function validateGRNSection() {
    return validateSection('#grnConfirmationSection');
}
function validatePaymentSection() {
    return validateSection('#PaymentSchedulingSection');
}
function showValidationErrorAlert(msg) {
        Swal.fire({
            icon: 'error',
            title: 'Required Field Missing',
            text: msg,
            confirmButtonColor: '#0d6efd'
        });
    }





function mynextsetp(btn) {
    var save = $(btn).attr('data-save') === "true";
    var move = $(btn).attr('data-move') === "true";
    var actionId = $(btn).attr('data-id');
    var assignmenttype = $(btn).attr('data-assignmenttype');
    var dynamicfunction = $(btn).attr('data-dynamicfunction');
    var buttonText = $(btn).text();

    if (save && move) {

        if (UserDetails.data.RoleID === 1005) {
            if (validateProcurementRequest()) {

                Swal.fire({
                    title: 'Do you want to save?',
                    showDenyButton: true,
                    showCancelButton: false,
                    confirmButtonText: 'Yes',
                    denyButtonText: 'No',
                }).then((result) => {
                    if (result.isConfirmed) {
                        $('#requestDate').prop('disabled', false);
                        formrewrite('intiateform');
                        var formElement = $('#intiateform')[0];
                        var Data = new FormData(formElement);
                        $('.item-row').each(function (index, element) {
                            var itemFile = $(element).find('input[type="file"]')[0].files[0];
                            if (itemFile) {
                                Data.append(`items[${index}].file`, itemFile);
                            }
                        });
                        Data.append('instanceid', instanceid);

                        new APICALL(GetGlobalURL('Base', 'SavePurchaseInitiateRequest'), 'POST', Data, true, true).FETCH((result, error) => {
                            if (result) {
                                var workflowMove = {
                                    instanceid: result.data[0].instanceid,
                                    actionid: actionId,
                                    dynamicfunction: dynamicfunction,
                                    assignmenttype: assignmenttype,
                                    comment: $("#remarks").val()
                                };
                                var urlEncodedData = Object.keys(workflowMove)
                                    .map(key => encodeURIComponent(key) + '=' + encodeURIComponent(workflowMove[key] ?? ''))
                                    .join('&');

                                new APICALL(GetGlobalURL('Base', 'MoveWorkflow'), 'POST', urlEncodedData, false, false, 'application/x-www-form-urlencoded').FETCH((result, error) => {
                                    if (result) {
                                        Swal.fire({
                                            icon: 'success',
                                            title: 'Success',
                                            text: 'Workflow moved successfully!',
                                            timer: 2000,
                                            showConfirmButton: false
                                        }).then(() => {
                                            const defaultForm = document.getElementById("defaultform").value;
                                            window.location.href = "/" + defaultForm;
                                        });
                                    }
                                    if (error) {
                                        Swal.fire({
                                            icon: 'error',
                                            title: 'Error...',
                                            text: error.data.responseText,
                                        });
                                    }
                                });

                            }
                            if (error) {
                                Swal.fire({
                                    icon: 'error',
                                    title: 'Error...',
                                    text: error.data.responseText,
                                });
                            }
                        });

                        backtoorignalform('intiateform');
                    }
                });
            }
        }

        else if (UserDetails.data.RoleID === 1010) {
            if (validateRFQIssuance()) {
                Swal.fire({
                    title: 'Do you want to save this?',
                    showDenyButton: true,
                    confirmButtonText: 'Save',
                    denyButtonText: 'No',
                }).then((result) => {
                    if (result.isConfirmed) {

                        Swal.fire({
                            title: 'Processing Bid...',
                            text: 'Sending Bid notifications to selected banks. Please wait...',
                            allowOutsideClick: false,
                            didOpen: () => {
                                Swal.showLoading();
                            }
                        });

                        let formData = new FormData();
                        const fileInput = document.getElementById("fileAttachment");
                        if (fileInput.files.length > 0) {
                            formData.append("file", fileInput.files[0]);
                        }

                        formData.append("category", 1);
                        formData.append("categoryName", 'Bank');
                        formData.append("startDate", document.getElementById("dtStart").value);
                        formData.append("endDate", document.getElementById("dtEnd").value);
                        formData.append("description", document.getElementById("txtDescription").value.trim());
                        formData.append('reissuanceduration', document.getElementById("txtReIssuanceDays").value ?? 1);
                        formData.append('reissuancecycle', 1);
                        formData.append('instanceid', instanceid);

                        const selectedVendors = $("#ddlVendorsSelect").select2('data');
                        selectedVendors.forEach(vendor => {
                            formData.append("vendors", vendor.id);
                            formData.append("vendorEmails", $(vendor.element).data('email'));
                            formData.append("vendorMobiles", $(vendor.element).data('mobile'));
                        });

                        new APICALL(GetGlobalURL('Base', 'SubmitRFQIssuancetoVendorsRequest'), 'POST', formData, true, true).FETCH((result, error) => {
                            if (result) {
                                var workflowMove = {
                                    instanceid: result.data[0].instanceid,
                                    actionid: actionId,
                                    dynamicfunction: dynamicfunction,
                                    assignmenttype: assignmenttype,
                                    comment: $("#remarks").val()
                                };
                                var urlEncodedData = Object.keys(workflowMove)
                                    .map(key => encodeURIComponent(key) + '=' + encodeURIComponent(workflowMove[key] ?? ''))
                                    .join('&');

                                new APICALL(GetGlobalURL('Base', 'MoveWorkflow'), 'POST', urlEncodedData, false, false, 'application/x-www-form-urlencoded').FETCH((result, error) => {

                                    Swal.close();

                                    if (result) {
                                        Swal.fire({
                                            icon: 'success',
                                            title: 'Success',
                                            text: 'Workflow moved successfully!',
                                            timer: 2000,
                                            showConfirmButton: false
                                        }).then(() => {
                                            const defaultForm = document.getElementById("defaultform").value;
                                            window.location.href = "/" + defaultForm;
                                        });
                                    }
                                    if (error) {
                                        Swal.fire({
                                            icon: 'error',
                                            title: 'Error...',
                                            text: error.data?.responseText || 'Failed to move workflow',
                                        });
                                    }
                                });

                            } else if (error) {
                                Swal.close();
                                Swal.fire({
                                    icon: 'error',
                                    title: 'Error...',
                                    text: error.data?.responseText || 'Failed to submit RFQ',
                                });
                            }
                        });
                    }
                });
            }
        }

        else if (UserDetails.data.RoleID === 1019) {
            if (validateVendorEvaluation()) {
                const $selectedRadio = $('input[name="vendorWinner"]:checked');
                const $row = $selectedRadio.closest('tr');

                const winnerVendorId = $selectedRadio.val();
                const winnerName = $row.find('td').eq(3).text().trim();
                const winnerEmail = $row.find('td').eq(5).text().trim();
                const winnerPrice = $row.find('.text-success').text().trim();
                const attachedFileName = $row.find('.DownloadPRFile').attr('data-filename');
                const justification = $("#txtEvaluationRemarks").val().trim();

                Swal.fire({
                    title: 'Confirm Selection',
                    html: `Are you sure you want to select <b>${winnerName}</b> as the winner?`,
                    icon: 'question',
                    showCancelButton: true,
                    confirmButtonColor: '#3085d6',
                    cancelButtonColor: '#d33',
                    confirmButtonText: 'Yes, Submit Evaluation',
                    cancelButtonText: 'Cancel'
                }).then((result) => {
                    if (result.isConfirmed) {
                        Swal.fire({
                            title: 'Processing...',
                            html: 'Please wait while we save the selection.',
                            allowOutsideClick: false,
                            didOpen: () => { Swal.showLoading(); }
                        });

                        let formData = new FormData();

                        formData.append("winnerVendorId", winnerVendorId);
                        formData.append("winnerName", winnerName);
                        formData.append("winnerEmail", winnerEmail);
                      //  formData.append("winnerPrice", winnerPrice.replace(/,/g, ''));
                        formData.append("winnerPrice",0);
                        formData.append("justification", justification);
                        formData.append("fileName", attachedFileName || "");
                        formData.append('instanceid', instanceid);

                        new APICALL(GetGlobalURL('Base', 'SubmitVendorSelection'), 'POST', formData, true, true).FETCH((result, error) => {
                            if (result) {
                                var workflowMove = {
                                    instanceid: result.data[0].instanceid,
                                    actionid: actionId,
                                    dynamicfunction: dynamicfunction,
                                    assignmenttype: assignmenttype,
                                    comment: justification
                                };

                                var urlEncodedData = Object.keys(workflowMove)
                                    .map(key => encodeURIComponent(key) + '=' + encodeURIComponent(workflowMove[key] ?? ''))
                                    .join('&');

                                new APICALL(GetGlobalURL('Base', 'MoveWorkflow'), 'POST', urlEncodedData, false, false, 'application/x-www-form-urlencoded').FETCH((res, err) => {
                                    Swal.close();
                                    if (res) {
                                        Swal.fire({ icon: 'success', title: 'Bank Selected!', timer: 2000, showConfirmButton: false })
                                            .then(() => { window.location.href = "/" + document.getElementById("defaultform").value; });
                                    }
                                });
                            } else {
                                Swal.close();
                                Swal.fire({ icon: 'error', title: 'Error', text: error?.data?.responseText || 'Submission failed' });
                            }
                        });
                    }
                });
            }
        }

    }

    else if (save) {
        SaveInitiatorDocument();
    }
    else if (move) {

        MoveMyRequest(actionId, dynamicfunction, assignmenttype, buttonText);
    }
}
function SaveInitiatorDocument() {

    if (UserDetails.data.RoleID === 1017) {
        if (validateRFQSubmission()) {

            Swal.fire({
                title: 'Do you want to save?',
                showDenyButton: true,
                showCancelButton: false,
                confirmButtonText: 'Yes',
                denyButtonText: 'No',
            }).then((result) => {

                if (result.isConfirmed) {

                    let formData = new FormData();

                    const fileInput = document.getElementById("fileRFQDoc");

                    if (fileInput != null && fileInput.files.length > 0) {
                        formData.append("file", fileInput.files[0]);
                    }

                    formData.append("RFQRemarks", document.getElementById("txtRFQRemarks").value);
                    formData.append("instanceid", instanceid);

                    // TABLE DATA
                    let quotationDetails = [];

                    $("#investmentTableBody tr").each(function () {

                        let tds = $(this).find("td");

                        let amount = $(tds[0]).find("input").val();
                        let month3 = formatDecimal($(tds[1]).find("input").val().replace("%", ""));
                        let month6 = formatDecimal($(tds[2]).find("input").val().replace("%", ""));
                        let month12 = formatDecimal($(tds[3]).find("input").val().replace("%", ""));

                        quotationDetails.push({
                            QuotedPrice: amount,

                            Details: [
                                {
                                    tenorid: 2,
                                    value: month3
                                },
                                {
                                    tenorid: 3,
                                    value: month6
                                },
                                {
                                    tenorid: 4,
                                    value: month12
                                }
                            ]
                        });
                    });

                    formData.append("QuotationData", JSON.stringify(quotationDetails));

                    new APICALL(GetGlobalURL('Base', 'SubmitRFQsRequest'), 'POST', formData, true, true)
                        .FETCH((result, error) => {

                            if (result) {

                                Swal.fire({
                                    icon: 'success',
                                    title: 'Saved Successfully!',
                                    timer: 2000,
                                    showConfirmButton: false
                                }).then(() => {

                                    const defaultForm = document.getElementById("defaultform").value;
                                    window.location.href = "/" + defaultForm;
                                });
                            }

                            if (error) {

                                Swal.fire({
                                    icon: 'error',
                                    title: 'Error...',
                                    text: error.data.responseText,
                                });
                            }
                        });
                }
            });


        }
    }
}

function MoveMyRequest(actionId, dynamicfunction, assignmenttype, buttonText) {

    Swal.fire({
        title: 'Do you want to move the workflow?',
        showDenyButton: true,
        showCancelButton: false,
        confirmButtonText: 'Yes',
        denyButtonText: 'No',
    }).then((result) => {
        if (result.isConfirmed) {
            var workflowMove = {
                instanceid: instanceid,
                actionid: actionId,
                dynamicfunction: dynamicfunction,
                assignmenttype: assignmenttype,
                comment: $("#remarks").val()
            };
            var urlEncodedData = Object.keys(workflowMove)
                .map(key => encodeURIComponent(key) + '=' + encodeURIComponent(workflowMove[key] ?? ''))
                .join('&');

             new APICALL(GetGlobalURL('Base', 'MoveWorkflow'), 'POST', urlEncodedData, false, false, 'application/x-www-form-urlencoded').FETCH((result, error) => {
                 if (result) {
                     Swal.fire({
                         icon: 'success',
                         title: 'Success...',
                         text: 'Workflow moved successfully!',
                         timer: 2000,
                         showConfirmButton: false
                     }).then(() => {
                         const defaultForm = document.getElementById("defaultform").value;
                         window.location.href = "/" + defaultForm;
                     });
                 }
                 if (error) {
                     Swal.fire({
                         icon: 'error',
                         title: 'Error...',
                         text: error.data.responseText,
                     });
                 }
             });
           
        }
    });
}

const TIMER_DURATION = 60 * 1000;
const STORAGE_KEY = "timerEndTime";

let timerInterval = null;

function startOrResumeTimer() {
    $('#timerdiv').removeClass('d-none');

    let endTime = localStorage.getItem(STORAGE_KEY);

    if (!endTime) {
        endTime = Date.now() + TIMER_DURATION;
        localStorage.setItem(STORAGE_KEY, endTime);
    } else {
        endTime = parseInt(endTime);
    }

    const timerElement = document.getElementById("timer");

    //  old interval kill karo (important)
    if (timerInterval) {
        clearInterval(timerInterval);
    }

    function updateTimer() {
        const remaining = endTime - Date.now();

        // YAHAN PE RAKHNA HAI
        if (remaining <= 0) {
            clearInterval(timerInterval);
            timerElement.innerText = "00:00";
            localStorage.removeItem(STORAGE_KEY);

            // enable buttons
            document.querySelectorAll(".toggleChild").forEach(btn => {
                btn.disabled = false;
            });
            document.querySelectorAll('input[name="vendorWinner"]').forEach(r => {
                r.disabled = false;
            });
            document.querySelectorAll('label.btn').forEach(l => {
                l.classList.remove('disabled');
            });
            document.querySelectorAll(".DownloadPRFile").forEach(btn => {
                btn.disabled = false;
            });
            document.getElementById("txtEvaluationRemarks").disabled = false;
            $(".savebutton").prop('disabled', false);
            return;
        }

        const seconds = Math.floor(remaining / 1000);
        const mins = String(Math.floor(seconds / 60)).padStart(2, "0");
        const secs = String(seconds % 60).padStart(2, "0");

        timerElement.innerText = `${mins}:${secs}`;
    }

    updateTimer(); // immediate update (no delay wait)

    timerInterval = setInterval(updateTimer, 1000);
}