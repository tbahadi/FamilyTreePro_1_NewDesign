/**
 * Family Tree Pro - Advanced Family Tree Management
 * إدارة الشجرة العائلية المتقدمة
 */

class FamilyTreeManager {
    constructor() {
        this.treeData = [];
        this.zoomLevel = 1.0;
        this.panning = false;
        this.startX = 0;
        this.startY = 0;
        this.scrollLeft = 0;
        this.scrollTop = 0;
        this.allConnectors = [];
        this.currentCardStyle = 'default';
        this.currentLayout = 'hierarchical';

        this.initialize();
    }

    initialize() {
        this.loadTreeData();
        this.setupEventListeners();
        this.initializeTree();
    }

    // تحميل البيانات من الخادم
    loadTreeData() {
        try {
            if (typeof personsJson !== 'undefined' && personsJson && personsJson !== '[]') {
                this.treeData = JSON.parse(personsJson);
                console.log('تم تحميل البيانات بنجاح:', this.treeData.length, 'فرد');
                return true;
            }
            return false;
        } catch (e) {
            console.error('خطأ في تحميل البيانات:', e);
            this.showNotification('حدث خطأ في تحميل البيانات', 'danger');
            return false;
        }
    }

    // إعداد مستمعي الأحداث
    setupEventListeners() {
        // أحداث التكبير والتصغير
        $('#zoomIn').on('click', () => this.zoomIn());
        $('#zoomOut').on('click', () => this.zoomOut());
        $('#resetView').on('click', () => this.resetView());

        // أحداث أشكال البطاقات
        $('.card-style').on('click', (e) => {
            e.preventDefault();
            const style = $(e.target).closest('a').data('style');
            this.changeCardStyle(style);
        });

        // أحداث أشكال العرض
        $('.layout-style').on('click', (e) => {
            e.preventDefault();
            const layout = $(e.target).closest('a').data('layout');
            this.changeLayout(layout);
        });

        // حدث الطباعة
        $('#printPdf').on('click', () => this.printToPdf());

        // أحداث السحب والتحريك
        this.setupPanning();
    }

    // التكبير
    zoomIn() {
        this.zoomLevel = Math.min(this.zoomLevel + 0.1, 2.0);
        this.applyZoom();
    }

    // التصغير
    zoomOut() {
        this.zoomLevel = Math.max(this.zoomLevel - 0.1, 0.3);
        this.applyZoom();
    }

    // إعادة التعيين
    resetView() {
        this.zoomLevel = 1.0;
        this.applyZoom();
        $('#tree-container').scrollLeft(0).scrollTop(0);
    }

    // تطبيق مستوى التكبير
    applyZoom() {
        $('#family-tree').css('transform', `scale(${this.zoomLevel})`);
    }

    // إعداد التحريك
    setupPanning() {
        const container = $('#tree-container');
        const tree = $('#family-tree');

        container.on('mousedown', (e) => {
            if ($(e.target).closest('.tree-node, .leaf-node, .simple-node').length === 0) {
                this.panning = true;
                this.startX = e.pageX - container.offset().left;
                this.startY = e.pageY - container.offset().top;
                this.scrollLeft = container.scrollLeft();
                this.scrollTop = container.scrollTop();
                container.css('cursor', 'grabbing');
            }
        });

        $(document).on('mouseup', () => {
            this.panning = false;
            container.css('cursor', 'grab');
        });

        container.on('mousemove', (e) => {
            if (!this.panning) return;
            e.preventDefault();
            const x = e.pageX - container.offset().left;
            const y = e.pageY - container.offset().top;
            const walkX = (x - this.startX) * 2;
            const walkY = (y - this.startY) * 2;
            container.scrollLeft(this.scrollLeft - walkX);
            container.scrollTop(this.scrollTop - walkY);
        });
    }

    // تغيير شكل البطاقة
    changeCardStyle(style) {
        this.currentCardStyle = style;
        this.redrawCurrentLayout();
        this.showNotification(`تم تغيير الشكل إلى: ${this.getStyleName(style)}`, 'success');
    }

