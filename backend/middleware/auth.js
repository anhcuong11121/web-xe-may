import jwt from 'jsonwebtoken';

// Verify JWT token
export const verifyToken = (req, res, next) => {
  try {
    const token = req.headers.authorization?.split(' ')[1];

    if (!token) {
      return res.status(401).json({ 
        error: 'Access token required',
        code: 'NO_TOKEN'
      });
    }

    const decoded = jwt.verify(token, process.env.JWT_SECRET);
    req.user = decoded;
    next();
  } catch (error) {
    if (error.name === 'TokenExpiredError') {
      return res.status(401).json({ 
        error: 'Token expired',
        code: 'TOKEN_EXPIRED'
      });
    }
    return res.status(401).json({ 
      error: 'Invalid token',
      code: 'INVALID_TOKEN'
    });
  }
};

// Check if user is customer
export const isCustomer = (req, res, next) => {
  if (req.user?.role !== 'customer') {
    return res.status(403).json({ 
      error: 'Access forbidden - Customer role required',
      code: 'FORBIDDEN'
    });
  }
  next();
};

// Check if user is staff
export const isStaff = (req, res, next) => {
  if (req.user?.role !== 'staff') {
    return res.status(403).json({ 
      error: 'Access forbidden - Staff role required',
      code: 'FORBIDDEN'
    });
  }
  next();
};

// Check if user is admin
export const isAdmin = (req, res, next) => {
  if (req.user?.role !== 'admin') {
    return res.status(403).json({ 
      error: 'Access forbidden - Admin role required',
      code: 'FORBIDDEN'
    });
  }
  next();
};

// Check if user is staff or admin
export const isStaffOrAdmin = (req, res, next) => {
  if (!['staff', 'admin'].includes(req.user?.role)) {
    return res.status(403).json({ 
      error: 'Access forbidden - Staff/Admin role required',
      code: 'FORBIDDEN'
    });
  }
  next();
};

// Optional token verification (doesn't fail if no token)
export const optionalAuth = (req, res, next) => {
  try {
    const token = req.headers.authorization?.split(' ')[1];
    if (token) {
      const decoded = jwt.verify(token, process.env.JWT_SECRET);
      req.user = decoded;
    }
  } catch (error) {
    // Silently fail for optional auth
  }
  next();
};
