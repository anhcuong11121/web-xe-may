// tasks.js - Logic CRUD cho motor chất

let motorbikes = JSON.parse(localStorage.getItem('motorbikes')) || [];
let currentMotorbikeId = null;
let currentPage = 1;
const motorbikesPerPage = 10;

// Auto-seed from motorbikes.json if localStorage is empty
async function seedIfEmpty() {
  if (localStorage.getItem('motorbikes')) {
    // Patch existing data: add default color if missing
    let changed = false;
    motorbikes = motorbikes.map(m => {
      if (!m.colors || !Array.isArray(m.colors) || !m.colors.length) {
      m.colors = m.color ? [m.color] : ['Đen'];
      delete m.color;
      changed = true;
    }
      return m;
    });
    if (changed) localStorage.setItem('motorbikes', JSON.stringify(motorbikes));
    return;
  }
  try {
    const res = await fetch('motorbikes.json');
    if (!res.ok) return;
    const data = await res.json();
    localStorage.setItem('motorbikes', JSON.stringify(data));
    motorbikes = data;
    if (document.getElementById('taskList')) renderMotorbikes();
    updateStats();
    updateQuickStats();
    window.dispatchEvent(new Event('motorbikesUpdated'));
  } catch (e) {
    console.warn('Không thể seed motorbikes.json:', e);
  }
}

// Load motorbikes on page load
document.addEventListener('DOMContentLoaded', () => {
  seedIfEmpty().then(() => {
    if (document.getElementById('taskList')) {
      renderMotorbikes();
    }
    updateStats();
    updateQuickStats();
  });
});

// Save motorbikes to LocalStorage and broadcast update
function saveMotorbikes() {
  localStorage.setItem('motorbikes', JSON.stringify(motorbikes));
  updateQuickStats();
  window.dispatchEvent(new Event('motorbikesUpdated'));
}

// When another tab/window updates, refresh local state and UI
window.addEventListener('storage', (event) => {
  if (event.key === 'motorbikes') {
    motorbikes = JSON.parse(event.newValue) || [];
    if (document.getElementById('taskList')) renderMotorbikes();
    updateStats();
  }
});

// In same tab, listen to custom update events
window.addEventListener('motorbikesUpdated', () => {
  motorbikes = JSON.parse(localStorage.getItem('motorbikes')) || [];
  if (document.getElementById('taskList')) renderMotorbikes();
  updateStats();
});

// Render motorbike list with pagination
function renderMotorbikes(filteredMotorbikes = motorbikes) {
  const taskList = document.getElementById('taskList');
  if (!taskList) {
    // about.html không có bảng task, chỉ cần cập nhật số liệu
    return;
  }

  const start = (currentPage - 1) * motorbikesPerPage;
  const end = start + motorbikesPerPage;
  const paginatedMotorbikes = filteredMotorbikes.slice(start, end);

  taskList.innerHTML = paginatedMotorbikes.map(motorbike => `
    <tr class="${motorbike.status === 'Sold' ? 'table-success' : ''}">
      <td>${motorbike.id}</td>
      <td>${motorbike.title}</td>
      <td>${motorbike.description.length > 50 ? motorbike.description.substring(0, 50) + '...' : motorbike.description}</td>
      <td>${motorbike.deadline}</td>
      <td><span class="badge bg-${getPriorityColor(motorbike.priority)}">${getPriorityText(motorbike.priority)}</span></td>
      <td>${renderColorSwatch(motorbike.colors)}</td>
      <td><span class="badge bg-${motorbike.status === 'Sold' ? 'success' : 'warning'}">${getStatusText(motorbike.status)}</span></td>
      <td>
        <button class="btn btn-sm btn-primary me-1" onclick="editMotorbike(${motorbike.id})">Sửa</button>
        <button class="btn btn-sm btn-success me-1" onclick="toggleStatus(${motorbike.id})">${motorbike.status === 'Sold' ? 'Còn hàng' : 'Đã bán'}</button>
        <button class="btn btn-sm btn-danger" onclick="deleteMotorbike(${motorbike.id})">Xóa</button>
      </td>
    </tr>
  `).join('');

  renderPagination(filteredMotorbikes.length);
}

