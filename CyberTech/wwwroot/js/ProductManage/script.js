let currentSubSubcategoryId = 0;
let currentPage = 1;

// Load categories hierarchically
function loadCategories() {
    console.log('Loading categories');
    $.ajax({
        url: '/ProductManage/GetCategories',
        method: 'GET',
        success: function (data) {
            console.log('Categories received:', data);
            renderCategories(data);
        },
        error: function (xhr, status, error) {
            console.error('Error loading categories:', error, xhr.status);
            $('#categoriesList').html('<li class="list-group-item">Không thể tải danh mục. Vui lòng thử lại.</li>');
        }
    });
}

function renderCategories(categories) {
    console.log('Rendering categories:', categories);
    const list = $('#categoriesList');
    list.empty();
    if (!categories || categories.length === 0) {
        list.append('<li class="list-group-item">Không có danh mục nào.</li>');
        return;
    }
    categories.forEach(category => {
        const categoryId = category.categoryID;
        const categoryItem = $(`
            <li class="category-item">
                <a href="#" data-bs-toggle="collapse" data-bs-target="#subcat-${categoryId}"><i class="fas fa-folder me-2"></i>${category.name}</a>
                <div class="category-actions">
                    <i class="fas fa-plus action-icon add" data-parentid="${categoryId}" data-level="subcategory" title="Thêm danh mục phụ"></i>
                    <i class="fas fa-edit action-icon edit" data-categoryid="${categoryId}" data-categoryname="${category.name}" title="Sửa danh mục"></i>
                    <i class="fas fa-trash action-icon delete" data-categoryid="${categoryId}" data-categoryname="${category.name}" title="Xóa danh mục"></i>
                </div>
                <ul class="collapse" id="subcat-${categoryId}">
                    ${renderSubcategories(category.subcategories, categoryId)}
                </ul>
            </li>
        `);
        list.append(categoryItem);
    });
}

function renderSubcategories(subcategories, parentId) {
    if (!subcategories || subcategories.length === 0) return '<li class="subcategory-item">Không có danh mục phụ.</li>';
    let html = '';
    subcategories.forEach(subcategory => {
        const subcategoryId = subcategory.subcategoryID;
        html += `
            <li class="subcategory-item">
                <a href="#" data-bs-toggle="collapse" data-bs-target="#subsubcat-${subcategoryId}"><i class="fas fa-folder-open me-2"></i>${subcategory.name}</a>
                <div class="category-actions">
                    <i class="fas fa-plus action-icon add" data-parentid="${subcategoryId}" data-level="subsubcategory" title="Thêm danh mục chi tiết"></i>
                    <i class="fas fa-edit action-icon edit" data-subcategoryid="${subcategoryId}" data-subcategoryname="${subcategory.name}" title="Sửa danh mục phụ"></i>
                    <i class="fas fa-trash action-icon delete" data-subcategoryid="${subcategoryId}" data-subcategoryname="${subcategory.name}" title="Xóa danh mục phụ"></i>
                </div>
                <ul class="collapse" id="subsubcat-${subcategoryId}">
                    ${renderSubSubcategories(subcategory.subSubcategories, subcategoryId)}
                </ul>
            </li>
        `;
    });
    return html;
}

function renderSubSubcategories(subsubcategories, parentId) {
    if (!subsubcategories || subsubcategories.length === 0) return '<li class="subsubcategory-item">Không có danh mục chi tiết.</li>';
    let html = '';
    subsubcategories.forEach(subsubcategory => {
        const subsubcategoryId = subsubcategory.subSubcategoryID;
        html += `
            <li class="subsubcategory-item">
                <a href="#" data-subsubcategoryid="${subsubcategoryId}"><i class="fas fa-file-alt me-2"></i>${subsubcategory.name}</a>
                <div class="category-actions">
                    <i class="fas fa-edit action-icon edit" data-subsubcategoryid="${subsubcategoryId}" data-subsubcategoryname="${subsubcategory.name}" title="Sửa danh mục chi tiết"></i>
                    <i class="fas fa-trash action-icon delete" data-subsubcategoryid="${subsubcategoryId}" data-subsubcategoryname="${subsubcategory.name}" title="Xóa danh mục chi tiết"></i>
                </div>
            </li>
        `;
    });
    return html;
}

// Load products with filters
function loadProducts(page = 1) {
    console.log('Loading products, page:', page);
    const search = $('#productSearchInput').val();
    const status = $('#statusFilter .active').data('status');
    const sort = $('#sortFilter').val();

    $.ajax({
        url: '/ProductManage/GetProducts',
        method: 'GET',
        data: {
            search: search,
            status: status,
            sort: sort,
            page: page,
            subsubcategoryId: currentSubSubcategoryId
        },
        success: function (data) {
            console.log('Products received:', data);
            renderProducts(data.products);
            renderPagination(data.totalPages, page);
        },
        error: function (xhr, status, error) {
            console.error('Error loading products:', error, xhr.status);
            $('#productsGrid').html('<div class="col">Không thể tải sản phẩm. Vui lòng thử lại.</div>');
        }
    });
}

function renderProducts(products) {
    console.log('Rendering products:', products);
    const grid = $('#productsGrid');
    grid.empty();
    if (!products || products.length === 0) {
        grid.append('<div class="col">Không có sản phẩm nào.</div>');
        return;
    }
    products.forEach(product => {
        const card = $(`
            <div class="col">
                <div class="card h-100">
                    <img src="${product.imageUrl || '/images/placeholder.jpg'}" class="card-img-top" alt="${product.name}">
                    <div class="card-body">
                        <h5 class="card-title">${product.name}</h5>
                        <p class="card-text">${product.price.toLocaleString()} ₫</p>
                        <p class="card-text">${product.stock > 0 ? 'Còn hàng' : 'Hết hàng'}</p>
                        <ul>
                            ${product.attributes.map(attr => `<li>${attr.name}: ${attr.value}</li>`).join('')}
                        </ul>
                    </div>
                    <div class="card-footer">
                        <button class="btn btn-primary edit-product" data-id="${product.id}">Sửa</button>
                        <button class="btn btn-danger delete-product" data-id="${product.id}" data-name="${product.name}">Xóa</button>
                    </div>
                </div>
            </div>
        `);
        grid.append(card);
    });
}

