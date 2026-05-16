import React, { useState } from 'react';
import { Card, Table, Button, Space, Modal, Form, Input, Switch, message, Popconfirm, Tag } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, ApiOutlined, CheckCircleOutlined, CloseCircleOutlined } from '@ant-design/icons';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sourceService } from '@/api/services';
import type { Source, SourceCreateDto, SourceUpdateDto } from '@/types/api';
import type { ColumnsType } from 'antd/es/table';
import { useNavigate } from 'react-router-dom';

export const Sources: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form] = Form.useForm();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingSource, setEditingSource] = useState<Source | null>(null);
  const [testingConnection, setTestingConnection] = useState(false);

  const { data: sources, isLoading } = useQuery({
    queryKey: ['sources'],
    queryFn: async () => {
      const response = await sourceService.getAll();
      return response.data;
    },
  });

  const createMutation = useMutation({
    mutationFn: (data: SourceCreateDto) => sourceService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sources'] });
      message.success('Sorgente creata con successo');
      handleCloseModal();
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || 'Errore durante la creazione');
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: SourceUpdateDto }) => sourceService.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sources'] });
      message.success('Sorgente aggiornata con successo');
      handleCloseModal();
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || 'Errore durante l\'aggiornamento');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => sourceService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sources'] });
      message.success('Sorgente eliminata con successo');
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || 'Errore durante l\'eliminazione');
    },
  });

  const handleCreate = () => {
    setEditingSource(null);
    form.resetFields();
    form.setFieldsValue({ isActive: true });
    setIsModalOpen(true);
  };

  const handleEdit = (source: Source) => {
    setEditingSource(source);
    form.setFieldsValue({
      name: source.name,
      serverName: source.serverName,
      instanceName: source.instanceName,
      databaseName: source.databaseName,
      landingSchema: source.landingSchema,
      connectionUser: source.connectionUser,
      isActive: source.isActive,
    });
    setIsModalOpen(true);
  };

  const handleDelete = (id: number) => {
    deleteMutation.mutate(id);
  };

  const handleTestConnection = async () => {
    if (!editingSource) {
      message.warning('Salva prima la sorgente per testare la connessione');
      return;
    }

    setTestingConnection(true);
    try {
      const response = await sourceService.testConnection(editingSource.id);
      if (response.data.success) {
        message.success(response.data.message);
      } else {
        message.error(response.data.message);
      }
    } catch (error: any) {
      message.error(error.response?.data?.message || 'Errore nel test di connessione');
    } finally {
      setTestingConnection(false);
    }
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
    setEditingSource(null);
    form.resetFields();
  };

  const handleSubmit = () => {
    form.validateFields().then((values) => {
      if (editingSource) {
        // Update existing source
        const updateData: SourceUpdateDto = {
          ...values,
          connectionPassword: values.connectionPassword || undefined,
        };
        updateMutation.mutate({ id: editingSource.id, data: updateData });
      } else {
        // Create new source
        createMutation.mutate(values as SourceCreateDto);
      }
    });
  };

  const columns: ColumnsType<Source> = [
    { title: 'Nome', dataIndex: 'name', key: 'name', width: 150 },
    { title: 'Server', dataIndex: 'serverName', key: 'serverName', width: 150 },
    { title: 'Istanza', dataIndex: 'instanceName', key: 'instanceName', width: 120 },
    { title: 'Database', dataIndex: 'databaseName', key: 'databaseName', width: 150 },
    { title: 'Schema Landing', dataIndex: 'landingSchema', key: 'landingSchema', width: 120 },
    { title: 'Utente', dataIndex: 'connectionUser', key: 'connectionUser', width: 120 },
    {
      title: 'Stato',
      key: 'isActive',
      width: 100,
      render: (_: any, record: Source) => (
        <Tag color={record.isActive ? 'green' : 'red'} icon={record.isActive ? <CheckCircleOutlined /> : <CloseCircleOutlined />}>
          {record.isActive ? 'Attiva' : 'Off'}
        </Tag>
      ),
    },
    {
      title: 'Azioni',
      key: 'actions',
      width: 200,
      render: (_: any, record: Source) => (
        <Space size="small">
          <Button type="link" size="small" onClick={() => navigate(`/sources/${record.id}/tables`)}>
            Tabelle
          </Button>
          <Button type="link" size="small" icon={<EditOutlined />} onClick={() => handleEdit(record)} />
          <Popconfirm
            title="Eliminare questa sorgente?"
            description="L'operazione non può essere annullata"
            onConfirm={() => handleDelete(record.id)}
            okText="Sì"
            cancelText="No"
          >
            <Button type="link" size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <>
      <Card
        title="Gestione Sorgenti"
        extra={
          <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            Nuova Sorgente
          </Button>
        }
      >
        <Table columns={columns} dataSource={sources} rowKey="id" loading={isLoading} pagination={{ pageSize: 10 }} />
      </Card>

      <Modal
        title={editingSource ? 'Modifica Sorgente' : 'Nuova Sorgente'}
        open={isModalOpen}
        onOk={handleSubmit}
        onCancel={handleCloseModal}
        width={600}
        confirmLoading={createMutation.isPending || updateMutation.isPending}
        okText={editingSource ? 'Aggiorna' : 'Crea'}
        cancelText="Annulla"
      >
        <Form form={form} layout="vertical" style={{ marginTop: 24 }}>
          <Form.Item name="name" label="Nome Sorgente" rules={[{ required: true, message: 'Campo obbligatorio' }]}>
            <Input placeholder="es. ERP_Produzione" />
          </Form.Item>

          <Form.Item name="serverName" label="Server Name" rules={[{ required: true, message: 'Campo obbligatorio' }]}>
            <Input placeholder="es. SQL-SERVER-01" />
          </Form.Item>

          <Form.Item name="instanceName" label="Instance Name (opzionale)">
            <Input placeholder="es. SQLEXPRESS" />
          </Form.Item>

          <Form.Item name="databaseName" label="Database Name" rules={[{ required: true, message: 'Campo obbligatorio' }]}>
            <Input placeholder="es. ERP_DB" />
          </Form.Item>

          <Form.Item name="landingSchema" label="Schema Landing" rules={[{ required: true, message: 'Campo obbligatorio' }]}>
            <Input placeholder="es. landing_erp" />
          </Form.Item>

          <Form.Item name="connectionUser" label="Connection User" rules={[{ required: true, message: 'Campo obbligatorio' }]}>
            <Input placeholder="es. dw_reader" />
          </Form.Item>

          <Form.Item
            name="connectionPassword"
            label={editingSource ? 'Connection Password (lascia vuoto per non modificare)' : 'Connection Password'}
            rules={editingSource ? [] : [{ required: true, message: 'Campo obbligatorio' }]}
          >
            <Input.Password placeholder="Password di connessione" />
          </Form.Item>

          <Form.Item name="isActive" label="Attiva" valuePropName="checked">
            <Switch />
          </Form.Item>

          {editingSource && (
            <Form.Item>
              <Button icon={<ApiOutlined />} onClick={handleTestConnection} loading={testingConnection} block>
                Testa Connessione
              </Button>
            </Form.Item>
          )}
        </Form>
      </Modal>
    </>
  );
};
