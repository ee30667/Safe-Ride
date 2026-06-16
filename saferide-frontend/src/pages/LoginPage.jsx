import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import api from '../api';
import { useAuth } from '../context/AuthContext';

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ email: '', password: '' });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(''); setLoading(true);
    try {
      const { data } = await api.post('/auth/login', form);
      login({ token: data.token, role: data.role, fullName: data.fullName, userId: data.userId });
      navigate(data.role === 'Admin' ? '/admin' : data.role === 'Driver' ? '/driver' : '/passenger');
    } catch (err) {
      setError(err.response?.data?.message || 'Invalid email or password.');
    } finally { setLoading(false); }
  };

  return (
    <div style={{ minHeight: '100vh', background: '#0D0D0D', display: 'flex', flexDirection: 'column' }}>
      {/* Minimal nav */}
      <div style={{ height: 60, display: 'flex', alignItems: 'center', padding: '0 32px', borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
        <Link to="/" style={{ display: 'flex', alignItems: 'center', gap: 10, textDecoration: 'none' }}>
          <div style={{ width: 20, height: 20, background: '#E91E8C', borderRadius: '50%' }} />
          <span style={{ fontSize: 16, fontWeight: 700, color: '#fff' }}>SafeRide</span>
        </Link>
      </div>

      {/* Form area */}
      <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '2rem' }}>
        <div style={{ width: '100%', maxWidth: 400 }}>
          {/* Eyebrow */}
          <div style={{ display: 'inline-flex', alignItems: 'center', gap: 8, fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.18em', color: '#E91E8C', marginBottom: 20 }}>
            <div style={{ width: 14, height: 1.5, background: '#E91E8C' }} /> Welcome back
          </div>

          <h1 style={{ fontSize: 36, fontWeight: 800, color: '#fff', letterSpacing: -1.5, lineHeight: 1.05, marginBottom: 8 }}>
            Sign in to<br />SafeRide
          </h1>
          <p style={{ fontSize: 14, color: 'rgba(255,255,255,0.4)', marginBottom: 32, fontWeight: 300 }}>
            Enter your credentials to continue.
          </p>

          {error && (
            <div style={{ background: 'rgba(233,30,140,0.1)', border: '1px solid rgba(233,30,140,0.3)', borderRadius: 10, padding: '0.75rem 1rem', marginBottom: '1.25rem', color: '#F8BBD9', fontSize: 13 }}>
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit}>
            <div style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.12em', color: 'rgba(255,255,255,0.35)', marginBottom: 6 }}>Email</label>
              <input
                type="email" required value={form.email}
                onChange={e => setForm({ ...form, email: e.target.value })}
                placeholder="you@example.com"
                style={{ width: '100%', padding: '0.75rem 1rem', border: '1.5px solid rgba(255,255,255,0.1)', borderRadius: 10, fontSize: 14, background: 'rgba(255,255,255,0.05)', color: '#fff', fontFamily: 'Inter, sans-serif', outline: 'none' }}
                onFocus={e => e.target.style.borderColor = '#E91E8C'}
                onBlur={e => e.target.style.borderColor = 'rgba(255,255,255,0.1)'}
              />
            </div>

            <div style={{ marginBottom: '1.5rem' }}>
              <label style={{ display: 'block', fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.12em', color: 'rgba(255,255,255,0.35)', marginBottom: 6 }}>Password</label>
              <input
                type="password" required value={form.password}
                onChange={e => setForm({ ...form, password: e.target.value })}
                placeholder="••••••••"
                style={{ width: '100%', padding: '0.75rem 1rem', border: '1.5px solid rgba(255,255,255,0.1)', borderRadius: 10, fontSize: 14, background: 'rgba(255,255,255,0.05)', color: '#fff', fontFamily: 'Inter, sans-serif', outline: 'none' }}
                onFocus={e => e.target.style.borderColor = '#E91E8C'}
                onBlur={e => e.target.style.borderColor = 'rgba(255,255,255,0.1)'}
              />
            </div>

            <button
              type="submit" disabled={loading}
              style={{ width: '100%', height: 48, background: '#E91E8C', color: '#fff', border: 'none', borderRadius: 999, fontSize: 14, fontWeight: 600, cursor: loading ? 'not-allowed' : 'pointer', opacity: loading ? 0.7 : 1, fontFamily: 'Inter, sans-serif' }}
            >
              {loading ? 'Signing in…' : 'Sign In'}
            </button>
          </form>

          <p style={{ textAlign: 'center', marginTop: '1.25rem', fontSize: 13, color: 'rgba(255,255,255,0.35)' }}>
            No account?{' '}
            <Link to="/register" style={{ color: '#E91E8C', fontWeight: 600, textDecoration: 'none' }}>
              Create one
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
