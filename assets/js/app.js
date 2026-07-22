// app.js - Hàm chung cho motor chất

// Utility functions
function showToast(message, type = 'success') {
  // Simple toast implementation
  const toast = document.createElement('div');
  toast.className = `alert alert-${type} position-fixed`;
  toast.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
  toast.textContent = message;
  document.body.appendChild(toast);
  setTimeout(() => toast.remove(), 3000);
}

function confirmAction(message) {
  return confirm(message);
}

// Dark mode toggle (bonus)
let darkMode = localStorage.getItem('darkMode') === 'true';

function applyDarkModeIcon() {
  const btn = document.querySelector('.dark-toggle i');
  if (!btn) return;
  if (darkMode) {
    btn.classList.remove('fa-moon');
    btn.classList.add('fa-sun');
  } else {
    btn.classList.remove('fa-sun');
    btn.classList.add('fa-moon');
  }
}

function toggleDarkMode() {
  darkMode = !darkMode;
  document.body.classList.toggle('bg-dark', darkMode);
  document.body.classList.toggle('text-white', darkMode);
  localStorage.setItem('darkMode', darkMode);
  applyDarkModeIcon();
}

function ensureChangePasswordUi(loginLink) {
  let navItem = document.getElementById('changePasswordNavItem');
  if (!navItem && loginLink?.parentElement) {
    navItem = document.createElement('li');
    navItem.id = 'changePasswordNavItem';
    navItem.className = 'nav-item';
    navItem.innerHTML = '<a class="nav-link" href="#" id="changePasswordLink"><i class="fa-solid fa-key me-1"></i>Đổi mật khẩu</a>';
    loginLink.parentElement.before(navItem);
  }

  if (!document.getElementById('changePasswordModal')) {
    const modal = document.createElement('div');
    modal.className = 'modal fade';
    modal.id = 'changePasswordModal';
    modal.tabIndex = -1;
    modal.innerHTML = `
      <div class="modal-dialog modal-dialog-centered"><div class="modal-content">
        <div class="modal-header"><h5 class="modal-title">Đổi mật khẩu</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
        <form id="changePasswordForm">
          <div class="modal-body">
            <div class="mb-3"><label class="form-label" for="currentPassword">Mật khẩu hiện tại</label><input class="form-control" type="password" id="currentPassword" maxlength="128" autocomplete="current-password" required></div>
            <div class="mb-3"><label class="form-label" for="newPassword">Mật khẩu mới</label><input class="form-control" type="password" id="newPassword" minlength="8" maxlength="128" autocomplete="new-password" required><div class="form-text">8–128 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt.</div></div>
            <div class="mb-3"><label class="form-label" for="confirmNewPassword">Xác nhận mật khẩu mới</label><input class="form-control" type="password" id="confirmNewPassword" minlength="8" maxlength="128" autocomplete="new-password" required></div>
            <div id="changePasswordMessage" class="small"></div>
          </div>
          <div class="modal-footer"><button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Hủy</button><button type="submit" class="btn btn-primary" id="changePasswordSubmit">Đổi mật khẩu</button></div>
        </form>
      </div></div>`;
    document.body.appendChild(modal);

    document.getElementById('changePasswordForm').addEventListener('submit', handleChangePassword);
  }

  const link = document.getElementById('changePasswordLink');
  if (link) {
    link.onclick = event => {
      event.preventDefault();
      document.getElementById('changePasswordForm').reset();
      document.getElementById('changePasswordMessage').textContent = '';
      bootstrap.Modal.getOrCreateInstance(document.getElementById('changePasswordModal')).show();
    };
  }

  if (navItem) navItem.style.display = '';
}

async function handleChangePassword(event) {
  event.preventDefault();
  const currentPassword = document.getElementById('currentPassword').value;
  const newPassword = document.getElementById('newPassword').value;
  const confirmPassword = document.getElementById('confirmNewPassword').value;
  const message = document.getElementById('changePasswordMessage');
  const submitButton = document.getElementById('changePasswordSubmit');
  const strongPassword = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,128}$/;

  if (!strongPassword.test(newPassword)) {
    message.className = 'small text-danger';
    message.textContent = 'Mật khẩu mới chưa đáp ứng chính sách bảo mật.';
    return;
  }
  if (newPassword !== confirmPassword) {
    message.className = 'small text-danger';
    message.textContent = 'Mật khẩu xác nhận không khớp.';
    return;
  }

  submitButton.disabled = true;
  try {
    const result = await Api.changePassword({ currentPassword, newPassword, confirmPassword });
    message.className = 'small text-success';
    message.textContent = result.message;
    setTimeout(() => {
      Session.clear();
      window.location.href = 'login.html';
    }, 800);
  } catch (error) {
    message.className = 'small text-danger';
    message.textContent = apiErrorMessage(error);
  } finally {
    submitButton.disabled = false;
  }
}

