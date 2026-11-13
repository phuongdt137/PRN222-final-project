(() => {
    const form = document.getElementById('attendance-form');
    if (!form || form.dataset.editable !== 'true') return;
    const selects = [...form.querySelectorAll('.attendance-status')];
    const counter = document.getElementById('attendance-count');
    const validate = select => {
        const leave = select.closest('tr').querySelector('.attendance-leave');
        const excused = select.value === 'Excused';
        leave.disabled = !excused;
        leave.required = excused;
        if (!excused) leave.value = '';
        select.setCustomValidity(select.value === 'Absent' && leave.dataset.hasApproved === 'true'
            ? 'Học sinh có đơn nghỉ đã duyệt; không được chọn vắng không phép.' : '');
    };
    const update = () => {
        selects.forEach(validate);
        counter.textContent = `Đã chọn ${selects.filter(s => s.value).length}/${selects.length} học sinh`;
    };
    selects.forEach(select => select.addEventListener('change', update));
    document.getElementById('mark-present').addEventListener('click', () => {
        selects.filter(s => !s.value).forEach(s => { s.value = 'Present'; });
        update();
    });
    const checkDeadline = () => {
        if (Date.now() < Date.parse(form.dataset.deadline)) return false;
        form.querySelectorAll('input, select, textarea, button').forEach(e => { e.disabled = true; });
        const message = document.getElementById('attendance-deadline');
        message.textContent = 'Đã đến 00:00 ngày kế tiếp (giờ Việt Nam). Không thể nhập/sửa điểm danh. Giáo viên chỉ được mở các tiết trong hôm nay.';
        message.className = 'alert alert-info';
        return true;
    };
    form.addEventListener('submit', event => {
        if (checkDeadline()) { event.preventDefault(); return; }
        update();
        if (!form.reportValidity()) event.preventDefault();
    });
    update();
    if (!checkDeadline()) {
        const timer = setInterval(() => { if (checkDeadline()) clearInterval(timer); }, 1000);
    }
})();
