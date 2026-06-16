import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import api from '../api';

const S = {
  eyebrow: { display: 'flex', alignItems: 'center', gap: 8, fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.18em', color: '#E91E8C', marginBottom: 6 },
  eyebrowLine: { width: 14, height: 1.5, background: '#E91E8C' },
};

export default function AdminPage() {
  const [dash, setDash] = useState(null);
  const [users, setUsers] = useState([]);
  const [tab, setTab] = useState('overview');
  const [msg, setMsg] = useState('');

  const load = async () => {
    try {
      const [d, u] = await Promise.all([api.get('/admin/dashboard'), api.get('/users')]);
      setDash(d.data); setUsers(u.data);
    } catch {}
  };
  useEffect(() => { load(); }, []);

  const approveDriver = async (id) => {
    try { await api.post(`/admin/drivers/${id}/approve`); setMsg('✅ Driver approved.'); load(); }
    catch (err) { setMsg(err.response?.data?.message || 'Failed'); }
  };

  const deactivateUser = async (id) => {
    if (!confirm('Deactivate this user?')) return;
    try { await api.post(`/admin/users/${id}/deactivate`); setMsg('User deactivated.'); load(); }
    catch (err) { setMsg(err.response?.data?.message || 'Failed'); }
  };

  if (!dash) return <div className="loading">Loading…</div>;

  const tabs = ['overview', 'users', 'pending'];

  return (
    <div style={{ background: '#f8f8f8', minHeight: 'calc(100vh - 60px)' }}>
      <div style={{ maxWidth: 1000, margin: '0 auto', padding: '2.5rem 2rem' }}>

        <div style={{ marginBottom: '2rem' }}>
          <div style={S.eyebrow}><div style={S.eyebrowLine} /> Admin</div>
          <h1 style={{ fontSize: 28, fontWeight: 800, letterSpacing: -1, color: '#0D0D0D' }}>Dashboard</h1>
        </div>

        {msg && (
          <div style={{ background: msg.startsWith('✅') ? '#f0fdf4' : '#fff5f5', border: `1px solid ${msg.startsWith('✅') ? '#bbf7d0' : '#fecaca'}`, borderRadius: 10, padding: '0.75rem 1rem', marginBottom: '1.25rem', fontSize: 13, color: msg.startsWith('✅') ? '#166534' : '#b91c1c' }}>
            {msg}
          </div>
        )}

        {/* Stats */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: '1rem', marginBottom: '1.5rem' }}>
          {[['Users', dash.totalUsers], ['Drivers', dash.totalDrivers], ['Total Rides', dash.totalRides], ['Completed', dash.completedRides]].map(([label, num]) => (
            <div key={label} style={{ background: 'white', borderRadius: 12, padding: '1.25rem', border: '1px solid #efefef' }}>
              <div style={{ fontSize: 32, fontWeight: 800, color: '#E91E8C', letterSpacing: -1.5, lineHeight: 1 }}>{num}</div>
              <div style={{ fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.1em', color: '#888', marginTop: 6 }}>{label}</div>
            </div>
          ))}
        </div>

        {/* Tabs */}
        <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1.25rem', borderBottom: '1px solid #efefef', paddingBottom: '0.75rem' }}>
          {tabs.map(t => (
            <button key={t} onClick={() => setTab(t)} style={{ height: 34, padding: '0 16px', borderRadius: 999, border: 'none', fontSize: 13, fontWeight: 600, cursor: 'pointer', fontFamily: 'Inter, sans-serif', background: tab === t ? '#E91E8C' : '#f0f0f0', color: tab === t ? '#fff' : '#555' }}>
              {t.charAt(0).toUpperCase() + t.slice(1)}
            </button>
          ))}
          <Link to="/admin/analytics" style={{ height: 34, padding: '0 16px', borderRadius: 999, fontSize: 13, fontWeight: 600, textDecoration: 'none', display: 'inline-flex', alignItems: 'center', background: '#f0f0f0', color: '#555' }}>📊 Analytics</Link>
          <Link to="/admin/sos" style={{ height: 34, padding: '0 16px', borderRadius: 999, fontSize: 13, fontWeight: 600, textDecoration: 'none', display: 'inline-flex', alignItems: 'center', background: '#f0f0f0', color: '#555' }}>🚨 SOS</Link>
        </div>

        {tab === 'overview' && (
          <div style={{ background: 'white', borderRadius: 16, border: '1px solid #efefef', padding: '1.5rem' }}>
            <div style={S.eyebrow}><div style={S.eyebrowLine} /> Overview</div>
            <h2 style={{ fontSize: 18, fontWeight: 800, letterSpacing: -0.5, marginBottom: '1rem' }}>Platform summary</h2>
            <table>
              <thead><tr><th>Metric</th><th>Value</th></tr></thead>
              <tbody>
                {[['Total Users', dash.totalUsers], ['Total Drivers', dash.totalDrivers], ['Pending Approvals', dash.pendingDrivers?.length ?? 0], ['Total Rides', dash.totalRides], ['Completed Rides', dash.completedRides]].map(([k, v]) => (
                  <tr key={k}><td>{k}</td><td style={{ fontWeight: 600 }}>{v}</td></tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {tab === 'users' && (
          <div style={{ background: 'white', borderRadius: 16, border: '1px solid #efefef', padding: '1.5rem' }}>
            <div style={S.eyebrow}><div style={S.eyebrowLine} /> Users</div>
            <h2 style={{ fontSize: 18, fontWeight: 800, letterSpacing: -0.5, marginBottom: '1rem' }}>All users</h2>
            <table>
              <thead><tr><th>Name</th><th>Email</th><th>Role</th><th>Phone</th><th>Active</th><th></th></tr></thead>
              <tbody>
                {users.map(u => (
                  <tr key={u.id}>
                    <td style={{ fontWeight: 600 }}>{u.fullName}</td>
                    <td style={{ color: '#888' }}>{u.email}</td>
                    <td><span className="badge badge-blue">{u.role}</span></td>
                    <td style={{ color: '#888' }}>{u.phoneNumber}</td>
                    <td>{u.isActive ? <span className="badge badge-green">Active</span> : <span className="badge badge-red">Inactive</span>}</td>
                    <td>{u.isActive && u.role !== 'Admin' && (
                      <button className="btn btn-danger btn-sm" onClick={() => deactivateUser(u.id)}>Deactivate</button>
                    )}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {tab === 'pending' && (
          <div style={{ background: 'white', borderRadius: 16, border: '1px solid #efefef', padding: '1.5rem' }}>
            <div style={S.eyebrow}><div style={S.eyebrowLine} /> Pending</div>
            <h2 style={{ fontSize: 18, fontWeight: 800, letterSpacing: -0.5, marginBottom: '1rem' }}>Driver approvals</h2>
            {dash.pendingDrivers?.length === 0 ? (
              <p style={{ color: '#888', fontSize: 14 }}>No pending approvals.</p>
            ) : (
              <table>
                <thead><tr><th>Driver ID</th><th>License</th><th></th></tr></thead>
                <tbody>
                  {dash.pendingDrivers?.map(d => (
                    <tr key={d.id}>
                      <td style={{ fontWeight: 600 }}>#{d.id}</td>
                      <td>{d.licenseNumber}</td>
                      <td><button className="btn btn-success btn-sm" onClick={() => approveDriver(d.id)}>Approve</button></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
