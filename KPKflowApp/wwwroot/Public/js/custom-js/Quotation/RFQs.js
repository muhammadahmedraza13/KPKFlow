var wfcode = "BR";
var refreshInterval;
var activeTimers = [];
var serverTimeOffset = 0;  

$(document).ready(function () {
    loadRFQData();

    refreshInterval = setInterval(function () {
        loadRFQData();
    }, 3000);

});

function loadRFQData() {
    new APICALL(GetGlobalURL('Base', 'GetRFQsTasks') + '?wfcode=' + wfcode, 'GET', '', true).FETCH((result, error) => {
        if (result && result.data) {
            if (result.currentTime) {
                var serverTime = parseTargetDate(result.currentTime);
                var localTime = Date.now();
                serverTimeOffset = serverTime - localTime;
            }

            updateCounters(result.data);
            activeTimers.forEach(id => clearInterval(id));
            activeTimers = [];

            renderTable(result.data);
        }
    });
}

function getAdjustedNow() {
    return Date.now() + serverTimeOffset;
}

function updateCounters(data) {
    $('#count-pending').text(data.filter(x => x.status === 'Pending').length);
    $('#count-submitted').text(data.filter(x => x.status === 'Submitted').length);
    $('#count-selected').text(data.filter(x => x.status === 'Selected').length);
    $('#count-expired').text(data.filter(x => x.status === 'Not Submitted').length);
}

function renderTable(data) {
    if ($.fn.DataTable.isDataTable('#rfqDataTable')) {
        $('#rfqDataTable').DataTable().destroy();
    }

    const $tbody = $('#rfqBody');
    let htmlBuffer = "";

    data.forEach((item, i) => {
        const isPending = item.status === "Pending";
        const statusClass = item.status.toLowerCase().replace(/\s+/g, "-");
        const initialTimerText = isPending ? getTimerString(item.endTime) : "Closed";

        const action = item.status === 'Not Submitted'
            ? `<i class='bx bx-lock text-muted fs-4'></i>`
            : `<a href="${item.viewpage}" class="fs-4 ${item.status === 'Selected' ? 'text-info' : 'text-primary'}">
                <i class='bx ${isPending ? 'bx-edit' : 'bx-show'}'></i></a>`;

        htmlBuffer += `
            <tr>
                <td><span class="fw-bold">${item.instanceId}</span></td>
                <td>${item.category}</td>
                <td><small>${item.endTime}</small></td>
                <td>
                    ${isPending
                ? `<span id="timer-${i}" class="timer-text fw-mono">${initialTimerText}</span>`
                : `<span class="text-muted small">Closed</span>`}
                </td>
                <td><span class="status-badge status-${statusClass}">${item.status}</span></td>
                <td class="text-center">${action}</td>
            </tr>`;
    });

    $tbody.html(htmlBuffer);

    $('#rfqDataTable').DataTable({
        "pageLength": 10,
        "searching": true,
        "stateSave": true,
        "order": [[0, "desc"]]
    });

    data.forEach((item, i) => {
        if (item.status === "Pending") {
            startCountdown(i, item.endTime);
        }
    });
}

function parseTargetDate(endStr) {
    if (!endStr) return NaN;

    const p = endStr.match(/\d+/g);
    if (p && p.length >= 5) {
        let y = parseInt(p[0]);
        let mo = parseInt(p[1]) - 1;
        let d = parseInt(p[2]);
        let h = parseInt(p[3]);
        let mi = parseInt(p[4]);
        let s = p[5] ? parseInt(p[5]) : 0;

        if (/PM/i.test(endStr) && h < 12) h += 12;
        if (/AM/i.test(endStr) && h === 12) h = 0;

        return new Date(y, mo, d, h, mi, s).getTime();
    }
    return Date.parse(endStr);
}

function getTimerString(endStr) {
    const target = parseTargetDate(endStr);
    const diff = target - getAdjustedNow();

    if (isNaN(target) || diff <= 0) return "00:00:00";

    const h = Math.floor(diff / 3600000);
    const m = Math.floor((diff % 3600000) / 60000);
    const s = Math.floor((diff % 60000) / 1000);

    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
}

function startCountdown(index, endStr) {
    const target = parseTargetDate(endStr);
    const $el = $(`#timer-${index}`);

    if (isNaN(target)) return $el.text("--:--:--");

    const timerId = setInterval(() => {
        const diff = target - getAdjustedNow(); 

        if (diff <= 0) {
            clearInterval(timerId);
            $el.text("00:00:00").css("color", "red");
            return;
        }

        const h = Math.floor(diff / 3600000);
        const m = Math.floor((diff % 3600000) / 60000);
        const s = Math.floor((diff % 60000) / 1000);

        $el.text(`${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`);

        if (diff < 1800000) {
            $el.addClass('timer-urgent text-danger fw-bold');
        }
    }, 1000);

    activeTimers.push(timerId);
}