# 🔐 HƯỚNG DẪN TEST JWT AUTHENTICATION

## Mục đích
Hướng dẫn test các chức năng JWT Authentication:
- ✅ Đăng nhập và nhận JWT token
- ✅ Sử dụng JWT token để truy cập API protected
- ✅ Kiểm tra phân quyền (Authorization) dựa trên Role (Admin/Employee/Customer)

---

## 📋 QUI TRÌNH HOẠT ĐỘNG

```
1. CLIENT GỬI THÔNG TIN ĐĂNG NHẬP
   ↓
2. SERVER KIỂM TRA USERNAME/PASSWORD
   ↓
3. NẾU HỢP LỆ → TẠO JWT TOKEN → GỬI VỀ CLIENT
   ↓
4. CLIENT GỬI JWT TRONG HEADER: Authorization: Bearer <token>
   ↓
5. SERVER KIỂM TRA JWT VÀ ROLE
   ↓
6. NẾU HỢP LỆ → TRUY CẬP API, NGƯỢC LẠI → 401/403
```

---

## 🚀 BƯỚC 1: CHẠY SERVER

```bash
cd "C:\Users\LENOVO\Desktop\web xe may - Copy\"
dotnet run --project MotorBikeShop.API.csproj
```

Server sẽ chạy tại: `https://localhost:7xxx` hoặc `http://localhost:5xxx`

---

## 🧪 BƯỚC 2: TEST VỚI SWAGGER (Dễ nhất)

### 2.1 Mở Swagger UI
Truy cập: **https://localhost:PORT/swagger** (hoặc http://localhost:PORT/swagger)

### 2.2 Tạo tài khoản ADMIN
```
POST /api/auth/register

Body (JSON):
{
  "email": "admin@test.com",
  "fullName": "Admin User",
  "password": "Admin@123",
  "role": "Admin"
}

Response (Nếu thành công):
{
  "message": "User registered successfully"
}
```

### 2.3 Tạo tài khoản CUSTOMER
```
POST /api/auth/register

Body (JSON):
{
  "email": "user@test.com",
  "fullName": "Normal User",
  "password": "User@123",
  "role": "Customer"
}
```

### 2.4 Đăng nhập với Admin
```
POST /api/auth/login

Body (JSON):
{
  "email": "admin@test.com",
  "password": "Admin@123"
}

Response:
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "admin@test.com",
  "fullName": "Admin User",
  "role": "Admin"
}
```

### 2.5 Copy JWT Token
- Lấy giá trị từ field `token` 
- Ví dụ: `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`

### 2.6 Thêm JWT vào Swagger
1. Tìm nút **"Authorize"** ở phía trên phải của Swagger
2. Click vào **"Authorize"**
3. Paste JWT token vào trường `Value`: `Bearer <token>`
4. Click **"Authorize"**

```
Ví dụ: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## ✅ BƯỚC 3: TEST CÁC ENDPOINT

### 📍 Endpoint Công Khai (Không cần JWT)

```
GET /api/test/public

Response:
{
  "message": "✅ Đây là endpoint công khai - không cần JWT",
  "timestamp": "2024-01-15T10:30:45.123Z"
}
```

---

### 🔒 Endpoint Protected (Cần JWT bất kỳ User nào)

```
GET /api/test/protected

Headers:
Authorization: Bearer <jwt_token>

Response:
{
  "message": "✅ Endpoint được bảo vệ - cần JWT hợp lệ",
  "userEmail": "admin@test.com",
  "userRole": "Admin",
  "timestamp": "2024-01-15T10:30:45.123Z"
}
```

---

### 👑 Endpoint Chỉ Admin (Role: Admin)

```
GET /api/test/admin-only

Headers:
Authorization: Bearer <admin_jwt_token>

Response (Nếu Admin):
{
  "message": "✅ Endpoint Admin - chỉ Admin truy cập được",
  "userEmail": "admin@test.com",
  "userRole": "Admin",
  "adminFeatures": [
	"Quản lý người dùng",
	"Xem báo cáo",
	"Xóa dữ liệu"
  ],
  "timestamp": "2024-01-15T10:30:45.123Z"
}

Response (Nếu dùng token Customer):
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "Forbidden"
}
```

---

### 👔 Endpoint Staff (Role: Admin hoặc Employee)

```
GET /api/test/staff-only

Headers:
Authorization: Bearer <admin_hoặc_employee_jwt_token>

Response (Nếu Admin/Employee):
{
  "message": "✅ Endpoint Staff - chỉ Admin và Employee truy cập được",
  "userEmail": "admin@test.com",
  "userRole": "Admin",
  "timestamp": "2024-01-15T10:30:45.123Z"
}

Response (Nếu Customer):
{
  "status": 403,
  "title": "Forbidden"
}
```

---

### 👤 Endpoint User Profile (Tất cả Authenticated User)

```
GET /api/test/user-profile