function renderPagination(totalPages, currentPage) {
    console.log('Rendering pagination, total pages:', totalPages);
    const paginationItems = $('#paginationItems');
    paginationItems.empty();
    if (totalPages === 0) {
        paginationItems.append('<li class="page-item disabled"><span class="page-link">Không có trang nào</span></li>');
        return;
    }
    for (let i = 1; i <= totalPages; i++) {
        const pageItem = $(`
            <li class="page-item ${i === currentPage ? 'active' : ''}">
                <a class="page-link" href="#">${i}</a>
            </li>
        `);
        pageItem.on('click', function (e) {
            e.preventDefault();
            currentPage = i;
            loadProducts(i);
        });
        paginationItems.append(pageItem);
    }
    $('#paginationPrev').toggleClass('disabled', currentPage === 1);
    $('#paginationNext').toggleClass('disabled', currentPage === totalPages);
}

// Event listeners for filters
$('#productSearchInput').on('input', function () {
    console.log('Search input changed:', $(this).val());
    currentPage = 1;
    loadProducts();
});

$('#statusFilter button').on('click', function () {
    $('#statusFilter button').removeClass('active');
    $(this).addClass('active');
    console.log('Status filter changed:', $(this).data('status'));
    currentPage = 1;
    loadProducts();
});

$('#sortFilter').on('change', function () {
    console.log('Sort filter changed:', $(this).val());
    currentPage = 1;
    loadProducts();
});

// Category selection
$('#categoriesList').on('click', 'a[data-subsubcategoryid]', function (e) {
    e.preventDefault();
    currentSubSubcategoryId = $(this).data('subsubcategoryid');
    console.log('Selected subsubcategory:', currentSubSubcategoryId);
    currentPage = 1;
    loadProducts();
});

// Add/Edit category/subcategory/subsubcategory
$('#categoriesList').on('click', '.action-icon.add', function () {
    const parentId = $(this).data('parentid');
    const level = $(this).data('level');
    $('#addCategoryModal').data('parentid', parentId);
    $('#addCategoryModal').data('level', level);
    $('#addCategoryModalLabel').text(`Thêm ${level === 'subcategory' ? 'Danh mục phụ' : level === 'subsubcategory' ? 'Danh mục chi tiết' : 'Danh mục'}`);
    $('#categoryName').val(''); if ($('#subsubcategoryList').length) { $('#subsubcategoryList').empty(); $('#subsubcategoryContainer').hide(); } if (level === 'subcategory') { if ($('#subsubcategoryContainer').length) { $('#subsubcategoryContainer').show(); } loadSubSubcategories(parentId); }
    $('#addCategoryModal').modal('show');
});

$('#addCategoryBtn').on('click', function () {
    $('#addCategoryModal').data('level', 'category');
    $('#addCategoryModalLabel').text('Thêm Danh mục');
    $('#categoryName').val('');
    $('#subsubcategoryList').empty();
    $('#addCategoryModal').modal('show');
});

function loadSubSubcategories(subcategoryId) { $.ajax({ url: '/ProductManage/GetSubSubcategories', method: 'GET', data: { subcategoryId: subcategoryId }, success: function (data) { console.log('SubSubcategories received:', data); renderSubSubcategoryList(data); }, error: function (xhr, status, error) { console.error('Error loading subsubcategories:', error, xhr.status); $('#subsubcategoryList').html('<div>Không thể tải danh mục chi tiết.</div>'); } }); }

function renderSubSubcategoryList(subsubcategories) {
    const list = $('#subsubcategoryList');
    list.empty();
    if (!subsubcategories || subsubcategories.length === 0) {
        list.append('<div class="text-muted">Không có danh mục chi tiết.</div>');
        return;
    }
    subsubcategories.forEach(subsubcategory => {
        const item = $(`
            <li class="list-group-item">
                <span>${subsubcategory.name}</span>
                <div class="category-actions">
                                        <i class="fas fa-edit action-icon edit" data-subsubcategoryid="${subsubcategory.id}" data-subsubcategoryname="${subsubcategory.name}" title="Sửa danh mục chi tiết"></i>                    <i class="fas fa-trash action-icon delete" data-subsubcategoryid="${subsubcategory.id}" data-subsubcategoryname="${subsubcategory.name}" title="Xóa danh mục chi tiết"></i>
                </div>
            </li>
        `);
        list.append(item);
    });
}

$('#saveCategoryBtn').on('click', function () {
    const name = $('#categoryName').val().trim();
    if (!name) {
        alert('Tên danh mục không được để trống.');
        return;
    }
    const level = $('#addCategoryModal').data('level');
    const parentId = $('#addCategoryModal').data('parentid');

    let url;
    let data = { name: name };
    if (level === 'category') {
        url = '/ProductManage/AddCategory';
    } else if (level === 'subcategory') {
        url = '/ProductManage/AddSubcategory';
        data.parentId = parentId;
    } else if (level === 'subsubcategory') {
        url = '/ProductManage/AddSubSubcategory';
        data.parentId = parentId;
    }

    $.ajax({
        url: url,
        method: 'POST',
        data: data,
        success: function () {
            console.log(`${level} added successfully`);
            $('#addCategoryModal').modal('hide');
            loadCategories();
            if (level === 'subcategory') {
                loadSubSubcategories(parentId);
            }
        },
        error: function (xhr, status, error) {
            console.error(`Error adding ${level}:`, error, xhr.status);
            alert(`Không thể thêm ${level === 'category' ? 'danh mục' : level === 'subcategory' ? 'danh mục phụ' : 'danh mục chi tiết'}.`);
        }
    });
});

// Edit category/subcategory/subsubcategory
$('#categoriesList').on('click', '.action-icon.edit', function () {
    const categoryId = $(this).data('categoryid');
    const subcategoryId = $(this).data('subcategoryid');
    const subsubcategoryId = $(this).data('subsubcategoryid');
    const name = $(this).data('categoryname') || $(this).data('subcategoryname') || $(this).data('subsubcategoryname');

    $('#editCategoryModal').data('categoryid', categoryId);
    $('#editCategoryModal').data('subcategoryid', subcategoryId);
    $('#editCategoryModal').data('subsubcategoryid', subsubcategoryId);
    $('#editCategoryName').val(name);
    $('#editCategoryModal').modal('show');
});

$('#saveEditCategoryBtn').on('click', function () {
    const categoryId = $('#editCategoryModal').data('categoryid');
    const subcategoryId = $('#editCategoryModal').data('subcategoryid');
    const subsubcategoryId = $('#editCategoryModal').data('subsubcategoryid');
    const name = $('#editCategoryName').val();

    let url = '';
    if (categoryId) {
        url = '/ProductManage/UpdateCategory';
        data = { id: categoryId, name: name };
    } else if (subcategoryId) {
        url = '/ProductManage/UpdateSubcategory';
        data = { id: subcategoryId, name: name };
    } else if (subsubcategoryId) {
        url = '/ProductManage/UpdateSubSubcategory';
        data = { id: subsubcategoryId, name: name };
    }

    $.ajax({
        url: url,
        method: 'PUT',
        data: data,
        success: function () {
            $('#editCategoryModal').modal('hide');
            loadCategories();
        },
        error: function (xhr, status, error) {
            console.error('Error updating category:', error);
            alert('Không thể cập nhật danh mục. Vui lòng thử lại.');
        }
    });
});

