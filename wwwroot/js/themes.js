// themes.js - إدارة نظام الثيمات
class ThemeManager {
    constructor() {
        this.currentTheme = localStorage.getItem('selectedTheme') || 'hilal';
        this.init();
    }

    init() {
        this.applyTheme(this.currentTheme);
        this.bindEvents();
    }

    applyTheme(themeName) {
        document.documentElement.setAttribute('data-theme', themeName);
        localStorage.setItem('selectedTheme', themeName);
        this.currentTheme = themeName;

        // تحديث الثيم النشط في القائمة
        this.updateActiveTheme(themeName);

        // إرسال إلى السيرفر لحفظ التفضيل
        this.saveThemePreference(themeName);
    }

    bindEvents() {
        // أحداث اختيار الثيم
        document.querySelectorAll('.theme-selector').forEach(selector => {
            selector.addEventListener('click', (e) => {
                e.preventDefault();
                const theme = e.target.closest('.theme-selector').dataset.theme;
                this.applyTheme(theme);
                this.showThemeNotification(theme);
            });
        });
    }

    updateActiveTheme(themeName) {
        // إزالة النشط من جميع الثيمات
        document.querySelectorAll('.theme-selector').forEach(item => {
            item.classList.remove('active');
        });

        // إضافة النشط للثيم المحدد
        const activeTheme = document.querySelector(`[data-theme="${themeName}"]`);
        if (activeTheme) {
            activeTheme.classList.add('active');
        }
    }

    async saveThemePreference(theme) {
        try {
            const response = await fetch('/Account/SaveTheme', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ theme: theme })
            });

            if (!response.ok) {
                throw new Error('Failed to save theme');
            }
        } catch (error) {
            console.error('Error saving theme:', error);
        }
    }

    showThemeNotification(theme) {
        const themeNames = {
            'hilal': 'الهلال',
            'nasr': 'النصر',
            'ittihad': 'الاتحاد',
            'ahly': 'الأهلي'
        };

        // إظهار إشعار بتغير الثيم
        const toastElement = document.getElementById('themeToast');
        const toastMessage = document.getElementById('themeToastMessage');

        if (toastElement && toastMessage) {
            toastMessage.textContent = `تم تطبيق ثيم ${themeNames[theme]}`;
            const toast = new bootstrap.Toast(toastElement);
            toast.show();
        }
    }
}

// تهيئة مدير الثيمات عند تحميل الصفحة
document.addEventListener('DOMContentLoaded', () => {
    window.themeManager = new ThemeManager();
});