function updateDateDisplay(input, spanId, placeholder) {
    var span = document.getElementById(spanId);
    if (!span) return;
    if (input.value) {
        var d = new Date(input.value);
        span.textContent = d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
        span.classList.remove('is-placeholder');
    } else {
        span.textContent = placeholder;
        span.classList.add('is-placeholder');
    }
}

document.addEventListener("DOMContentLoaded", function () {
    var pickup = document.getElementById('pickup-input');
    var ret = document.getElementById('return-input');

    if (pickup) {
        updateDateDisplay(pickup, 'pickup-text', 'Day of Pick Up');
        pickup.addEventListener("change", function () {
            updateDateDisplay(pickup, 'pickup-text', 'Day of Pick Up');
            if (ret && pickup.value) ret.min = pickup.value;
        });
    }

    if (ret) {
        updateDateDisplay(ret, 'return-text', 'Day of Return');
        ret.addEventListener("change", function () {
            updateDateDisplay(ret, 'return-text', 'Day of Return');
        });
    }

    if (pickup && ret && pickup.value) ret.min = pickup.value;
});
