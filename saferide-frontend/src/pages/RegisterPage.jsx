import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import api from '../api';
import { useAuth } from '../context/AuthContext';

const inputStyle = {
  width: '100%', padding: '0.75rem 1rem',
  border: '1.5px solid rgba(255,255,255,0.1)', borderRadius: 10,
  fontSize: 14, background: 'rgba(255,255,255,0.05)', color: '#fff',
  fontFamily: 'Inter, sans-serif', outline: 'none',
};
const labelStyle = {
  display: 'block', fontSize: 11, fontWeight: 600,
  textTransform: 'uppercase', letterSpacing: '0.12em',
  color: 'rgba(255,255,255,0.35)', marginBottom: 6,
};

export default function RegisterPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ fullName: '', email: '', password: '', phoneNumber: '', role: 'Passenger' });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const set = (k) => (e) => setForm({ ...form, [k]: e.target.value });

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(''); setLoading(true);
    try {
      const { data } = await api.post('/auth/register', form);
      login({ token: data.token, role: data.role, fullName: data.fullName, userId: data.userId });
      navigate(data.role === 'Driver' ? '/driver' : '/passenger');
    } catch (err) {
      setError(err.response?.data?.message || 'Registration failed.');
    } finally { setLoading(false); }
  };

  const focus = (e) => e.target.style.borderColor = '#E91E8C';
  const blur  = (e) => e.target.style.borderColor = 'rgba(255,255,255,0.1)';

  return (
    <div style={{ minHeight: '100vh', background: '#0D0D0D', display: 'flex', flexDirection: 'column' }}>
      <div style={{ height: 60, display: 'flex', alignItems: 'center', padding: '0 32px', borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
        <Link to="/" style={{ display: 'flex', alignItems: 'center', gap: 10, textDecoration: 'none' }}>
          <div style={{ width: 20, height: 20, background: '#E91E8C', borderRadius: '50%' }} />
          <span style={{ fontSize: 16, fontWeight: 700, color: '#fff' }}>SafeRide</span>
        </Link>
      </div>

      <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '2rem' }}>
        <div style={{ width: '100%', maxWidth: 460 }}>
          <div style={{ display: 'inline-flex', alignItems: 'center', gap: 8, fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.18em', color: '#E91E8C', marginBottom: 20 }}>
            <div style={{ width: 14, height: 1.5, background: '#E91E8C' }} /> Join today
          </div>

          <h1 style={{ fontSize: 36, fontWeight: 800, color: '#fff', letterSpacing: -1.5, lineHeight: 1.05, marginBottom: 8 }}>
            Create your<br />SafeRide account
          </h1>
          <p style={{ fontSize: 14, color: 'rgba(255,255,255,0.4)', marginBottom: 32, fontWeight: 300 }}>
            It takes under a minute.
          </p>

          {error && (
            <div style={{ background: 'rgba(233,30,140,0.1)', border: '1px solid rgba(233,30,140,0.3)', borderRadius: 10, padding: '0.75rem 1rem', marginBottom: '1.25rem', color: '#F8BBD9', fontSize: 13 }}>
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1rem' }}>
              <div>
                <label style={labelStyle}>Full Name</label>
                <input required value={form.fullName} onChange={set('fullName')} placeholder="Arta Emurli" style={inputStyle} onFocus={focus} onBlur={blur} />
              </div>
              <div>
                <label style={labelStyle}>Phone</label>
                <input required value={form.phoneNumber} onChange={set('phoneNumber')} placeholder="+389 70 123 456" style={inputStyle} onFocus={focus} onBlur={blur} />
              </div>
            </div>

            <div style={{ marginBottom: '1rem' }}>
              <label style={labelStyle}>Email</label>
              <input type="email" required value={form.email} onChange={set('email')} placeholder="you@example.com" style={inputStyle} onFocus={focus} onBlur={blur} />
            </div>

            <div style={{ marginBottom: '1rem' }}>
              <label style={labelStyle}>Password</label>
              <input type="password" required value={form.password} onChange={set('password')} placeholder="Min 8 characters" style={inputStyle} onFocus={focus} onBlur={blur} />
            </div>

            {/* Role selector */}
            <div style={{ marginBottom: '1.5rem' }}>
              <label style={labelStyle}>I am a…</label>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
                {['Passenger', 'Driver'].map(r => (
                  <button
                    key={r} type="button"
                    onClick={() => setForm({ ...form, role: r })}
                    style={{
                      padding: '0.75rem', borderRadius: 10, border: `1.5px solid ${form.role === r ? '#E91E8C' : 'rgba(255,255,255,0.1)'}`,
                      background: form.role === r ? 'rgba(233,30,140,0.1)' : 'rgba(255,255,255,0.03)',
                      color: form.role === r ? '#E91E8C' : 'rgba(255,255,255,0.5)',
                      fontWeight: 600, fontSize: 14, cursor: 'pointer', transition: 'all 0.15s',
                      fontFamily: 'Inter, sans-serif',
                    }}
                  >
                    {r === 'Passenger' ? '🧳 Passenger' : '🚗 Driver'}
                  </button>
                ))}
              </div>
            </div>

            <button
              type="submit" disabled={loading}
              style={{ width: '100%', height: 48, background: '#E91E8C', color: '#fff', border: 'none', borderRadius: 999, fontSize: 14, fontWeight: 600, cursor: loading ? 'not-allowed' : 'pointer', opacity: loading ? 0.7 : 1, fontFamily: 'Inter, sans-serif' }}
            >
              {loading ? 'Creating account…' : 'Create Account'}
            </button>
          </form>

          <p style={{ textAlign: 'center', marginTop: '1.25rem', fontSize: 13, color: 'rgba(255,255,255,0.35)' }}>
            Already have an account?{' '}
            <Link to="/login" style={{ color: '#E91E8C', fontWeight: 600, textDecoration: 'none' }}>Sign in</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