function ensureNavbarLink(href, label, loginLink) {
  let link = document.querySelector(`.navbar-nav a[href="${href}"]`);
  if (link) return link;

  const navbar = loginLink?.closest('.navbar-nav') || document.querySelector('.navbar-nav');
  if (!navbar) return null;

  const item = document.createElement('li');
  item.className = 'nav-item';
  link = document.createElement('a');
  link.className = 'nav-link';
  link.href = href;
  link.textContent = label;
  if (window.location.pathname.toLowerCase().endsWith('/' + href.toLowerCase())) {
    link.classList.add('active');
  }
  item.appendChild(link);
  navbar.insertBefore(item, loginLink?.parentElement || navbar.lastElementChild);
  return link;
}

function setNavbarItemVisible(link, visible) {
  if (!link) return;
  const item = link.closest('.nav-item') || link;
  item.style.setProperty('display', visible ? '' : 'none', 'important');
}

// Update navbar based on login status
function updateNavbar() {
  const userRole = (typeof Session !== 'undefined' && Session.isLoggedIn())
    ? roleToAppRole(Session.getRole())
    : null;
  let homeLink = document.querySelector('.navbar-nav a[href="index.html"]');
  const sanphamLink = document.querySelector('.navbar-nav a[href="sanpham.html"]');
  const tasksLink = document.querySelector('.navbar-nav a[href="tasks.html"]');
  let staffLink = document.querySelector('.navbar-nav a[href="staff.html"]');
  let suppliersLink = document.querySelector('.navbar-nav a[href="suppliers.html"]');
  const usersLink = document.querySelector('.navbar-nav a[href="users.html"]');
  const aboutLink = document.querySelector('.navbar-nav a[href="about.html"]');
  const contactLink = document.querySelector('.navbar-nav a[href="lienhe.html"]');
  const loginLink = document.querySelector('.navbar-nav a[href="login.html"]');

  function bindLogout() {
    if (loginLink) {
      ensureChangePasswordUi(loginLink);
      loginLink.textContent = 'Đăng xuất (' + (Session.getUser()?.fullName || '') + ')';
      loginLink.href = '#';
      loginLink.onclick = async function(e) {
        e.preventDefault();
        try {
          await Api.logout();
        } catch {
          // Vẫn xóa dữ liệu local nếu server không thể kết nối.
        } finally {
          Session.clear();
          window.location.href = 'login.html';
        }
      };
    }
  }

  if (userRole === 'admin') {
    if (sanphamLink) sanphamLink.style.display = 'none';
    if (tasksLink) tasksLink.style.display = '';
    if (staffLink) staffLink.style.display = 'none';
    if (aboutLink) aboutLink.style.display = '';
    if (homeLink) homeLink.style.display = 'none';
    if (contactLink) contactLink.style.display = 'none';
    bindLogout();
  } else if (userRole === 'staff') {
    homeLink = ensureNavbarLink('index.html', 'Trang chủ', loginLink);
    staffLink = ensureNavbarLink('staff.html', 'Quản lý', loginLink);
    suppliersLink = ensureNavbarLink('suppliers.html', 'Nhà cung cấp', loginLink);

    const navbar = loginLink?.closest('.navbar-nav');
    const menuAnchor = loginLink?.parentElement;
    if (navbar && menuAnchor) {
      [homeLink, staffLink, suppliersLink].forEach(link => {
        if (link?.parentElement) navbar.insertBefore(link.parentElement, menuAnchor);
      });
    }

    setNavbarItemVisible(homeLink, true);
    setNavbarItemVisible(staffLink, true);
    setNavbarItemVisible(suppliersLink, true);
    setNavbarItemVisible(sanphamLink, false);
    setNavbarItemVisible(tasksLink, false);
    setNavbarItemVisible(usersLink, false);
    setNavbarItemVisible(aboutLink, false);
    setNavbarItemVisible(contactLink, false);
    bindLogout();
  } else if (userRole === 'customer') {
    if (sanphamLink) sanphamLink.style.display = '';
    if (tasksLink) tasksLink.style.display = 'none';
    if (staffLink) staffLink.style.display = 'none';
    setNavbarItemVisible(suppliersLink, false);
    setNavbarItemVisible(usersLink, false);
    if (aboutLink) aboutLink.style.display = 'none';
    if (homeLink) homeLink.style.display = '';
    if (contactLink) contactLink.style.display = '';
    bindLogout();
  } else {
    const changePasswordNavItem = document.getElementById('changePasswordNavItem');
    if (changePasswordNavItem) changePasswordNavItem.style.display = 'none';
    if (sanphamLink) sanphamLink.style.display = '';
    setNavbarItemVisible(tasksLink, false);
    setNavbarItemVisible(staffLink, false);
    setNavbarItemVisible(suppliersLink, false);
    setNavbarItemVisible(usersLink, false);
    setNavbarItemVisible(aboutLink, false);
    if (homeLink) homeLink.style.display = '';
    if (contactLink) contactLink.style.display = '';
    if (loginLink) {
      loginLink.textContent = 'Đăng nhập';
      loginLink.href = 'login.html';
      loginLink.onclick = null;
    }
  }
}