// Delete category/subcategory/subsubcategory
$('#categoriesList').on('click', '.action-icon.delete', function () {
    const categoryId = $(this).data('categoryid');
    const subcategoryId = $(this).data('subcategoryid');
    const subsubcategoryId = $(this).data('subsubcategoryid');
    const name = $(this).data('categoryname') || $(this).data('subcategoryname') || $(this).data('subsubcategoryname');

    if (categoryId) {
        $('#deleteCategoryModal #deleteCategoryName').text(name);
        $('#deleteCategoryModal #deleteCategoryId').val(categoryId);
        $('#deleteCategoryModal').modal('show');
    } else if (subcategoryId) {
        $('#deleteSubcategoryModal #deleteSubcategoryName').text(name);
        $('#deleteSubcategoryModal #deleteSubcategoryId').val(subcategoryId);
        $('#deleteSubcategoryModal').modal('show');
    } else if (subsubcategoryId) {
        $('#deleteSubSubcategoryModal #deleteSubSubcategoryName').text(name);
        $('#deleteSubSubcategoryModal #deleteSubSubcategoryId').val(subsubcategoryId);
        $('#deleteSubSubcategoryModal').modal('show');
    }
});

$('#subsubcategoryList').on('click', '.action-icon.delete', function () {
    const subsubcategoryId = $(this).data('subsubcategoryid');
    const subsubcategoryName = $(this).data('subsubcategoryname');
    $('#deleteSubSubcategoryModal #deleteSubSubcategoryName').text(subsubcategoryName);
    $('#deleteSubSubcategoryModal #deleteSubSubcategoryId').val(subsubcategoryId);
    $('#deleteSubSubcategoryModal').modal('show');
});

$('#confirmDeleteCategoryBtn').on('click', function () {
    const categoryId = $('#deleteCategoryId').val();
    $.ajax({
        url: '/ProductManage/DeleteCategory/' + categoryId,
        method: 'DELETE',
        success: function () {
            console.log('Category deleted');
            $('#deleteCategoryModal').modal('hide');
            loadCategories();
            loadProducts();
        },
        error: function (xhr, status, error) {
            console.error('Error deleting category:', error, xhr.status);
            alert('Không thể xóa danh mục.');
        }
    });
});

$('#confirmDeleteSubcategoryBtn').on('click', function () {
    const subcategoryId = $('#deleteSubcategoryId').val();
    $.ajax({
        url: '/ProductManage/DeleteSubcategory/' + subcategoryId,
        method: 'DELETE',
        success: function () {
            console.log('Subcategory deleted');
            $('#deleteSubcategoryModal').modal('hide');
            loadCategories();
            loadProducts();
        },
        error: function (xhr, status, error) {
            console.error('Error deleting subcategory:', error, xhr.status);
            alert('Không thể xóa danh mục phụ.');
        }
    });
});

$('#confirmDeleteSubSubcategoryBtn').on('click', function () {
    const subsubcategoryId = $('#deleteSubSubcategoryId').val();
    $.ajax({
        url: '/ProductManage/DeleteSubSubcategory/' + subsubcategoryId,
        method: 'DELETE',
        success: function () {
            console.log('SubSubcategory deleted');
            $('#deleteSubSubcategoryModal').modal('hide');
            loadCategories();
            loadProducts();
            if ($('#addCategoryModal').data('level') === 'subcategory') {
                loadSubSubcategories($('#addCategoryModal').data('id') || $('#addCategoryModal').data('parentid'));
            }
        },
        error: function (xhr, status, error) {
            console.error('Error deleting subsubcategory:', error, xhr.status);
            alert('Không thể xóa danh mục chi tiết.');
        }
    });
});

// Load categories for add product form
function loadCategoriesForAddProduct() {
    $.get('/ProductManage/GetCategories', function (categories) {
        const categorySelect = $('#productCategory');
        categorySelect.empty().append('<option value="" selected disabled>Chọn danh mục</option>');

        categories.forEach(category => {
            categorySelect.append(`<option value="${category.categoryID}">${category.name}</option>`);
        });

        // Reset and disable other dropdowns
        $('#productSubcategory')
            .empty()
            .append('<option value="" selected disabled>Chọn danh mục phụ</option>')
            .prop('disabled', true);

        $('#productSubSubcategory')
            .empty()
            .append('<option value="" selected disabled>Chọn danh mục chi tiết</option>')
            .prop('disabled', true);
    });
}

