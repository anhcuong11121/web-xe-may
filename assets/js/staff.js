// staff.js - Logic quản lý cho nhân viên (Employee) - dùng API thật

let currentEditingProductId = null;
let paymentsPage = 1;
let paymentsTotalPages = 0;
const paymentsPageSize = 20;
let customersPage = 1;
let customersTotalPages = 0;
const customersPageSize = 20;

document.addEventListener('DOMContentLoaded', () => {
  loadBrandsForStaff();
  loadVehicleTypesForStaff();
  loadStaffProducts();
  loadConsultations();
  loadStaffOrders();
  loadPaymentAttempts();
  loadPromotions();
  loadCustomers();

  const consultationForm = document.getElementById('consultationResponseForm');
  const consultationResponse = document.getElementById('consultationResponse');
  consultationForm?.addEventListener('submit', submitConsultationResponse);
  consultationResponse?.addEventListener('input', () => {
    document.getElementById('consultationResponseCount').textContent = consultationResponse.value.length;
  });
  document.getElementById('promotionForm')?.addEventListener('submit', savePromotion);
  document.getElementById('promotionImageFile')?.addEventListener('change', previewPromotionFile);
  document.getElementById('promotionImageUrl')?.addEventListener('input', event => showPromotionPreview(event.target.value));
});