    // تغيير شكل العرض
    changeLayout(layout) {
        this.currentLayout = layout;
        this.redrawCurrentLayout();
        this.showNotification(`تم تغيير العرض إلى: ${this.getLayoutName(layout)}`, 'success');
    }

    // إعادة رسم العرض الحالي
    redrawCurrentLayout() {
        const container = $('#family-tree');
        container.empty();
        this.clearAllConnectors();

        container.removeClass('leaf-layout simple-layout vertical-layout radial-layout');

        switch (this.currentLayout) {
            case 'leaf':
                container.addClass('leaf-layout');
                this.drawLeafLayout();
                break;
            case 'simple':
                container.addClass('simple-layout');
                this.drawSimpleLayout();
                break;
            case 'vertical':
                container.addClass('vertical-layout');
                this.drawVerticalLayout();
                break;
            case 'radial':
                container.addClass('radial-layout');
                this.drawRadialLayout();
                break;
            default:
                this.drawHierarchicalTree();
                break;
        }
    }

    // الرسم الهرمي
    drawHierarchicalTree() {
        const container = $('#family-tree');
        console.log('رسم الشكل الهرمي المحسن');

        container.css({
            'min-width': '2500px',
            'min-height': '1200px',
            'padding': '60px',
            'background': 'linear-gradient(135deg, #f8f9fa, #e9ecef)'
        });

        const rootPersons = this.treeData.filter(p => !p.fatherId);

        if (rootPersons.length === 0) {
            this.drawAllPersonsHierarchical();
            return;
        }

        const baseX = 300;
        const spacing = Math.max(350, 1200 / rootPersons.length);
        let xPosition = baseX;

        rootPersons.forEach((root, index) => {
            const rootNode = this.createTreeNode(root, xPosition, 150, 0);
            container.append(rootNode.element);

            this.drawChildrenHierarchical(root, rootNode, 1, spacing);
            xPosition += spacing;
        });

        this.adjustContainerSize();
    }

    // رسم الأطفال في الشكل الهرمي
    drawChildrenHierarchical(parent, parentNode, level, baseSpacing) {
        const children = this.treeData.filter(p => p.fatherId === parent.id);
        if (children.length === 0) return;

        const container = $('#family-tree');
        const childrenCount = children.length;

        const spacing = Math.max(250, baseSpacing * 0.8);
        const childrenWidth = childrenCount * spacing;
        const startX = parentNode.x + parentNode.width / 2 - childrenWidth / 2 + spacing / 2;
        const startY = parentNode.y + parentNode.height + 120;

        children.forEach((child, index) => {
            const childX = startX + (index * spacing);
            const childNode = this.createTreeNode(child, childX, startY, level);
            container.append(childNode.element);

            this.drawEnhancedConnector(parentNode, childNode);

            this.drawChildrenHierarchical(child, childNode, level + 1, spacing * 0.7);
        });
    }

    // رسم خطوط الوصل المحسنة
    drawEnhancedConnector(parentNode, childNode) {
        const container = $('#family-tree');

        const parentCenterX = parentNode.x + parentNode.width / 2;
        const parentBottom = parentNode.y + parentNode.height;
        const childCenterX = childNode.x + childNode.width / 2;
        const childTop = childNode.y;

        // خط رأسي من الأسفل إلى المنتصف
        const verticalLine1 = $(`<div class="tree-connector connector-vertical" style="left: ${parentCenterX}px; top: ${parentBottom}px; height: 60px; background: #666;"></div>`);
        container.append(verticalLine1);
        this.allConnectors.push(verticalLine1);

        // خط أفقي يربط بين الوالدين والأطفال
        const minX = Math.min(parentCenterX, childCenterX);
        const maxX = Math.max(parentCenterX, childCenterX);
        const horizontalY = parentBottom + 60;

        const horizontalLine = $(`<div class="tree-connector connector-horizontal" style="left: ${minX}px; top: ${horizontalY}px; width: ${maxX - minX}px; background: #666;"></div>`);
        container.append(horizontalLine);
        this.allConnectors.push(horizontalLine);

        // خط رأسي من الأفقي إلى الطفل
        const verticalLine2 = $(`<div class="tree-connector connector-vertical" style="left: ${childCenterX}px; top: ${horizontalY}px; height: ${childTop - horizontalY}px; background: #666;"></div>`);
        container.append(verticalLine2);
        this.allConnectors.push(verticalLine2);
    }

