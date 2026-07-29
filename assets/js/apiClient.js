// assets/js/apiClient.js
// API client dùng chung cho toàn bộ frontend để kết nối với MotorBikeShop.API backend.
// LUÔN dùng HTTPS để tránh bị redirect HTTP->HTTPS làm mất header Authorization.
const API_BASE_URL = 'https://localhost:7114';

const Session = {
  KEY_TOKEN: 'mc_token',
  KEY_REFRESH_TOKEN: 'mc_refresh_token',
  KEY_USER: 'mc_user',

  save(loginResponse) {
    localStorage.setItem(this.KEY_TOKEN, loginResponse.token);
    localStorage.setItem(this.KEY_REFRESH_TOKEN, loginResponse.refreshToken);
    localStorage.setItem(this.KEY_USER, JSON.stringify({
      id: loginResponse.userId,
      email: loginResponse.email,
      fullName: loginResponse.fullName,
      role: loginResponse.role,
      expiresAt: loginResponse.expiresAt,
      refreshTokenExpiresAt: loginResponse.refreshTokenExpiresAt
    }));
  },

  clear() {
    localStorage.removeItem(this.KEY_TOKEN);
    localStorage.removeItem(this.KEY_REFRESH_TOKEN);
    localStorage.removeItem(this.KEY_USER);
  },

  getToken() {
    return localStorage.getItem(this.KEY_TOKEN);
  },

  getRefreshToken() {
    return localStorage.getItem(this.KEY_REFRESH_TOKEN);
  },

  getUser() {
    try {
      return JSON.parse(localStorage.getItem(this.KEY_USER) || 'null');
    } catch {
      return null;
    }
  },

  isLoggedIn() {
    const token = this.getToken();
    const refreshToken = this.getRefreshToken();
    const user = this.getUser();
    if (!token || !refreshToken || !user) return false;
    if (user.refreshTokenExpiresAt && new Date(user.refreshTokenExpiresAt) <= new Date()) {
      this.clear();
      return false;
    }
    return true;
  },

  getRole() {
    const user = this.getUser();
    return user ? user.role : null; // 'Customer' | 'Employee' | 'Admin'
  }
};

let refreshPromise = null;

async function refreshAccessToken() {
  if (refreshPromise) return refreshPromise;

  const refreshToken = Session.getRefreshToken();
  if (!refreshToken) throw new Error('Phiên đăng nhập đã hết hạn.');

  refreshPromise = (async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken })
      });
      const data = await response.json().catch(() => null);
      if (!response.ok || !data) {
        throw new Error((data && data.errors && data.errors.join(', ')) || 'Phiên đăng nhập đã hết hạn.');
      }

      Session.save(data);
      return data;
    } catch (error) {
      Session.clear();
      throw error;
    } finally {
      refreshPromise = null;
    }
  })();

  return refreshPromise;
}

// Chuyển Role backend (Customer/Employee/Admin) sang app-role dùng trong UI (customer/staff/admin)
function roleToAppRole(role) {
  switch (role) {
    case 'Admin': return 'admin';
    case 'Employee': return 'staff';
    case 'Customer': return 'customer';
    default: return null;
  }
}

async function apiRequest(path, { method = 'GET', body, auth = true, isFormData = false, retried = false } = {}) {
  const headers = {};
  if (!isFormData) headers['Content-Type'] = 'application/json';
  if (auth && Session.isLoggedIn()) {
    headers['Authorization'] = `Bearer ${Session.getToken()}`;
  }

  let response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      method,
      headers,
      body: body ? (isFormData ? body : JSON.stringify(body)) : undefined
    });
  } catch (networkError) {
    const err = new Error('Không thể kết nối tới server. Vui lòng kiểm tra backend đang chạy tại ' + API_BASE_URL);
    err.status = 0;
    throw err;
  }

  const contentType = response.headers.get('content-type') || '';
  const data = contentType.includes('application/json') ? await response.json().catch(() => null) : null;

  if (response.status === 401 && auth && !retried && Session.getRefreshToken()) {
    await refreshAccessToken();
    return apiRequest(path, { method, body, auth, isFormData, retried: true });
  }

  if (!response.ok) {
    const message = (data && (data.error || (data.errors && (Array.isArray(data.errors) ? data.errors.join(', ') : JSON.stringify(data.errors))))) || `Lỗi ${response.status}`;
    const error = new Error(message);
    error.status = response.status;
    error.data = data;
    throw error;
  }

  return data;
}

