// هيلال ثيم - JavaScript للمشروع
document.addEventListener('DOMContentLoaded', function () {
    // التحكم بالاستجابة للشاشات الصغيرة
    function handleResponsive() {
        const width = window.innerWidth;
        const stars = document.querySelectorAll('.star');
        const crescent = document.querySelector('.crescent-design');
        const mainActions = document.querySelector('.main-actions');
        const responsiveActions = document.querySelectorAll('.responsive-actions');

        if (width <= 768) {
            // تعديلات للشاشات الصغيرة
            if (mainActions) mainActions.style.flexDirection = 'column';
            responsiveActions.forEach(action => {
                if (action) action.style.justifyContent = 'center';
            });
            // إخفاء النجوم في الشاشات الصغيرة
            stars.forEach(star => {
                star.style.display = 'none';
            });
            if (crescent) {
                crescent.style.width = '50px';
                crescent.style.height = '50px';
                crescent.style.left = '20px';
                crescent.style.top = '15px';
            }
        } else {
            // إعادة الضبط للشاشات الكبيرة
            if (mainActions) mainActions.style.flexDirection = 'row';
            // إظهار النجوم في الشاشات الكبيرة
            stars.forEach(star => {
                star.style.display = 'block';
            });
            if (crescent) {
                crescent.style.width = '60px';
                crescent.style.height = '60px';
                crescent.style.left = '30px';
                crescent.style.top = '20px';
            }
        }
    }

    // إظهار/إخفاء كلمة المرور
    function initPasswordToggle() {
        const toggleButtons = document.querySelectorAll('.password-toggle');
        toggleButtons.forEach(button => {
            button.addEventListener('click', function () {
                const passwordInput = this.parentElement.querySelector('input[type="password"]');
                if (passwordInput) {
                    const type = passwordInput.getAttribute('type') === 'password' ? 'text' : 'password';
                    passwordInput.setAttribute('type', type);
                    this.innerHTML = type === 'password' ? '<i class="fas fa-eye"></i>' : '<i class="fas fa-eye-slash"></i>';
                }
            });
        });
    }

    // تهيئة جميع المكونات
    function initTheme() {
        handleResponsive();
        initPasswordToggle();

        // إضافة تأثيرات للحقول
        const inputs = document.querySelectorAll('.hilal-input');
        inputs.forEach(input => {
            input.addEventListener('focus', function () {
                this.style.transform = 'translateY(-2px)';
            });

            input.addEventListener('blur', function () {
                this.style.transform = 'translateY(0)';
            });
        });
    }

    // استدعاء الدوال عند التحميل وعند تغيير حجم النافذة
    initTheme();
    window.addEventListener('resize', handleResponsive);
});