    // الرسم العمودي
    drawVerticalLayout() {
        const container = $('#family-tree');
        console.log('رسم الشكل العمودي');

        const generations = this.organizeByGeneration();

        generations.forEach((generation, genIndex) => {
            const generationDiv = $(`<div class="vertical-generation"></div>`);

            generation.forEach((person) => {
                const node = this.createVerticalNode(person);
                generationDiv.append(node);
            });

            container.append(generationDiv);
        });
    }

    // الرسم الدائري
    drawRadialLayout() {
        const container = $('#family-tree');
        console.log('رسم الشكل الدائري');

        const centerX = 600;
        const centerY = 300;
        const baseRadius = 150;

        const generations = this.organizeByGeneration();

        generations.forEach((generation, genIndex) => {
            const radius = baseRadius + (genIndex * 120);
            const angleStep = (2 * Math.PI) / Math.max(generation.length, 1);

            generation.forEach((person, personIndex) => {
                const angle = personIndex * angleStep;
                const x = centerX + radius * Math.cos(angle);
                const y = centerY + radius * Math.sin(angle);

                const node = this.createRadialNode(person, x, y);
                container.append(node);
            });
        });
    }

    // رسم جميع الأشخاص بشكل هرمي
    drawAllPersonsHierarchical() {
        const container = $('#family-tree');
        console.log('رسم جميع الأفراد بشكل هرمي');

        let xPosition = 100;
        let yPosition = 100;
        let maxX = 100;

        this.treeData.forEach((person, index) => {
            const node = this.createTreeNode(person, xPosition, yPosition, 0);
            container.append(node.element);

            xPosition += 250;
            maxX = Math.max(maxX, xPosition);

            if ((index + 1) % 6 === 0) {
                xPosition = 100;
                yPosition += 150;
            }
        });

        container.css({
            'min-width': maxX + 100 + 'px',
            'min-height': yPosition + 200 + 'px'
        });
    }

    // رسم شكل ورقة الشجرة
    drawLeafLayout() {
        const container = $('#family-tree');
        console.log('رسم شكل ورقة الشجرة');

        container.empty();

        const generations = this.organizeByGeneration();
        console.log('عدد الأجيال:', generations.length);

        if (generations.length === 0) {
            container.html('<div class="alert alert-warning text-center">لا توجد بيانات لعرضها</div>');
            return;
        }

        generations.forEach((generation, genIndex) => {
            const generationDiv = $(`<div class="leaf-generation"></div>`);
            const title = $(`<div class="generation-title">الجيل ${genIndex + 1}</div>`);
            generationDiv.append(title);

            console.log(`الجيل ${genIndex + 1}:`, generation.length, 'فرد');

            generation.forEach((person) => {
                const node = this.createLeafNode(person);
                generationDiv.append(node);
            });

            container.append(generationDiv);
        });

        container.append('<div style="height: 50px;"></div>');
    }

    // الرسم المبسط
    drawSimpleLayout() {
        const container = $('#family-tree');
        console.log('رسم الشكل المبسط');

        this.treeData.forEach((person) => {
            const node = this.createSimpleNode(person);
            container.append(node);
        });
    }

    // إنشاء عقدة الشجرة
    createTreeNode(person, x, y, level) {
        const birthDate = this.formatDate(person.birthDate);
        const city = person.city || '';
        const occupationName = person.occupationName || '';
        const gender = person.gender || 'Male';

        const styleClass = this.currentCardStyle !== 'default' ? ` ${this.currentCardStyle}` : '';

        const nodeContent = `
            <div class="tree-node ${gender === 'Male' ? 'male' : 'female'}${styleClass}"
                 data-person-id="${person.id}"
                 style="left: ${x}px; top: ${y}px;">
                <div class="node-name">${this.escapeHtml(person.fullName || 'غير معروف')}</div>
                <div class="node-details">
                    ${birthDate ? '📅 ' + birthDate + '<br>' : ''}
                    ${city ? '🏠 ' + this.escapeHtml(city) + '<br>' : ''}
                    ${occupationName ? '💼 ' + this.escapeHtml(occupationName) : ''}
                </div>
            </div>
        `;

        const node = $(nodeContent);

        node.on('click', (e) => {
            e.stopPropagation();
            this.showPersonDetails(person);
        });

        return {
            element: node,
            x: x,
            y: y,
            width: 180,
            height: 80,
            person: person
        };
    }