async function apiDownload(path, retried = false) {
  const headers = {};
  if (Session.isLoggedIn()) headers.Authorization = `Bearer ${Session.getToken()}`;
  const response = await fetch(`${API_BASE_URL}${path}`, { headers });
  if (response.status === 401 && !retried && Session.getRefreshToken()) {
    await refreshAccessToken();
    return apiDownload(path, true);
  }
  if (!response.ok) {
    const data = await response.json().catch(() => null);
    throw new Error((data && data.error) || `Lỗi ${response.status}`);
  }
  const disposition = response.headers.get('content-disposition') || '';
  const filename = disposition.match(/filename\*?=(?:UTF-8''|\")?([^\";]+)/i)?.[1] || 'thong-ke.csv';
  return { blob: await response.blob(), filename: decodeURIComponent(filename.replace(/\"/g, '')) };
}

const Api = {
  // Auth
  register: (payload) => apiRequest('/api/auth/register', { method: 'POST', body: payload, auth: false }),
  login: (payload) => apiRequest('/api/auth/login', { method: 'POST', body: payload, auth: false }),
  logout: () => apiRequest('/api/auth/logout', { method: 'POST', body: { refreshToken: Session.getRefreshToken() }, auth: false }),
  refresh: () => refreshAccessToken(),
  changePassword: (payload) => apiRequest('/api/auth/change-password', { method: 'POST', body: payload }),
  profile: () => apiRequest('/api/auth/profile'),
  updateProfile: (payload) => apiRequest('/api/auth/profile', { method: 'PUT', body: payload }),

  // Product (Giai đoạn 4)
  getProducts: (query = '') => apiRequest(`/api/products${query}`, { auth: false }),
  getProduct: (id) => apiRequest(`/api/products/${id}`, { auth: false }),
  getProductCatalog: (id) => apiRequest(`/api/products/${id}/catalog`, { auth: false }),
  getCatalogProducts: (query = '') => apiRequest(`/api/products/catalog${query}`, { auth: false }),
  recordProductInterest: (id) => apiRequest(`/api/products/${id}/interest`, { method: 'POST', auth: false }),
  searchProducts: (query = '') => apiRequest(`/api/products/search${query}`, { auth: false }),
  createProduct: (payload) => apiRequest('/api/products', { method: 'POST', body: payload }),
  updateProduct: (id, payload) => apiRequest(`/api/products/${id}`, { method: 'PUT', body: payload }),
  deleteProduct: (id) => apiRequest(`/api/products/${id}`, { method: 'DELETE' }),
  getProductVariants: (productId, manage = false) =>
    apiRequest(`/api/products/${productId}/variants${manage ? '/manage' : ''}`, { auth: manage }),
  createProductVariant: (productId, payload) =>
    apiRequest(`/api/products/${productId}/variants`, { method: 'POST', body: payload }),
  updateProductVariant: (productId, variantId, payload) =>
    apiRequest(`/api/products/${productId}/variants/${variantId}`, { method: 'PUT', body: payload }),
  updateVariantSpecification: (productId, variantId, payload) =>
    apiRequest(`/api/products/${productId}/variants/${variantId}/specification`, { method: 'PUT', body: payload }),
  deleteProductVariant: (productId, variantId) =>
    apiRequest(`/api/products/${productId}/variants/${variantId}`, { method: 'DELETE' }),
  getProductSkus: (productId, variantId, manage = false) =>
    apiRequest(`/api/products/${productId}/variants/${variantId}/skus${manage ? '/manage' : ''}`, { auth: manage }),
  createProductSku: (productId, variantId, payload) =>
    apiRequest(`/api/products/${productId}/variants/${variantId}/skus`, { method: 'POST', body: payload }),
  updateProductSku: (productId, variantId, skuId, payload) =>
    apiRequest(`/api/products/${productId}/variants/${variantId}/skus/${skuId}`, { method: 'PUT', body: payload }),
  deleteProductSku: (productId, variantId, skuId) =>
    apiRequest(`/api/products/${productId}/variants/${variantId}/skus/${skuId}`, { method: 'DELETE' }),
  getProductSkuImages: (productId, variantId, skuId, manage = false) =>
    apiRequest(`/api/products/${productId}/variants/${variantId}/skus/${skuId}/images${manage ? '/manage' : ''}`, { auth: manage }),
  uploadProductSkuImage: (productId, variantId, skuId, file, metadata = {}) => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('altText', metadata.altText || '');
    formData.append('displayOrder', String(metadata.displayOrder || 0));
    formData.append('isPrimary', String(Boolean(metadata.isPrimary)));
    return apiRequest(`/api/products/${productId}/variants/${variantId}/skus/${skuId}/images`, {
      method: 'POST',
      body: formData,
      isFormData: true
    });
  },
  updateProductSkuImage: (productId, variantId, skuId, imageId, payload) =>
    apiRequest(`/api/products/${productId}/variants/${variantId}/skus/${skuId}/images/${imageId}`, { method: 'PUT', body: payload }),
  deleteProductSkuImage: (productId, variantId, skuId, imageId) =>
    apiRequest(`/api/products/${productId}/variants/${variantId}/skus/${skuId}/images/${imageId}`, { method: 'DELETE' }),

  // Vehicle types
  getVehicleTypes: () => apiRequest('/api/vehicle-types', { auth: false }),
  getVehicleType: (id) => apiRequest(`/api/vehicle-types/${id}`, { auth: false }),
  createVehicleType: (payload) => apiRequest('/api/vehicle-types', { method: 'POST', body: payload }),
  updateVehicleType: (id, payload) => apiRequest(`/api/vehicle-types/${id}`, { method: 'PUT', body: payload }),
  deleteVehicleType: (id) => apiRequest(`/api/vehicle-types/${id}`, { method: 'DELETE' }),

  // Brand (Giai đoạn 5)
  getBrands: () => apiRequest('/api/brands', { auth: false }),
  createBrand: (payload) => apiRequest('/api/brands', { method: 'POST', body: payload }),
  updateBrand: (id, payload) => apiRequest(`/api/brands/${id}`, { method: 'PUT', body: payload }),
  deleteBrand: (id) => apiRequest(`/api/brands/${id}`, { method: 'DELETE' }),

  // Customer Support (Giai đoạn 7)
  createSupport: (payload) => apiRequest('/api/support', { method: 'POST', body: payload }),
  getSupportRequests: () => apiRequest('/api/support'),
  getSupportRequest: (id) => apiRequest(`/api/support/${id}`),
  updateSupportRequest: (id, payload) => apiRequest(`/api/support/${id}`, { method: 'PUT', body: payload }),

  // Order (Giai đoạn 8)
  createOrder: (payload) => apiRequest('/api/orders', { method: 'POST', body: payload }),
  getOrders: () => apiRequest('/api/orders'),
  getOrder: (id) => apiRequest(`/api/orders/${id}`),
  updateOrderStatus: (payload) => apiRequest('/api/orders/status', { method: 'PUT', body: payload }),

  // Deposit (Giai đoạn 9)
  getDeposit: (orderId) => apiRequest(`/api/deposit/${orderId}`),

  // Payment reconciliation
  getPaymentConfiguration: () => apiRequest('/api/payments/configuration', { auth: false }),
  getPaymentAttempts: (query = '') => apiRequest(`/api/payments${query}`),
  completeManualPayment: (id) => apiRequest(`/api/payments/${id}/complete-manual`, { method: 'POST' }),
  initiatePayment: (payload) => apiRequest('/api/payments/initiate', { method: 'POST', body: payload }),
  confirmFakePayment: (id) => apiRequest(`/api/payments/${id}/confirm`, { method: 'POST' }),
  getPaymentAttempt: (id) => apiRequest(`/api/payments/${id}`),

  // News (Giai đoạn 10)
  getNews: () => apiRequest('/api/news', { auth: false }),
  getNewsById: (id) => apiRequest(`/api/news/${id}`, { auth: false }),
  getNewsManagement: () => apiRequest('/api/news/manage'),
  getNewsByIdManagement: (id) => apiRequest(`/api/news/manage/${id}`),
  createNews: (payload) => apiRequest('/api/news', { method: 'POST', body: payload }),
  updateNews: (id, payload) => apiRequest(`/api/news/${id}`, { method: 'PUT', body: payload }),
  deleteNews: (id) => apiRequest(`/api/news/${id}`, { method: 'DELETE' }),
  uploadNewsImage: (id, file) => {
    const formData = new FormData();
    formData.append('file', file);
    return apiRequest(`/api/news/${id}/image`, { method: 'POST', body: formData, isFormData: true });
  },

  // Supplier & Import (Giai đoạn 11)
  getSuppliers: () => apiRequest('/api/suppliers'),
  createSupplier: (payload) => apiRequest('/api/suppliers', { method: 'POST', body: payload }),
  updateSupplier: (id, payload) => apiRequest(`/api/suppliers/${id}`, { method: 'PUT', body: payload }),
  deleteSupplier: (id) => apiRequest(`/api/suppliers/${id}`, { method: 'DELETE' }),
  getImports: () => apiRequest('/api/imports'),
  getImport: (id) => apiRequest(`/api/imports/${id}`),
  createImport: (payload) => apiRequest('/api/imports', { method: 'POST', body: payload }),
  cancelImport: (id) => apiRequest(`/api/imports/${id}`, { method: 'DELETE' }),

  // User Management (Giai đoạn 12, Admin)
  getUsers: (query = '') => apiRequest(`/api/users${query}`),
  getUser: (id) => apiRequest(`/api/users/${id}`),
  updateUser: (id, payload) => apiRequest(`/api/users/${id}`, { method: 'PUT', body: payload }),
  updateUserRole: (id, payload) => apiRequest(`/api/users/${id}/role`, { method: 'PUT', body: payload }),
  lockUser: (id) => apiRequest(`/api/users/${id}/lock`, { method: 'PUT' }),
  unlockUser: (id) => apiRequest(`/api/users/${id}/unlock`, { method: 'PUT' }),

  // Customer lookup (Employee, Admin)
  getCustomers: (query = '') => apiRequest(`/api/customers${query}`),
  getCustomer: (id) => apiRequest(`/api/customers/${id}`),

  // Employee CRUD (Admin)
  getEmployees: (query = '') => apiRequest(`/api/employees${query}`),
  getEmployee: (id) => apiRequest(`/api/employees/${id}`),
  createEmployee: (payload) => apiRequest('/api/employees', { method: 'POST', body: payload }),
  updateEmployee: (id, payload) => apiRequest(`/api/employees/${id}`, { method: 'PUT', body: payload }),
  deactivateEmployee: (id) => apiRequest(`/api/employees/${id}`, { method: 'DELETE' }),
  activateEmployee: (id) => apiRequest(`/api/employees/${id}/activate`, { method: 'POST' }),

  // Dashboard & Statistics (Giai đoạn 13, Admin)
  getDashboard: () => apiRequest('/api/dashboard'),
  getRevenueStats: (query = '') => apiRequest(`/api/statistics/revenue${query}`),
  getOrderStats: (query = '') => apiRequest(`/api/statistics/order${query}`),
  getCustomerStats: (query = '') => apiRequest(`/api/statistics/customer${query}`),
  getProductStats: (top, query = '') => apiRequest(`/api/statistics/product?top=${top || 10}${query ? '&' + query.replace(/^\?/, '') : ''}`),
  getInventoryStats: () => apiRequest('/api/statistics/inventory'),
  getPurchaseStats: (query = '') => apiRequest(`/api/statistics/purchases${query}`),
  getInterestStats: (top, query = '') => apiRequest(`/api/statistics/interests?top=${top || 10}${query ? '&' + query.replace(/^\?/, '') : ''}`),
  exportStatistics: (query = '') => apiDownload(`/api/statistics/export${query}`)
};

function requireLogin(redirectTo = 'login.html') {
  if (!Session.isLoggedIn()) {
    window.location.href = redirectTo;
    return false;
  }
  return true;
}

function requireRole(appRoles, redirectTo = 'login.html') {
  if (!Session.isLoggedIn()) {
    window.location.href = redirectTo;
    return false;
  }
  const currentAppRole = roleToAppRole(Session.getRole());
  if (!appRoles.includes(currentAppRole)) {
    alert('Bạn không có quyền truy cập trang này.');
    window.location.href = 'index.html';
    return false;
  }
  return true;
}

function formatCurrencyVnd(amount) {
  const value = Number(amount) || 0;
  return value.toLocaleString('vi-VN') + ' VNĐ';
}

function apiErrorMessage(err) {
  return err && err.message ? err.message : 'Đã xảy ra lỗi không xác định.';
}
