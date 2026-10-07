(function () {
    function loadNotes(customerId) {
        var target = document.getElementById('customer-notes');
        if (!target) { return; }
        fetch('/api/audit/comments?target=' + encodeURIComponent('customer:' + customerId))
            .then(function (r) { return r.json(); })
            .then(function (entries) {
                var html = '';
                entries.forEach(function (e) {
                    html += '<p><b>' + e.actor + '</b>: ' + e.detail + '</p>';
                });
                target.innerHTML = html;
            });
    }

    var notes = document.getElementById('customer-notes');
    if (notes) { loadNotes(notes.getAttribute('data-customer')); }

    var table = document.getElementById('audit-table');
    if (table) {
        table.addEventListener('click', function (ev) {
            var cell = ev.target.closest('td');
            if (cell) { cell.classList.toggle('selected'); }
        });
    }

    window.showBanner = function (text) {
        var banner = document.createElement('div');
        banner.innerHTML = text;
        document.body.prepend(banner);
    };

    var params = new URLSearchParams(window.location.search);
    if (params.get('banner')) { window.showBanner(params.get('banner')); }
})();