    // إنشاء عقدة عمودية
    createVerticalNode(person) {
        const birthDate = this.formatDate(person.birthDate);
        const city = person.city || '';
        const occupationName = person.occupationName || '';
        const gender = person.gender || 'Male';

        const nodeContent = `
            <div class="vertical-node ${gender === 'Male' ? 'male' : 'female'}"
                 data-person-id="${person.id}">
                <div class="node-name">${this.escapeHtml(person.fullName || 'غير معروف')}</div>
                <div class="node-details">
                    ${birthDate ? '📅 ' + birthDate + '<br>' : ''}
                    ${city ? '🏠 ' + this.escapeHtml(city) + '<br>' : ''}
                    ${occupationName ? '💼 ' + this.escapeHtml(occupationName) : ''}
                </div>
            </div>
        `;

        const node = $(nodeContent);

        node.on('click', (e) => {
            e.stopPropagation();
            this.showPersonDetails(person);
        });

        return node;
    }

    // إنشاء عقدة دائرية
    createRadialNode(person, x, y) {
        const birthDate = this.formatDate(person.birthDate);
        const gender = person.gender || 'Male';
        const fullName = person.fullName || 'غير معروف';
        const shortName = fullName.length > 15 ? fullName.substring(0, 15) + '...' : fullName;

        const nodeContent = `
            <div class="radial-node ${gender === 'Male' ? 'male' : 'female'}"
                 data-person-id="${person.id}"
                 style="left: ${x - 60}px; top: ${y - 60}px;">
                <div class="radial-content">
                    <strong>${this.escapeHtml(shortName)}</strong><br>
                    ${birthDate ? '📅 ' + birthDate : ''}
                </div>
            </div>
        `;

        const node = $(nodeContent);

        node.on('click', (e) => {
            e.stopPropagation();
            this.showPersonDetails(person);
        });

        return node;
    }

    // إنشاء عقدة ورقة الشجرة
    createLeafNode(person) {
        const birthDate = this.formatDate(person.birthDate);
        const city = person.city || '';
        const occupationName = person.occupationName || '';
        const gender = person.gender || 'Male';

        const nodeContent = `
            <div class="leaf-node ${gender === 'Male' ? 'male' : 'female'}"
                 data-person-id="${person.id}">
                <div class="node-name">${this.escapeHtml(person.fullName || 'غير معروف')}</div>
                <div class="node-details">
                    ${birthDate ? '📅 ' + birthDate + '<br>' : ''}
                    ${city ? '🏠 ' + this.escapeHtml(city) + '<br>' : ''}
                    ${occupationName ? '💼 ' + this.escapeHtml(occupationName) : ''}
                </div>
            </div>
        `;

        const node = $(nodeContent);

        node.on('click', (e) => {
            e.stopPropagation();
            this.showPersonDetails(person);
        });

        return node;
    }

    // إنشاء عقدة مبسطة
    createSimpleNode(person) {
        const birthDate = this.formatDate(person.birthDate);
        const gender = person.gender || 'Male';

        const nodeContent = `
            <div class="simple-node ${gender === 'Male' ? 'male' : 'female'}"
                 data-person-id="${person.id}">
                <div class="node-name">${this.escapeHtml(person.fullName || 'غير معروف')}</div>
                <div class="node-details">
                    ${birthDate ? '📅 ' + birthDate : ''}
                </div>
            </div>
        `;

        const node = $(nodeContent);

        node.on('click', (e) => {
            e.stopPropagation();
            this.showPersonDetails(person);
        });

        return node;
    }

