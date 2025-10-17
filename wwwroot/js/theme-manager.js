class ThemeManager {
    constructor() {
        this.themes = ['hilal', 'desert', 'palm'];
        this.currentTheme = this.getSavedTheme();
    }

    init() {
        this.applyTheme(this.currentTheme);
        this.setupThemeSwitcher();
    }

    getSavedTheme() {
        return localStorage.getItem('theme') || 'hilal';
    }

    saveTheme(theme) {
        localStorage.setItem('theme', theme);
        document.cookie = `theme=${theme}; path=/; max-age=31536000`;
    }

    applyTheme(themeName) {
        // تغيير ملف الـ CSS
        const themeLink = document.getElementById('theme-style');
        if (themeLink) {
            themeLink.href = `/css/themes/${themeName}-theme.css`;
        }

        // تغيير كلاس الجسم
        document.body.className = document.body.className.replace(/\b\w+-theme\b/, '') + ' ' + themeName + '-theme';

        this.currentTheme = themeName;
        this.saveTheme(themeName);

        // تحديث الأزرار النشطة
        this.updateActiveButtons(themeName);
    }

    setupThemeSwitcher() {
        // إضافة أزرار تغيير الثيم ديناميكياً
        const themeSwitcher = document.querySelector('.theme-switcher');
        if (!themeSwitcher) return;

        this.themes.forEach(theme => {
            const button = document.createElement('button');
            button.className = `btn btn-outline-secondary btn-sm ${theme === this.currentTheme ? 'active' : ''}`;
            button.innerHTML = this.getThemeIcon(theme) + ' ' + this.getThemeName(theme);
            button.onclick = () => this.applyTheme(theme);
            themeSwitcher.appendChild(button);
        });
    }

    getThemeIcon(theme) {
        const icons = {
            'hilal': '<i class="fas fa-moon"></i>',
            'desert': '<i class="fas fa-sun"></i>',
            'palm': '<i class="fas fa-tree"></i>'
        };
        return icons[theme] || '';
    }

    getThemeName(theme) {
        const names = {
            'hilal': 'الهلال',
            'desert': 'الصحراء',
            'palm': 'النخيل'
        };
        return names[theme] || theme;
    }

    updateActiveButtons(activeTheme) {
        document.querySelectorAll('.theme-switcher .btn').forEach(btn => {
            btn.classList.remove('active');
            if (btn.textContent.includes(this.getThemeName(activeTheme))) {
                btn.classList.add('active');
            }
        });
    }
}

// تهيئة مدير الثيمات
const themeManager = new ThemeManager();
document.addEventListener('DOMContentLoaded', () => themeManager.init());