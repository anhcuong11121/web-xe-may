// staff.js - Logic quản lý cho nhân viên

let staffMotorbikes = JSON.parse(localStorage.getItem('motorbikes')) || [];
let staffConsultations = JSON.parse(localStorage.getItem('contactMessages') || '[]');
let staffOrders = JSON.parse(localStorage.getItem('orders') || '[]');
let staffPromotions = JSON.parse(localStorage.getItem('promotions') || '[]');
let currentEditingProductId = null;

// Initialize staff page on load
document.addEventListener('DOMContentLoaded', () => {
  renderProductsList();
  renderCustomersList();
  renderConsultationsList();
  renderOrdersList();
  renderPromotionsList();
});

// ── PRODUCTS ────────────────────────────────────────────
function renderProductsList() {
  const listBody = document.getElementById('productsList');
  if (!listBody) return;

  listBody.innerHTML = staffMotorbikes.map(m => `
    <tr>
      <td>${m.id}</td>
      <td><strong>${m.title}</strong></td>
      <td>${m.description.substring(0, 50)}...</td>
      <td><span class="badge bg-info">${m.priority || '-'}</span></td>
      <td><span class="badge bg-${m.status === 'Sold' ? 'success' : 'warning'}">${m.status === 'Sold' ? 'Đã bán' : 'Còn hàng'}</span></td>
      <td>
        <button class="btn btn-sm btn-primary" onclick="editProduct(${m.id})"><i class="fa-solid fa-edit"></i></button>
        <button class="btn btn-sm btn-success" onclick="toggleProductStatus(${m.id})">${m.status === 'Sold' ? 'Còn hàng' : 'Đã bán'}</button>
        <button class="btn btn-sm btn-danger" onclick="deleteProduct(${m.id})"><i class="fa-solid fa-trash"></i></button>
      </td>
    </tr>
  `).join('');
}

function openAddProductModal() {
  currentEditingProductId = null;
  document.getElementById('productModalTitle').textContent = 'Thêm sản phẩm';
  document.getElementById('productForm').reset();
}

function editProduct(id) {
  const product = staffMotorbikes.find(p => p.id === id);
  if (!product) return;

  currentEditingProductId = id;
  document.getElementById('productModalTitle').textContent = 'Sửa sản phẩm';
  document.getElementById('productTitle').value = product.title;
  document.getElementById('productDescription').value = product.description;
  document.getElementById('productYear').value = product.deadline;
  document.getElementById('productBrand').value = product.priority || 'Honda';

  const modal = new bootstrap.Modal(document.getElementById('productModal'));
  modal.show();
}

function saveProduct() {
  const title = document.getElementById('productTitle').value.trim();
  const description = document.getElementById('productDescription').value.trim();
  const year = document.getElementById('productYear').value || new Date().getFullYear();
  const brand = document.getElementById('productBrand').value;

  if (!title || !description) {
    alert('Vui lòng nhập đầy đủ thông tin!');
    return;
  }

  if (currentEditingProductId) {
    // Update
    const idx = staffMotorbikes.findIndex(p => p.id === currentEditingProductId);
    if (idx > -1) {
      staffMotorbikes[idx].title = title;
      staffMotorbikes[idx].description = description;
      staffMotorbikes[idx].deadline = year;
      staffMotorbikes[idx].priority = brand;
    }
  } else {
    // Create
    const newProduct = {
      id: Date.now(),
      title,
      description,
      deadline: year,
      priority: brand,
      colors: ['Đen'],
      status: 'Available'
    };
    staffMotorbikes.push(newProduct);
  }

  localStorage.setItem('motorbikes', JSON.stringify(staffMotorbikes));
  document.getElementById('productForm').reset();
  bootstrap.Modal.getInstance(document.getElementById('productModal')).hide();
  renderProductsList();
  showToast('Lưu sản phẩm thành công!', 'success');
}

