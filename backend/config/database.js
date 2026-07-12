import mysql from 'mysql2/promise';
import dotenv from 'dotenv';

dotenv.config();

// Create connection pool for better performance
const pool = mysql.createPool({
  host: process.env.DB_HOST || 'localhost',
  port: process.env.DB_PORT || 3306,
  user: process.env.DB_USER || 'root',
  password: process.env.DB_PASSWORD || '',
  database: process.env.DB_NAME || 'motor_chat',
  waitForConnections: true,
  connectionLimit: 10,
  queueLimit: 0,
  enableKeepAlive: true,
  keepAliveInitialDelayMs: 0
});

// Test connection
pool.getConnection()
  .then(conn => {
    console.log('✅ Database connected successfully');
    conn.release();
  })
  .catch(err => {
    console.error('❌ Database connection failed:', err.message);
  });

// Export both pool and individual connection function
export const getConnection = async () => {
  return pool.getConnection();
};

export const query = async (sql, values = []) => {
  try {
    const connection = await pool.getConnection();
    const [results] = await connection.query(sql, values);
    connection.release();
    return results;
  } catch (error) {
    console.error('Database Query Error:', error);
    throw error;
  }
};

export default pool;