// ── Color definitions ────────────────────────────────────
const COLOR_STYLES = {
  'Đen':        'background:#111111',
  'Đỏ':         'background:#dc3545',
  'Trắng':      'background:#f5f5f5;border:1.5px solid #ccc',
  'Bạc':        'background:#c0c0c0',
  'Xám':        'background:#6c757d',
  'Vàng':       'background:#ffc107',
  'Xanh lá':    'background:#198754',
  'Xanh biển':  'background:#0d6efd',
  'Cam':        'background:#fd7e14',
  'Nâu':        'background:#8B6E4E',
  'Đồng':       'background:#b87333',
  'Xanh':       'background:#0d6efd',
  'Xám nhám':   'background:#9e9e9e',
  'Vàng cam':   'background:linear-gradient(135deg,#ffc107 50%,#fd7e14 50%)',
  'Đỏ đen':     'background:linear-gradient(135deg,#dc3545 50%,#111 50%)',
  'Trắng đỏ':   'background:linear-gradient(135deg,#f5f5f5 50%,#dc3545 50%)',
  'Bạc đen':    'background:linear-gradient(135deg,#c0c0c0 50%,#111 50%)',
  'Đỏ xám':     'background:linear-gradient(135deg,#dc3545 50%,#6c757d 50%)',
  'Nâu xám':    'background:linear-gradient(135deg,#8B6E4E 50%,#6c757d 50%)',
  'Xám đen':    'background:linear-gradient(135deg,#9e9e9e 50%,#111 50%)',
  'Xanh đen':   'background:linear-gradient(135deg,#0d6efd 50%,#111 50%)',
  'Xanh xám':   'background:linear-gradient(135deg,#0dcaf0 50%,#9e9e9e 50%)',
  'Đen bạc':    'background:linear-gradient(135deg,#111 50%,#c0c0c0 50%)',
  'Đỏ bạc':     'background:linear-gradient(135deg,#dc3545 50%,#c0c0c0 50%)',
  'Đen vàng':   'background:linear-gradient(135deg,#111 50%,#ffc107 50%)',
  'Xám bạc':    'background:linear-gradient(135deg,#6c757d 50%,#c0c0c0 50%)',
  'Xanh bạc':   'background:linear-gradient(135deg,#0d6efd 50%,#c0c0c0 50%)',
  'Đen đỏ':     'background:linear-gradient(135deg,#111 50%,#dc3545 50%)',
  'Đen xám':    'background:linear-gradient(135deg,#111 50%,#9e9e9e 50%)',
  'Trắng đen':  'background:linear-gradient(135deg,#f5f5f5 50%,#111 50%)',
};

