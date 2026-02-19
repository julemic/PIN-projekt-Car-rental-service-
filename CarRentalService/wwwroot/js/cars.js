document.addEventListener("DOMContentLoaded", function () {
    var pickup = document.querySelector('input[name="Search.Pickup"]');
    var ret = document.querySelector('input[name="Search.Return"]');

    function syncMin() {
        if (pickup && ret && pickup.value)
            ret.min = pickup.value;
    }

    if (pickup) {
        pickup.addEventListener("change", syncMin);
    }

    syncMin();
});