// Handle category change in add product form
$('#productCategory').change(function () {
    const categoryId = $(this).val();
    const subcategorySelect = $('#productSubcategory');
    const subSubcategorySelect = $('#productSubSubcategory');

    console.log('Category changed to:', categoryId);

    // Reset subcategory dropdown
    subcategorySelect
        .empty()
        .append('<option value="" selected disabled>Đang tải...</option>')
        .prop('disabled', true);

    // Reset subsubcategory dropdown
    subSubcategorySelect
        .empty()
        .append('<option value="" selected disabled>Chọn danh mục chi tiết</option>')
        .prop('disabled', true);

    if (categoryId) {
        $.ajax({
            url: '/ProductManage/GetSubcategories',
            method: 'GET',
            data: { categoryId: categoryId },
            success: function (subcategories) {
                console.log('Received subcategories:', subcategories);
                subcategorySelect.empty().append('<option value="" selected disabled>Chọn danh mục phụ</option>');

                if (subcategories && subcategories.length > 0) {
                    subcategories.forEach(subcategory => {
                        subcategorySelect.append(`<option value="${subcategory.id}">${subcategory.name}</option>`);
                    });
                    subcategorySelect.prop('disabled', false);
                } else {
                    subcategorySelect.append('<option value="" disabled>Không có danh mục phụ</option>');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading subcategories:', error);
                subcategorySelect.empty().append('<option value="" disabled>Lỗi khi tải danh mục phụ</option>');
                showToast('Không thể tải danh mục phụ. Vui lòng thử lại.', 'error');
            }
        });
    } else {
        subcategorySelect.empty().append('<option value="" selected disabled>Chọn danh mục phụ</option>');
    }
});

// Handle subcategory change in add product form
$('#productSubcategory').change(function () {
    const subcategoryId = $(this).val();
    const subSubcategorySelect = $('#productSubSubcategory');

    console.log('Subcategory changed to:', subcategoryId);

    // Reset subsubcategory dropdown
    subSubcategorySelect
        .empty()
        .append('<option value="" selected disabled>Đang tải...</option>')
        .prop('disabled', true);

    if (subcategoryId) {
        $.ajax({
            url: '/ProductManage/GetSubSubcategories',
            method: 'GET',
            data: { subcategoryId: subcategoryId },
            success: function (subSubcategories) {
                console.log('Received subsubcategories:', subSubcategories);
                subSubcategorySelect.empty().append('<option value="" selected disabled>Chọn danh mục chi tiết</option>');

                if (subSubcategories && subSubcategories.length > 0) {
                    subSubcategories.forEach(subSubcategory => {
                        console.log('Processing subsubcategory:', subSubcategory);
                        subSubcategorySelect.append(`<option value="${subSubcategory.id}">${subSubcategory.name}</option>`);
                    });
                    subSubcategorySelect.prop('disabled', false);
                } else {
                    subSubcategorySelect.append('<option value="" disabled>Không có danh mục chi tiết</option>');
                    showToast('Danh mục phụ này chưa có danh mục chi tiết nào', 'warning');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading subsubcategories:', error);
                console.error('Response:', xhr.responseText);
                subSubcategorySelect.empty().append('<option value="" disabled>Lỗi khi tải danh mục chi tiết</option>');
                showToast('Không thể tải danh mục chi tiết. Vui lòng thử lại.', 'error');
            }
        });
    } else {
        subSubcategorySelect.empty().append('<option value="" selected disabled>Chọn danh mục chi tiết</option>');
    }
});

// Load categories when add product modal is shown
$('#addProductModal').on('show.bs.modal', function () {
    console.log('Add product modal opening');
    resetProductForm();
    loadCategoriesForAddProduct();
});

// Handle modal close event
$('#addProductModal').on('hidden.bs.modal', function () {
    console.log('Add product modal closed');
    resetProductForm();
});

// Handle product status change
$('#productStatus').change(function () {
    const stockInput = $('#productStock');
    if ($(this).val() === 'active') {
        stockInput.prop('disabled', false).val('');
    } else {
        stockInput.prop('disabled', true).val('0');
    }
});

// Handle image upload
let selectedImages = [];
let primaryImageIndex = -1;

$('#uploadImageBtn').click(function () {
    $('#productImage').click();
});

$('#productImage').change(function (e) {
    const files = Array.from(e.target.files);
    if (files.length > 0) {
        selectedImages = selectedImages.concat(files);
        updateImageTable();
    }
});

function updateImageTable() {
    const tbody = $('#imageTableBody');
    tbody.empty();

    if (selectedImages.length === 0) {
        tbody.append('<tr><td colspan="4" class="text-center">Chưa có ảnh nào được chọn</td></tr>');
        return;
    }

    selectedImages.forEach((file, index) => {
        const row = $(`
            <tr>
                <td>${file.name}</td>
                <td>
                    <div class="form-check">
                        <input class="form-check-input primary-image-radio" type="radio" 
                            name="primaryImage" value="${index}" 
                            ${index === primaryImageIndex ? 'checked' : ''}>
                    </div>
                </td>
                <td>
                    <button type="button" class="btn btn-sm btn-info watch-image" data-index="${index}">
                        <i class="fas fa-eye"></i>
                    </button>
                </td>
                <td>
                    <button type="button" class="btn btn-sm btn-danger remove-image" data-index="${index}">
                        <i class="fas fa-trash"></i>
                    </button>
                </td>
            </tr>
        `);
        tbody.append(row);
    });
}

$(document).on('change', '.primary-image-radio', function () {
    primaryImageIndex = parseInt($(this).val());
});

$(document).on('click', '.watch-image', function () {
    const index = $(this).data('index');
    const file = selectedImages[index];
    const reader = new FileReader();

    reader.onload = function (e) {
        const modal = $(`
            <div class="modal fade" id="imagePreviewModal" tabindex="-1">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">Xem trước ảnh</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body text-center">
                            <img src="${e.target.result}" class="img-fluid" alt="Preview">
                        </div>
                    </div>
                </div>
            </div>
        `);
        modal.modal('show');
    };
    reader.readAsDataURL(file);
});

$(document).on('click', '.remove-image', function () {
    const index = $(this).data('index');
    selectedImages.splice(index, 1);
    if (primaryImageIndex === index) {
        primaryImageIndex = -1;
    } else if (primaryImageIndex > index) {
        primaryImageIndex--;
    }
    updateImageTable();
});

// Handle form submission
$('#saveProductBtn').click(function () {
    if (!validateProductForm()) {
        return;
    }

    const formData = new FormData();

    // Thông tin cơ bản
    formData.append('Name', $('#productName').val());
    formData.append('Description', $('#productDescription').val() || '');
    formData.append('Price', $('#productPrice').val());
    formData.append('Stock', $('#productStock').val() || '0');
    formData.append('SubSubcategoryID', $('#productSubSubcategory').val());
    formData.append('Brand', $('#productBrand').val() || '');

    // Trạng thái sản phẩm
    const status = $('#productStatus').val();
    const isActive = status === 'active';
    formData.append('IsActive', isActive);

    console.log('Submitting form with SubSubcategoryID:', $('#productSubSubcategory').val());
    console.log('Product status:', status, 'IsActive:', isActive);

    // Thuộc tính sản phẩm
    const attributes = [];
    $('#productAttributes .attribute-item').each(function () {
        const name = $(this).find('input[name*="name"]').val();
        const value = $(this).find('input[name*="value"]').val();
        if (name && value) {
            attributes.push({ name: name, value: value });
        }
    });
    if (attributes.length > 0) {
        formData.append('Attributes', JSON.stringify(attributes));
    }

    // Hình ảnh
    selectedImages.forEach((file, index) => {
        formData.append('images', file);
    });

    // Đánh dấu ảnh chính
    if (primaryImageIndex >= 0) {
        formData.append('PrimaryImageIndex', primaryImageIndex);
    }

    // Show loading
    $('#saveProductBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Đang xử lý...');

    // Submit form
    $.ajax({
        url: '/ProductManage/AddProduct',
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (response) {
            console.log('Product added successfully:', response);
            $('#addProductModal').modal('hide');
            loadProducts();
            showToast('Thêm sản phẩm thành công!', 'success');
            resetProductForm();
        },
        error: function (xhr, status, error) {
            console.error('Error adding product:', error);
            console.error('Response:', xhr.responseText);
            console.error('Status:', xhr.status);

            let errorMessage = 'Lỗi khi thêm sản phẩm';

            if (xhr.status === 400) {
                try {
                    const response = JSON.parse(xhr.responseText);
                    if (response.errors) {
                        const errors = Object.values(response.errors).flat();
                        errorMessage = errors.join('. ');
                    } else if (response.message) {
                        errorMessage = response.message;
                    } else if (typeof response === 'string') {
                        errorMessage = response;
                    }
                } catch (e) {
                    errorMessage = xhr.responseText || 'Dữ liệu không hợp lệ';
                }
            } else if (xhr.status === 500) {
                errorMessage = 'Lỗi server. Vui lòng thử lại sau.';
            } else if (xhr.responseText) {
                errorMessage = xhr.responseText;
            }

            showToast(errorMessage, 'error');
        },
        complete: function () {
            $('#saveProductBtn').prop('disabled', false).html('Thêm Sản phẩm');
        }
    });
});

// Cập nhật hàm validate
function validateProductForm() {
    // Kiểm tra tên sản phẩm
    const productName = $('#productName').val().trim();
    if (!productName) {
        showToast('Vui lòng nhập tên sản phẩm', 'error');
        $('#productName').focus();
        return false;
    }
    if (productName.length < 3) {
        showToast('Tên sản phẩm phải có ít nhất 3 ký tự', 'error');
        $('#productName').focus();
        return false;
    }

    // Kiểm tra danh mục
    if (!$('#productCategory').val()) {
        showToast('Vui lòng chọn danh mục chính', 'error');
        $('#productCategory').focus();
        return false;
    }
    if (!$('#productSubcategory').val()) {
        showToast('Vui lòng chọn danh mục phụ', 'error');
        $('#productSubcategory').focus();
        return false;
    }
    if (!$('#productSubSubcategory').val()) {
        showToast('Vui lòng chọn danh mục chi tiết', 'error');
        $('#productSubSubcategory').focus();
        return false;
    }

    // Kiểm tra giá
    const price = parseFloat($('#productPrice').val());
    if (!price || price <= 0) {
        showToast('Vui lòng nhập giá sản phẩm hợp lệ (lớn hơn 0)', 'error');
        $('#productPrice').focus();
        return false;
    }
    if (price > 999999999) {
        showToast('Giá sản phẩm không được vượt quá 999,999,999 đ', 'error');
        $('#productPrice').focus();
        return false;
    }

    // Kiểm tra trạng thái và số lượng
    const status = $('#productStatus').val();
    const stock = parseInt($('#productStock').val()) || 0;

    if (!status) {
        showToast('Vui lòng chọn trạng thái sản phẩm', 'error');
        $('#productStatus').focus();
        return false;
    }

    if (status === 'active' && stock <= 0) {
        showToast('Sản phẩm còn hàng phải có số lượng lớn hơn 0', 'error');
        $('#productStock').focus();
        return false;
    }

    if (stock < 0) {
        showToast('Số lượng không được âm', 'error');
        $('#productStock').focus();
        return false;
    }

    // Kiểm tra hình ảnh
    if (selectedImages.length === 0) {
        showToast('Vui lòng chọn ít nhất một ảnh sản phẩm', 'error');
        $('#uploadImageBtn').focus();
        return false;
    }

    if (selectedImages.length > 10) {
        showToast('Chỉ được tải lên tối đa 10 ảnh', 'error');
        return false;
    }

    if (primaryImageIndex === -1 && selectedImages.length > 0) {
        // Tự động chọn ảnh đầu tiên làm ảnh chính
        primaryImageIndex = 0;
        $('input[name="primaryImage"][value="0"]').prop('checked', true);
        showToast('Đã tự động chọn ảnh đầu tiên làm ảnh chính', 'info');
    }

    // Kiểm tra kích thước file ảnh
    for (let i = 0; i < selectedImages.length; i++) {
        const file = selectedImages[i];
        if (file.size > 5 * 1024 * 1024) { // 5MB
            showToast(`Ảnh "${file.name}" vượt quá 5MB. Vui lòng chọn ảnh nhỏ hơn.`, 'error');
            return false;
        }
    }

    // Kiểm tra thuộc tính (nếu có)
    const attributeItems = $('#productAttributes .attribute-item');
    let hasInvalidAttribute = false;

    attributeItems.each(function () {
        const name = $(this).find('input[name*="name"]').val().trim();
        const value = $(this).find('input[name*="value"]').val().trim();

        if ((name && !value) || (!name && value)) {
            showToast('Thuộc tính phải có đầy đủ tên và giá trị', 'error');
            hasInvalidAttribute = true;
            return false;
        }
    });

    if (hasInvalidAttribute) {
        return false;
    }

    return true;
}

function resetProductForm() {
    $('#addProductForm')[0].reset();

    // Reset trạng thái các field
    $('#productStock').prop('disabled', true).val('0');
    $('#productSubcategory').prop('disabled', true).empty().append('<option value="" selected disabled>Chọn danh mục phụ</option>');
    $('#productSubSubcategory').prop('disabled', true).empty().append('<option value="" selected disabled>Chọn danh mục chi tiết</option>');

    // Reset thuộc tính
    $('#productAttributes').empty();

    // Reset hình ảnh
    selectedImages = [];
    primaryImageIndex = -1;
    updateImageTable();

    // Reset file input
    $('#productImage').val('');

    // Reload categories
    loadCategoriesForAddProduct();

    console.log('Product form reset successfully');
}

// Handle add attribute button
$('#addAttributeBtn').click(function () {
    const attributeHtml = `
        <div class="attribute-item mb-2">
            <div class="input-group">
                <input type="text" class="form-control" placeholder="Tên thuộc tính (VD: Màu sắc)" name="attributes[].name">
                <input type="text" class="form-control" placeholder="Giá trị (VD: Đỏ)" name="attributes[].value">
                <button type="button" class="btn btn-outline-danger remove-attribute" title="Xóa thuộc tính">
                    <i class="fas fa-trash"></i>
                </button>
            </div>
        </div>
    `;
    $('#productAttributes').append(attributeHtml);
});

// Handle remove attribute
$(document).on('click', '.remove-attribute', function () {
    $(this).closest('.attribute-item').remove();
});

// Edit Product Variables
let editSelectedImages = [];
let editPrimaryImageIndex = -1;
let editExistingImages = [];
let editDeletedImageIds = [];

// Handle edit product click
$(document).on('click', '.edit-product', function () {
    const productId = $(this).data('id');
    console.log('Edit product clicked, ID:', productId);
    loadProductForEdit(productId);
});

// Load product data for editing
function loadProductForEdit(productId) {
    console.log('Loading product for edit:', productId);

    $.ajax({
        url: '/ProductManage/GetProduct',
        method: 'GET',
        data: { id: productId },
        success: function (product) {
            console.log('Product data loaded:', product);
            populateEditForm(product);
            $('#editProductModal').modal('show');
        },
        error: function (xhr, status, error) {
            console.error('Error loading product:', error);
            showToast('Không thể tải thông tin sản phẩm', 'error');
        }
    });
}

// Populate edit form with product data
function populateEditForm(product) {
    console.log('Populating edit form with:', product);

    // Basic info
    $('#editProductId').val(product.id);
    $('#editProductName').val(product.name);
    $('#editProductDescription').val(product.description);
    $('#editProductPrice').val(product.price);
    $('#editProductStock').val(product.stock);
    $('#editProductBrand').val(product.brand);
    $('#editProductStatus').val(product.isActive ? 'active' : 'inactive');

    // Handle stock field based on status
    if (product.isActive) {
        $('#editProductStock').prop('disabled', false);
    } else {
        $('#editProductStock').prop('disabled', true).val('0');
    }

    // Reset and load categories
    loadCategoriesForEdit(product.categoryId, product.subcategoryId, product.subSubcategoryId);

    // Reset images
    editSelectedImages = [];
    editPrimaryImageIndex = -1;
    editExistingImages = product.images || [];
    editDeletedImageIds = [];
    updateEditImageTable();

    // Load attributes
    loadAttributesForEdit(product.attributes || []);
}

// Load categories for edit form with preselection
function loadCategoriesForEdit(selectedCategoryId, selectedSubcategoryId, selectedSubSubcategoryId) {
    $.get('/ProductManage/GetCategories', function (categories) {
        const categorySelect = $('#editProductCategory');
        categorySelect.empty().append('<option value="" selected disabled>Chọn danh mục</option>');

        categories.forEach(category => {
            const isSelected = category.categoryID === selectedCategoryId ? 'selected' : '';
            categorySelect.append(`<option value="${category.categoryID}" ${isSelected}>${category.name}</option>`);
        });

        // Load subcategories if category is selected
        if (selectedCategoryId) {
            loadSubcategoriesForEdit(selectedCategoryId, selectedSubcategoryId, selectedSubSubcategoryId);
        }
    });
}

// Load subcategories for edit
function loadSubcategoriesForEdit(categoryId, selectedSubcategoryId, selectedSubSubcategoryId) {
    $.ajax({
        url: '/ProductManage/GetSubcategories',
        method: 'GET',
        data: { categoryId: categoryId },
        success: function (subcategories) {
            const subcategorySelect = $('#editProductSubcategory');
            subcategorySelect.empty().append('<option value="" disabled>Chọn danh mục phụ</option>');

            if (subcategories && subcategories.length > 0) {
                subcategories.forEach(subcategory => {
                    const isSelected = subcategory.id === selectedSubcategoryId ? 'selected' : '';
                    subcategorySelect.append(`<option value="${subcategory.id}" ${isSelected}>${subcategory.name}</option>`);
                });
                subcategorySelect.prop('disabled', false);

                // Load subsubcategories if subcategory is selected
                if (selectedSubcategoryId) {
                    loadSubSubcategoriesForEdit(selectedSubcategoryId, selectedSubSubcategoryId);
                }
            }
        },
        error: function () {
            showToast('Không thể tải danh mục phụ', 'error');
        }
    });
}

// Load subsubcategories for edit
function loadSubSubcategoriesForEdit(subcategoryId, selectedSubSubcategoryId) {
    $.ajax({
        url: '/ProductManage/GetSubSubcategories',
        method: 'GET',
        data: { subcategoryId: subcategoryId },
        success: function (subSubcategories) {
            const subSubcategorySelect = $('#editProductSubSubcategory');
            subSubcategorySelect.empty().append('<option value="" disabled>Chọn danh mục chi tiết</option>');

            if (subSubcategories && subSubcategories.length > 0) {
                subSubcategories.forEach(subSubcategory => {
                    const isSelected = subSubcategory.id === selectedSubSubcategoryId ? 'selected' : '';
                    subSubcategorySelect.append(`<option value="${subSubcategory.id}" ${isSelected}>${subSubcategory.name}</option>`);
                });
                subSubcategorySelect.prop('disabled', false);
            }
        },
        error: function () {
            showToast('Không thể tải danh mục chi tiết', 'error');
        }
    });
}

// Load attributes for edit
function loadAttributesForEdit(attributes) {
    const container = $('#editProductAttributes');
    container.empty();

    attributes.forEach(attr => {
        const attributeHtml = `
            <div class="attribute-item mb-2">
                <div class="input-group">
                    <input type="text" class="form-control" placeholder="Tên thuộc tính" value="${attr.name}" name="attributes[].name">
                    <input type="text" class="form-control" placeholder="Giá trị" value="${attr.value}" name="attributes[].value">
                    <button type="button" class="btn btn-outline-danger remove-attribute" title="Xóa thuộc tính">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </div>
        `;
        container.append(attributeHtml);
    });
}

// Update edit image table
function updateEditImageTable() {
    const tbody = $('#editImageTableBody');
    tbody.empty();

    // Show existing images first
    editExistingImages.forEach((image, index) => {
        if (!editDeletedImageIds.includes(image.id)) {
            const row = $(`
                <tr data-image-type="existing" data-image-id="${image.id}">
                    <td>${image.fileName}</td>
                    <td>
                        <div class="form-check">
                            <input class="form-check-input edit-primary-image-radio" type="radio" 
                                name="editPrimaryImage" value="existing-${index}" 
                                ${image.isPrimary ? 'checked' : ''}>
                        </div>
                    </td>
                    <td>
                        <button type="button" class="btn btn-sm btn-info watch-edit-image" data-url="${image.url}">
                            <i class="fas fa-eye"></i>
                        </button>
                    </td>
                    <td>
                        <button type="button" class="btn btn-sm btn-danger remove-existing-image" data-image-id="${image.id}">
                            <i class="fas fa-trash"></i>
                        </button>
                    </td>
                </tr>
            `);
            tbody.append(row);
        }
    });

    // Show new images
    editSelectedImages.forEach((file, index) => {
        const newIndex = editExistingImages.length + index;
        const row = $(`
            <tr data-image-type="new" data-image-index="${index}">
                <td>${file.name}</td>
                <td>
                    <div class="form-check">
                        <input class="form-check-input edit-primary-image-radio" type="radio" 
                            name="editPrimaryImage" value="new-${index}">
                    </div>
                </td>
                <td>
                    <button type="button" class="btn btn-sm btn-info watch-new-edit-image" data-index="${index}">
                        <i class="fas fa-eye"></i>
                    </button>
                </td>
                <td>
                    <button type="button" class="btn btn-sm btn-danger remove-new-edit-image" data-index="${index}">
                        <i class="fas fa-trash"></i>
                    </button>
                </td>
            </tr>
        `);
        tbody.append(row);
    });

    if (editExistingImages.filter(img => !editDeletedImageIds.includes(img.id)).length === 0 && editSelectedImages.length === 0) {
        tbody.append('<tr><td colspan="4" class="text-center">Chưa có ảnh nào</td></tr>');
    }
}

// Handle edit category change
$('#editProductCategory').change(function () {
    const categoryId = $(this).val();
    loadSubcategoriesForEdit(categoryId, null, null);

    // Reset lower level dropdowns
    $('#editProductSubSubcategory').empty().append('<option value="" disabled>Chọn danh mục chi tiết</option>').prop('disabled', true);
});

// Handle edit subcategory change
$('#editProductSubcategory').change(function () {
    const subcategoryId = $(this).val();
    loadSubSubcategoriesForEdit(subcategoryId, null);
});

// Handle edit product status change
$('#editProductStatus').change(function () {
    const stockInput = $('#editProductStock');
    if ($(this).val() === 'active') {
        stockInput.prop('disabled', false).val('');
    } else {
        stockInput.prop('disabled', true).val('0');
    }
});

// Handle edit image upload
$('#editUploadImageBtn').click(function () {
    $('#editProductImage').click();
});

$('#editProductImage').change(function (e) {
    const files = Array.from(e.target.files);
    if (files.length > 0) {
        editSelectedImages = editSelectedImages.concat(files);
        updateEditImageTable();
    }
});

// Handle edit attribute buttons
$('#editAddAttributeBtn').click(function () {
    const attributeHtml = `
        <div class="attribute-item mb-2">
            <div class="input-group">
                <input type="text" class="form-control" placeholder="Tên thuộc tính (VD: Màu sắc)" name="attributes[].name">
                <input type="text" class="form-control" placeholder="Giá trị (VD: Đỏ)" name="attributes[].value">
                <button type="button" class="btn btn-outline-danger remove-attribute" title="Xóa thuộc tính">
                    <i class="fas fa-trash"></i>
                </button>
            </div>
        </div>
    `;
    $('#editProductAttributes').append(attributeHtml);
});

// Event handlers for edit image table
$(document).on('click', '.remove-existing-image', function () {
    const imageId = $(this).data('image-id');
    editDeletedImageIds.push(imageId);
    updateEditImageTable();
});

$(document).on('click', '.remove-new-edit-image', function () {
    const index = $(this).data('index');
    editSelectedImages.splice(index, 1);
    updateEditImageTable();
});

$(document).on('click', '.watch-edit-image', function () {
    const url = $(this).data('url');
    showImagePreview(url);
});

$(document).on('click', '.watch-new-edit-image', function () {
    const index = $(this).data('index');
    const file = editSelectedImages[index];
    const reader = new FileReader();

    reader.onload = function (e) {
        showImagePreview(e.target.result);
    };
    reader.readAsDataURL(file);
});

$(document).on('change', '.edit-primary-image-radio', function () {
    const value = $(this).val();
    console.log('Primary image changed to:', value);
    // Store the selection for later use
    editPrimaryImageIndex = value;
});

// Show image preview modal
function showImagePreview(imageSrc) {
    const modal = $(`
        <div class="modal fade" id="editImagePreviewModal" tabindex="-1">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Xem trước ảnh</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body text-center">
                        <img src="${imageSrc}" class="img-fluid" alt="Preview">
                    </div>
                </div>
            </div>
        </div>
    `);

    // Remove existing preview modal
    $('#editImagePreviewModal').remove();

    $('body').append(modal);
    modal.modal('show');

    // Auto remove when hidden
    modal.on('hidden.bs.modal', function () {
        $(this).remove();
    });
}

// Handle edit modal events
$('#editProductModal').on('show.bs.modal', function () {
    console.log('Edit product modal opening');
});

$('#editProductModal').on('hidden.bs.modal', function () {
    console.log('Edit product modal closed');
    resetEditForm();
});

// Reset edit form
function resetEditForm() {
    $('#editProductForm')[0].reset();
    $('#editProductStock').prop('disabled', true);
    $('#editProductSubcategory').prop('disabled', true);
    $('#editProductSubSubcategory').prop('disabled', true);
    $('#editProductAttributes').empty();
    editSelectedImages = [];
    editPrimaryImageIndex = -1;
    editExistingImages = [];
    editDeletedImageIds = [];
    updateEditImageTable();
}

// Handle update product button click
$('#updateProductBtn').click(function () {
    if (!validateEditProductForm()) {
        return;
    }

    const formData = new FormData();

    // Basic product info
    formData.append('ProductID', $('#editProductId').val());
    formData.append('Name', $('#editProductName').val());
    formData.append('Description', $('#editProductDescription').val() || '');
    formData.append('Price', $('#editProductPrice').val());
    formData.append('Stock', $('#editProductStock').val() || '0');
    formData.append('SubSubcategoryID', $('#editProductSubSubcategory').val());
    formData.append('Brand', $('#editProductBrand').val() || '');

    // Status
    const status = $('#editProductStatus').val();
    const isActive = status === 'active';
    formData.append('IsActive', isActive);

    // Deleted image IDs
    if (editDeletedImageIds.length > 0) {
        formData.append('DeletedImageIds', editDeletedImageIds.join(','));
    }

    // New images
    editSelectedImages.forEach((file, index) => {
        formData.append('images', file);
    });

    // Primary image index (need to calculate correctly)
    if (editPrimaryImageIndex && editPrimaryImageIndex !== -1) {
        if (editPrimaryImageIndex.startsWith('existing-')) {
            const existingIndex = parseInt(editPrimaryImageIndex.split('-')[1]);
            formData.append('PrimaryImageIndex', existingIndex);
        } else if (editPrimaryImageIndex.startsWith('new-')) {
            const newIndex = parseInt(editPrimaryImageIndex.split('-')[1]);
            const remainingExistingCount = editExistingImages.filter(img => !editDeletedImageIds.includes(img.id)).length;
            formData.append('PrimaryImageIndex', remainingExistingCount + newIndex);
        }
    }

    // Attributes
    const attributes = [];
    $('#editProductAttributes .attribute-item').each(function () {
        const name = $(this).find('input[name*="name"]').val().trim();
        const value = $(this).find('input[name*="value"]').val().trim();
        if (name && value) {
            attributes.push({ name: name, value: value });
        }
    });
    if (attributes.length > 0) {
        formData.append('Attributes', JSON.stringify(attributes));
    }

    // Show loading
    $('#updateProductBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Đang cập nhật...');

    // Submit form
    $.ajax({
        url: '/ProductManage/UpdateProduct',
        type: 'PUT',
        data: formData,
        processData: false,
        contentType: false,
        success: function (response) {
            console.log('Product updated successfully:', response);
            $('#editProductModal').modal('hide');
            loadProducts();
            showToast('Cập nhật sản phẩm thành công!', 'success');
        },
        error: function (xhr, status, error) {
            console.error('Error updating product:', error);
            console.error('Response:', xhr.responseText);

            let errorMessage = 'Lỗi khi cập nhật sản phẩm';

            if (xhr.status === 400) {
                try {
                    const response = JSON.parse(xhr.responseText);
                    if (response.errors) {
                        const errors = Object.values(response.errors).flat();
                        errorMessage = errors.join('. ');
                    } else if (response.message) {
                        errorMessage = response.message;
                    } else if (typeof response === 'string') {
                        errorMessage = response;
                    }
                } catch (e) {
                    errorMessage = xhr.responseText || 'Dữ liệu không hợp lệ';
                }
            } else if (xhr.status === 500) {
                errorMessage = 'Lỗi server. Vui lòng thử lại sau.';
            } else if (xhr.responseText) {
                errorMessage = xhr.responseText;
            }

            showToast(errorMessage, 'error');
        },
        complete: function () {
            $('#updateProductBtn').prop('disabled', false).html('Cập nhật Sản phẩm');
        }
    });
});

// Validate edit product form
function validateEditProductForm() {
    // Check product name
    const productName = $('#editProductName').val().trim();
    if (!productName) {
        showToast('Vui lòng nhập tên sản phẩm', 'error');
        $('#editProductName').focus();
        return false;
    }
    if (productName.length < 3) {
        showToast('Tên sản phẩm phải có ít nhất 3 ký tự', 'error');
        $('#editProductName').focus();
        return false;
    }

    // Check categories
    if (!$('#editProductCategory').val()) {
        showToast('Vui lòng chọn danh mục chính', 'error');
        $('#editProductCategory').focus();
        return false;
    }
    if (!$('#editProductSubcategory').val()) {
        showToast('Vui lòng chọn danh mục phụ', 'error');
        $('#editProductSubcategory').focus();
        return false;
    }
    if (!$('#editProductSubSubcategory').val()) {
        showToast('Vui lòng chọn danh mục chi tiết', 'error');
        $('#editProductSubSubcategory').focus();
        return false;
    }

    // Check price
    const price = parseFloat($('#editProductPrice').val());
    if (!price || price <= 0) {
        showToast('Vui lòng nhập giá sản phẩm hợp lệ (lớn hơn 0)', 'error');
        $('#editProductPrice').focus();
        return false;
    }
    if (price > 999999999) {
        showToast('Giá sản phẩm không được vượt quá 999,999,999 đ', 'error');
        $('#editProductPrice').focus();
        return false;
    }

    // Check status and stock
    const status = $('#editProductStatus').val();
    const stock = parseInt($('#editProductStock').val()) || 0;

    if (!status) {
        showToast('Vui lòng chọn trạng thái sản phẩm', 'error');
        $('#editProductStatus').focus();
        return false;
    }

    if (status === 'active' && stock <= 0) {
        showToast('Sản phẩm còn hàng phải có số lượng lớn hơn 0', 'error');
        $('#editProductStock').focus();
        return false;
    }

    if (stock < 0) {
        showToast('Số lượng không được âm', 'error');
        $('#editProductStock').focus();
        return false;
    }

    // Check if at least one image remains (existing + new - deleted)
    const remainingExistingImages = editExistingImages.filter(img => !editDeletedImageIds.includes(img.id));
    const totalImages = remainingExistingImages.length + editSelectedImages.length;

    if (totalImages === 0) {
        showToast('Sản phẩm phải có ít nhất một ảnh', 'error');
        return false;
    }

    if (editSelectedImages.length > 10) {
        showToast('Chỉ được tải lên tối đa 10 ảnh mới', 'error');
        return false;
    }

    // Check file sizes for new images
    for (let i = 0; i < editSelectedImages.length; i++) {
        const file = editSelectedImages[i];
        if (file.size > 5 * 1024 * 1024) { // 5MB
            showToast(`Ảnh "${file.name}" vượt quá 5MB. Vui lòng chọn ảnh nhỏ hơn.`, 'error');
            return false;
        }
    }

    // Check attributes
    const attributeItems = $('#editProductAttributes .attribute-item');
    let hasInvalidAttribute = false;

    attributeItems.each(function () {
        const name = $(this).find('input[name*="name"]').val().trim();
        const value = $(this).find('input[name*="value"]').val().trim();

        if ((name && !value) || (!name && value)) {
            showToast('Thuộc tính phải có đầy đủ tên và giá trị', 'error');
            hasInvalidAttribute = true;
            return false;
        }
    });

    if (hasInvalidAttribute) {
        return false;
    }

    return true;
}

// Initial load
$(document).ready(function () {
    console.log('Document ready');
    loadCategories();
    loadProducts();
});

function showToast(message, type = 'info') {
    // Tạo toast container nếu chưa có
    if ($('.toast-container').length === 0) {
        $('body').append('<div class="toast-container position-fixed top-0 end-0 p-3" style="z-index: 9999;"></div>');
    }

    const toastId = 'toast-' + Date.now();
    const iconClass = type === 'success' ? 'fas fa-check-circle text-success' :
        type === 'error' ? 'fas fa-times-circle text-danger' :
            type === 'warning' ? 'fas fa-exclamation-triangle text-warning' :
                'fas fa-info-circle text-info';

    const toast = $(`
        <div id="${toastId}" class="toast" role="alert" aria-live="assertive" aria-atomic="true" data-bs-delay="4000">
            <div class="toast-header">
                <i class="${iconClass} me-2"></i>
                <strong class="me-auto">Thông báo</strong>
                <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        </div>
    `);

    $('.toast-container').append(toast);

    // Sử dụng Bootstrap toast
    const bsToast = new bootstrap.Toast(toast[0]);
    bsToast.show();

    // Tự động xóa sau khi ẩn
    toast.on('hidden.bs.toast', function () {
        $(this).remove();
    });
}