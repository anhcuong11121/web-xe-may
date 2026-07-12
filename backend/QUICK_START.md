# 🚀 Quick Start - Backend Setup

## ⚡ Setup nhanh (5 phút)

### 1️⃣ Điều kiện tiên quyết

```bash
# Kiểm tra Node.js
node --version    # >= 16.0.0
npm --version     # >= 8.0.0

# Cài đặt MySQL (nếu chưa có)
# Windows: https://dev.mysql.com/downloads/mysql/
# macOS: brew install mysql
# Linux: sudo apt install mysql-server
```

### 2️⃣ Cài đặt dependencies

```bash
cd backend
npm install
```

**Output mong đợi:**
```
added 150+ packages in 30s
```

### 3️⃣ Tạo file `.env`

```bash
cp .env.example .env
```

**Chỉnh sửa `.env`:**
```env
PORT=5000
DB_HOST=localhost
DB_USER=root
DB_PASSWORD=          # (để trống nếu MySQL không có password)
DB_NAME=motor_chat
JWT_SECRET=your_secret_key_here
```

### 4️⃣ Tạo Database

**Cách 1: Dùng MySQL CLI**
```bash
mysql -u root -p

# Rồi chạy:
CREATE DATABASE motor_chat;
USE motor_chat;

# (Các table schema sẽ được tạo từ migration sau)
```

**Cách 2: Dùng MySQL Workbench**
- Kết nối tới MySQL
- Tạo schema `motor_chat`
- Done!

### 5️⃣ Chạy Server

```bash
npm run dev
```

**Output mong đợi:**
```
✅ Motor Chát Backend Server running on http://localhost:5000
🔗 Environment: development
🗄️  Database: localhost:3306/motor_chat
```

### 6️⃣ Test API

Mở Postman hoặc Terminal:

```bash
curl http://localhost:5000/api/health
```

**Response:**
```json
{
  "status": "OK",
  "message": "Motor Chát API is running",
  "timestamp": "2026-06-15T10:30:00.000Z"
}
```

---

## 📁 Backend Structure

```
backend/
├── config/          - Database configuration
├── routes/          - API routes (auth, products, orders, etc.)
├── controllers/     - (to be filled) Business logic
├── models/          - (to be filled) Database models
├── middleware/      - Authentication, error handling
├── utils/           - Helper functions
├── server.js        - Express app entry point
├── package.json     - Dependencies
├── .env             - Environment variables (gitignored)
├── .env.example     - Template for .env
└── README.md        - Full documentation
```

---

## 🔌 API Endpoints (Available Now)

Tất cả endpoints sau đã được setup với routing cơ bản:

### Auth
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/logout`

### Products
- `GET /api/products`
- `GET /api/products/:id`
- `POST /api/products` (Staff/Admin)
- `PUT /api/products/:id` (Staff/Admin)
- `DELETE /api/products/:id` (Admin)

### Orders
- `GET /api/orders`
- `GET /api/orders/:id`
- `POST /api/orders` (Customer)
- `PUT /api/orders/:id/status` (Staff/Admin)

### Customers
- `GET /api/customers` (Staff/Admin)
- `GET /api/customers/:id`

### Consultations
- `POST /api/consultations` (Public)
- `GET /api/consultations` (Staff/Admin)

### Statistics & Admin
- `GET /api/stats/dashboard`
- `GET /api/admin/staff`
- `POST /api/admin/staff` (Admin)

---

## ⚙️ Troubleshooting

### ❌ "Cannot find module 'express'"

```bash
npm install
```

### ❌ "Error: connect ECONNREFUSED 127.0.0.1:3306"

- MySQL server chưa chạy
- **Windows**: Mở Services → MySQL → Start
- **macOS**: `brew services start mysql`
- **Linux**: `sudo systemctl start mysql`

### ❌ "ER_BAD_DB_ERROR: Unknown database 'motor_chat'"

```bash
mysql -u root -p
CREATE DATABASE motor_chat;
```

### ❌ Port 5000 đã được sử dụng

```bash
# Thay đổi port trong .env
PORT=5001

# Hoặc kill process cũ
# Windows: netstat -ano | findstr :5000
# Linux/Mac: lsof -ti:5000 | xargs kill -9
```

---

## 📋 Tiếp theo (Sau khi chạy xong)

### Phase 1: Database Schema
- [ ] Tạo các table schema
- [ ] Setup migration system (Sequelize/Knex)
- [ ] Seed test data

### Phase 2: Authentication
- [ ] Viết authController (register, login)
- [ ] Hash password với bcrypt
- [ ] Generate JWT token
- [ ] Implement middleware

### Phase 3: CRUD Operations
- [ ] productController
- [ ] orderController
- [ ] customerController

### Phase 4: Integration
- [ ] Email notification
- [ ] Payment gateway (Stripe/Momo)
- [ ] Error logging

---

## 💡 Tips & Tricks

**Reload server tự động:**
```bash
npm run dev  # Uses nodemon
```

**Xem database:**
```bash
mysql -u root -p motor_chat
SHOW TABLES;
DESC users;
```

**Kiểm tra logs:**
```bash
# Trong server logs, xem query SQL được thực hiện
# Nếu không, tạo file logs/database.log
```

---

## 📚 Tài liệu thêm

- Express docs: https://expressjs.com
- MySQL2 docs: https://github.com/sidorares/node-mysql2
- JWT auth: https://jwt.io
- Postman: https://www.postman.com

---

**✅ Backend API ready to go!** 🚀

Tiếp bước nào? → Chọn Phase nào từ trên để implement tiếp.