const BIKE_COLORS = {
  'Honda Airblade': [
    { name: 'Đen',      style: 'background:#111111' },
    { name: 'Đỏ đen',   style: 'background:linear-gradient(135deg,#dc3545 50%,#111 50%)' },
    { name: 'Trắng đỏ', style: 'background:linear-gradient(135deg,#f5f5f5 50%,#dc3545 50%)' }
  ],
  'Honda Vision': [
    { name: 'Đen',     style: 'background:#111111' },
    { name: 'Bạc đen', style: 'background:linear-gradient(135deg,#c0c0c0 50%,#111 50%)' },
    { name: 'Đỏ xám',  style: 'background:linear-gradient(135deg,#dc3545 50%,#6c757d 50%)' },
    { name: 'Nâu xám', style: 'background:linear-gradient(135deg,#8B6E4E 50%,#6c757d 50%)' },
    { name: 'Xám đen', style: 'background:linear-gradient(135deg,#9e9e9e 50%,#111 50%)' }
  ],
  'Yamaha Janus': [
    { name: 'Đen',      style: 'background:#111111' },
    { name: 'Đỏ đen',   style: 'background:linear-gradient(135deg,#dc3545 50%,#111 50%)' },
    { name: 'Xám nhám', style: 'background:#9e9e9e' },
    { name: 'Xanh đen', style: 'background:linear-gradient(135deg,#0d6efd 50%,#111 50%)' },
    { name: 'Xanh xám', style: 'background:linear-gradient(135deg,#0dcaf0 50%,#9e9e9e 50%)' }
  ],
  'Yamaha Grande': [
    { name: 'Đen bạc',  style: 'background:linear-gradient(135deg,#111 50%,#c0c0c0 50%)' },
    { name: 'Đỏ bạc',   style: 'background:linear-gradient(135deg,#dc3545 50%,#c0c0c0 50%)' },
    { name: 'Đen vàng', style: 'background:linear-gradient(135deg,#111 50%,#ffc107 50%)' },
    { name: 'Đỏ đen',   style: 'background:linear-gradient(135deg,#dc3545 50%,#111 50%)' },
    { name: 'Xám bạc',  style: 'background:linear-gradient(135deg,#6c757d 50%,#c0c0c0 50%)' },
    { name: 'Xanh bạc', style: 'background:linear-gradient(135deg,#0d6efd 50%,#c0c0c0 50%)' }
  ],
  'Honda CT125': [
    { name: 'Đỏ',       style: 'background:#dc3545' },
    { name: 'Đen',      style: 'background:#111111' },
    { name: 'Vàng cam', style: 'background:linear-gradient(135deg,#ffc107 50%,#fd7e14 50%)' }
  ],
  'Honda Wave Alpha': [
    { name: 'Đen',   style: 'background:#111111' },
    { name: 'Đỏ',    style: 'background:#dc3545' },
    { name: 'Trắng', style: 'background:#f5f5f5;border:1.5px solid #ccc' },
    { name: 'Xanh',  style: 'background:#0d6efd' }
  ],
  'Honda Winner R': [
    { name: 'Đen',      style: 'background:#111111' },
    { name: 'Đen bạc',  style: 'background:linear-gradient(135deg,#111 50%,#c0c0c0 50%)' },
    { name: 'Đỏ đen',   style: 'background:linear-gradient(135deg,#dc3545 50%,#111 50%)' },
    { name: 'Xám đen',  style: 'background:linear-gradient(135deg,#9e9e9e 50%,#111 50%)' },
    { name: 'Xanh đen', style: 'background:linear-gradient(135deg,#0d6efd 50%,#111 50%)' }
  ],
  'Honda CBR150R': [
    { name: 'Đen đỏ',  style: 'background:linear-gradient(135deg,#111 50%,#dc3545 50%)' },
    { name: 'Đen xám', style: 'background:linear-gradient(135deg,#111 50%,#9e9e9e 50%)' },
    { name: 'Đỏ',      style: 'background:#dc3545' }
  ],
  'Yamaha YZF-R15': [
    { name: 'Đen',      style: 'background:#111111' },
    { name: 'Xanh đen', style: 'background:linear-gradient(135deg,#0d6efd 50%,#111 50%)' }
  ],
  'Honda Rebel 1100 2025': [
    { name: 'Đen', style: 'background:#111111' },
    { name: 'Xám', style: 'background:#6c757d' }
  ],
  'Honda NX500': [
    { name: 'Đen',       style: 'background:#111111' },
    { name: 'Đỏ đen',    style: 'background:linear-gradient(135deg,#dc3545 50%,#111 50%)' },
    { name: 'Trắng đen', style: 'background:linear-gradient(135deg,#f5f5f5 50%,#111 50%)' }
  ],
  'VinFast Evo': [
    { name: 'Đen',   style: 'background:#111111' },
    { name: 'Đỏ',    style: 'background:#dc3545' },
    { name: 'Trắng', style: 'background:#f5f5f5;border:1.5px solid #ccc' },
    { name: 'Xanh',  style: 'background:#0d6efd' },
    { name: 'Đồng',  style: 'background:#b87333' }
  ],
  'VinFast Feliz': [
    { name: 'Đen',   style: 'background:#111111' },
    { name: 'Đỏ',    style: 'background:#dc3545' },
    { name: 'Trắng', style: 'background:#f5f5f5;border:1.5px solid #ccc' }
  ]
};

