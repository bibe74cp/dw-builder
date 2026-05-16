import React from 'react';
import { Card, Form, Input, Button, message, Divider } from 'antd';
import { SaveOutlined, DatabaseOutlined, KeyOutlined } from '@ant-design/icons';

export const Settings: React.FC = () => {
  const [form] = Form.useForm();

  const onFinish = (values: any) => {
    console.log('Settings:', values);
    message.info('Funzionalità di salvataggio impostazioni non ancora implementata');
  };

  return (
    <div style={{ maxWidth: 800 }}>
      <Card title="Impostazioni Sistema" extra={<DatabaseOutlined />}>
        <Form form={form} layout="vertical" onFinish={onFinish}>
          <Divider orientation="left">Connessione Data Warehouse</Divider>

          <Form.Item
            label="Connection String Data Warehouse"
            name="dwConnection"
            tooltip="Stringa di connessione al database Data Warehouse di destinazione"
          >
            <Input.TextArea
              rows={3}
              placeholder="Server=localhost;Database=DwBuilderDW;User Id=sa;Password=******;TrustServerCertificate=True;"
            />
          </Form.Item>

          <Divider orientation="left">Sicurezza</Divider>

          <Form.Item
            label="Encryption Key (AES-256)"
            name="encryptionKey"
            tooltip="Chiave di cifratura per le password delle sorgenti (32 caratteri hex)"
          >
            <Input.Password
              prefix={<KeyOutlined />}
              placeholder="0123456789ABCDEF0123456789ABCDEF"
              maxLength={32}
            />
          </Form.Item>

          <Form.Item
            label="JWT Secret Key"
            name="jwtSecretKey"
            tooltip="Chiave segreta per la firma dei token JWT"
          >
            <Input.Password
              prefix={<KeyOutlined />}
              placeholder="Chiave segreta JWT (minimo 32 caratteri)"
            />
          </Form.Item>

          <Divider orientation="left">Configurazione CORS</Divider>

          <Form.Item
            label="Origini Consentite (CORS)"
            name="allowedCorsOrigins"
            tooltip="Elenco di origini consentite separate da virgola"
          >
            <Input placeholder="http://localhost:5173,https://dwbuilder.example.com" />
          </Form.Item>

          <Divider orientation="left">Logging</Divider>

          <Form.Item
            label="Livello Log Minimo"
            name="logLevel"
            tooltip="Livello minimo di logging (Information, Warning, Error)"
          >
            <Input placeholder="Information" />
          </Form.Item>

          <Form.Item>
            <Button type="primary" htmlType="submit" icon={<SaveOutlined />} size="large">
              Salva Impostazioni
            </Button>
          </Form.Item>
        </Form>
      </Card>

      <Card title="Informazioni Sistema" style={{ marginTop: 16 }}>
        <p><strong>Versione:</strong> 1.0.0</p>
        <p><strong>Backend API:</strong> {import.meta.env.VITE_API_BASE_URL}</p>
        <p><strong>Framework:</strong> React 18 + TypeScript + Vite</p>
        <p><strong>UI Library:</strong> Ant Design 5</p>
      </Card>
    </div>
  );
};