Headers:
Authorization: Bearer <any_valid_jwt_token>

Response:
{
  "message": "✅ Lấy profile người dùng thành công",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "admin@test.com",
  "role": "Admin",
  "timestamp": "2024-01-15T10:30:45.123Z"
}
```

---

## 📊 TEST CASE TABLE

| Endpoint | Require JWT | Required Role | Admin Token | Customer Token | Result |
|----------|------------|--------------|------------|----------------|--------|
| /api/test/public | ❌ No | - | ✅ 200 | ✅ 200 | Success |
| /api/test/protected | ✅ Yes | Any | ✅ 200 | ✅ 200 | Success |
| /api/test/admin-only | ✅ Yes | Admin | ✅ 200 | ❌ 403 | Role Check |
| /api/test/staff-only | ✅ Yes | Admin, Employee | ✅ 200 | ❌ 403 | Role Check |
| /api/test/user-profile | ✅ Yes | Any | ✅ 200 | ✅ 200 | Success |

---

## 🔧 TEST VỚI POSTMAN

### Setup Postman Environment

1. **Tạo request POST Đăng nhập**
   ```
   URL: http://localhost:5xxx/api/auth/login
   Method: POST
   Body (raw JSON):
   {
	 "email": "admin@test.com",
	 "password": "Admin@123"
   }
   ```

2. **Lưu token vào Postman Variable**
   - Vào tab "Tests"
   - Paste code:
   ```javascript
   var jsonData = pm.response.json();
   pm.environment.set("jwt_token", jsonData.token);
   ```

3. **Test Endpoint Protected**
   ```
   URL: http://localhost:5xxx/api/test/protected
   Method: GET
   Headers:
   Authorization: Bearer {{jwt_token}}
   ```

---

## 🎥 HƯỚNG DẪN TẠO VIDEO THUYẾT MINH

### Nội dung Video (8-10 phút)

**1. Giới thiệu (1 phút)**
- JWT là gì?
- Tại sao cần JWT?
- Lợi ích của JWT Authentication

**2. Kiến trúc hệ thống (1.5 phút)**
- Vẽ sơ đồ: Client → Đăng nhập → Server → JWT → Client → Request với JWT
- Giải thích: JWT token, Claims, Role

**3. Demo Code (2 phút)**
- Mở Visual Studio
- Chỉ file `AuthController.cs` → Endpoint Login
- Chỉ file `AuthService.cs` → Method GenerateJwtToken
- Chỉ file `TestController.cs` → Endpoint Admin-only

**4. Demo Test (3-4 phút)**
- Mở Swagger UI
- Register Admin account
- Register Customer account
- Login → Copy token
- Test /api/test/public (không cần JWT)
- Test /api/test/protected (cần JWT)
- Test /api/test/admin-only (Admin JWT → Success, Customer JWT → 403)
- Test /api/test/staff-only (Role check)

**5. Kết luận (0.5 phút)**
- Tóm tắt quy trình
- Ứng dụng thực tế

### Tools để quay video:
- **OBS Studio** (miễn phí)
- **ScreenFlow** (Mac)
- **ShareX** (miễn phí, Windows)
- **Camtasia** (có trả phí)

---

## 📝 SAI SỐ THƯỜNG GẶP

### ❌ Error: 401 Unauthorized
**Nguyên nhân**: JWT token không hợp lệ, hết hạn, hoặc không được gửi
**Giải pháp**:
- Kiểm tra token có bao gồm `Bearer ` không
- Đăng nhập lại để lấy token mới
- Kiểm tra format header: `Authorization: Bearer <token>`

### ❌ Error: 403 Forbidden
**Nguyên nhân**: User không có role cần thiết
**Giải pháp**:
- Tạo tài khoản với role đúng (Admin, Employee, etc.)
- Kiểm tra Role trong JWT token

### ❌ Error: 400 Bad Request
**Nguyên nhân**: Dữ liệu gửi không hợp lệ (email, password format)
**Giải pháp**:
- Email phải có format đúng: `user@example.com`
- Password tối thiểu 6 ký tự, phải có chữ hoa, chữ thường, số

---

## 🔗 GITHUB LINK

```
Repository: https://github.com/anhcuong11121/web-xe-may
Branch: main
```

---

## 📚 TÀI LIỆU THAM KHẢO

- **JWT.io**: https://jwt.io/
- **ASP.NET Core Auth**: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10
- **Bearer Token**: https://tools.ietf.org/html/rfc6750

---

## ✨ KẾT QUẢ MONG ĐỢI

Sau khi test hoàn thành, bạn sẽ hiểu:
✅ JWT token hoạt động như thế nào
✅ Cách gửi JWT trong HTTP Header
✅ Cách server xác thực JWT
✅ Cách phân quyền dựa trên Role
✅ Cách xử lý 401/403 errors

---

**Chúc bạn test thành công! 🎉**