function escHtml(str) {
  return (str || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

// ── PRODUCTS ────────────────────────────────────────────
async function loadBrandsForStaff() {
  try {
    const brands = await Api.getBrands();
    const select = document.getElementById('productBrand');
    if (select) {
      select.innerHTML = brands.map(b => `<option value="${b.id}">${escHtml(b.name)}</option>`).join('');
    }
  } catch (err) {
    showToast('Không tải được hãng xe: ' + apiErrorMessage(err), 'danger');
  }
}

async function loadVehicleTypesForStaff() {
  try {
    const vehicleTypes = await Api.getVehicleTypes();
    const select = document.getElementById('productVehicleType');
    if (select) {
      select.innerHTML = '<option value="">Chưa phân loại</option>' +
        vehicleTypes.map(v => `<option value="${v.id}">${escHtml(v.name)}</option>`).join('');
    }
  } catch (err) {
    showToast('Không tải được loại xe: ' + apiErrorMessage(err), 'danger');
  }
}

async function loadCustomers() {
  const listBody = document.getElementById('customersList');
  if (!listBody) return;

  try {
    const result = await Api.getCustomers(`?pageNumber=${customersPage}&pageSize=${customersPageSize}`);
    customersTotalPages = result.totalPages;
    listBody.innerHTML = result.items.length ? result.items.map(customer => `
      <tr>
        <td><strong>${escHtml(customer.fullName)}</strong></td>
        <td>${escHtml(customer.email)}</td>
        <td>${escHtml(customer.phoneNumber || '-')}</td>
        <td>${customer.totalOrders}</td>
        <td><span class="badge bg-${customer.isActive ? 'success' : 'secondary'}">${customer.isActive ? 'Hoạt động' : 'Đã khóa'}</span></td>
        <td>${new Date(customer.createdAt).toLocaleDateString('vi-VN')}</td>
      </tr>
    `).join('') : '<tr><td colspan="6" class="text-center text-muted py-3">Chưa có khách hàng.</td></tr>';

    const pageInfo = document.getElementById('customersPageInfo');
    if (pageInfo) pageInfo.textContent = `Trang ${result.pageNumber}/${Math.max(result.totalPages, 1)} · ${result.totalCount} khách hàng`;
    document.getElementById('customersPrevious').disabled = result.pageNumber <= 1;
    document.getElementById('customersNext').disabled = result.pageNumber >= result.totalPages;
  } catch (err) {
    listBody.innerHTML = `<tr><td colspan="6" class="text-center text-danger py-3">${escHtml(apiErrorMessage(err))}</td></tr>`;
  }
}

function changeCustomersPage(offset) {
  const nextPage = customersPage + offset;
  if (nextPage < 1 || (customersTotalPages > 0 && nextPage > customersTotalPages)) return;
  customersPage = nextPage;
  loadCustomers();
}

async function loadStaffProducts() {
  const listBody = document.getElementById('productsList');
  if (!listBody) return;

  try {
    const result = await Api.getProducts('?pageSize=100');
    listBody.innerHTML = result.items.length ? result.items.map(p => `
      <tr>
        <td>${p.id}</td>
        <td><strong>${escHtml(p.name)}</strong></td>
        <td>${escHtml((p.description || '').substring(0, 50))}...</td>
        <td>${formatCurrencyVnd(p.price)}</td>
        <td><span class="badge bg-info">${escHtml(p.brandName || '-')}</span></td>
        <td><span class="badge bg-${p.stockQuantity > 0 ? 'success' : 'warning'}">${p.stockQuantity > 0 ? p.stockQuantity + ' xe' : 'Hết hàng'}</span></td>
        <td>
          <button class="btn btn-sm btn-primary" onclick="editProduct(${p.id})"><i class="fa-solid fa-edit"></i></button>
          <button class="btn btn-sm btn-danger" onclick="deleteProduct(${p.id})"><i class="fa-solid fa-trash"></i></button>
        </td>
      </tr>
    `).join('') : '<tr><td colspan="7" class="text-center text-muted py-3">Chưa có sản phẩm nào.</td></tr>';
  } catch (err) {
    listBody.innerHTML = `<tr><td colspan="7" class="text-center text-danger py-3">${apiErrorMessage(err)}</td></tr>`;
  }
}

function openAddProductModal() {
  currentEditingProductId = null;
  document.getElementById('productModalTitle').textContent = 'Thêm sản phẩm';
  document.getElementById('productForm').reset();
}

async function editProduct(id) {
  try {
    const p = await Api.getProduct(id);
    currentEditingProductId = id;
    document.getElementById('productModalTitle').textContent = 'Sửa sản phẩm';
    document.getElementById('productTitle').value = p.name;
    document.getElementById('productDescription').value = p.description;
    document.getElementById('productYear').value = p.price;
    document.getElementById('productBrand').value = p.brandId;
    document.getElementById('productStock').value = p.stockQuantity;
    document.getElementById('productColor').value = p.color;
    document.getElementById('productStatus').value = p.status;
    document.getElementById('productVehicleType').value = p.vehicleTypeId || '';
    document.getElementById('specEngineType').value = p.specification?.engineType || '';
    document.getElementById('specFuelType').value = p.specification?.fuelType || '';
    document.getElementById('specEngineCapacity').value = p.specification?.engineCapacityCc ?? '';
    document.getElementById('specHorsePower').value = p.specification?.horsePower ?? '';

    const modal = new bootstrap.Modal(document.getElementById('productModal'));
    modal.show();
  } catch (err) {
    showToast('Không tải được sản phẩm: ' + apiErrorMessage(err), 'danger');
  }
}

async function saveProduct() {
  const name = document.getElementById('productTitle').value.trim();
  const description = document.getElementById('productDescription').value.trim();
  const price = Number(document.getElementById('productYear').value);
  const brandId = Number(document.getElementById('productBrand').value);
  const stockQuantity = Number(document.getElementById('productStock').value);
  const color = document.getElementById('productColor').value.trim();
  const status = document.getElementById('productStatus').value;
  const vehicleTypeValue = document.getElementById('productVehicleType').value;
  const vehicleTypeId = vehicleTypeValue ? Number(vehicleTypeValue) : null;
  const specification = {
    engineType: document.getElementById('specEngineType').value.trim(),
    fuelType: document.getElementById('specFuelType').value.trim(),
    engineCapacityCc: Number(document.getElementById('specEngineCapacity').value),
    horsePower: Number(document.getElementById('specHorsePower').value)
  };

  if (!name || description.length < 10) {
    showToast('Vui lòng nhập đầy đủ thông tin (mô tả tối thiểu 10 ký tự)!', 'danger');
    return;
  }
  if (!brandId) {
    showToast('Vui lòng chọn hãng xe!', 'danger');
    return;
  }

  if (!color) {
    showToast('Vui lòng nhập màu sắc!', 'danger');
    return;
  }
  if (!specification.engineType || !specification.fuelType) {
    showToast('Vui lòng nhập đầy đủ thông số kỹ thuật bắt buộc!', 'danger');
    return;
  }

  const payload = { name, description, price, stockQuantity, color, status, brandId, vehicleTypeId, specification };

  try {
    if (currentEditingProductId) {
      await Api.updateProduct(currentEditingProductId, payload);
    } else {
      await Api.createProduct(payload);
    }

    document.getElementById('productForm').reset();
    bootstrap.Modal.getInstance(document.getElementById('productModal')).hide();
    loadStaffProducts();
    showToast('Lưu sản phẩm thành công!', 'success');
  } catch (err) {
    showToast('Lưu thất bại: ' + apiErrorMessage(err), 'danger');
  }
}

async function deleteProduct(id) {
  if (!confirm('Bạn chắc chắn muốn xóa sản phẩm này?')) return;
  try {
    await Api.deleteProduct(id);
    loadStaffProducts();
    showToast('Xóa sản phẩm thành công!', 'success');
  } catch (err) {
    showToast('Xóa thất bại: ' + apiErrorMessage(err), 'danger');
  }
}

// ── CONSULTATIONS (Support Requests, Giai đoạn 7) ─────────────────────
let consultationModalInstance = null;

function consultationStatusLabel(status) {
  return {
    Open: 'Chưa tiếp nhận',
    InProgress: 'Đang tư vấn',
    Resolved: 'Đã phản hồi',
    Closed: 'Đã đóng'
  }[status] || status;
}

function consultationStatusClass(status) {
  return {
    Open: 'bg-warning text-dark',
    InProgress: 'bg-info text-dark',
    Resolved: 'bg-success',
    Closed: 'bg-secondary'
  }[status] || 'bg-secondary';
}

async function loadConsultations() {
  const listBody = document.getElementById('consultationsList');
  if (!listBody) return;

  try {
    const requests = await Api.getSupportRequests();
    listBody.innerHTML = requests.length ? requests.map(r => `
      <tr>
        <td>${r.id}</td>
        <td>${escHtml(r.userFullName)}</td>
        <td><span class="badge bg-info me-1">${escHtml(r.supportType)}</span><span class="badge bg-secondary">${escHtml(r.subject)}</span></td>
        <td><button type="button" class="btn btn-link text-start text-decoration-none p-0 text-body" onclick="openConsultation(${r.id})" title="Mở toàn bộ hội thoại">${escHtml((r.message || '').substring(0, 70))}${(r.message || '').length > 70 ? '…' : ''}</button></td>
        <td><span class="badge ${consultationStatusClass(r.status)}">${escHtml(consultationStatusLabel(r.status))}</span></td>
        <td>
          <button type="button" class="btn btn-sm ${r.status === 'Resolved' || r.status === 'Closed' ? 'btn-outline-secondary' : 'btn-primary'}" onclick="openConsultation(${r.id})">
            <i class="fa-solid ${r.status === 'Resolved' || r.status === 'Closed' ? 'fa-eye' : 'fa-comments'} me-1"></i>${r.status === 'Resolved' || r.status === 'Closed' ? 'Xem hội thoại' : 'Mở hội thoại'}
          </button>
        </td>
      </tr>
    `).join('') : '<tr><td colspan="6" class="text-center text-muted py-3">Chưa có yêu cầu nào.</td></tr>';
  } catch (err) {
    listBody.innerHTML = `<tr><td colspan="6" class="text-center text-danger py-3">${apiErrorMessage(err)}</td></tr>`;
  }
}

async function openConsultation(id) {
  const messageBox = document.getElementById('consultationResponseMessage');
  messageBox.textContent = '';
  try {
    const request = await Api.getSupportRequest(id);
    document.getElementById('consultationId').value = request.id;
    document.getElementById('consultationCustomerMeta').textContent = `${request.userFullName} · ${request.userEmail}`;
    document.getElementById('consultationType').textContent = request.supportType;
    document.getElementById('consultationSubject').textContent = request.subject;
    const statusBadge = document.getElementById('consultationStatus');
    statusBadge.className = `badge ${consultationStatusClass(request.status)}`;
    statusBadge.textContent = consultationStatusLabel(request.status);
    document.getElementById('consultationCreatedAt').textContent = `Gửi lúc ${new Date(request.createdAt).toLocaleString('vi-VN')}`;
    document.getElementById('consultationMessage').textContent = request.message;

    const existingWrap = document.getElementById('consultationExistingResponseWrap');
    if (request.response) {
      existingWrap.style.setProperty('display', 'flex', 'important');
      document.getElementById('consultationExistingResponse').textContent = request.response;
      document.getElementById('consultationResponder').textContent = request.assignedEmployeeName || 'Nhân viên';
      document.getElementById('consultationRespondedAt').textContent = request.respondedAt
        ? new Date(request.respondedAt).toLocaleString('vi-VN')
        : '';
    } else {
      existingWrap.style.setProperty('display', 'none', 'important');
    }

    const canRespond = request.status === 'Open' || request.status === 'InProgress';
    const form = document.getElementById('consultationResponseForm');
    const sendButton = document.getElementById('consultationSendButton');
    form.style.display = canRespond ? '' : 'none';
    sendButton.style.display = canRespond ? '' : 'none';
    document.getElementById('consultationResponse').value = '';
    document.getElementById('consultationResponseCount').textContent = '0';

    consultationModalInstance = bootstrap.Modal.getOrCreateInstance(document.getElementById('consultationModal'));
    consultationModalInstance.show();
  } catch (err) {
    showToast('Không mở được hội thoại: ' + apiErrorMessage(err), 'danger');
  }
}

async function submitConsultationResponse(event) {
  event.preventDefault();
  const id = Number(document.getElementById('consultationId').value);
  const response = document.getElementById('consultationResponse').value.trim();
  const messageBox = document.getElementById('consultationResponseMessage');
  const sendButton = document.getElementById('consultationSendButton');
  if (!response) {
    messageBox.className = 'alert alert-warning py-2';
    messageBox.textContent = 'Vui lòng nhập nội dung phản hồi.';
    return;
  }

  sendButton.disabled = true;
  sendButton.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Đang gửi';
  try {
    const current = await Api.getSupportRequest(id);
    if (current.status === 'Open') {
      await Api.updateSupportRequest(id, { status: 'InProgress', response });
    }
    await Api.updateSupportRequest(id, { status: 'Resolved', response });
    consultationModalInstance?.hide();
    await loadConsultations();
    showToast('Đã gửi phản hồi cho khách hàng!', 'success');
  } catch (err) {
    messageBox.className = 'alert alert-danger py-2';
    messageBox.textContent = 'Gửi phản hồi thất bại: ' + apiErrorMessage(err);
  } finally {
    sendButton.disabled = false;
    sendButton.innerHTML = '<i class="fa-solid fa-paper-plane me-1"></i>Gửi phản hồi';
  }
}

// ── ORDERS (Giai đoạn 8) ────────────────────────────────────────────
function orderStatusOptions(currentStatus) {
  const labels = {
    Pending: 'Chờ đặt cọc',
    Deposited: 'Đã đặt cọc',
    Confirmed: 'Đã xác nhận',
    Processing: 'Đang xử lý',
    Completed: 'Hoàn thành',
    Cancelled: 'Đã hủy'
  };
  const next = {
    Pending: ['Cancelled'],
    Deposited: ['Confirmed'],
    Confirmed: ['Processing'],
    Processing: ['Completed'],
    Completed: [],
    Cancelled: []
  };
  return [`<option value="${currentStatus}" selected>${labels[currentStatus] || currentStatus}</option>`]
    .concat((next[currentStatus] || []).map(status => `<option value="${status}">${labels[status]}</option>`))
    .join('');
}

async function loadStaffOrders() {
  const listBody = document.getElementById('ordersList');
  if (!listBody) return;

  try {
    const orders = await Api.getOrders();
    listBody.innerHTML = orders.length ? orders.map(o => `
      <tr>
        <td>${o.id}</td>
        <td>${escHtml(o.receiverName || o.userFullName)}<div class="small text-muted">${escHtml(o.receiverPhone || '')}</div></td>
        <td>${escHtml(o.items.map(i => i.productName).join(', '))}</td>
        <td>${new Date(o.orderDate).toLocaleDateString('vi-VN')}</td>
        <td>
          <select class="form-select form-select-sm" onchange="updateOrderStatusStaff(${o.id}, this.value)">
            ${orderStatusOptions(o.status)}
          </select>
        </td>
        <td>
          <span class="text-muted small">${formatCurrencyVnd(o.totalAmount)}</span>
          ${o.deposit ? `<div class="small text-success">Cọc: ${formatCurrencyVnd(o.deposit.amount)}<br>${escHtml(o.deposit.transactionCode)}</div>` : '<div class="small text-warning">Chưa đặt cọc</div>'}
        </td>
      </tr>
    `).join('') : '<tr><td colspan="6" class="text-center text-muted py-3">Chưa có đơn hàng nào.</td></tr>';
  } catch (err) {
    listBody.innerHTML = `<tr><td colspan="6" class="text-center text-danger py-3">${apiErrorMessage(err)}</td></tr>`;
  }
}

async function updateOrderStatusStaff(orderId, status) {
  try {
    await Api.updateOrderStatus({ orderId, status });
    showToast('Cập nhật trạng thái đơn hàng!', 'success');
    loadStaffOrders();
  } catch (err) {
    showToast('Cập nhật thất bại: ' + apiErrorMessage(err), 'danger');
    loadStaffOrders();
  }
}

// ── NEWS / PROMOTIONS (Giai đoạn 10) ────────────────────────────────
function paymentStatusBadge(status) {
  const styles = { Pending: 'warning', Succeeded: 'success', Failed: 'danger', Expired: 'secondary' };
  const labels = { Pending: 'Đang chờ', Succeeded: 'Thành công', Failed: 'Thất bại', Expired: 'Hết hạn' };
  return `<span class="badge bg-${styles[status] || 'secondary'}">${labels[status] || escHtml(status)}</span>`;
}

function paymentMethodLabel(method) {
  return { BankTransfer: 'Chuyển khoản', Cash: 'Tiền mặt', Fake: 'Giả lập' }[method] || method;
}

async function loadPaymentAttempts() {
  const listBody = document.getElementById('paymentsList');
  if (!listBody) return;

  const params = new URLSearchParams({ pageNumber: paymentsPage, pageSize: paymentsPageSize });
  const status = document.getElementById('paymentStatusFilter').value;
  const method = document.getElementById('paymentMethodFilter').value;
  const orderId = document.getElementById('paymentOrderFilter').value;
  if (status) params.set('status', status);
  if (method) params.set('paymentMethod', method);
  if (orderId) params.set('orderId', orderId);

  listBody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">Đang tải...</td></tr>';
  try {
    const result = await Api.getPaymentAttempts(`?${params.toString()}`);
    paymentsTotalPages = result.totalPages;
    listBody.innerHTML = result.items.length ? result.items.map(p => `
      <tr>
        <td><code>${escHtml(p.transactionCode)}</code></td>
        <td>#${p.orderId}</td>
        <td>${formatCurrencyVnd(p.amount)}</td>
        <td>${escHtml(paymentMethodLabel(p.paymentMethod))}</td>
        <td><div>${new Date(p.createdAt).toLocaleString('vi-VN')}</div><small class="text-muted">Hết hạn: ${new Date(p.expiresAt).toLocaleString('vi-VN')}</small></td>
        <td>${paymentStatusBadge(p.status)}${p.failureReason ? `<div class="small text-danger mt-1">${escHtml(p.failureReason)}</div>` : ''}</td>
        <td>${escHtml(p.processedByName || '-')}</td>
        <td>${p.status === 'Pending' && (p.paymentMethod === 'BankTransfer' || p.paymentMethod === 'Cash')
          ? `<button class="btn btn-sm btn-success" onclick="completeManualPayment('${p.id}', '${escHtml(p.transactionCode)}')"><i class="fa-solid fa-check me-1"></i>Xác nhận</button>`
          : '<span class="text-muted small">Không có thao tác</span>'}</td>
      </tr>`).join('') : '<tr><td colspan="8" class="text-center text-muted py-3">Không có phiên thanh toán phù hợp.</td></tr>';

    document.getElementById('paymentsPageInfo').textContent = `Trang ${result.pageNumber}/${Math.max(result.totalPages, 1)} · ${result.totalCount} phiên`;
    document.getElementById('paymentsPrev').disabled = result.pageNumber <= 1;
    document.getElementById('paymentsNext').disabled = result.pageNumber >= result.totalPages;
  } catch (err) {
    listBody.innerHTML = `<tr><td colspan="8" class="text-center text-danger py-3">${escHtml(apiErrorMessage(err))}</td></tr>`;
  }
}

function filterPaymentAttempts(event) {
  event.preventDefault();
  paymentsPage = 1;
  loadPaymentAttempts();
}

function changePaymentsPage(offset) {
  const nextPage = paymentsPage + offset;
  if (nextPage < 1 || (paymentsTotalPages > 0 && nextPage > paymentsTotalPages)) return;
  paymentsPage = nextPage;
  loadPaymentAttempts();
}

async function completeManualPayment(id, transactionCode) {
  if (!confirm(`Xác nhận đã nhận tiền cho giao dịch ${transactionCode}? Thao tác này sẽ ghi nhận đặt cọc cho đơn hàng.`)) return;
  try {
    await Api.completeManualPayment(id);
    showToast('Đối soát thanh toán thành công!', 'success');
    await Promise.all([loadPaymentAttempts(), loadStaffOrders()]);
  } catch (err) {
    showToast('Đối soát thất bại: ' + apiErrorMessage(err), 'danger');
    loadPaymentAttempts();
  }
}

async function loadPromotions() {
  const listBody = document.getElementById('promotionsList');
  if (!listBody) return;

  try {
    const news = await Api.getNewsManagement();
    listBody.innerHTML = news.length ? news.map(n => `
      <tr>
        <td>${n.id}</td>
        <td><div class="d-flex align-items-center gap-2">${n.imageUrl ? `<img src="${escHtml(promotionImageSrc(n.imageUrl))}" alt="" class="rounded" style="width:76px;height:44px;object-fit:cover">` : '<span class="rounded bg-light d-inline-flex align-items-center justify-content-center" style="width:76px;height:44px"><i class="fa-regular fa-image text-muted"></i></span>'}<strong>${escHtml(n.title)}</strong></div></td>
        <td>${escHtml((n.content || '').substring(0, 40))}...</td>
        <td><span class="badge bg-info">${escHtml(n.contentType)}</span></td>
        <td><span class="badge bg-${n.status === 'Published' ? 'success' : (n.status === 'Draft' ? 'warning' : 'secondary')}">${escHtml(n.status)}</span></td>
        <td>${n.publishedAt ? new Date(n.publishedAt).toLocaleDateString('vi-VN') : '-'}</td>
        <td>
          <button class="btn btn-sm btn-primary" onclick="editPromotion(${n.id})"><i class="fa-solid fa-edit"></i></button>
          <button class="btn btn-sm btn-danger" onclick="deletePromotion(${n.id})"><i class="fa-solid fa-trash"></i></button>
        </td>
      </tr>
    `).join('') : '<tr><td colspan="7" class="text-center text-muted py-3">Chưa có tin tức nào.</td></tr>';
  } catch (err) {
    listBody.innerHTML = `<tr><td colspan="7" class="text-center text-danger py-3">${apiErrorMessage(err)}</td></tr>`;
  }
}

async function addPromotion() {
  document.getElementById('promotionForm').reset();
  document.getElementById('promotionId').value = '';
  document.getElementById('promotionModalTitle').innerHTML = '<i class="fa-solid fa-tags me-2"></i>Thêm khuyến mãi';
  document.getElementById('promotionFormMessage').textContent = '';
  showPromotionPreview('');
  bootstrap.Modal.getOrCreateInstance(document.getElementById('promotionModal')).show();
}

async function editPromotion(id) {
  try {
    const news = await Api.getNewsByIdManagement(id);
    document.getElementById('promotionId').value = news.id;
    document.getElementById('promotionTitle').value = news.title;
    document.getElementById('promotionContent').value = news.content;
    document.getElementById('promotionType').value = news.contentType;
    document.getElementById('promotionStatus').value = news.status;
    document.getElementById('promotionImageUrl').value = news.imageUrl || '';
    document.getElementById('promotionImageFile').value = '';
    document.getElementById('promotionModalTitle').innerHTML = '<i class="fa-solid fa-pen-to-square me-2"></i>Cập nhật nội dung';
    document.getElementById('promotionFormMessage').textContent = '';
    showPromotionPreview(news.imageUrl || '');
    bootstrap.Modal.getOrCreateInstance(document.getElementById('promotionModal')).show();
  } catch (err) {
    showToast('Không mở được nội dung: ' + apiErrorMessage(err), 'danger');
  }
}

function promotionImageSrc(url) {
  if (!url) return '';
  if (url.startsWith('/assets/')) return url;
  if (url.startsWith('/uploads/')) return API_BASE_URL + url;
  return url;
}

function showPromotionPreview(url) {
  const image = document.getElementById('promotionImagePreview');
  const placeholder = document.getElementById('promotionImagePlaceholder');
  const src = promotionImageSrc(url);
  image.style.display = src ? 'block' : 'none';
  placeholder.style.display = src ? 'none' : 'block';
  if (src) image.src = src;
}

function previewPromotionFile(event) {
  const file = event.target.files[0];
  if (!file) return showPromotionPreview(document.getElementById('promotionImageUrl').value.trim());
  if (file.size > 5 * 1024 * 1024) {
    showToast('Ảnh không được vượt quá 5MB.', 'danger');
    event.target.value = '';
    return;
  }
  const reader = new FileReader();
  reader.onload = () => showPromotionPreview(reader.result);
  reader.readAsDataURL(file);
}

async function savePromotion(event) {
  event.preventDefault();
  const id = Number(document.getElementById('promotionId').value) || null;
  const file = document.getElementById('promotionImageFile').files[0];
  const payload = {
    title: document.getElementById('promotionTitle').value.trim(),
    content: document.getElementById('promotionContent').value.trim(),
    contentType: document.getElementById('promotionType').value,
    status: document.getElementById('promotionStatus').value,
    imageUrl: document.getElementById('promotionImageUrl').value.trim() || null
  };
  const button = document.getElementById('promotionSaveButton');
  const message = document.getElementById('promotionFormMessage');
  button.disabled = true;
  try {
    let saved = id ? await Api.updateNews(id, payload) : await Api.createNews(payload);
    if (file) saved = await Api.uploadNewsImage(saved.id, file);
    bootstrap.Modal.getOrCreateInstance(document.getElementById('promotionModal')).hide();
    await loadPromotions();
    showToast(id ? 'Cập nhật nội dung thành công!' : 'Thêm khuyến mãi thành công!', 'success');
  } catch (err) {
    message.className = 'alert alert-danger py-2';
    message.textContent = apiErrorMessage(err);
  } finally {
    button.disabled = false;
  }
}

async function deletePromotion(id) {
  if (!confirm('Xóa tin tức này?')) return;
  try {
    await Api.deleteNews(id);
    loadPromotions();
    showToast('Xóa tin tức thành công!', 'success');
  } catch (err) {
    showToast('Xóa thất bại: ' + apiErrorMessage(err), 'danger');
  }
}
