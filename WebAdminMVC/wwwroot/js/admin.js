// ==========================
// TABLE SEARCH
// ==========================
(function () {
    const input = document.querySelector('[data-table-search]');
    const table = document.querySelector('[data-table]');
    if (!input || !table) return;

    input.addEventListener('input', () => {
        const q = input.value.toLowerCase().trim();
        const rows = table.querySelectorAll('tbody tr');

        rows.forEach(r => {
            const text = r.innerText.toLowerCase();
            r.style.display = text.includes(q) ? '' : 'none';
        });
    });
})();


// ==========================
// DELETE MODAL
// ==========================
function confirmDelete(id, name) {
    document.getElementById("deleteId").value = id;

    document.getElementById("amenityName").innerText = name;

    document.getElementById("deleteForm").action = "/Amenities/Delete";

    document.getElementById("deleteModal").style.display = "flex";
}

function closeModal() {
    document.getElementById("deleteModal").style.display = "none";
}

window.onclick = function (e) {
    const modal = document.getElementById("deleteModal");

    if (e.target === modal) {
        modal.style.display = "none";
    }
}