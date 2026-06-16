import { createContext, useContext, useState, useEffect } from 'react';
import api from '../api';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    const stored = localStorage.getItem('saferide_user');
    return stored ? JSON.parse(stored) : null;
  });

  useEffect(() => {
    if (user) {
      localStorage.setItem('saferide_user', JSON.stringify(user));
      api.defaults.headers.common['Authorization'] = `Bearer ${user.token}`;
    } else {
      localStorage.removeItem('saferide_user');
      delete api.defaults.headers.common['Authorization'];
    }
  }, [user]);

  const login = (userData) => setUser(userData);
  const logout = () => setUser(null);

  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => useContext(AuthContext);
