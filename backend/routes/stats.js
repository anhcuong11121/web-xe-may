import express from 'express';
import { asyncHandler } from '../middleware/errorHandler.js';
import { verifyToken, isStaffOrAdmin } from '../middleware/auth.js';

const router = express.Router();

// GET /api/stats/dashboard - Get dashboard statistics (Staff/Admin only)
router.get('/dashboard', verifyToken, isStaffOrAdmin, asyncHandler(async (req, res) => {
  res.status(200).json({ 
    message: 'Get dashboard stats - to be implemented',
    route: '/api/stats/dashboard'
  });
}));

// GET /api/stats/revenue - Get revenue statistics (Admin only)
router.get('/revenue', verifyToken, isStaffOrAdmin, asyncHandler(async (req, res) => {
  res.status(200).json({ 
    message: 'Get revenue stats - to be implemented',
    route: '/api/stats/revenue'
  });
}));

// GET /api/stats/orders - Get order statistics (Staff/Admin only)
router.get('/orders', verifyToken, isStaffOrAdmin, asyncHandler(async (req, res) => {
  res.status(200).json({ 
    message: 'Get order stats - to be implemented',
    route: '/api/stats/orders'
  });
}));

// GET /api/stats/customers - Get customer statistics (Admin only)
router.get('/customers', verifyToken, isStaffOrAdmin, asyncHandler(async (req, res) => {
  res.status(200).json({ 
    message: 'Get customer stats - to be implemented',
    route: '/api/stats/customers'
  });
}));

// GET /api/stats/popular-products - Get popular products statistics (Staff/Admin only)
router.get('/popular-products', verifyToken, isStaffOrAdmin, asyncHandler(async (req, res) => {
  res.status(200).json({ 
    message: 'Get popular products - to be implemented',
    route: '/api/stats/popular-products'
  });
}));

export default router;