// Initialize on load
document.addEventListener('DOMContentLoaded', () => {
  if (darkMode) {
    document.body.classList.add('bg-dark', 'text-white');
  }
  applyDarkModeIcon();
  updateNavbar();
});

// JavaScript for Airblade Section
const abVariants = [
  {
    id: 'sp', name: 'Airblade 160',
    specs: { engine: '160cc eSP+', power: '12.2 kW', torque: '14.3 Nm', weight: '113 kg', fuel: '4.2 L' },
    img: 'assets/img/xe-ga/airblade-trang-do.png',
  },
  {
    id: 'premium', name: 'Airblade 160 Premium',
    specs: { engine: '160cc eSP+', power: '12.2 kW', torque: '14.3 Nm', weight: '112 kg', fuel: '4.2 L' },
    img: 'assets/img/xe-ga/airblade-feature-2.png',
  },
  {
    id: 'standard', name: 'Airblade 125',
    specs: { engine: '125cc eSP', power: '10.5 kW', torque: '12.5 Nm', weight: '108 kg', fuel: '3.8 L' },
    img: 'assets/img/xe-ga/airblade-feature-3.png',
  }
];

const abTabs = [
  { id: 'specs', label: 'Thông số' },
  { id: 'feature', label: 'Tính năng' },
  { id: 'design', label: 'Thiết kế' },
  { id: 'engine', label: 'Động cơ' },
  { id: 'tech', label: 'Công nghệ' },
];