function toggleProductStatus(id) {
  const product = staffMotorbikes.find(p => p.id === id);
  if (product) {
    product.status = product.status === 'Sold' ? 'Available' : 'Sold';
    localStorage.setItem('motorbikes', JSON.stringify(staffMotorbikes));
    renderProductsList();
    showToast('Cập nhật trạng thái thành công!', 'success');
  }
}

function deleteProduct(id) {
  if (confirm('Bạn chắc chắn muốn xóa sản phẩm này?')) {
    staffMotorbikes = staffMotorbikes.filter(p => p.id !== id);
    localStorage.setItem('motorbikes', JSON.stringify(staffMotorbikes));
    renderProductsList();
    showToast('Xóa sản phẩm thành công!', 'success');
  }
}

// ── CUSTOMERS ────────────────────────────────────────────
function renderCustomersList() {
  const listBody = document.getElementById('customersList');
  if (!listBody) return;

  const customers = JSON.parse(localStorage.getItem('customerAccounts') || '[]');
  listBody.innerHTML = customers.map(c => `
    <tr>
      <td><strong>${c.username}</strong></td>
      <td>${c.email}</td>
      <td>${c.phone}</td>
      <td>-</td>
      <td>
        <button class="btn btn-sm btn-info" onclick="viewCustomer('${c.username}')"><i class="fa-solid fa-eye"></i></button>
      </td>
    </tr>
  `).join('');
}

function viewCustomer(username) {
  const customers = JSON.parse(localStorage.getItem('customerAccounts') || '[]');
  const customer = customers.find(c => c.username === username);
  if (customer) {
    alert(`Khách hàng: ${customer.username}\nEmail: ${customer.email}\nSDT: ${customer.phone}`);
  }
}

// ── CONSULTATIONS ────────────────────────────────────────────
function renderConsultationsList() {
  const listBody = document.getElementById('consultationsList');
  if (!listBody) return;

  listBody.innerHTML = staffConsultations.map(c => `
    <tr>
      <td>${c.id}</td>
      <td>${c.name}</td>
      <td><span class="badge bg-secondary">${c.subject}</span></td>
      <td>${c.message.substring(0, 40)}...</td>
      <td><span class="badge bg-${c.read ? 'success' : 'warning'}">${c.read ? 'Đã xem' : 'Chưa xem'}</span></td>
      <td>
        <button class="btn btn-sm btn-primary" onclick="markConsultationAsRead(${c.id})">Xem</button>
        <button class="btn btn-sm btn-danger" onclick="deleteConsultation(${c.id})"><i class="fa-solid fa-trash"></i></button>
      </td>
    </tr>
  `).join('');
}

function markConsultationAsRead(id) {
  const consultation = staffConsultations.find(c => c.id === id);
  if (consultation) {
    consultation.read = true;
    localStorage.setItem('contactMessages', JSON.stringify(staffConsultations));
    renderConsultationsList();
    showToast('Đánh dấu đã xem!', 'success');
  }
}

function deleteConsultation(id) {
  if (confirm('Xóa yêu cầu này?')) {
    staffConsultations = staffConsultations.filter(c => c.id !== id);
    localStorage.setItem('contactMessages', JSON.stringify(staffConsultations));
    renderConsultationsList();
    showToast('Xóa yêu cầu thành công!', 'success');
  }
}