const DEFAULT_COLORS = [
  { name: 'Đen',       style: 'background:#111111' },
  { name: 'Đỏ',        style: 'background:#dc3545' },
  { name: 'Trắng',     style: 'background:#f5f5f5;border:1.5px solid #ccc' },
  { name: 'Bạc',       style: 'background:#c0c0c0' },
  { name: 'Xám',       style: 'background:#6c757d' },
  { name: 'Vàng',      style: 'background:#ffc107' },
  { name: 'Xanh lá',   style: 'background:#198754' },
  { name: 'Xanh biển', style: 'background:#0d6efd' },
  { name: 'Cam',       style: 'background:#fd7e14' }
];

// Render color swatches for table cell (handles color name array)
function renderColorSwatch(colors) {
  if (!colors) return '<span class="text-muted small">-</span>';
  const arr = Array.isArray(colors) ? colors : [colors];
  if (!arr.length) return '<span class="text-muted small">-</span>';
  const dots = arr.map(name => {
    const s = COLOR_STYLES[name] || 'background:#999';
    return `<span class="table-color-dot" style="${s}" title="${name}"></span>`;
  }).join('');
  return `<span class="d-inline-flex align-items-center gap-1 flex-wrap">${dots}<span class="small text-muted">${arr.join(', ')}</span></span>`;
}

// Get priority color
function getPriorityColor(priority) {
  switch (priority) {
    case 'Honda': return 'danger';
    case 'Yamaha': return 'primary';
    case 'VinFast': return 'success';
    default: return 'secondary';
  }
}

// Get priority text
function getPriorityText(priority) {
  return priority; // Honda, Yamaha, etc.
}

// Get status text
function getStatusText(status) {
  return status === 'Sold' ? 'Đã bán' : 'Còn hàng';
}

// Realtime validation
document.getElementById('title').addEventListener('input', validateTitle);
document.getElementById('description').addEventListener('input', validateDescription);
document.getElementById('deadline').addEventListener('change', validateDeadline);

function validateTitle() {
  const title = document.getElementById('title').value.trim();
  if (!title) {
    showFieldError('title', 'Tiêu đề không được rỗng!');
  } else {
    clearFieldError('title');
  }
}

function validateDescription() {
  const desc = document.getElementById('description').value.trim();
  if (desc.length < 10) {
    showFieldError('description', 'Mô tả phải có ít nhất 10 ký tự!');
  } else {
    clearFieldError('description');
  }
}

function validateDeadline() {
  const deadline = document.getElementById('deadline').value;
  if (!deadline || new Date(deadline) <= new Date()) {
    showFieldError('deadline', 'Deadline phải lớn hơn ngày hiện tại!');
  } else {
    clearFieldError('deadline');
  }
}

function showFieldError(fieldId, message) {
  const field = document.getElementById(fieldId);
  field.classList.add('is-invalid');
  let error = document.getElementById(fieldId + 'Error');
  if (!error) {
    error = document.createElement('div');
    error.id = fieldId + 'Error';
    error.className = 'invalid-feedback';
    field.parentNode.appendChild(error);
  }
  error.textContent = message;
}

function clearFieldError(fieldId) {
  const field = document.getElementById(fieldId);
  field.classList.remove('is-invalid');
  const error = document.getElementById(fieldId + 'Error');
  if (error) error.remove();
}

