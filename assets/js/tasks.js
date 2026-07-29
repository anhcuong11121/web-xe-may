// tasks.js - Quản lý sản phẩm (Admin) - CRUD thật qua MotorBikeShop.API + danh sách yêu cầu chăm sóc khách hàng

let currentProductId = null;
let currentPage = 1;
const pageSize = 10;

function escHtml(str) {
  return (str || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

async function loadBrandsIntoSelects() {
  let brands = [];
  try {
    brands = await Api.getBrands();
  } catch (err) {
    showToast('Không tải được danh sách hãng xe: ' + apiErrorMessage(err), 'danger');
  }

  const prioritySelect = document.getElementById('priority');
  const priorityFilter = document.getElementById('priorityFilter');
  if (prioritySelect) {
    prioritySelect.innerHTML = brands.map(b => `<option value="${b.id}">${escHtml(b.name)}</option>`).join('');
  }
  if (priorityFilter) {
    priorityFilter.innerHTML = '<option value="">Tất cả hãng</option>' +
      brands.map(b => `<option value="${b.id}">${escHtml(b.name)}</option>`).join('');
  }
}

async function loadVehicleTypesIntoSelect() {
  try {
    const vehicleTypes = await Api.getVehicleTypes();
    const select = document.getElementById('vehicleType');
    if (select) {
      select.innerHTML = '<option value="">Chưa phân loại</option>' +
        vehicleTypes.map(v => `<option value="${v.id}">${escHtml(v.name)}</option>`).join('');
    }
  } catch (err) {
    showToast('Không tải được danh sách loại xe: ' + apiErrorMessage(err), 'danger');
  }
}

async function loadProducts() {
  const taskList = document.getElementById('taskList');
  if (!taskList) return;

  try {
    const search = document.getElementById('searchInput')?.value.trim() || '';
    const brandId = document.getElementById('priorityFilter')?.value || '';
    const query = new URLSearchParams();
    query.set('pageNumber', currentPage);
    query.set('pageSize', pageSize);
    if (search) query.set('keyword', search);
    if (brandId) query.set('brandId', brandId);

    const result = await Api.getCatalogProducts('?' + query.toString());

    const statusFilter = document.getElementById('statusFilter')?.value || '';
    let items = result.items;
    if (statusFilter === 'Available') items = items.filter(p => p.totalStock > 0);
    if (statusFilter === 'Sold') items = items.filter(p => p.totalStock <= 0);

    renderProducts(items);
    renderPagination(result.totalCount, result.pageNumber, result.pageSize);
    updateQuickStats(result.totalCount, items);
  } catch (err) {
    taskList.innerHTML = `<tr><td colspan="7" class="text-center text-danger py-3">Không tải được sản phẩm: ${apiErrorMessage(err)}</td></tr>`;
  }
}

function renderProducts(items) {
  const taskList = document.getElementById('taskList');
  if (!items.length) {
    taskList.innerHTML = '<tr><td colspan="7" class="text-center text-muted py-3">Chưa có sản phẩm nào.</td></tr>';
    return;
  }

  taskList.innerHTML = items.map(p => `
    <tr class="${p.totalStock <= 0 ? 'table-warning' : ''}">
      <td>${p.id}</td>
      <td>${escHtml(p.name)}</td>
      <td>${escHtml((p.description || '').length > 50 ? p.description.substring(0, 50) + '...' : (p.description || ''))}</td>
      <td>${p.minimumPrice == null ? 'Liên hệ' : (p.maximumPrice !== p.minimumPrice ? `${formatCurrencyVnd(p.minimumPrice)} - ${formatCurrencyVnd(p.maximumPrice)}` : formatCurrencyVnd(p.minimumPrice))}</td>
      <td><span class="badge bg-danger">${escHtml(p.brandName || '-')}</span></td>
      <td><span class="badge bg-${p.totalStock > 0 ? 'success' : 'secondary'}">${p.totalStock > 0 ? p.totalStock + ' xe' : 'Hết hàng'}</span></td>
      <td>
        <button class="btn btn-sm btn-primary me-1" onclick="editProductRow(${p.id})">Sửa</button>
        <button class="btn btn-sm btn-outline-primary me-1" onclick="openCatalogManager(${p.id})">Phiên bản/SKU</button>
        <button class="btn btn-sm btn-danger" onclick="deleteProductRow(${p.id})">Xóa</button>
      </td>
    </tr>
  `).join('');
}

function renderPagination(totalCount, pageNumber, size) {
  const container = document.getElementById('paginationContainer');
  if (!container) return;
  const totalPages = Math.ceil(totalCount / size);
  if (totalPages <= 1) { container.innerHTML = ''; return; }

  container.innerHTML = `
    <nav>
      <ul class="pagination justify-content-center">
        <li class="page-item ${pageNumber === 1 ? 'disabled' : ''}">
          <a class="page-link" href="#" onclick="event.preventDefault(); changePage(${pageNumber - 1})">Trước</a>
        </li>
        ${Array.from({ length: totalPages }, (_, i) => `
          <li class="page-item ${i + 1 === pageNumber ? 'active' : ''}">
            <a class="page-link" href="#" onclick="event.preventDefault(); changePage(${i + 1})">${i + 1}</a>
          </li>
        `).join('')}
        <li class="page-item ${pageNumber === totalPages ? 'disabled' : ''}">
          <a class="page-link" href="#" onclick="event.preventDefault(); changePage(${pageNumber + 1})">Sau</a>
        </li>
      </ul>
    </nav>`;
}

function changePage(page) {
  if (page < 1) return;
  currentPage = page;
  loadProducts();
}

function updateQuickStats(totalCount, items) {
  const total = document.getElementById('quickTotal');
  if (!total) return;
  const available = items.filter(p => p.totalStock > 0).length;
  const sold = items.filter(p => p.totalStock <= 0).length;
  const counts = {};
  items.forEach(p => { counts[p.brandName] = (counts[p.brandName] || 0) + 1; });
  const topBrand = Object.keys(counts).sort((a, b) => counts[b] - counts[a])[0] || '-';

  total.textContent = totalCount;
  document.getElementById('quickAvailable').textContent = available;
  document.getElementById('quickSold').textContent = sold;
  document.getElementById('quickBrand').textContent = topBrand;
}

function openAddModal() {
  currentProductId = null;
  document.getElementById('modalTitle').textContent = 'Thêm sản phẩm';
  document.getElementById('taskForm').reset();
  setCatalogSeedFieldsMode(false);
}

function setCatalogSeedFieldsMode(isEditing) {
  document.getElementById('catalogSeedHint')?.classList.toggle('d-none', !isEditing);
  document.getElementById('catalogSeedImage')?.classList.toggle('d-none', isEditing);
  [
    'deadline',
    'productColor',
    'specEngineType',
    'specFuelType',
    'specEngineCapacity',
    'specHorsePower'
  ].forEach(id => {
    const input = document.getElementById(id);
    if (input) input.disabled = isEditing;
  });
}

async function editProductRow(id) {
  try {
    const p = await Api.getProduct(id);
    currentProductId = id;
    document.getElementById('modalTitle').textContent = 'Sửa sản phẩm';
    document.getElementById('title').value = p.name;
    document.getElementById('description').value = p.description;
    document.getElementById('deadline').value = '';
    document.getElementById('priority').value = p.brandId;
    document.getElementById('productColor').value = '';
    document.getElementById('productStatus').value = p.status;
    document.getElementById('vehicleType').value = p.vehicleTypeId || '';
    document.getElementById('specEngineType').value = '';
    document.getElementById('specFuelType').value = '';
    document.getElementById('specEngineCapacity').value = '';
    document.getElementById('specHorsePower').value = '';
    document.getElementById('productImage').value = '';
    setCatalogSeedFieldsMode(true);

    const modal = new bootstrap.Modal(document.getElementById('taskModal'));
    modal.show();
  } catch (err) {
    showToast('Không tải được sản phẩm: ' + apiErrorMessage(err), 'danger');
  }
}

async function saveMotorbike() {
  const name = document.getElementById('title').value.trim();
  const description = document.getElementById('description').value.trim();
  const price = Number(document.getElementById('deadline').value);
  const brandId = Number(document.getElementById('priority').value);
  const color = document.getElementById('productColor').value.trim();
  const status = document.getElementById('productStatus').value;
  const vehicleTypeValue = document.getElementById('vehicleType').value;
  const vehicleTypeId = vehicleTypeValue ? Number(vehicleTypeValue) : null;
  const specification = {
    engineType: document.getElementById('specEngineType').value.trim(),
    fuelType: document.getElementById('specFuelType').value.trim(),
    engineCapacityCc: Number(document.getElementById('specEngineCapacity').value),
    horsePower: Number(document.getElementById('specHorsePower').value)
  };
  const imageFile = document.getElementById('productImage').files[0];

  if (!name) { showToast('Tên xe không được rỗng!', 'danger'); return; }
  if (description.length < 10) { showToast('Mô tả phải có ít nhất 10 ký tự!', 'danger'); return; }
  if (!brandId) { showToast('Vui lòng chọn hãng xe!', 'danger'); return; }
  if (!currentProductId) {
    if (price < 0) { showToast('Giá phải >= 0!', 'danger'); return; }
    if (!color) { showToast('Vui lòng nhập màu sắc!', 'danger'); return; }
    if (!specification.engineType || !specification.fuelType) {
      showToast('Vui lòng nhập đầy đủ thông số kỹ thuật bắt buộc!', 'danger');
      return;
    }
  }

  const payload = { name, description, status, brandId, vehicleTypeId };

  try {
    let productId = currentProductId;
    let defaultVariant = null;
    let defaultSku = null;
    if (currentProductId) {
      await Api.updateProduct(currentProductId, payload);
      if (imageFile) {
        showToast('Ảnh của xe đã lưu phải được tải trong mục Phiên bản/SKU.', 'warning');
      }
    } else {
      const created = await Api.createProduct(payload);
      productId = created.id;
      try {
        defaultVariant = await Api.createProductVariant(productId, {
          name: 'Phiên bản mặc định',
          versionCode: 'V1',
          status: 'Active',
          specification
        });
        defaultSku = await Api.createProductSku(productId, defaultVariant.id, {
          skuCode: `P${String(productId).padStart(10, '0')}-DEFAULT`,
          colorName: color,
          colorHexCode: null,
          price,
          status: 'Active'
        });
        if (imageFile) {
          await Api.uploadProductSkuImage(productId, defaultVariant.id, defaultSku.id, imageFile, {
            altText: `${name} - ${color}`,
            displayOrder: 0,
            isPrimary: true
          });
        }
      } catch (catalogError) {
        await Api.deleteProduct(productId).catch(() => {});
        throw new Error('Không thể tạo catalog mặc định; sản phẩm mới đã được hoàn tác. ' + apiErrorMessage(catalogError));
      }
    }

    const modal = bootstrap.Modal.getInstance(document.getElementById('taskModal'));
    if (modal) modal.hide();
    showToast(currentProductId ? 'Sản phẩm đã được cập nhật!' : 'Sản phẩm đã được thêm!');
    loadProducts();
  } catch (err) {
    showToast('Lưu thất bại: ' + apiErrorMessage(err), 'danger');
  }
}

let currentCatalogProductId = null;
let currentCatalogVariants = [];

function catalogImageUrl(url) {
  if (!url) return 'assets/img/banner-1.jpg';
  if (/^https?:\/\//i.test(url) || url.startsWith('/assets/') || url.startsWith('assets/')) return url;
  return API_BASE_URL + (url.startsWith('/') ? url : '/' + url);
}

async function refreshCatalogManager() {
  currentCatalogVariants = await Api.getProductVariants(currentCatalogProductId, true);
  renderCatalogManager();
  await loadProducts();
}

async function openCatalogManager(productId) {
  try {
    const product = await Api.getProduct(productId);
    currentCatalogProductId = productId;
    document.getElementById('catalogModalTitle').textContent = `Phiên bản và SKU — ${product.name}`;
    closeVariantForm();
    closeSkuForm();
    await refreshCatalogManager();
    new bootstrap.Modal(document.getElementById('catalogModal')).show();
  } catch (err) {
    showToast('Không tải được catalog: ' + apiErrorMessage(err), 'danger');
  }
}

function renderCatalogManager() {
  const container = document.getElementById('catalogVariantsList');
  if (!currentCatalogVariants.length) {
    container.innerHTML = '<div class="alert alert-warning mb-0">Sản phẩm chưa có phiên bản. Hãy tạo phiên bản trước, sau đó thêm SKU theo màu.</div>';
    return;
  }

  container.innerHTML = currentCatalogVariants.map(variant => `
    <section class="card border mb-3">
      <div class="card-header d-flex justify-content-between align-items-start gap-2 flex-wrap">
        <div>
          <div class="fw-bold">${escHtml(variant.name)} <span class="badge bg-${variant.status === 'Active' ? 'success' : 'secondary'}">${escHtml(variant.status)}</span></div>
          <div class="small text-muted">Mã: ${escHtml(variant.versionCode)} · ${variant.specification?.engineCapacityCc || 0}cc · ${escHtml(variant.specification?.engineType || '')}</div>
        </div>
        <div>
          <button class="btn btn-outline-primary btn-sm" onclick="openSkuForm(${variant.id})"><i class="fa-solid fa-plus me-1"></i>Thêm SKU</button>
          <button class="btn btn-primary btn-sm" onclick="openVariantForm(${variant.id})">Sửa</button>
          <button class="btn btn-outline-danger btn-sm" onclick="removeCatalogVariant(${variant.id})">Xóa/Ngừng bán</button>
        </div>
      </div>
      <div class="table-responsive">
        <table class="table table-sm align-middle mb-0">
          <thead><tr><th>Ảnh</th><th>SKU</th><th>Màu</th><th>Giá</th><th>Tồn</th><th>Trạng thái</th><th>Hành động</th></tr></thead>
          <tbody>
            ${(variant.skus || []).length ? variant.skus.map(sku => {
              const imageGallery = (sku.images || []).length
                ? sku.images.map(image => `<span class="d-inline-flex flex-column align-items-center gap-1">
                    <img src="${escHtml(catalogImageUrl(image.url))}" alt="${escHtml(image.altText || sku.colorName)}" class="rounded border ${image.isPrimary ? 'border-danger border-2' : ''}" style="width:54px;height:42px;object-fit:cover" onerror="this.src='assets/img/banner-1.jpg'">
                    <label class="small text-muted d-flex align-items-center gap-1" title="Thứ tự hiển thị">
                      <span>STT</span>
                      <input type="number" min="0" class="form-control form-control-sm py-0 px-1" style="width:52px"
                        value="${image.displayOrder}"
                        onchange="updateCatalogImageOrder(${variant.id}, ${sku.id}, ${image.id}, this.value)">
                    </label>
                    <span class="btn-group btn-group-sm">
                      ${image.isPrimary ? '<span class="badge bg-danger">Chính</span>' : `<button class="btn btn-outline-secondary py-0 px-1" title="Đặt làm ảnh chính" onclick="setCatalogPrimaryImage(${variant.id}, ${sku.id}, ${image.id})"><i class="fa-solid fa-star"></i></button>`}
                      <button class="btn btn-outline-danger py-0 px-1" title="Xóa ảnh" onclick="deleteCatalogSkuImage(${variant.id}, ${sku.id}, ${image.id})"><i class="fa-solid fa-xmark"></i></button>
                    </span>
                  </span>`).join('')
                : '<img src="assets/img/banner-1.jpg" alt="Chưa có ảnh" class="rounded border" style="width:54px;height:42px;object-fit:cover">';
              return `<tr>
                <td><div class="d-flex flex-wrap gap-2">${imageGallery}</div></td>
                <td><code>${escHtml(sku.skuCode)}</code></td>
                <td><span class="d-inline-block rounded-circle border me-1" style="width:16px;height:16px;vertical-align:middle;background:${escHtml(sku.colorHexCode || '#6c757d')}"></span>${escHtml(sku.colorName)}</td>
                <td>${formatCurrencyVnd(sku.price)}</td>
                <td><span class="badge bg-${sku.stockQuantity > 0 ? 'success' : 'secondary'}">${sku.stockQuantity}</span></td>
                <td>${escHtml(sku.status)}</td>
                <td>
                  <button class="btn btn-primary btn-sm" onclick="openSkuForm(${variant.id}, ${sku.id})">Sửa</button>
                  <label class="btn btn-outline-primary btn-sm mb-0">Ảnh<input type="file" class="d-none" accept=".jpg,.png,.webp" onchange="uploadCatalogSkuImage(${variant.id}, ${sku.id}, this)"></label>
                  <button class="btn btn-outline-danger btn-sm" onclick="removeCatalogSku(${variant.id}, ${sku.id})">Xóa/Ngừng</button>
                </td>
              </tr>`;
            }).join('') : '<tr><td colspan="7" class="text-center text-muted py-3">Chưa có SKU.</td></tr>'}
          </tbody>
        </table>
      </div>
    </section>
  `).join('');
}

function openVariantForm(variantId = null) {
  const form = document.getElementById('variantCatalogForm');
  form.reset();
  const variant = currentCatalogVariants.find(item => item.id === variantId);
  document.getElementById('catalogVariantId').value = variant?.id || '';
  document.getElementById('variantCatalogFormTitle').textContent = variant ? 'Sửa phiên bản' : 'Thêm phiên bản';
  document.getElementById('catalogVariantName').value = variant?.name || '';
  document.getElementById('catalogVersionCode').value = variant?.versionCode || '';
  document.getElementById('catalogVersionCode').readOnly = Boolean(variant);
  document.getElementById('catalogVariantStatus').value = variant?.status || 'Active';
  document.getElementById('catalogEngineType').value = variant?.specification?.engineType || '';
  document.getElementById('catalogFuelType').value = variant?.specification?.fuelType || '';
  document.getElementById('catalogEngineCapacity').value = variant?.specification?.engineCapacityCc ?? 0;
  document.getElementById('catalogHorsePower').value = variant?.specification?.horsePower ?? 0;
  document.getElementById('catalogOtherDetails').value = variant?.specification?.otherDetails || '';
  form.classList.remove('d-none');
  closeSkuForm();
  form.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

function closeVariantForm() {
  document.getElementById('variantCatalogForm')?.classList.add('d-none');
}

function catalogSpecificationPayload() {
  return {
    engineType: document.getElementById('catalogEngineType').value.trim(),
    fuelType: document.getElementById('catalogFuelType').value.trim(),
    engineCapacityCc: Number(document.getElementById('catalogEngineCapacity').value),
    horsePower: Number(document.getElementById('catalogHorsePower').value),
    curbWeightKg: null,
    dimensions: null,
    fuelTankCapacityLiters: null,
    maxPower: null,
    fuelConsumptionLitersPer100Km: null,
    otherDetails: document.getElementById('catalogOtherDetails').value.trim() || null
  };
}

async function saveCatalogVariant(event) {
  event.preventDefault();
  const variantId = Number(document.getElementById('catalogVariantId').value) || null;
  const common = {
    name: document.getElementById('catalogVariantName').value.trim(),
    status: document.getElementById('catalogVariantStatus').value,
    specification: catalogSpecificationPayload()
  };
  try {
    if (variantId) {
      await Api.updateProductVariant(currentCatalogProductId, variantId, common);
    } else {
      await Api.createProductVariant(currentCatalogProductId, {
        ...common,
        versionCode: document.getElementById('catalogVersionCode').value.trim()
      });
    }
    closeVariantForm();
    await refreshCatalogManager();
    showToast('Đã lưu phiên bản.');
  } catch (err) {
    showToast('Lưu phiên bản thất bại: ' + apiErrorMessage(err), 'danger');
  }
}

async function removeCatalogVariant(variantId) {
  if (!confirmAction('Xóa phiên bản này? Nếu đã phát sinh dữ liệu, hệ thống sẽ chuyển sang ngừng bán.')) return;
  try {
    await Api.deleteProductVariant(currentCatalogProductId, variantId);
    await refreshCatalogManager();
    showToast('Đã cập nhật phiên bản.');
  } catch (err) {
    showToast('Không thể xóa/ngừng phiên bản: ' + apiErrorMessage(err), 'danger');
  }
}

function openSkuForm(variantId, skuId = null) {
  const form = document.getElementById('skuCatalogForm');
  form.reset();
  const variant = currentCatalogVariants.find(item => item.id === variantId);
  const sku = variant?.skus.find(item => item.id === skuId);
  document.getElementById('catalogSkuVariantId').value = variantId;
  document.getElementById('catalogSkuId').value = sku?.id || '';
  document.getElementById('catalogSkuRowVersion').value = sku?.rowVersion || '';
  document.getElementById('skuCatalogFormTitle').textContent = sku ? `Sửa SKU — ${variant.name}` : `Thêm SKU — ${variant.name}`;
  document.getElementById('catalogSkuCode').value = sku?.skuCode || '';
  document.getElementById('catalogSkuCode').readOnly = Boolean(sku);
  document.getElementById('catalogColorName').value = sku?.colorName || '';
  document.getElementById('catalogColorHex').value = sku?.colorHexCode || '';
  document.getElementById('catalogSkuPrice').value = sku?.price ?? '';
  document.getElementById('catalogSkuStatus').value = sku?.status || 'Active';
  form.classList.remove('d-none');
  closeVariantForm();
  form.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

function closeSkuForm() {
  document.getElementById('skuCatalogForm')?.classList.add('d-none');
}

async function saveCatalogSku(event) {
  event.preventDefault();
  const variantId = Number(document.getElementById('catalogSkuVariantId').value);
  const skuId = Number(document.getElementById('catalogSkuId').value) || null;
  const common = {
    colorName: document.getElementById('catalogColorName').value.trim(),
    colorHexCode: document.getElementById('catalogColorHex').value.trim() || null,
    price: Number(document.getElementById('catalogSkuPrice').value),
    status: document.getElementById('catalogSkuStatus').value
  };
  try {
    if (skuId) {
      await Api.updateProductSku(currentCatalogProductId, variantId, skuId, {
        ...common,
        rowVersion: document.getElementById('catalogSkuRowVersion').value
      });
    } else {
      await Api.createProductSku(currentCatalogProductId, variantId, {
        ...common,
        skuCode: document.getElementById('catalogSkuCode').value.trim()
      });
    }
    closeSkuForm();
    await refreshCatalogManager();
    showToast('Đã lưu SKU.');
  } catch (err) {
    showToast('Lưu SKU thất bại: ' + apiErrorMessage(err), 'danger');
  }
}

async function removeCatalogSku(variantId, skuId) {
  if (!confirmAction('Xóa SKU này? Nếu đã phát sinh dữ liệu, hệ thống sẽ chuyển sang ngừng bán.')) return;
  try {
    await Api.deleteProductSku(currentCatalogProductId, variantId, skuId);
    await refreshCatalogManager();
    showToast('Đã cập nhật SKU.');
  } catch (err) {
    showToast('Không thể xóa/ngừng SKU: ' + apiErrorMessage(err), 'danger');
  }
}

async function uploadCatalogSkuImage(variantId, skuId, input) {
  const file = input.files?.[0];
  if (!file) return;
  try {
    const variant = currentCatalogVariants.find(item => item.id === variantId);
    const sku = variant?.skus.find(item => item.id === skuId);
    await Api.uploadProductSkuImage(currentCatalogProductId, variantId, skuId, file, {
      altText: `${variant?.name || ''} - ${sku?.colorName || ''}`.trim(),
      displayOrder: sku?.images?.length || 0,
      isPrimary: !sku?.images?.length
    });
    await refreshCatalogManager();
    showToast('Đã tải ảnh SKU.');
  } catch (err) {
    showToast('Tải ảnh thất bại: ' + apiErrorMessage(err), 'danger');
  } finally {
    input.value = '';
  }
}

async function setCatalogPrimaryImage(variantId, skuId, imageId) {
  const variant = currentCatalogVariants.find(item => item.id === variantId);
  const sku = variant?.skus.find(item => item.id === skuId);
  const image = sku?.images.find(item => item.id === imageId);
  if (!image) return;
  try {
    await Api.updateProductSkuImage(currentCatalogProductId, variantId, skuId, imageId, {
      altText: image.altText,
      displayOrder: image.displayOrder,
      isPrimary: true
    });
    await refreshCatalogManager();
    showToast('Đã đổi ảnh chính.');
  } catch (err) {
    showToast('Không thể đổi ảnh chính: ' + apiErrorMessage(err), 'danger');
  }
}

async function updateCatalogImageOrder(variantId, skuId, imageId, rawDisplayOrder) {
  const displayOrder = Number(rawDisplayOrder);
  if (!Number.isInteger(displayOrder) || displayOrder < 0) {
    showToast('Thứ tự ảnh phải là số nguyên không âm.', 'danger');
    await refreshCatalogManager();
    return;
  }

  const variant = currentCatalogVariants.find(item => item.id === variantId);
  const sku = variant?.skus.find(item => item.id === skuId);
  const image = sku?.images.find(item => item.id === imageId);
  if (!image || image.displayOrder === displayOrder) return;

  try {
    await Api.updateProductSkuImage(currentCatalogProductId, variantId, skuId, imageId, {
      altText: image.altText,
      displayOrder,
      isPrimary: image.isPrimary
    });
    await refreshCatalogManager();
    showToast('Đã cập nhật thứ tự ảnh.');
  } catch (err) {
    await refreshCatalogManager();
    showToast('Không thể cập nhật thứ tự ảnh: ' + apiErrorMessage(err), 'danger');
  }
}

async function deleteCatalogSkuImage(variantId, skuId, imageId) {
  if (!confirmAction('Xóa ảnh SKU này?')) return;
  try {
    await Api.deleteProductSkuImage(currentCatalogProductId, variantId, skuId, imageId);
    await refreshCatalogManager();
    showToast('Đã xóa ảnh SKU.');
  } catch (err) {
    showToast('Xóa ảnh thất bại: ' + apiErrorMessage(err), 'danger');
  }
}

async function deleteProductRow(id) {
  if (!confirmAction('Bạn có chắc muốn xóa sản phẩm này?')) return;
  try {
    await Api.deleteProduct(id);
    showToast('Sản phẩm đã được xóa!');
    loadProducts();
  } catch (err) {
    showToast('Xóa thất bại: ' + apiErrorMessage(err), 'danger');
  }
}

function sortByYear() {
  currentPage = 1;
  loadProducts();
  showToast('Danh sách được sắp xếp mới nhất trước!');
}

async function getAllProductsForTransfer() {
  const pageSize = 100;
  const firstPage = await Api.getCatalogProducts(`?pageNumber=1&pageSize=${pageSize}`);
  const productSummaries = [...firstPage.items];
  for (let page = 2; page <= firstPage.totalPages; page += 1) {
    const result = await Api.getCatalogProducts(`?pageNumber=${page}&pageSize=${pageSize}`);
    productSummaries.push(...result.items);
  }
  return Promise.all(productSummaries.map(product => Api.getProductCatalog(product.id)));
}

async function exportMotorbikes() {
  try {
    const products = await getAllProductsForTransfer();
    const documentData = {
      schema: 'motorbike-products-v2-catalog',
      exportedAt: new Date().toISOString(),
      totalCount: products.length,
      products
    };
    const blob = new Blob([JSON.stringify(documentData, null, 2)], { type: 'application/json;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `motorbike-products-${new Date().toISOString().slice(0, 10)}.json`;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
    showToast(`Đã xuất ${products.length} sản phẩm.`);
  } catch (err) {
    showToast('Xuất JSON thất bại: ' + apiErrorMessage(err), 'danger');
  }
}

function importMotorbikes() {
  const input = document.createElement('input');
  input.type = 'file';
  input.accept = '.json,application/json';
  input.addEventListener('change', async () => {
    const file = input.files?.[0];
    if (!file) return;

    try {
      const parsed = JSON.parse(await file.text());
      const products = Array.isArray(parsed) ? parsed : parsed?.products;
      if (!Array.isArray(products) || products.length === 0) {
        throw new Error('File JSON phải chứa mảng products có ít nhất một sản phẩm.');
      }
      if (products.length > 500) {
        throw new Error('Mỗi lần chỉ được import tối đa 500 sản phẩm.');
      }
      if (!confirmAction(`Import ${products.length} sản phẩm? Sản phẩm trùng tên sẽ được bỏ qua.`)) return;

      const [brands, vehicleTypes, existingProducts] = await Promise.all([
        Api.getBrands(), Api.getVehicleTypes(), getAllProductsForTransfer()
      ]);
      const existingNames = new Set(existingProducts.map(product => product.name.trim().toLocaleLowerCase('vi-VN')));
      let imported = 0;
      let skipped = 0;
      let stockIgnored = 0;
      const errors = [];
      const normalizeSpecification = source => ({
        engineType: String(source?.engineType || 'Chưa cập nhật').trim(),
        fuelType: String(source?.fuelType || 'Chưa cập nhật').trim(),
        engineCapacityCc: Number(source?.engineCapacityCc || 0),
        horsePower: Number(source?.horsePower || 0),
        curbWeightKg: source?.curbWeightKg ?? null,
        dimensions: source?.dimensions ?? null,
        fuelTankCapacityLiters: source?.fuelTankCapacityLiters ?? null,
        maxPower: source?.maxPower ?? null,
        fuelConsumptionLitersPer100Km: source?.fuelConsumptionLitersPer100Km ?? null,
        otherDetails: source?.otherDetails ?? null
      });

      for (let index = 0; index < products.length; index += 1) {
        const product = products[index];
        const name = typeof product?.name === 'string' ? product.name.trim() : '';
        if (!name) {
          errors.push(`Dòng ${index + 1}: thiếu tên sản phẩm.`);
          continue;
        }
        const normalizedName = name.toLocaleLowerCase('vi-VN');
        if (existingNames.has(normalizedName)) {
          skipped += 1;
          continue;
        }

        const brand = brands.find(item => item.id === Number(product.brandId)) ||
          brands.find(item => item.name.toLocaleLowerCase('vi-VN') === String(product.brandName || '').trim().toLocaleLowerCase('vi-VN'));
        const vehicleType = product.vehicleTypeId == null && !product.vehicleTypeName
          ? null
          : vehicleTypes.find(item => item.id === Number(product.vehicleTypeId)) ||
            vehicleTypes.find(item => item.name.toLocaleLowerCase('vi-VN') === String(product.vehicleTypeName || '').trim().toLocaleLowerCase('vi-VN'));
        if (!brand) {
          errors.push(`Dòng ${index + 1} (${name}): không tìm thấy hãng xe.`);
          continue;
        }
        if ((product.vehicleTypeId != null || product.vehicleTypeName) && !vehicleType) {
          errors.push(`Dòng ${index + 1} (${name}): không tìm thấy loại xe.`);
          continue;
        }

        const sourceVariants = Array.isArray(product.variants) && product.variants.length
          ? product.variants
          : [{
              name: 'Phiên bản mặc định',
              versionCode: 'V1',
              status: 'Active',
              specification: product.specification || {},
              skus: [{
                colorName: product.color || 'Chưa cập nhật',
                colorHexCode: null,
                price: product.price,
                stockQuantity: product.stockQuantity,
                status: 'Active'
              }]
            }];
        const firstVariant = sourceVariants[0];
        const firstSku = (firstVariant.skus || [])[0] || {};
        const specification = normalizeSpecification(firstVariant.specification);
        const payload = {
          name,
          description: String(product.description || '').trim(),
          status: String(product.status || 'Available'),
          brandId: brand.id,
          vehicleTypeId: vehicleType?.id ?? null
        };

        let createdProduct = null;
        try {
          createdProduct = await Api.createProduct(payload);
          for (let variantIndex = 0; variantIndex < sourceVariants.length; variantIndex += 1) {
            const sourceVariant = sourceVariants[variantIndex];
            const createdVariant = await Api.createProductVariant(createdProduct.id, {
              name: String(sourceVariant.name || `Phiên bản ${variantIndex + 1}`).trim(),
              versionCode: `V${String(variantIndex + 1).padStart(2, '0')}`,
              status: sourceVariant.status === 'Inactive' ? 'Inactive' : 'Active',
              specification: normalizeSpecification(sourceVariant.specification)
            });
            const sourceSkus = Array.isArray(sourceVariant.skus) && sourceVariant.skus.length
              ? sourceVariant.skus
              : [{
                  colorName: firstSku.colorName || product.color || 'Chưa cập nhật',
                  colorHexCode: firstSku.colorHexCode || null,
                  price: firstSku.price ?? product.minimumPrice ?? product.price ?? 0,
                  stockQuantity: 0,
                  status: 'Active'
                }];
            for (let skuIndex = 0; skuIndex < sourceSkus.length; skuIndex += 1) {
              const sourceSku = sourceSkus[skuIndex];
              stockIgnored += Math.max(0, Number(sourceSku.stockQuantity) || 0);
              await Api.createProductSku(createdProduct.id, createdVariant.id, {
                skuCode: `P${String(createdProduct.id).padStart(10, '0')}-V${String(variantIndex + 1).padStart(2, '0')}-S${String(skuIndex + 1).padStart(2, '0')}`,
                colorName: String(sourceSku.colorName || `Màu ${skuIndex + 1}`).trim(),
                colorHexCode: sourceSku.colorHexCode || null,
                price: Number(sourceSku.price || 0),
                status: sourceSku.status === 'Inactive' ? 'Inactive' : 'Active'
              });
            }
          }
          existingNames.add(normalizedName);
          imported += 1;
        } catch (err) {
          if (createdProduct) {
            await Api.deleteProduct(createdProduct.id).catch(() => {});
          }
          errors.push(`Dòng ${index + 1} (${name}): ${apiErrorMessage(err)}`);
        }
      }

      currentPage = 1;
      await loadProducts();
      if (errors.length) {
        console.warn('Chi tiết lỗi import:', errors);
        showToast(`Import xong: ${imported} thành công, ${skipped} trùng tên, ${errors.length} lỗi. Tồn kho JSON không được nhập; hãy tạo phiếu nhập SKU.`, 'warning');
      } else {
        showToast(`Import xong: ${imported} thành công, ${skipped} trùng tên. ${stockIgnored > 0 ? 'Tồn kho JSON được đặt về 0; hãy tạo phiếu nhập SKU.' : ''}`);
      }
    } catch (err) {
      showToast('Import JSON thất bại: ' + apiErrorMessage(err), 'danger');
    }
  }, { once: true });
  input.click();
}

document.getElementById('searchInput')?.addEventListener('input', () => { currentPage = 1; loadProducts(); });
document.getElementById('statusFilter')?.addEventListener('change', () => { currentPage = 1; loadProducts(); });
document.getElementById('priorityFilter')?.addEventListener('change', () => { currentPage = 1; loadProducts(); });

// ── Yêu cầu chăm sóc khách hàng (Support Requests, Giai đoạn 7) ──────────
async function loadSupportRequests() {
  const list = document.getElementById('messageList');
  if (!list) return;

  try {
    const requests = await Api.getSupportRequests();
    const badge = document.getElementById('msgBadge');
    const openCount = requests.filter(r => r.status === 'Open').length;
    if (badge) {
      badge.textContent = openCount;
      badge.classList.toggle('d-none', openCount === 0);
    }

    if (!requests.length) {
      list.innerHTML = '<p class="text-muted text-center py-3">Chưa có yêu cầu nào.</p>';
      return;
    }

    list.innerHTML = requests.map(r => `
      <div class="card mb-2 border-0 shadow-sm${r.status === 'Open' ? ' border-start border-danger border-3' : ''}">
        <div class="card-body py-3 px-4">
          <div class="d-flex justify-content-between align-items-start gap-2 flex-wrap">
            <div>
              <span class="fw-bold">${escHtml(r.userFullName)}</span>
              <span class="badge ${r.status === 'Open' ? 'bg-danger' : 'bg-success'} ms-2">${escHtml(r.status)}</span>
              <span class="text-muted small ms-2">${escHtml(r.userEmail)}</span>
            </div>
            <small class="text-muted">${new Date(r.createdAt).toLocaleString('vi-VN')}</small>
          </div>
          <div class="mt-1"><span class="badge bg-info me-1">${escHtml(r.supportType)}</span><span class="badge bg-secondary">${escHtml(r.subject)}</span></div>
          ${r.assignedEmployeeName ? `<div class="small text-muted mt-1">Phụ trách: ${escHtml(r.assignedEmployeeName)}</div>` : ''}
          <p class="mt-2 mb-2 text-muted" style="white-space: pre-line;">${escHtml(r.message)}</p>
          ${r.response ? `<div class="alert alert-light border py-2 mb-2"><strong>Phản hồi:</strong> ${escHtml(r.response)}</div>` : ''}
          ${r.status === 'Open' ? `<button class="btn btn-outline-primary btn-sm" onclick="respondToSupport(${r.id})">Phản hồi</button>` : ''}
        </div>
      </div>
    `).join('');
  } catch (err) {
    list.innerHTML = `<p class="text-danger text-center py-3">Không tải được: ${apiErrorMessage(err)}</p>`;
  }
}

async function respondToSupport(id) {
  const response = prompt('Nhập nội dung phản hồi:');
  if (!response) return;
  try {
    await Api.updateSupportRequest(id, { status: 'Resolved', response });
    showToast('Đã phản hồi yêu cầu!');
    loadSupportRequests();
  } catch (err) {
    showToast('Phản hồi thất bại: ' + apiErrorMessage(err), 'danger');
  }
}

document.addEventListener('DOMContentLoaded', () => {
  if (document.getElementById('taskList')) {
    loadVehicleTypesIntoSelect();
    loadBrandsIntoSelects().then(async () => {
      await loadProducts();
      const catalogId = Number(new URLSearchParams(window.location.search).get('catalog'));
      if (catalogId > 0) await openCatalogManager(catalogId);
    });
  }
  if (document.getElementById('messageList')) {
    loadSupportRequests();
  }
});
