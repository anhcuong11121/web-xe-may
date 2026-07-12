# 🏍️ Motor Chát Backend API

RESTful API cho website quản lý bán xe máy Motor Chát

## 📋 Mục lục

- [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
- [Cài đặt](#cài-đặt)
- [Cấu hình](#cấu-hình)
- [Chạy ứng dụng](#chạy-ứng-dụng)
- [Cấu trúc dự án](#cấu-trúc-dự-án)
- [API Endpoints](#api-endpoints)
- [Database Schema](#database-schema)

---

## 🔧 Yêu cầu hệ thống

- **Node.js** >= 16.0.0
- **npm** >= 8.0.0 hoặc **yarn** >= 3.0.0
- **MySQL** >= 5.7 (hoặc MariaDB)
- **Postman** (để test API - optional)

---

## 📥 Cài đặt

### 1. Clone repository hoặc download project

```bash
cd backend
```

### 2. Cài đặt dependencies

```bash
npm install
```

### 3. Tạo file `.env` từ template

```bash
cp .env.example .env
```

### 4. Cấu hình file `.env`

Mở file `.env` và cập nhật các giá trị:

```env
NODE_ENV=development
PORT=5000
HOST=localhost

# Database
DB_HOST=localhost
DB_PORT=3306
DB_USER=root
DB_PASSWORD=your_password
DB_NAME=motor_chat

# JWT
JWT_SECRET=your_super_secret_key_here
JWT_EXPIRE=7d

# Email
EMAIL_USER=your_email@gmail.com
EMAIL_PASSWORD=your_app_password

# Frontend
FRONTEND_URL=http://localhost:3000
```

### 5. Tạo Database MySQL

```bash
# Mở MySQL CLI
mysql -u root -p

# Tạo database
CREATE DATABASE motor_chat;
USE motor_chat;

# Import schema (chạy script SQL)
# source path/to/schema.sql
```

---

## 🚀 Chạy ứng dụng

### Development (với auto-reload)

```bash
npm run dev
```

Output:
```
✅ Motor Chát Backend Server running on http://localhost:5000
🔗 Environment: development
🗄️  Database: localhost:3306/motor_chat
```

### Production

```bash
npm start
```

---

## 📁 Cấu trúc dự án

```
backend/
├── config/
│   └── database.js           # Database connection setup
├── routes/
│   ├── auth.js              # Authentication routes
│   ├── products.js          # Product CRUD routes
│   ├── orders.js            # Order management routes
│   ├── customers.js         # Customer management routes
│   ├── consultations.js     # Consultation request routes
│   ├── stats.js             # Statistics routes
│   └── admin.js             # Admin management routes
├── controllers/             # Route handlers (to be implemented)
│   └── (individual controllers for each module)
├── models/                  # Database models
│   └── (ORM models for tables)
├── middleware/
│   ├── auth.js             # JWT verification & role checks
│   └── errorHandler.js     # Global error handling
├── utils/
│   ├── helpers.js          # Utility functions
│   ├── email.js            # Email sending (to be implemented)
│   └── logger.js           # Logging utility (to be implemented)
├── .env.example            # Environment variables template
├── .gitignore              # Git ignore file
├── package.json            # Dependencies & scripts
├── server.js               # Express server entry point
└── README.md               # This file
```

---

## 🔌 API Endpoints

### Authentication (`/api/auth`)

```
POST   /api/auth/register          # Đăng ký tài khoản mới
POST   /api/auth/login             # Đăng nhập
POST   /api/auth/logout            # Đăng xuất
POST   /api/auth/refresh-token     # Làm mới token JWT
```

### Products (`/api/products`)

```
GET    /api/products               # Lấy danh sách xe
GET    /api/products/:id           # Lấy chi tiết xe
POST   /api/products               # Tạo xe mới (Staff/Admin)
PUT    /api/products/:id           # Cập nhật xe (Staff/Admin)
DELETE /api/products/:id           # Xóa xe (Admin)
GET    /api/products/search/:keyword # Tìm kiếm xe
```

### Orders (`/api/orders`)

```
GET    /api/orders                 # Lấy danh sách đơn hàng
GET    /api/orders/:id             # Lấy chi tiết đơn hàng
POST   /api/orders                 # Tạo đơn hàng mới (Customer)
PUT    /api/orders/:id             # Cập nhật đơn hàng
PUT    /api/orders/:id/status      # Cập nhật trạng thái (Staff/Admin)
DELETE /api/orders/:id             # Hủy đơn hàng
POST   /api/orders/:id/deposit     # Thanh toán cọc
```

### Customers (`/api/customers`)

```
GET    /api/customers              # Lấy danh sách khách hàng (Staff/Admin)
GET    /api/customers/:id          # Lấy chi tiết khách hàng
GET    /api/customers/:id/orders   # Lấy đơn hàng của khách hàng
PUT    /api/customers/:id          # Cập nhật khách hàng
POST   /api/customers/:id/block    # Khóa tài khoản
POST   /api/customers/:id/unblock  # Mở khóa tài khoản
DELETE /api/customers/:id          # Xóa khách hàng
```

### Consultations (`/api/consultations`)

```
GET    /api/consultations          # Lấy danh sách yêu cầu tư vấn
GET    /api/consultations/:id      # Lấy chi tiết yêu cầu
POST   /api/consultations          # Tạo yêu cầu tư vấn
PUT    /api/consultations/:id/status # Cập nhật trạng thái
DELETE /api/consultations/:id      # Xóa yêu cầu
```

### Statistics (`/api/stats`)

```
GET    /api/stats/dashboard        # Thống kê dashboard
GET    /api/stats/revenue          # Thống kê doanh thu
GET    /api/stats/orders           # Thống kê đơn hàng
GET    /api/stats/customers        # Thống kê khách hàng
GET    /api/stats/popular-products # Thống kê sản phẩm bán chạy
```

### Admin (`/api/admin`)

```
GET    /api/admin/staff            # Danh sách nhân viên
POST   /api/admin/staff            # Tạo nhân viên
PUT    /api/admin/staff/:id        # Cập nhật nhân viên
DELETE /api/admin/staff/:id        # Xóa nhân viên
GET    /api/admin/accounts         # Danh sách tài khoản
POST   /api/admin/accounts/:id/lock        # Khóa tài khoản
POST   /api/admin/accounts/:id/unlock      # Mở khóa tài khoản
GET    /api/admin/activity-logs    # Lấy activity logs
GET    /api/admin/system-info      # Thông tin hệ thống
```

---

## 🗄️ Database Schema

### Bảng Users

```sql
CREATE TABLE users (
  id INT PRIMARY KEY AUTO_INCREMENT,
  username VARCHAR(50) UNIQUE NOT NULL,
  email VARCHAR(100) UNIQUE NOT NULL,
  phone VARCHAR(20),
  password_hash VARCHAR(255) NOT NULL,
  role ENUM('customer', 'staff', 'admin') DEFAULT 'customer',
  status ENUM('active', 'inactive', 'blocked') DEFAULT 'active',
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

### Bảng Products

```sql
CREATE TABLE products (
  id INT PRIMARY KEY AUTO_INCREMENT,
  title VARCHAR(255) NOT NULL,
  description TEXT,
  price DECIMAL(12, 2) NOT NULL,
  brand VARCHAR(100),
  year INT,
  colors JSON,
  status ENUM('available', 'sold', 'discontinued') DEFAULT 'available',
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

### Bảng Orders

```sql
CREATE TABLE orders (
  id INT PRIMARY KEY AUTO_INCREMENT,
  customer_id INT NOT NULL,
  product_id INT NOT NULL,
  quantity INT DEFAULT 1,
  price DECIMAL(12, 2) NOT NULL,
  deposit_amount DECIMAL(12, 2),
  status ENUM('pending', 'confirmed', 'completed', 'cancelled') DEFAULT 'pending',
  delivery_address TEXT,
  notes TEXT,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (customer_id) REFERENCES users(id),
  FOREIGN KEY (product_id) REFERENCES products(id)
);
```

### Bảng Consultations

```sql
CREATE TABLE consultations (
  id INT PRIMARY KEY AUTO_INCREMENT,
  customer_name VARCHAR(100),
  customer_email VARCHAR(100),
  customer_phone VARCHAR(20),
  subject VARCHAR(255),
  message TEXT,
  status ENUM('new', 'assigned', 'in_progress', 'completed') DEFAULT 'new',
  assigned_staff_id INT,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (assigned_staff_id) REFERENCES users(id)
);
```

---

## 🧪 Testing API

### Sử dụng Postman

1. Mở Postman
2. Tạo collection "Motor Chát API"
3. Import các request từ file `postman-collection.json` (sẽ được tạo)
4. Set environment variables:
   - `BASE_URL`: http://localhost:5000
   - `TOKEN`: (sẽ được lấy từ login response)

### Sử dụng cURL

```bash
# Register
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "password123",
    "phone": "0987654321"
  }'

# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "password123"
  }'
```

---

## 📝 Ghi chú

- Tất cả endpoints đã được setup nhưng chỉ trả về message "to be implemented"
- Controllers cần được triển khai trong các file tương ứng
- Database models cần được tạo (có thể dùng ORM như Sequelize hoặc Prisma)
- Email notification chưa được cấu hình
- Payment gateway (Stripe/Momo) chưa được tích hợp

---

## 🚀 Bước tiếp theo

1. ✅ Setup backend structure (đã xong)
2. ⏳ Viết Database schema & migration
3. ⏳ Tạo controllers cho từng module
4. ⏳ Implement authentication logic
5. ⏳ Tích hợp thanh toán
6. ⏳ Setup email notification
7. ⏳ Viết test cases
8. ⏳ Deploy lên production

---

**Created:** 2026-06-15  
**Version:** 1.0.0  
**Contact:** Motor Chát Team
