import React, { useState } from 'react';
import { Form, Input, Button, Card, message } from 'antd';
import { UserOutlined, LockOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { authService } from '@/api/services';
import type { LoginRequest } from '@/types/api';

export const Login: React.FC = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);

  const onFinish = async (values: LoginRequest) => {
    setLoading(true);
    try {
      const response = await authService.login(values);
      localStorage.setItem('jwt_token', response.data.token);
      localStorage.setItem('username', response.data.username);
      message.success('Login effettuato con successo');
      navigate('/');
    } catch (error: any) {
      console.error('Login error:', error);
      message.error(error.response?.data?.message || 'Credenziali non valide');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        minHeight: '100vh',
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
      }}
    >
      <Card
        title={
          <div style={{ textAlign: 'center' }}>
            <h2 style={{ margin: 0 }}>DW-Builder</h2>
            <p style={{ margin: 0, fontSize: 14, color: '#888' }}>Data Warehouse Configuration Platform</p>
          </div>
        }
        style={{ width: 400, boxShadow: '0 10px 40px rgba(0,0,0,0.2)' }}
      >
        <Form name="login" onFinish={onFinish} autoComplete="off" layout="vertical">
          <Form.Item name="username" rules={[{ required: true, message: 'Inserisci username' }]}>
            <Input prefix={<UserOutlined />} placeholder="Username" size="large" />
          </Form.Item>
          <Form.Item name="password" rules={[{ required: true, message: 'Inserisci password' }]}>
            <Input.Password prefix={<LockOutlined />} placeholder="Password" size="large" />
          </Form.Item>
          <Form.Item>
            <Button type="primary" htmlType="submit" loading={loading} block size="large">
              Accedi
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};
