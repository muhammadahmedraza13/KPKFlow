$(document).ready(function () {
    GetFundName();
});
function GetFundName() {
    const $select = $('#ddlfundname').empty().append('<option value="" disabled selected>Select Fund</option>');

    return new Promise((resolve) => {
        new APICALL(GetGlobalURL('Base', 'GetFundName'), 'GET', '', true)
            .FETCH((result) => {
                if (result.data?.data) {
                    result.data.data.forEach(item => $select.append(new Option(item.Text, item.Id)));
                }
                resolve();
            });
    });
}

$("#ddlfundname").on("change", function () {
    GetBankRateByInstanceId($("#ddlfundname").val());
});
function GetBankRateByInstanceId(instanceid) {
    return new Promise((resolve, reject) => {
        new APICALL(GetGlobalURL('Base', 'GetBankRateByInstanceId?instanceid=' + instanceid), 'GET', '', true).FETCH((result, error) => {
            if (error) {
                $('#bankratestbody').html(`
                    <tr>
                        <td colspan="6" class="text-danger py-4">Failed to load data from server.</td>
                    </tr>
                `);
                reject(error);
                return;
            }

            const bankList = result.data || result || [];

            if (bankList.length === 0) {
                $('#bankratestbody').html('<tr><td colspan="6" class="text-muted py-4">No data available.</td></tr>');
                resolve([]);
                return;
            }

            let groupedBanks = {};

            $('#date').val(bankList[0].createddate);
            $('#targetAmount').val(bankList[0].targetAmount);

            // Dynamic Target Amount
            const targetVal = parseFloat(bankList[0].targetAmount) || 0;

            $.each(bankList, function (index, item) {
                let bankName = item.BusinessName || item.businessName || 'Unknown Bank';
                let userEmail = item.UserEmail || item.userEmail || '';
                let amount = item.amount || '-';
                let tenor = (item.tenorname || item.TenorName || '').toString().trim().toUpperCase();
                let rateVal = parseFloat(item.rate || item.Rate);
                let vendorId = parseInt(item.VendorID || item.vendorID || 0);

                // Read database status (0 = Email Sent, 1 = Vendor Updated, null = Not Sent)
                let emailStatus = item.isupdated !== undefined && item.isupdated !== null ? parseInt(item.isupdated) : null;

                if (isNaN(rateVal)) {
                    rateVal = null;
                }

                if (!groupedBanks[bankName]) {
                    groupedBanks[bankName] = {
                        bankName: bankName,
                        vendorId: vendorId,
                        userEmail: userEmail,
                        amount: amount,
                        emailStatus: emailStatus, // State manage karne ke liye status save kiya
                        rate3m: null,
                        rate6m: null,
                        rate12m: null
                    };
                }

                if (tenor === "3M") {
                    groupedBanks[bankName].rate3m = rateVal;
                } else if (tenor === "6M") {
                    groupedBanks[bankName].rate6m = rateVal;
                } else if (tenor === "1Y" || tenor === "12M") {
                    groupedBanks[bankName].rate12m = rateVal;
                }
            });

            // Trackers for the lowest (best) and highest rates
            let best3m = { rate: null, bank: '-' };
            let best6m = { rate: null, bank: '-' };
            let best12m = { rate: null, bank: '-' };

            let highest3m = { rate: null, bank: '-' };
            let highest6m = { rate: null, bank: '-' };
            let highest12m = { rate: null, bank: '-' };

            // PEHLE LOOP: Sirf Highest aur Lowest rates calculate karne ke liye
            $.each(groupedBanks, function (bankName, bankData) {
                trackExtremes(bankData.rate3m, bankName, best3m, highest3m);
                trackExtremes(bankData.rate6m, bankName, best6m, highest6m);
                trackExtremes(bankData.rate12m, bankName, best12m, highest12m);
            });

            let rowsHtml = '';
            let serialNumber = 1;

            // DOSRA LOOP: Rows generate karne aur button/status badge lagane ke liye
            $.each(groupedBanks, function (bankName, bankData) {
                let matchLabel = '';
                let highestRatesToMatch = [];

                // 1. 3M Tenor Check
                if (bankData.rate3m !== null && highest3m.rate !== null && bankData.rate3m < highest3m.rate) {
                    highestRatesToMatch.push(`3M: ${highest3m.rate.toFixed(2)}%`);
                }

                // 2. 6M Tenor Check
                if (bankData.rate6m !== null && highest6m.rate !== null && bankData.rate6m < highest6m.rate) {
                    highestRatesToMatch.push(`6M: ${highest6m.rate.toFixed(2)}%`);
                }

                // 3. 12M Tenor Check
                if (bankData.rate12m !== null && highest12m.rate !== null && bankData.rate12m < highest12m.rate) {
                    highestRatesToMatch.push(`12M: ${highest12m.rate.toFixed(2)}%`);
                }

                // Agar bank ka koi bhi rate highest se kam mila
                if (bankData.emailStatus == 1) {
                    // Agar status 1 hai (Vendor updated back), toh chahe highestRatesToMatch khali hi kyun na ho, yehi status dikhega
                    matchLabel = `<span class="badge bg-success ms-2 py-1 px-2" style="font-size: 0.75rem;">Vendor Updated</span>`;
                }
                else if (highestRatesToMatch.length > 0) {
                    let ratesString = highestRatesToMatch.join(', ');

                    if (bankData.emailStatus === 0) {
                        // Agar email status 0 hai (Sent) aur abhi tak update nahi kiya
                        matchLabel = `<span class="badge bg-info text-dark ms-2 py-1 px-2" style="font-size: 0.75rem;">Email Sent</span>`;
                    }
                    else {
                        // Agar status null/empty hai aur rates match karne hain (Standard Trigger Button)
                        matchLabel = `
                        <button class="btn btn-warning btn-sm match-bidder-text btnemail ms-2" 
                                type="button" 
                                data-bank="${bankName}" 
                                data-email="${bankData.userEmail}" 
                                data-vendorid="${bankData.vendorId}" 
                                data-rates="${ratesString}">
                            Match the highest bidder
                        </button>`;
                    }
                }
               

                rowsHtml += `
                    <tr>
                        <td>${serialNumber++}</td>
                        <td class="text-start ps-3">
                            ${bankData.bankName}
                            ${matchLabel}
                        </td>
                        <td>${bankData.amount !== null ? formatNumber(bankData.amount) : '-'}</td> 
                        <td>${bankData.rate3m !== null ? bankData.rate3m.toFixed(2) + '%' : '-'}</td>
                        <td>${bankData.rate6m !== null ? bankData.rate6m.toFixed(2) + '%' : '-'}</td>
                        <td>${bankData.rate12m !== null ? bankData.rate12m.toFixed(2) + '%' : '-'}</td>
                    </tr>
                `;
            });

            $('#bankratestbody').html(rowsHtml);

            $('#best-3m').html(formatDisplay(best3m));
            $('#best-6m').html(formatDisplay(best6m));
            $('#best-12m').html(formatDisplay(best12m));

            $('#highest-3m').html(formatDisplay(highest3m));
            $('#highest-6m').html(formatDisplay(highest6m));
            $('#highest-12m').html(formatDisplay(highest12m));

            resolve(groupedBanks);
        });
    });
}
function trackExtremes(rate, bankName, bestObj, highestObj) {
    if (rate !== null) {
        if (bestObj.rate === null || rate < bestObj.rate) {
            bestObj.rate = rate;
            bestObj.bank = bankName;
        }
        if (highestObj.rate === null || rate > highestObj.rate) {
            highestObj.rate = rate;
            highestObj.bank = bankName;
        }
    }
}
function formatDisplay(obj) {
    if (obj.rate === null || obj.bank === '-') return '-';
    return `${obj.bank} (${obj.rate.toFixed(2)}%)`;
}
$(document).on('click', '.btnemail', function () {
    Swal.fire({
        title: 'Do you want to send this email?',
        showDenyButton: true,
        confirmButtonText: 'Save',
        denyButtonText: 'No',
    }).then((result) => {
        if (result.isConfirmed)
        {

            Swal.fire({
                title: 'Processing Email...',
                text: 'Sending Email to selected banks. Please wait...',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });
            let bankName = $(this).attr('data-bank');
            let userEmail = $(this).attr('data-email');
            let vendorId = $(this).attr('data-vendorId');
            let highestRates = $(this).attr('data-rates');
            let instanceId = $("#ddlfundname").val();
            let ratesArray = [];
            if (highestRates) {
                ratesArray = highestRates.split(',').map(r => r.trim());
            }
            let queryParams = $.param({
                vendorid: vendorId,
                bankname: bankName,
                bankemail: userEmail,
                instanceId: instanceId
            });

            $.each(ratesArray, function (index, rateVal) {
                queryParams += `&rate=${encodeURIComponent(rateVal)}`;
            });
            const apiEndpoint = GetGlobalURL('Base', 'Matchthehighestbidder?' + queryParams);
            new APICALL(apiEndpoint, 'GET', '', true).FETCH((result, error) => {
                if (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'System could not send the email matching request',
                        timer: 2000,
                        showConfirmButton: false
                    });
                    return;
                }
                Swal.fire({
                    icon: 'success',
                    title: 'Email invitation sent to ' + bankName + ' successfully!',
                    timer: 2000,
                    showConfirmButton: false
                });
                GetBankRateByInstanceId($("#ddlfundname").val());
            });
        }
    });
});
