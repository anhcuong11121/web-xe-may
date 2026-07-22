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

    const result = await Api.getProducts('?' + query.toString());

    const statusFilter = document.getElementById('statusFilter')?.value || '';
    let items = result.items;
    if (statusFilter === 'Available') items = items.filter(p => p.stockQuantity > 0);
    if (statusFilter === 'Sold') items = items.filter(p => p.stockQuantity <= 0);

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
    <tr class="${p.stockQuantity <= 0 ? 'table-warning' : ''}">
      <td>${p.id}</td>
      <td>${escHtml(p.name)}</td>
      <td>${escHtml((p.description || '').length > 50 ? p.description.substring(0, 50) + '...' : (p.description || ''))}</td>
      <td>${formatCurrencyVnd(p.price)}</td>
      <td><span class="badge bg-danger">${escHtml(p.brandName || '-')}</span></td>
      <td><span class="badge bg-${p.stockQuantity > 0 ? 'success' : 'secondary'}">${p.stockQuantity > 0 ? p.stockQuantity + ' xe' : 'Hết hàng'}</span></td>
      <td>
        <button class="btn btn-sm btn-primary me-1" onclick="editProductRow(${p.id})">Sửa</button>
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
  const available = items.filter(p => p.stockQuantity > 0).length;
  const sold = items.filter(p => p.stockQuantity <= 0).length;
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
}

async function editProductRow(id) {
  try {
    const p = await Api.getProduct(id);
    currentProductId = id;
    document.getElementById('modalTitle').textContent = 'Sửa sản phẩm';
    document.getElementById('title').value = p.name;
    document.getElementById('description').value = p.description;
    document.getElementById('deadline').value = p.price;
    document.getElementById('priority').value = p.brandId;
    document.getElementById('stockQuantity').value = p.stockQuantity;
    document.getElementById('productColor').value = p.color;
    document.getElementById('productStatus').value = p.status;
    document.getElementById('vehicleType').value = p.vehicleTypeId || '';
    document.getElementById('specEngineType').value = p.specification?.engineType || '';
    document.getElementById('specFuelType').value = p.specification?.fuelType || '';
    document.getElementById('specEngineCapacity').value = p.specification?.engineCapacityCc ?? '';
    document.getElementById('specHorsePower').value = p.specification?.horsePower ?? '';

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
  const stockQuantity = Number(document.getElementById('stockQuantity').value);
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
  if (price < 0 || stockQuantity < 0) { showToast('Giá và tồn kho phải >= 0!', 'danger'); return; }

  if (!color) { showToast('Vui lòng nhập màu sắc!', 'danger'); return; }
  if (!specification.engineType || !specification.fuelType) {
    showToast('Vui lòng nhập đầy đủ thông số kỹ thuật bắt buộc!', 'danger');
    return;
  }

  const payload = { name, description, price, stockQuantity, color, status, brandId, vehicleTypeId, specification };

  try {
    let productId = currentProductId;
    if (currentProductId) {
      await Api.updateProduct(currentProductId, payload);
    } else {
      const created = await Api.createProduct(payload);
      productId = created.id;
    }

    if (imageFile && productId) {
      await Api.uploadProductImage(productId, imageFile);
    }

    const modal = bootstrap.Modal.getInstance(document.getElementById('taskModal'));
    if (modal) modal.hide();
    showToast(currentProductId ? 'Sản phẩm đã được cập nhật!' : 'Sản phẩm đã được thêm!');
    loadProducts();
  } catch (err) {
    showToast('Lưu thất bại: ' + apiErrorMessage(err), 'danger');
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
  const firstPage = await Api.getProducts(`?pageNumber=1&pageSize=${pageSize}`);
  const products = [...firstPage.items];
  for (let page = 2; page <= firstPage.totalPages; page += 1) {
    const result = await Api.getProducts(`?pageNumber=${page}&pageSize=${pageSize}`);
    products.push(...result.items);
  }
  return products;
}

async function exportMotorbikes() {
  try {
    const products = await getAllProductsForTransfer();
    const documentData = {
      schema: 'motorbike-products-v1',
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
      const errors = [];

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

        const specification = product.specification || {};
        const payload = {
          name,
          description: String(product.description || '').trim(),
          price: Number(product.price),
          stockQuantity: Number(product.stockQuantity),
          color: String(product.color || '').trim(),
          status: String(product.status || 'Available'),
          brandId: brand.id,
          vehicleTypeId: vehicleType?.id ?? null,
          specification: {
            engineType: String(specification.engineType || '').trim(),
            fuelType: String(specification.fuelType || '').trim(),
            engineCapacityCc: Number(specification.engineCapacityCc || 0),
            horsePower: Number(specification.horsePower || 0),
            curbWeightKg: specification.curbWeightKg ?? null,
            dimensions: specification.dimensions ?? null,
            fuelTankCapacityLiters: specification.fuelTankCapacityLiters ?? null,
            maxPower: specification.maxPower ?? null,
            fuelConsumptionLitersPer100Km: specification.fuelConsumptionLitersPer100Km ?? null,
            otherDetails: specification.otherDetails ?? null
          }
        };

        try {
          await Api.createProduct(payload);
          existingNames.add(normalizedName);
          imported += 1;
        } catch (err) {
          errors.push(`Dòng ${index + 1} (${name}): ${apiErrorMessage(err)}`);
        }
      }

      currentPage = 1;
      await loadProducts();
      if (errors.length) {
        console.warn('Chi tiết lỗi import:', errors);
        showToast(`Import xong: ${imported} thành công, ${skipped} trùng tên, ${errors.length} lỗi. Xem Console để biết chi tiết.`, 'warning');
      } else {
        showToast(`Import xong: ${imported} thành công, ${skipped} trùng tên.`);
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
    loadBrandsIntoSelects().then(loadProducts);
  }
  if (document.getElementById('messageList')) {
    loadSupportRequests();
  }
});
