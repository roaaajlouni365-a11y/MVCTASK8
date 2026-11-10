(() => {
    const rows = [...document.querySelectorAll('[data-line]')];
    const total = document.getElementById('request-total');
    if (!total) return;
    function update() {
        let cents = 0;
        for (const row of rows) {
            const quantity = Math.max(0, Number(row.querySelector('[data-quantity]').value) || 0);
            const subtotal = Math.round(Number(row.dataset.price) * 100) * quantity;
            row.querySelector('[data-subtotal]').textContent = (subtotal / 100).toFixed(2);
            cents += subtotal;
        }
        total.textContent = (cents / 100).toFixed(2);
    }
    rows.forEach(row => row.querySelector('[data-quantity]').addEventListener('input', update));
    update();
})();
