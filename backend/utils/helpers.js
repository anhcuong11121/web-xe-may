import jwt from 'jsonwebtoken';
import bcrypt from 'bcryptjs';

// Generate JWT token
export const generateToken = (userId, role, expiresIn = process.env.JWT_EXPIRE || '7d') => {
  return jwt.sign(
    { userId, role },
    process.env.JWT_SECRET,
    { expiresIn }
  );
};

// Hash password
export const hashPassword = async (password) => {
  const salt = await bcrypt.genSalt(10);
  return bcrypt.hash(password, salt);
};

// Compare password
export const comparePassword = async (password, hashedPassword) => {
  return bcrypt.compare(password, hashedPassword);
};

// Validate email format
export const isValidEmail = (email) => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
};

// Validate phone number (Vietnamese format)
export const isValidPhone = (phone) => {
  const phoneRegex = /^(0|\+84)[1-9]\d{8,9}$/;
  return phoneRegex.test(phone.replace(/\s/g, ''));
};

// Format response
export const successResponse = (data, message = 'Success') => {
  return {
    success: true,
    message,
    data
  };
};

// Create error object
export const createError = (message, code = 'INTERNAL_SERVER_ERROR', status = 500) => {
  const error = new Error(message);
  error.code = code;
  error.status = status;
  return error;
};

// Format pagination params
export const getPaginationParams = (page = 1, limit = 10) => {
  const pageNum = Math.max(1, parseInt(page) || 1);
  const limitNum = Math.min(100, Math.max(1, parseInt(limit) || 10));
  const offset = (pageNum - 1) * limitNum;

  return { page: pageNum, limit: limitNum, offset };
};

// Generate unique ID
export const generateId = () => {
  return `${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
};

// Format date to Vietnamese locale
export const formatDateVN = (date) => {
  return new Date(date).toLocaleDateString('vi-VN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  });
};

// SQL injection prevention - escape dangerous characters
export const escapeSQL = (str) => {
  if (typeof str !== 'string') return str;
  return str.replace(/['\"\\]/g, '\\$&');
};

// Check object fields
export const validateFields = (obj, requiredFields) => {
  const missing = requiredFields.filter(field => !obj[field]);
  if (missing.length > 0) {
    return {
      valid: false,
      missing
    };
  }
  return { valid: true };
};
