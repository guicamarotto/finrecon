import { Layout, Menu, Button, Typography } from 'antd';
import { UploadOutlined, UnorderedListOutlined, LogoutOutlined } from '@ant-design/icons';
import { useNavigate, useLocation, Outlet } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';

const { Header, Sider, Content } = Layout;
const { Text } = Typography;

export function AppLayout() {
  const navigate = useNavigate();
  const location = useLocation();
  const { email, logout } = useAuth();

  const menuItems = [
    { key: '/upload', icon: <UploadOutlined />, label: 'Upload File' },
    { key: '/', icon: <UnorderedListOutlined />, label: 'Reconciliations' },
  ];

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider breakpoint="lg" collapsedWidth="0">
        <div style={{ padding: '16px', color: 'white', fontWeight: 700, fontSize: 18 }}>
          FinRecon
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[location.pathname]}
          items={menuItems}
          onClick={({ key }) => navigate(key)}
        />
      </Sider>
      <Layout>
        <Header style={{ background: '#fff', padding: '0 24px', display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: 12 }}>
          <Text type="secondary">{email}</Text>
          <Button icon={<LogoutOutlined />} onClick={handleLogout} type="text">
            Sign Out
          </Button>
        </Header>
        <Content style={{ margin: 24 }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}
