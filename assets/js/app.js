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

// Update navbar based on login status
function updateNavbar() {
  const userRole = sessionStorage.getItem('userRole');
  const homeLink = document.querySelector('a[href="index.html"]');
  const sanphamLink = document.querySelector('a[href="sanpham.html"]');
  const tasksLink = document.querySelector('a[href="tasks.html"]');
  const aboutLink = document.querySelector('a[href="about.html"]');
  const contactLink = document.querySelector('a[href="lienhe.html"]');
  const loginLink = document.querySelector('a[href="login.html"]');

  if (userRole === 'admin') {
    if (sanphamLink) sanphamLink.style.display = '';
    if (tasksLink) tasksLink.style.display = '';
    if (aboutLink) aboutLink.style.display = '';
    if (homeLink) homeLink.style.display = 'none';
    if (contactLink) contactLink.style.display = 'none';
    if (loginLink) {
      loginLink.textContent = 'Đăng xuất';
      loginLink.href = '#';
      loginLink.onclick = function(e) {
        e.preventDefault();
        sessionStorage.removeItem('userRole');
        window.location.href = 'login.html';
      };
    }
  } else if (userRole === 'customer') {
    if (sanphamLink) sanphamLink.style.display = '';
    if (tasksLink) tasksLink.style.display = 'none';
    if (aboutLink) aboutLink.style.display = 'none';
    if (homeLink) homeLink.style.display = '';
    if (contactLink) contactLink.style.display = '';
    if (loginLink) {
      loginLink.textContent = 'Đăng xuất';
      loginLink.href = '#';
      loginLink.onclick = function(e) {
        e.preventDefault();
        sessionStorage.removeItem('userRole');
        window.location.href = 'login.html';
      };
    }
  } else {
    if (sanphamLink) sanphamLink.style.display = '';
    if (tasksLink) tasksLink.style.display = 'none';
    if (aboutLink) aboutLink.style.display = 'none';
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
  renderStoredProducts();
  applyIndexRoleUI();
});

function applyIndexRoleUI() {
  const role = sessionStorage.getItem('userRole');
  const features = document.getElementById('featuresSection');
  const stats = document.getElementById('statsSection');
  const viewAllBtn = document.getElementById('viewAllBtn');

  if (role === 'customer') {
    if (features) features.style.display = 'none';
    if (stats) stats.style.display = 'none';
    if (viewAllBtn) {
      viewAllBtn.href = '#storedProducts';
      viewAllBtn.addEventListener('click', function(e) {
        e.preventDefault();
        const target = document.getElementById('storedProducts');
        if (target) {
          target.scrollIntoView({ behavior: 'smooth' });
          renderStoredProducts();
        }
      });
    }
  } else if (role === 'admin') {
    if (features) features.style.display = '';
    if (stats) stats.style.display = '';
    if (viewAllBtn) {
      viewAllBtn.href = 'tasks.html';
      viewAllBtn.onclick = null;
    }
  } else {
    if (features) features.style.display = '';
    if (stats) stats.style.display = '';
    if (viewAllBtn) {
      viewAllBtn.href = 'tasks.html';
      viewAllBtn.onclick = null;
    }
  }
}

function renderStoredProducts() {
  const container = document.getElementById('storedProducts');
  if (!container) return;

  const motorbikes = JSON.parse(localStorage.getItem('motorbikes') || '[]');
  if (motorbikes.length === 0) {
    container.innerHTML = '';
    return;
  }

  container.innerHTML = motorbikes.map(m => `
    <div class="col-lg-4 col-md-6">
      <div class="card product-card h-100">
        <img src="assets/img/${m.title.toLowerCase().replace(/\s+/g, '')}.jpg" class="card-img-top" alt="${m.title}" onerror="this.src='assets/img/banner.jpg'">
        <div class="card-body">
          <span class="product-tag">${m.priority || 'Không rõ'}</span>
          <h5 class="card-title">${m.title}</h5>
          <p class="product-price">${m.price || 'Liên hệ'}</p>
          <p class="card-text text-muted">${m.description || 'Mô tả đang cập nhật...'}</p>
          <a href="tasks.html" class="btn btn-outline-primary">Xem chi tiết</a>
        </div>
      </div>
    </div>
  `).join('');
}

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