// ── ORDERS ────────────────────────────────────────────
function renderOrdersList() {
  const listBody = document.getElementById('ordersList');
  if (!listBody) return;

  listBody.innerHTML = staffOrders.map(o => `
    <tr>
      <td>${o.id}</td>
      <td>${o.customerName}</td>
      <td>${o.productName}</td>
      <td>${o.orderDate}</td>
      <td>
        <select class="form-select form-select-sm" onchange="updateOrderStatus(${o.id}, this.value)">
          <option value="Pending" ${o.status === 'Pending' ? 'selected' : ''}>Chờ xử lý</option>
          <option value="Confirmed" ${o.status === 'Confirmed' ? 'selected' : ''}>Xác nhận</option>
          <option value="Completed" ${o.status === 'Completed' ? 'selected' : ''}>Hoàn thành</option>
          <option value="Cancelled" ${o.status === 'Cancelled' ? 'selected' : ''}>Hủy</option>
        </select>
      </td>
      <td>
        <button class="btn btn-sm btn-danger" onclick="deleteOrder(${o.id})"><i class="fa-solid fa-trash"></i></button>
      </td>
    </tr>
  `).join('');
}

function updateOrderStatus(id, status) {
  const order = staffOrders.find(o => o.id === id);
  if (order) {
    order.status = status;
    localStorage.setItem('orders', JSON.stringify(staffOrders));
    renderOrdersList();
    showToast('Cập nhật trạng thái đơn hàng!', 'success');
  }
}

function deleteOrder(id) {
  if (confirm('Xóa đơn hàng này?')) {
    staffOrders = staffOrders.filter(o => o.id !== id);
    localStorage.setItem('orders', JSON.stringify(staffOrders));
    renderOrdersList();
    showToast('Xóa đơn hàng thành công!', 'success');
  }
}

// ── PROMOTIONS ────────────────────────────────────────────
function renderPromotionsList() {
  const listBody = document.getElementById('promotionsList');
  if (!listBody) return;

  listBody.innerHTML = staffPromotions.map(p => `
    <tr>
      <td>${p.id}</td>
      <td><strong>${p.title}</strong></td>
      <td>${p.description.substring(0, 40)}...</td>
      <td><span class="badge bg-success">${p.discount}%</span></td>
      <td>${p.startDate}</td>
      <td>
        <button class="btn btn-sm btn-primary" onclick="editPromotion(${p.id})"><i class="fa-solid fa-edit"></i></button>
        <button class="btn btn-sm btn-danger" onclick="deletePromotion(${p.id})"><i class="fa-solid fa-trash"></i></button>
      </td>
    </tr>
  `).join('');
}

function addPromotion() {
  const title = prompt('Tên chương trình khuyến mãi:');
  if (!title) return;

  const description = prompt('Mô tả:');
  if (!description) return;

  const discount = parseInt(prompt('Phần trăm giảm giá (%):')) || 0;
  const startDate = prompt('Ngày bắt đầu (DD/MM/YYYY):') || new Date().toLocaleDateString('vi-VN');

  const newPromo = {
    id: Date.now(),
    title,
    description,
    discount,
    startDate
  };

  staffPromotions.push(newPromo);
  localStorage.setItem('promotions', JSON.stringify(staffPromotions));
  renderPromotionsList();
  showToast('Thêm chương trình khuyến mãi thành công!', 'success');
}

function editPromotion(id) {
  const promo = staffPromotions.find(p => p.id === id);
  if (!promo) return;

  const title = prompt('Tên chương trình:', promo.title);
  const description = prompt('Mô tả:', promo.description);
  const discount = parseInt(prompt('Phần trăm giảm giá (%):', promo.discount)) || 0;
  const startDate = prompt('Ngày bắt đầu:', promo.startDate);

  if (title && description) {
    promo.title = title;
    promo.description = description;
    promo.discount = discount;
    promo.startDate = startDate;
    localStorage.setItem('promotions', JSON.stringify(staffPromotions));
    renderPromotionsList();
    showToast('Cập nhật chương trình khuyến mãi thành công!', 'success');
  }
}

function deletePromotion(id) {
  if (confirm('Xóa chương trình khuyến mãi này?')) {
    staffPromotions = staffPromotions.filter(p => p.id !== id);
    localStorage.setItem('promotions', JSON.stringify(staffPromotions));
    renderPromotionsList();
    showToast('Xóa chương trình khuyến mãi thành công!', 'success');
  }
}
