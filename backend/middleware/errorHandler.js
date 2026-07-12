// Global error handler middleware
export const errorHandler = (err, req, res, next) => {
  console.error('Error:', err);

  // Validation error
  if (err.code === 'VALIDATION_ERROR') {
    return res.status(400).json({
      error: 'Validation failed',
      code: 'VALIDATION_ERROR',
      details: err.details
    });
  }

  // Database error
  if (err.code === 'ER_DUP_ENTRY') {
    return res.status(409).json({
      error: 'Record already exists',
      code: 'DUPLICATE_ENTRY'
    });
  }

  if (err.code === 'ER_NO_REFERENCED_ROW') {
    return res.status(404).json({
      error: 'Referenced record not found',
      code: 'NOT_FOUND'
    });
  }

  // Not found error
  if (err.status === 404 || err.code === 'NOT_FOUND') {
    return res.status(404).json({
      error: err.message || 'Resource not found',
      code: 'NOT_FOUND'
    });
  }

  // Unauthorized error
  if (err.status === 401 || err.code === 'UNAUTHORIZED') {
    return res.status(401).json({
      error: err.message || 'Unauthorized',
      code: 'UNAUTHORIZED'
    });
  }

  // Forbidden error
  if (err.status === 403 || err.code === 'FORBIDDEN') {
    return res.status(403).json({
      error: err.message || 'Forbidden',
      code: 'FORBIDDEN'
    });
  }

  // Default error response
  const statusCode = err.status || 500;
  const message = err.message || 'Internal Server Error';

  res.status(statusCode).json({
    error: message,
    code: err.code || 'INTERNAL_SERVER_ERROR',
    ...(process.env.NODE_ENV === 'development' && { stack: err.stack })
  });
};

// Async error wrapper - wrap async route handlers
export const asyncHandler = (fn) => (req, res, next) => {
  Promise.resolve(fn(req, res, next)).catch(next);
};
