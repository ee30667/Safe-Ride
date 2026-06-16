import axios from 'axios';

const api = axios.create({
  baseURL: `${import.meta.env.VITE_API_URL}/api`,
  headers: { 'Content-Type': 'application/json' },
});

const stored = localStorage.getItem('saferide_user');
if (stored) {
  const { token } = JSON.parse(stored);
  api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
}

export default api;