import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => { logout(); navigate('/login'); };

  const dashboardPath = user?.role === 'Admin' ? '/admin'
    : user?.role === 'Driver' ? '/driver' : '/passenger';

  if (!user) return null; // landing page has its own nav

  return (
    <nav style={{
      height: 60, display: 'flex', alignItems: 'center',
      padding: '0 32px', background: '#0D0D0D',
      borderBottom: '1px solid rgba(255,255,255,0.06)',
      position: 'sticky', top: 0, zIndex: 100,
    }}>
      <Link to="/" style={{ display: 'flex', alignItems: 'center', gap: 10, textDecoration: 'none' }}>
        <div style={{ width: 22, height: 22, background: '#E91E8C', borderRadius: '50%' }} />
        <span style={{ fontSize: 17, fontWeight: 700, color: '#fff', letterSpacing: '-0.3px' }}>SafeRide</span>
      </Link>

      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginLeft: 'auto' }}>
        <Link to={dashboardPath} style={{ color: 'rgba(255,255,255,0.6)', textDecoration: 'none', fontSize: 13, padding: '6px 12px' }}>
          Dashboard
        </Link>

        {user.role === 'Passenger' && (
          <Link to="/passenger/notifications" style={{ color: 'rgba(255,255,255,0.6)', textDecoration: 'none', fontSize: 13, padding: '6px 12px' }}>
            Notifications
          </Link>
        )}
        {user.role === 'Driver' && (
          <>
            <Link to="/driver/earnings" style={{ color: 'rgba(255,255,255,0.6)', textDecoration: 'none', fontSize: 13, padding: '6px 12px' }}>Earnings</Link>
            <Link to="/driver/notifications" style={{ color: 'rgba(255,255,255,0.6)', textDecoration: 'none', fontSize: 13, padding: '6px 12px' }}>Notifications</Link>
          </>
        )}
        {user.role === 'Admin' && (
          <>
            <Link to="/admin/analytics" style={{ color: 'rgba(255,255,255,0.6)', textDecoration: 'none', fontSize: 13, padding: '6px 12px' }}>Analytics</Link>
            <Link to="/admin/sos" style={{ color: 'rgba(255,255,255,0.6)', textDecoration: 'none', fontSize: 13, padding: '6px 12px' }}>SOS</Link>
          </>
        )}

        <span style={{ fontSize: 13, fontWeight: 600, color: '#E91E8C', padding: '0 8px' }}>{user.fullName}</span>

        <button onClick={handleLogout} style={{
          height: 34, padding: '0 16px', borderRadius: 17, fontSize: 12, fontWeight: 600,
          background: 'transparent', border: '1px solid rgba(255,255,255,0.18)',
          color: '#fff', cursor: 'pointer', fontFamily: 'Inter, sans-serif',
        }}>
          Log out
        </button>
      </div>
    </nav>
  );
}