// Render pagination
function renderPagination(totalMotorbikes) {
  const existing = document.getElementById('paginationNav');
  if (existing) existing.remove();

  const totalPages = Math.ceil(totalMotorbikes / motorbikesPerPage);
  if (totalPages <= 1) return;

  const nav = document.createElement('nav');
  nav.id = 'paginationNav';
  nav.innerHTML = `
    <ul class="pagination justify-content-center">
      <li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
        <a class="page-link" href="#" onclick="event.preventDefault();changePage(${currentPage - 1})">Trước</a>
      </li>
      ${Array.from({length: totalPages}, (_, i) => `
        <li class="page-item ${i + 1 === currentPage ? 'active' : ''}">
          <a class="page-link" href="#" onclick="event.preventDefault();changePage(${i + 1})">${i + 1}</a>
        </li>
      `).join('')}
      <li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
        <a class="page-link" href="#" onclick="event.preventDefault();changePage(${currentPage + 1})">Sau</a>
      </li>
    </ul>
  `;
  const container = document.getElementById('paginationContainer');
  if (container) container.appendChild(nav);
  else document.querySelector('.table-responsive').appendChild(nav);
}

// Change page
function changePage(page) {
  const searchTerm = document.getElementById('searchInput').value.toLowerCase();
  const statusFilter = document.getElementById('statusFilter').value;
  const priorityFilter = document.getElementById('priorityFilter').value;
  const filtered = motorbikes.filter(m => {
    const matchesSearch = m.title.toLowerCase().includes(searchTerm) || m.description.toLowerCase().includes(searchTerm);
    const matchesStatus = !statusFilter || m.status === statusFilter;
    const matchesPriority = !priorityFilter || m.priority === priorityFilter;
    return matchesSearch && matchesStatus && matchesPriority;
  });
  const totalPages = Math.ceil(filtered.length / motorbikesPerPage);
  if (page < 1 || page > totalPages) return;
  currentPage = page;
  renderMotorbikes(filtered);
}

// Open add modal
function openAddModal() {
  currentMotorbikeId = null;
  document.getElementById('modalTitle').textContent = 'Thêm Xe';
  document.getElementById('taskForm').reset();
  document.getElementById('deadline').min = new Date().toISOString().split('T')[0];
  loadColorOptions('', []);
}

// ── Color picker (multi-select, per-bike) ──────────────────────
function loadColorOptions(title, selectedArr) {
  const wrap = document.getElementById('colorPickerWrap');
  if (!wrap) return;
  const options = BIKE_COLORS[title] || DEFAULT_COLORS;
  wrap.innerHTML = options.map(c =>
    `<button type="button" class="color-chip${selectedArr.includes(c.name) ? ' is-selected' : ''}" data-color="${c.name}">
      <span class="color-chip-dot" style="${c.style}"></span>${c.name}
    </button>`
  ).join('');
  updateColorLabel(selectedArr);
}

function getSelectedColors() {
  const wrap = document.getElementById('colorPickerWrap');
  if (!wrap) return [];
  return [...wrap.querySelectorAll('.color-chip.is-selected')].map(c => c.dataset.color);
}

function toggleColor(colorName) {
  const wrap = document.getElementById('colorPickerWrap');
  if (!wrap) return;
  const chip = wrap.querySelector(`.color-chip[data-color="${colorName}"]`);
  if (chip) chip.classList.toggle('is-selected');
  updateColorLabel(getSelectedColors());
}

function updateColorLabel(arr) {
  const label = document.getElementById('colorLabel');
  if (label) label.textContent = arr.length ? arr.join(', ') : 'Chưa chọn';
}