function renderAbTabContent(variant, tab) {
  switch (tab) {
    case 'specs':
      return `<table class="specs-table">
        <tr><th>Thông số kỹ thuật</th><th>Giá trị</th></tr>
        <tr><td>Dung tích xy-lanh</td><td>${variant.specs.engine}</td></tr>
        <tr><td>Công suất cực đại</td><td>${variant.specs.power}</td></tr>
        <tr><td>Mô-men xoắn cực đại</td><td>${variant.specs.torque}</td></tr>
        <tr><td>Khối lượng</td><td>${variant.specs.weight}</td></tr>
        <tr><td>Dung tích bình xăng</td><td>${variant.specs.fuel}</td></tr>
      </table>`;
    case 'feature':
      return `<div class="feature-list">
        <div class="feature-item"><h5>ABS</h5><p>Hệ thống chống bó cứng phanh ABS giúp an toàn khi phanh gấp.</p></div>
        <div class="feature-item"><h5>Smart Key</h5><p>Chìa khóa thông minh giúp mở/tắt xe tiện lợi, chống trộm hiệu quả.</p></div>
        <div class="feature-item"><h5>Đèn LED</h5><p>Đèn pha LED hiện đại, chiếu sáng mạnh, tiết kiệm điện.</p></div>
        <div class="feature-item"><h5>Cốp rộng</h5><p>Cốp xe dung tích lớn, chứa được nhiều vật dụng cá nhân.</p></div>
      </div>`;
    case 'design':
      return `<div class="row">
        <div class="col-md-6">
          <h2>THIẾT KẾ THỂ THAO</h2>
          <ul>
            <li>Mặt nạ góc cạnh</li>
            <li>Yên xe thoải mái</li>
            <li>Tem xe cá tính</li>
          </ul>
        </div>
        <div class="col-md-6">
          <img src="assets/img/xe-ga/airblade-trang-do.png" alt="Thiết kế Airblade">
        </div>
      </div>`;
    case 'engine':
      return `<div class="row">
        <div class="col-md-6">
          <h2>ĐỘNG CƠ eSP+</h2>
          <ul>
            <li>Tiết kiệm nhiên liệu</li>
            <li>Công suất mạnh</li>
            <li>Vận hành êm ái</li>
          </ul>
        </div>
        <div class="col-md-6">
          <img src="assets/img/xe-ga/airblade-feature-3.png" alt="Động cơ Airblade">
        </div>
      </div>`;
    case 'tech':
      return `<div class="row">
        <div class="col-md-6">
          <h2>CÔNG NGHỆ NỔI BẬT</h2>
          <ul>
            <li>Smart Key</li>
            <li>Mặt đồng hồ hiện đại</li>
            <li>Đèn LED an toàn</li>
          </ul>
        </div>
        <div class="col-md-6">
          <img src="assets/img/xe-ga/airblade-the-thao.png" alt="Công nghệ Airblade">
        </div>
      </div>`;
    default:
      return '';
  }
}

function renderAbVariants(selectedId) {
  return abVariants.map(v => `<button class="variant-btn${v.id === selectedId ? ' active' : ''}" data-id="${v.id}">${v.name}</button>`).join('');
}

function renderAbTabs(selectedTab) {
  return abTabs.map(t => `<button class="tab-btn${t.id === selectedTab ? ' active':''}" data-tab="${t.id}">${t.label}</button>`).join('');
}

let abCurrentVariant = abVariants[0];
let abCurrentTab = abTabs[0].id;

function updateAbUI() {
  if (!document.getElementById('variant-buttons')) return; // Trang không có section Airblade
  document.getElementById('variant-buttons').innerHTML = renderAbVariants(abCurrentVariant.id);
  document.getElementById('details-tabs').innerHTML = renderAbTabs(abCurrentTab);
  document.getElementById('tab-content').innerHTML = renderAbTabContent(abCurrentVariant, abCurrentTab);

  // Dropdown phiên bản Air Blade
  const versionBox = document.getElementById('versionBox');
  const currentLabel = document.getElementById('currentVariantLabel');
  const variantList = document.getElementById('variantList');
  if (versionBox && currentLabel && variantList) {
    // Render các nút phiên bản vào variantList
    variantList.innerHTML = abVariants.map(v => `
      <button class="ab-version-btn${v.id === abCurrentVariant.id ? ' active' : ''}" data-id="${v.id}">${v.name}</button>
    `).join('');

    // Đóng mặc định
    versionBox.classList.remove('open');

    // Toggle dropdown khi click vào current
    currentLabel.onclick = function() {
      versionBox.classList.toggle('open');
    };
    currentLabel.onkeydown = function(e) {
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        versionBox.classList.toggle('open');
      }
    };

    // Chọn phiên bản
    variantList.querySelectorAll('.ab-version-btn').forEach(btn => {
      btn.onclick = function() {
        abCurrentVariant = abVariants.find(v => v.id === this.dataset.id);
        versionBox.classList.remove('open');
        updateAbUI();
      };
    });
    // Cập nhật label
    currentLabel.childNodes[0].nodeValue = abCurrentVariant.name + ' ';
  }

  document.querySelectorAll('.variant-btn').forEach(btn => btn.onclick = function () {
    abCurrentVariant = abVariants.find(v => v.id === this.dataset.id);
    updateAbUI();
  });

  document.querySelectorAll('.tab-btn').forEach(btn => btn.onclick = function () {
    abCurrentTab = this.dataset.tab;
    updateAbUI();
  });
}

document.addEventListener('DOMContentLoaded', updateAbUI);
