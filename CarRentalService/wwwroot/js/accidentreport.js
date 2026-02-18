document.addEventListener("DOMContentLoaded", function () {

    const check = document.getElementById("otherPartyCheck");
    const section = document.getElementById("otherPartySection");

    if (!check || !section) return;

    function toggleSection() {
        section.style.display = check.checked ? "block" : "none";
    }

    check.addEventListener("change", toggleSection);

    // inicijalno stanje
    toggleSection();
});
