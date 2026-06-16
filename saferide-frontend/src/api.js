import axios from 'axios';

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

// Restore token on page refresh
const stored = localStorage.getItem('saferide_user');
if (stored) {
  const { token } = JSON.parse(stored);
  api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
}

export default api;