// Bind color picker events (delegation + title-input listener)
document.addEventListener('DOMContentLoaded', () => {
  const wrap = document.getElementById('colorPickerWrap');
  if (wrap) {
    wrap.addEventListener('click', e => {
      const chip = e.target.closest('.color-chip');
      if (chip) toggleColor(chip.dataset.color);
    });
  }
  const titleInput = document.getElementById('title');
  if (titleInput) {
    titleInput.addEventListener('input', () => {
      loadColorOptions(titleInput.value.trim(), []);
    });
  }
});

// Edit motorbike
function editMotorbike(id) {
  const motorbike = motorbikes.find(m => m.id === id);
  if (!motorbike) return;

  currentMotorbikeId = id;
  document.getElementById('modalTitle').textContent = 'Sửa Xe';
  document.getElementById('title').value = motorbike.title;
  document.getElementById('description').value = motorbike.description;
  document.getElementById('deadline').value = motorbike.deadline;
  document.getElementById('priority').value = motorbike.priority;
  const savedColors = Array.isArray(motorbike.colors) ? motorbike.colors : (motorbike.color ? [motorbike.color] : []);
  loadColorOptions(motorbike.title, savedColors);

  const modal = new bootstrap.Modal(document.getElementById('taskModal'));
  modal.show();
}

// Save motorbike (add or update)
function saveMotorbike() {
  const title = document.getElementById('title').value.trim();
  const description = document.getElementById('description').value.trim();
  const deadline = document.getElementById('deadline').value;
  const priority = document.getElementById('priority').value;
  const colors = getSelectedColors();

  // Validation
  if (!title) {
    showToast('Tên xe không được rỗng!', 'danger');
    return;
  }
  if (description.length < 10) {
    showToast('Mô tả phải có ít nhất 10 ký tự!', 'danger');
    return;
  }
  if (!deadline || deadline < 1900 || deadline > 2030) {
    showToast('Năm sản xuất phải từ 1900-2030!', 'danger');
    return;
  }

  if (currentMotorbikeId) {
    // Update
    const motorbike = motorbikes.find(m => m.id === currentMotorbikeId);
    if (motorbike) {
      motorbike.title = title;
      motorbike.description = description;
      motorbike.deadline = deadline;
      motorbike.priority = priority;
      motorbike.colors = colors;
      delete motorbike.color;
    }
  } else {
    // Add
    const maxId = motorbikes.reduce((max, m) => Math.max(max, m.id), 1000000000);
    const newMotorbike = {
      id: maxId + 1,
      title,
      description,
      deadline,
      priority,
      colors,
      status: 'Available'
    };
    motorbikes.push(newMotorbike);
  }

  currentPage = 1;
  saveMotorbikes();
  renderMotorbikes();
  updateStats();

  const modal = bootstrap.Modal.getInstance(document.getElementById('taskModal'));
  modal.hide();

  showToast(currentMotorbikeId ? 'Xe đã được cập nhật!' : 'Xe đã được thêm!');
}

// Toggle status
function toggleStatus(id) {
  const motorbike = motorbikes.find(m => m.id === id);
  if (motorbike) {
    motorbike.status = motorbike.status === 'Sold' ? 'Available' : 'Sold';
    saveMotorbikes();
    renderMotorbikes();
    updateStats();
  }
}

// Delete motorbike
function deleteMotorbike(id) {
  if (confirmAction('Bạn có chắc muốn xóa xe này?')) {
    motorbikes = motorbikes.filter(m => m.id !== id);
    currentPage = 1;
    saveMotorbikes();
    renderMotorbikes();
    updateStats();
    showToast('Xe đã được xóa!');
  }
}

// Clear all motorbikes
function clearAllMotorbikes() {
  if (confirmAction('Bạn có chắc muốn xóa tất cả xe?')) {
    motorbikes = [];
    currentPage = 1;
    saveMotorbikes();
    renderMotorbikes();
    updateStats();
    showToast('Tất cả xe đã được xóa!');
  }
}