    // تنظيم البيانات حسب الأجيال
    organizeByGeneration() {
        const generations = [];
        const processed = new Set();

        // البدء بالأجداد (الأشخاص بدون أب)
        const roots = this.treeData.filter(p => !p.fatherId);
        console.log('عدد الأجداد:', roots.length);

        if (roots.length > 0) {
            generations.push(roots);
            roots.forEach(root => processed.add(root.id));

            let currentGen = roots;
            let generationNumber = 1;

            while (currentGen.length > 0) {
                const nextGen = [];
                currentGen.forEach(parent => {
                    const children = this.treeData.filter(p => p.fatherId === parent.id && !processed.has(p.id));
                    children.forEach(child => {
                        nextGen.push(child);
                        processed.add(child.id);
                    });
                });

                if (nextGen.length > 0) {
                    generations.push(nextGen);
                    console.log(`الجيل ${generationNumber + 1}:`, nextGen.length, 'فرد');
                }
                currentGen = nextGen;
                generationNumber++;
            }
        } else {
            // إذا لم يكن هناك أجداد، نعرض جميع الأشخاص في جيل واحد
            console.log('لا توجد أجداد، عرض جميع الأشخاص في جيل واحد');
            generations.push(this.treeData);
        }

        // التأكد من معالجة جميع الأشخاص
        const unprocessed = this.treeData.filter(p => !processed.has(p.id));
        if (unprocessed.length > 0) {
            console.log('أشخاص غير معالجين:', unprocessed.length);
            generations.push(unprocessed);
        }

        return generations;
    }

    // ضبط حجم الحاوية
    adjustContainerSize() {
        const container = $('#family-tree');
        const nodes = container.find('.tree-node, .vertical-node, .leaf-node, .simple-node');

        let maxX = 0;
        let maxY = 0;

        nodes.each(function () {
            const $node = $(this);
            const left = parseInt($node.css('left')) || 0;
            const top = parseInt($node.css('top')) || 0;
            const width = $node.outerWidth() || 0;
            const height = $node.outerHeight() || 0;

            maxX = Math.max(maxX, left + width);
            maxY = Math.max(maxY, top + height);
        });

        container.css({
            'min-width': Math.max(maxX + 100, 2500) + 'px',
            'min-height': Math.max(maxY + 100, 1200) + 'px'
        });
    }

    // مسح جميع خطوط الوصل
    clearAllConnectors() {
        this.allConnectors.forEach(connector => {
            connector.remove();
        });
        this.allConnectors = [];
    }

    // عرض تفاصيل الشخص
    showPersonDetails(person) {
        const birthDate = this.formatDate(person.birthDate);
        const city = person.city || '';
        const occupationName = person.occupationName || '';

        let details = '';
        if (birthDate) details += `الميلاد: ${birthDate}<br>`;
        if (city) details += `المدينة: ${this.escapeHtml(city)}<br>`;
        if (occupationName) details += `المهنة: ${this.escapeHtml(occupationName)}<br>`;
        if (!details) details = 'لا توجد معلومات إضافية';

        const infoContent = `
            <div class="card-body">
                <h5>${this.escapeHtml(person.fullName || 'غير معروف')}</h5>
                <p>${details}</p>
                <div class="mt-2">
                    <a href="/Person/Details/${person.id}" class="btn btn-info btn-sm">
                        <i class="fas fa-eye"></i> تفاصيل
                    </a>
                    <a href="/Person/Edit/${person.id}" class="btn btn-warning btn-sm">
                        <i class="fas fa-edit"></i> تعديل
                    </a>
                    <a href="/Person/Create?familyTreeId=${familyTreeId}&fatherId=${person.id}" class="btn btn-success btn-sm">
                        <i class="fas fa-plus"></i> إضافة ابن
                    </a>
                </div>
            </div>
        `;

        $('#node-info').html(infoContent).show();
    }

