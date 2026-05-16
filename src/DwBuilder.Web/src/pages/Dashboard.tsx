import React from 'react';
import { Card, Row, Col, Statistic, Table, Tag, Button, Space } from 'antd';
import { DatabaseOutlined, CheckCircleOutlined, CloseCircleOutlined, SyncOutlined, PlusOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { sourceService } from '@/api/services';
import { useNavigate } from 'react-router-dom';
import type { Source } from '@/types/api';
import type { ColumnsType } from 'antd/es/table';
import { BimlDownloadButton } from '@/components/BimlDownloadButton';

export const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  
  const { data: sources, isLoading } = useQuery({
    queryKey: ['sources'],
    queryFn: async () => {
      const response = await sourceService.getAll();
      return response.data;
    },
  });

  const activeSources = sources?.filter(s => s.isActive).length || 0;
  const totalSources = sources?.length || 0;

  const columns: ColumnsType<Source> = [
    { title: 'Nome', dataIndex: 'name', key: 'name', width: 200 },
    { title: 'Server', dataIndex: 'serverName', key: 'serverName', width: 200 },
    { title: 'Database', dataIndex: 'databaseName', key: 'databaseName', width: 200 },
    {
      title: 'Schema Landing',
      dataIndex: 'landingSchema',
      key: 'landingSchema',
      width: 150,
    },
    {
      title: 'Stato',
      key: 'isActive',
      width: 120,
      render: (_: any, record: Source) => (
        <Tag color={record.isActive ? 'green' : 'red'} icon={record.isActive ? <CheckCircleOutlined /> : <CloseCircleOutlined />}>
          {record.isActive ? 'Attiva' : 'Disattivata'}
        </Tag>
      ),
    },
    {
      title: 'Azioni',
      key: 'actions',
      width: 150,
      render: (_: any, record: Source) => (
        <Button type="link" size="small" onClick={() => navigate(`/sources/${record.id}/tables`)}>
          Configura Tabelle
        </Button>
      ),
    },
  ];

  return (
    <>
      <Row gutter={16} style={{ marginBottom: 24 }}>
        <Col span={6}>
          <Card>
            <Statistic
              title="Sorgenti Totali"
              value={totalSources}
              prefix={<DatabaseOutlined style={{ color: '#1890ff' }} />}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="Sorgenti Attive"
              value={activeSources}
              valueStyle={{ color: '#3f8600' }}
              prefix={<CheckCircleOutlined />}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="Sorgenti Disattivate"
              value={totalSources - activeSources}
              valueStyle={{ color: '#cf1322' }}
              prefix={<CloseCircleOutlined />}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic title="Ultima Sincronizzazione" value="N/A" prefix={<SyncOutlined />} />
          </Card>
        </Col>
      </Row>

      <Card
        title="Sorgenti Configurate"
        extra={
          <Space>
            <BimlDownloadButton />
            <Button type="primary" icon={<PlusOutlined />} onClick={() => navigate('/sources')}>
              Gestisci Sorgenti
            </Button>
          </Space>
        }
      >
        <Table columns={columns} dataSource={sources} rowKey="id" loading={isLoading} pagination={{ pageSize: 10 }} />
      </Card>
    </>
  );
};