// Export motorbikes as JSON
function exportMotorbikes() {
  const dataStr = JSON.stringify(motorbikes, null, 2);
  const dataBlob = new Blob([dataStr], { type: 'application/json' });
  const url = URL.createObjectURL(dataBlob);
  const link = document.createElement('a');
  link.href = url;
  link.download = 'motorbikes.json';
  link.click();
  URL.revokeObjectURL(url);
  showToast('File JSON đã được tải xuống!');
}

// Import motorbikes from JSON file
function importMotorbikes() {
  const input = document.createElement('input');
  input.type = 'file';
  input.accept = '.json';
  input.onchange = (e) => {
    const file = e.target.files[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (event) => {
        try {
          const importedMotorbikes = JSON.parse(event.target.result);
          if (Array.isArray(importedMotorbikes)) {
            motorbikes = importedMotorbikes;
            saveMotorbikes();
            renderMotorbikes();
            updateStats();
            showToast('Xe đã được import thành công!');
          } else {
            showToast('File JSON không hợp lệ!', 'danger');
          }
        } catch (error) {
          showToast('Lỗi khi đọc file!', 'danger');
        }
      };
      reader.readAsText(file);
    }
  };
  input.click();
}

// Search and filter
document.getElementById('searchInput').addEventListener('input', () => {
  currentPage = 1;
  filterMotorbikes();
});
document.getElementById('statusFilter').addEventListener('change', () => {
  currentPage = 1;
  filterMotorbikes();
});
document.getElementById('priorityFilter').addEventListener('change', () => {
  currentPage = 1;
  filterMotorbikes();
});

function filterMotorbikes() {
  const searchTerm = document.getElementById('searchInput').value.toLowerCase();
  const statusFilter = document.getElementById('statusFilter').value;
  const priorityFilter = document.getElementById('priorityFilter').value;

  const filtered = motorbikes.filter(motorbike => {
    const matchesSearch = motorbike.title.toLowerCase().includes(searchTerm) || motorbike.description.toLowerCase().includes(searchTerm);
    const matchesStatus = !statusFilter || motorbike.status === statusFilter;
    const matchesPriority = !priorityFilter || motorbike.priority === priorityFilter;
    return matchesSearch && matchesStatus && matchesPriority;
  });

  renderMotorbikes(filtered);
}

// Sort by year
function sortByYear() {
  motorbikes.sort((a, b) => b.deadline - a.deadline); // Newest first
  saveMotorbikes();
  renderMotorbikes();
  showToast('Đã sắp xếp theo năm sản xuất!');
}

// Update quick stats strip on tasks.html
function updateQuickStats() {
  const total = document.getElementById('quickTotal');
  const available = document.getElementById('quickAvailable');
  const sold = document.getElementById('quickSold');
  const brand = document.getElementById('quickBrand');
  if (!total) return;

  const data = JSON.parse(localStorage.getItem('motorbikes') || '[]');
  total.textContent = data.length;
  available.textContent = data.filter(m => m.status === 'Available').length;
  sold.textContent = data.filter(m => m.status === 'Sold').length;

  const counts = data.reduce((acc, m) => {
    acc[m.priority] = (acc[m.priority] || 0) + 1;
    return acc;
  }, {});
  const top = Object.keys(counts).sort((a, b) => counts[b] - counts[a])[0] || '-';
  brand.textContent = top;
}

// Update stats for about page
function updateStats() {
  if (document.getElementById('totalTasks')) {
    const total = motorbikes.length;
    const available = motorbikes.filter(t => t.status === 'Available').length;
    const sold = motorbikes.filter(t => t.status === 'Sold').length;
    const honda = motorbikes.filter(t => t.priority === 'Honda').length;
    const yamaha = motorbikes.filter(t => t.priority === 'Yamaha').length;
    const vinfast = motorbikes.filter(t => t.priority === 'VinFast').length;

    document.getElementById('totalTasks').textContent = total;
    document.getElementById('availableTasks').textContent = available;
    document.getElementById('soldTasks').textContent = sold;
    document.getElementById('lowPriority').textContent = honda;
    document.getElementById('mediumPriority').textContent = yamaha;
    document.getElementById('highPriority').textContent = vinfast;

    // Render chart
    renderChart(available, sold);
  }
}

