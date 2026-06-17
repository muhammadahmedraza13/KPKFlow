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
                console.error("Failed to fetch bank rates:", error);
                $('#bankratestbody').html(`
                    <tr>
                        <td colspan="6" class="text-danger py-4">Failed to load data from server.</td>
                    </tr>
                `);
                reject(error);
                return;
            }

            // Fallback to result if result.data does not exist explicitly
            const bankList = result.data || result || [];

            if (bankList.length === 0) {
                $('#bankratestbody').html('<tr><td colspan="6" class="text-muted py-4">No data available.</td></tr>');
                resolve([]);
                return;
            }

            let groupedBanks = {};
            $('#date').val(bankList[0].createddate)
            $.each(bankList, function (index, item) {

                let bankName = item.BusinessName || item.businessName || 'Unknown Bank';
                let amount = item.amount || '-';
                let tenor = (item.tenorname || item.TenorName || '').toString().trim().toUpperCase();
                let rateVal = parseFloat(item.rate || item.Rate);

                if (isNaN(rateVal)) {
                    rateVal = null; // Ignore invalid values safely
                }

                if (!groupedBanks[bankName]) {
                    groupedBanks[bankName] = {
                        bankName: bankName,
                        amount: amount,
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

            let rates3m = [];
            let rates6m = [];
            let rates12m = [];
            let rowsHtml = '';
            let serialNumber = 1;

            $.each(groupedBanks, function (bankName, bankData) {
                if (bankData.rate3m !== null) rates3m.push(bankData.rate3m);
                if (bankData.rate6m !== null) rates6m.push(bankData.rate6m);
                if (bankData.rate12m !== null) rates12m.push(bankData.rate12m);

                rowsHtml += `
                    <tr>
                        <td>${serialNumber++}</td>
                        <td class="text-start ps-3">${bankData.bankName}</td>
                        <td>${bankData.amount !== null ? formatNumber(bankData.amount) : '-'}</td> 
                        <td>${bankData.rate3m !== null ? bankData.rate3m.toFixed(2) + '%' : '-'}</td>
                        <td>${bankData.rate6m !== null ? bankData.rate6m.toFixed(2) + '%' : '-'}</td>
                        <td>${bankData.rate12m !== null ? bankData.rate12m.toFixed(2) + '%' : '-'}</td>
                    </tr>
                `;
            });

            $('#bankratestbody').html(rowsHtml);

            const best3m = rates3m.length ? Math.min.apply(Math, rates3m) : 0;
            const best6m = rates6m.length ? Math.min.apply(Math, rates6m) : 0;
            const best12m = rates12m.length ? Math.min.apply(Math, rates12m) : 0;

            const highest3m = rates3m.length ? Math.max.apply(Math, rates3m) : 0;
            const highest6m = rates6m.length ? Math.max.apply(Math, rates6m) : 0;
            const highest12m = rates12m.length ? Math.max.apply(Math, rates12m) : 0;

            // DOM manipulation
            $('#best-3m').text(best3m.toFixed(2) + '%');
            $('#best-6m').text(best6m.toFixed(2) + '%');
            $('#best-12m').text(best12m.toFixed(2) + '%');

            $('#highest-3m').text(highest3m.toFixed(2) + '%');
            $('#highest-6m').text(highest6m.toFixed(2) + '%');
            $('#highest-12m').text(highest12m.toFixed(2) + '%');

            resolve(groupedBanks);
        });
    });
}
function formatNumber(num) {
    if (num == null || isNaN(num)) return '';
    return parseFloat(num).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}