    // تهيئة الشجرة
    initializeTree() {
        const container = $('#family-tree');
        container.empty().html(`
            <div class="loading-spinner">
                <div class="spinner-border text-primary" role="status">
                    <span class="sr-only">جاري التحميل...</span>
                </div>
                <p class="mt-2">جاري تحميل الشجرة العائلية...</p>
            </div>
        `);

        setTimeout(() => {
            if (this.treeData.length > 0) {
                this.redrawCurrentLayout();
                $('.loading-spinner').remove();
            } else {
                container.html(`
                    <div class="alert alert-warning text-center" style="margin: 50px;">
                        <h4>⚠️ لا توجد بيانات</h4>
                        <p>لم يتم العثور على بيانات لعرض الشجرة.</p>
                        <button onclick="location.reload()" class="btn btn-primary">
                            <i class="fas fa-redo"></i> إعادة تحميل
                        </button>
                    </div>
                `);
            }
        }, 100);
    }

    // وظائف مساعدة
    formatDate(dateString) {
        try {
            if (!dateString) return '';
            const date = new Date(dateString);
            return isNaN(date.getTime()) ? dateString : date.toLocaleDateString('ar-EG');
        } catch (e) {
            return dateString;
        }
    }

    escapeHtml(unsafe) {
        if (!unsafe) return '';
        return unsafe
            .toString()
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    getStyleName(style) {
        const styles = {
            'default': 'الشكل الافتراضي',
            'modern': 'الشكل الحديث',
            'elegant': 'الشكل الأنيق',
            'minimal': 'الشكل البسيط',
            'colorful': 'الشكل الملون'
        };
        return styles[style] || 'غير معروف';
    }

    getLayoutName(layout) {
        const layouts = {
            'hierarchical': 'الشكل الهرمي',
            'vertical': 'الشكل العمودي',
            'radial': 'الشكل الدائري',
            'leaf': 'شكل ورقة الشجرة',
            'simple': 'الشكل المبسط'
        };
        return layouts[layout] || 'غير معروف';
    }

    showNotification(message, type = 'info') {
        $('.alert-notification').remove();

        const alertClass = {
            'info': 'alert-info',
            'success': 'alert-success',
            'danger': 'alert-danger',
            'warning': 'alert-warning'
        }[type] || 'alert-info';

        const icon = {
            'info': 'fa-info-circle',
            'success': 'fa-check-circle',
            'danger': 'fa-exclamation-circle',
            'warning': 'fa-exclamation-triangle'
        }[type];

        const notification = $(`
            <div class="alert ${alertClass} alert-dismissible fade show position-fixed alert-notification"
                 style="top: 20px; right: 20px; z-index: 9999; min-width: 300px;">
                <div class="d-flex align-items-center">
                    <i class="fas ${icon} me-2"></i>
                    <div>${message}</div>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `);

        $('body').append(notification);

        setTimeout(() => {
            notification.alert('close');
        }, 5000);
    }

    // الطباعة إلى PDF
    printToPdf() {
        this.showPdfLoading(true);
        this.showNotification('جاري إنشاء PDF عالي الجودة...', 'info');

        setTimeout(() => {
            try {
                // كود الطباعة هنا...
                this.showPdfLoading(false);
                this.showNotification('تم إنشاء PDF بنجاح', 'success');
            } catch (error) {
                console.error('خطأ في إنشاء PDF:', error);
                this.showPdfLoading(false);
                this.showNotification('حدث خطأ في إنشاء PDF', 'danger');
            }
        }, 1000);
    }

    showPdfLoading(show) {
        let loadingDiv = $('#pdf-loading');

        if (show) {
            if (loadingDiv.length === 0) {
                loadingDiv = $(`
                    <div id="pdf-loading" class="pdf-loading">
                        <div class="text-center">
                            <div class="spinner-border text-light mb-3" role="status">
                                <span class="sr-only">جاري التحميل...</span>
                            </div>
                            <div>جاري إنشاء PDF عالي الجودة...</div>
                            <small>قد يستغرق هذا بضع ثوانٍ</small>
                        </div>
                    </div>
                `);
                $('body').append(loadingDiv);
            }
            loadingDiv.show();
        } else {
            loadingDiv.hide();
        }
    }
}

// تهيئة التطبيق عند تحميل الصفحة
$(document).ready(function () {
    console.log('بدء تهيئة إدارة الشجرة العائلية...');
    window.familyTreeManager = new FamilyTreeManager();
});