// Render chart
function renderChart(available, sold) {
  const ctx = document.getElementById('statsChart');
  if (ctx && typeof Chart !== 'undefined') {
    // Destroy existing chart if any
    if (window.statsChartInstance) {
      window.statsChartInstance.destroy();
    }
    window.statsChartInstance = new Chart(ctx, {
      type: 'pie',
      data: {
        labels: ['Còn hàng', 'Đã bán'],
        datasets: [{
          data: [available, sold],
          backgroundColor: ['#ffc107', '#28a745'],
          borderWidth: 1
        }]
      },
      options: {
        responsive: true,
        plugins: {
          legend: {
            position: 'bottom',
          }
        }
      }
    });
  }
}

// ── Contact messages ───────────────────────────────────────
function renderMessages() {
  const list = document.getElementById('messageList');
  if (!list) return;

  const messages = JSON.parse(localStorage.getItem('contactMessages') || '[]');
  const badge = document.getElementById('msgBadge');

  const unread = messages.filter(m => !m.read).length;
  if (badge) {
    badge.textContent = unread;
    badge.classList.toggle('d-none', unread === 0);
  }

  if (!messages.length) {
    list.innerHTML = '<p class="text-muted text-center py-3">Ch\u01b0a c\u00f3 tin nh\u1eafn n\u00e0o.</p>';
    return;
  }

  list.innerHTML = messages.map(m => `
    <div class="card mb-2 border-0 shadow-sm${m.read ? '' : ' border-start border-danger border-3'}" id="msg-${m.id}">
      <div class="card-body py-3 px-4">
        <div class="d-flex justify-content-between align-items-start gap-2 flex-wrap">
          <div>
            <span class="fw-bold">${escHtml(m.name)}</span>
            ${!m.read ? '<span class="badge bg-danger ms-2">M\u1edbi</span>' : ''}
            <span class="text-muted small ms-2">${escHtml(m.email)}${m.phone ? ' \xb7 ' + escHtml(m.phone) : ''}</span>
          </div>
          <div class="d-flex gap-2 align-items-center">
            <small class="text-muted">${escHtml(m.time)}</small>
            ${!m.read ? `<button class="btn btn-outline-secondary btn-sm" onclick="markRead(${m.id})">D\u00e1nh d\u1ea5u d\u00e3 d\u1ecdc</button>` : ''}
            <button class="btn btn-outline-danger btn-sm" onclick="deleteMessage(${m.id})"><i class="fa-solid fa-trash"></i></button>
          </div>
        </div>
        ${m.subject ? `<div class="mt-1"><span class="badge bg-secondary">${escHtml(m.subject)}</span></div>` : ''}
        <p class="mt-2 mb-0 text-muted">${escHtml(m.message)}</p>
      </div>
    </div>
  `).join('');
}

function escHtml(str) {
  return (str || '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

function markRead(id) {
  const messages = JSON.parse(localStorage.getItem('contactMessages') || '[]');
  const msg = messages.find(m => m.id === id);
  if (msg) msg.read = true;
  localStorage.setItem('contactMessages', JSON.stringify(messages));
  renderMessages();
}

function deleteMessage(id) {
  const messages = JSON.parse(localStorage.getItem('contactMessages') || '[]');
  localStorage.setItem('contactMessages', JSON.stringify(messages.filter(m => m.id !== id)));
  renderMessages();
}

function clearAllMessages() {
  if (!confirm('X\u00f3a to\u00e0n b\u1ed9 tin nh\u1eafn li\u00ean h\u1ec7?')) return;
  localStorage.removeItem('contactMessages');
  renderMessages();
}

document.addEventListener('DOMContentLoaded', () => {
  renderMessages();
});