import express from 'express';
import { asyncHandler } from '../middleware/errorHandler.js';

const router = express.Router();

// POST /api/auth/register - Register new user
router.post('/register', asyncHandler(async (req, res) => {
  res.status(201).json({ 
    message: 'Register endpoint - to be implemented',
    route: '/api/auth/register'
  });
}));

// POST /api/auth/login - Login user
router.post('/login', asyncHandler(async (req, res) => {
  res.status(200).json({ 
    message: 'Login endpoint - to be implemented',
    route: '/api/auth/login'
  });
}));

// POST /api/auth/logout - Logout user
router.post('/logout', asyncHandler(async (req, res) => {
  res.status(200).json({ 
    message: 'Logout endpoint - to be implemented',
    route: '/api/auth/logout'
  });
}));

// POST /api/auth/refresh-token - Refresh JWT token
router.post('/refresh-token', asyncHandler(async (req, res) => {
  res.status(200).json({ 
    message: 'Refresh token endpoint - to be implemented',
    route: '/api/auth/refresh-token'
  });
}));

export default router;
