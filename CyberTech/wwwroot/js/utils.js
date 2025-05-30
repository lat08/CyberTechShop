const utils = {
    async fetchData(url, method = 'GET', data = null) {
        try {
            const options = {
                method,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                credentials: 'same-origin' // Thêm credentials để gửi cookies
            };

            // Xử lý dữ liệu gửi đi
            if (data) {
                if (data instanceof FormData) {
                    // Không cần set Content-Type cho FormData
                    options.body = data;
                } else if (typeof data === 'object') {
                    options.headers['Content-Type'] = 'application/json';
                    options.body = JSON.stringify(data);
                } else {
                    options.body = data;
                }
            }

            // Thêm antiforgery token nếu có
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            if (token) {
                options.headers['RequestVerificationToken'] = token;
            }

            const response = await fetch(url, options);

            // Xử lý response
            if (!response.ok) {
                const errorData = await response.json().catch(() => null);
                throw new Error(errorData?.errorMessage || `HTTP error! status: ${response.status}`);
            }

            // Xử lý response trống
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                return await response.json();
            }
            return null;
        } catch (error) {
            console.error('Fetch error:', error);
            throw error;
        }
    },

    getInitials(name) {
        return name?.split(' ').map(word => word[0]).join('').toUpperCase() || '?';
    },

    getEnumText(enumType, value) {
        const enumMaps = {
            UserRole: {
                'Customer': 'Khách hàng',
                'Support': 'Hỗ trợ',
                'Manager': 'Quản lý',
                'SuperAdmin': 'Quản trị viên'
            },
            UserStatus: {
                'Active': 'Hoạt động',
                'Inactive': 'Tạm khóa',
                'Suspended': 'Bị đình chỉ'
            },
            AuthType: {
                'Password': 'Mật khẩu',
                'Google': 'Google',
                'Facebook': 'Facebook'
            },
            DiscountType: {
                'PERCENT': 'Phần trăm',
                'FIXED': 'Cố định'
            },
            OrderStatus: {
                'Pending': 'Đang chờ',
                'Processing': 'Đang xử lý',
                'Shipped': 'Đã giao hàng',
                'Delivered': 'Đã giao',
                'Cancelled': 'Đã hủy'
            },
            PaymentMethod: {
                'COD': 'Thanh toán khi nhận hàng',
                'VNPay': 'VNPay',
                'Momo': 'Momo'
            },
            PaymentStatus: {
                'Pending': 'Đang chờ',
                'Completed': 'Hoàn tất',
                'Failed': 'Thất bại',
                'Refunded': 'Đã hoàn tiền'
            },
            ShippingMethod: {
                'Standard': 'Tiêu chuẩn',
                'Express': 'Nhanh'
            },
            ShippingStatus: {
                'Pending': 'Đang chờ',
                'Shipped': 'Đã giao hàng',
                'InTransit': 'Đang vận chuyển',
                'Delivered': 'Đã giao'
            }
        };

        if (!enumMaps[enumType] || typeof value !== 'string') {
            return 'Không xác định';
        }

        return enumMaps[enumType][value] || 'Không xác định';
    },

    showToast(message, type = 'info') {
        const container = document.getElementById('toastContainer') || Object.assign(document.createElement('div'), {
            id: 'toastContainer',
            className: 'toast-container position-fixed bottom-0 end-0 p-3'
        });
        document.body.appendChild(container);

        const toastId = `toast-${Date.now()}`;
        const toastEl = Object.assign(document.createElement('div'), {
            id: toastId,
            className: `toast align-items-center text-white bg-${type === 'success' ? 'success' : type === 'error' ? 'danger' : 'primary'} border-0`,
            role: 'alert',
            innerHTML: `
                <div class="d-flex">
                    <div class="toast-body">${message}</div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
                </div>
            `
        });

        container.appendChild(toastEl);
        const toast = new bootstrap.Toast(toastEl, { autohide: true, delay: 3000 });
        toast.show();
        toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
    },

    formatMoney(amount, locale = 'vi-VN', currency = 'VND') {
        if (amount > 9999999999) {
            console.warn("Amount exceeds maximum allowed value:", amount);
            amount = 9999999999;
        }
        return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(amount);
    },

    renderPagination({ currentPage, totalPages }, container, itemsContainer, prevButton, nextButton, pageChangeCallback) {
        if (!container || !itemsContainer || totalPages <= 1) {
            container?.classList.add('d-none');
            return;
        }

        container.classList.remove('d-none');
        itemsContainer.innerHTML = '';

        const startPage = Math.max(1, currentPage - 2);
        const endPage = Math.min(totalPages, startPage + 4);

        if (startPage > 1) {
            this.addPageItem(1, itemsContainer, pageChangeCallback, currentPage);
            if (startPage > 2) this.addEllipsis(itemsContainer);
        }

        for (let i = startPage; i <= endPage; i++) {
            this.addPageItem(i, itemsContainer, pageChangeCallback, currentPage);
        }

        if (endPage < totalPages) {
            if (endPage < totalPages - 1) this.addEllipsis(itemsContainer);
            this.addPageItem(totalPages, itemsContainer, pageChangeCallback, currentPage);
        }

        prevButton?.classList.toggle('disabled', currentPage <= 1);
        nextButton?.classList.toggle('disabled', currentPage >= totalPages);
    },

    addPageItem(pageNum, container, callback, currentPage) {
        const li = document.createElement('li');
        li.className = `page-item ${pageNum === currentPage ? 'active' : ''}`;
        
        const a = document.createElement('a');
        a.className = 'page-link';
        a.href = '#';
        a.textContent = pageNum;

        if (pageNum !== currentPage) {
            a.addEventListener('click', e => {
                e.preventDefault();
                callback(pageNum);
            });
        }

        li.appendChild(a);
        container.appendChild(li);
    },

    addEllipsis(container) {
        const li = document.createElement('li');
        li.className = 'page-item disabled';
        li.innerHTML = '<span class="page-link">…</span>';
        container.appendChild(li);
    },

    showLoadingOverlay(show = true) {
        let overlay = document.getElementById('loadingOverlay');
        if (!overlay && show) {
            overlay = document.createElement('div');
            overlay.id = 'loadingOverlay';
            overlay.innerHTML = `
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Đang tải...</span>
                </div>
            `;
            overlay.style.cssText = 'position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(255,255,255,0.7); display: flex; justify-content: center; align-items: center; z-index: 9999;';
            document.body.appendChild(overlay);
        } else if (overlay && !show) {
            overlay.remove();
        }
    }
};

window.utils = utils;