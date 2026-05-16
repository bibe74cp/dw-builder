import React, { useState, useEffect } from 'react';
import { Card, Table, Button, Space, message, Input, Switch, Spin, Tag } from 'antd';
import { SaveOutlined, ReloadOutlined, ArrowLeftOutlined, KeyOutlined } from '@ant-design/icons';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sourceTableService } from '@/api/services';
import { useParams, useNavigate } from 'react-router-dom';
import type { AvailableField, SourceField, SourceFieldUpsertDto } from '@/types/api';
import type { ColumnsType } from 'antd/es/table';

interface FieldRow extends AvailableField {
  key: string;
  id?: number;
  landingColumnName: string;
  isBusinessKey: boolean;
  isSelected: boolean;
}

export const Fields: React.FC = () => {
  const { sourceId, tableId } = useParams<{ sourceId: string; tableId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [fieldRows, setFieldRows] = useState<FieldRow[]>([]);
  const [saving, setSaving] = useState(false);

  const { data: availableFields, isLoading: availableLoading } = useQuery({
    queryKey: ['available-fields', tableId],
    queryFn: async () => {
      const response = await sourceTableService.getAvailableFields(Number(tableId));
      return response.data;
    },
    enabled: !!tableId,
  });

  const { data: configuredFields, isLoading: configuredLoading } = useQuery({
    queryKey: ['source-fields', tableId],
    queryFn: async () => {
      const response = await sourceTableService.getFields(Number(tableId));
      return response.data;
    },
    enabled: !!tableId,
  });

  useEffect(() => {
    if (availableFields && configuredFields) {
      const rows: FieldRow[] = availableFields.map((availField) => {
        const configured = configuredFields.find((cf) => cf.sourceColumnName === availField.columnName);
        return {
          key: availField.columnName,
          columnName: availField.columnName,
          dataType: availField.dataType,
          isNullable: availField.isNullable,
          ordinalPosition: availField.ordinalPosition,
          id: configured?.id,
          landingColumnName: configured?.landingColumnName || availField.columnName,
          isBusinessKey: configured?.isBusinessKey ?? false,
          isSelected: !!configured,
        };
      });
      setFieldRows(rows.sort((a, b) => a.ordinalPosition - b.ordinalPosition));
    }
  }, [availableFields, configuredFields]);

  const bulkUpsertMutation = useMutation({
    mutationFn: (fields: SourceFieldUpsertDto[]) => sourceTableService.bulkUpsertFields(Number(tableId), fields),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['source-fields', tableId] });
      message.success('Configurazione campi salvata con successo');
      setSaving(false);
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || 'Errore durante il salvataggio');
      setSaving(false);
    },
  });

  const handleSelectionChange = (key: string, selected: boolean) => {
    setFieldRows((prev) =>
      prev.map((row) => (row.key === key ? { ...row, isSelected: selected } : row))
    );
  };

  const handleLandingNameChange = (key: string, landingName: string) => {
    setFieldRows((prev) =>
      prev.map((row) => (row.key === key ? { ...row, landingColumnName: landingName } : row))
    );
  };

  const handleBusinessKeyChange = (key: string, isBusinessKey: boolean) => {
    setFieldRows((prev) =>
      prev.map((row) => (row.key === key ? { ...row, isBusinessKey } : row))
    );
  };

  const handleSave = () => {
    const selectedFields: SourceFieldUpsertDto[] = fieldRows
      .filter((row) => row.isSelected)
      .map((row) => ({
        id: row.id,
        sourceColumnName: row.columnName,
        landingColumnName: row.landingColumnName,
        isBusinessKey: row.isBusinessKey,
        ordinalPosition: row.ordinalPosition,
      }));

    if (selectedFields.length === 0) {
      message.warning('Seleziona almeno un campo');
      return;
    }

    setSaving(true);
    bulkUpsertMutation.mutate(selectedFields);
  };

  const handleRefresh = () => {
    queryClient.invalidateQueries({ queryKey: ['available-fields', tableId] });
    queryClient.invalidateQueries({ queryKey: ['source-fields', tableId] });
  };

  const columns: ColumnsType<FieldRow> = [
    {
      title: 'Seleziona',
      key: 'select',
      width: 80,
      render: (_: any, record: FieldRow) => (
        <Switch
          checked={record.isSelected}
          onChange={(checked) => handleSelectionChange(record.key, checked)}
        />
      ),
    },
    {
      title: '#',
      dataIndex: 'ordinalPosition',
      key: 'ordinalPosition',
      width: 60,
      sorter: (a, b) => a.ordinalPosition - b.ordinalPosition,
    },
    { title: 'Colonna Sorgente', dataIndex: 'columnName', key: 'columnName', width: 200 },
    {
      title: 'Nome Colonna Landing',
      key: 'landingColumnName',
      width: 200,
      render: (_: any, record: FieldRow) => (
        <Input
          value={record.landingColumnName}
          onChange={(e) => handleLandingNameChange(record.key, e.target.value)}
          disabled={!record.isSelected}
          placeholder="Nome colonna landing"
        />
      ),
    },
    {
      title: 'Tipo Dati',
      dataIndex: 'dataType',
      key: 'dataType',
      width: 120,
      render: (dataType: string) => <Tag color="blue">{dataType}</Tag>,
    },
    {
      title: 'Nullable',
      key: 'isNullable',
      width: 80,
      render: (_: any, record: FieldRow) => (
        <Tag color={record.isNullable ? 'orange' : 'green'}>{record.isNullable ? 'Sì' : 'No'}</Tag>
      ),
    },
    {
      title: 'Business Key',
      key: 'isBusinessKey',
      width: 120,
      render: (_: any, record: FieldRow) => (
        <Switch
          checked={record.isBusinessKey}
          onChange={(checked) => handleBusinessKeyChange(record.key, checked)}
          disabled={!record.isSelected}
          checkedChildren={<KeyOutlined />}
        />
      ),
    },
    {
      title: 'Azioni',
      key: 'actions',
      width: 150,
      render: (_: any, record: FieldRow) =>
        record.id && record.isSelected ? (
          <Button
            type="link"
            size="small"
            onClick={() => navigate(`/sources/${sourceId}/tables/${tableId}/ddl`)}
          >
            Genera DDL
          </Button>
        ) : null,
    },
  ];

  const isLoading = availableLoading || configuredLoading;

  return (
    <Spin spinning={isLoading}>
      <Card
        title="Configurazione Campi"
        extra={
          <Space>
            <Button icon={<ArrowLeftOutlined />} onClick={() => navigate(`/sources/${sourceId}/tables`)}>
              Indietro
            </Button>
            <Button icon={<ReloadOutlined />} onClick={handleRefresh}>
              Aggiorna
            </Button>
            <Button type="primary" icon={<SaveOutlined />} onClick={handleSave} loading={saving}>
              Salva Configurazione
            </Button>
          </Space>
        }
      >
        <Table
          columns={columns}
          dataSource={fieldRows}
          rowKey="key"
          pagination={{ pageSize: 20 }}
          scroll={{ y: 500 }}
        />
      </Card>
    </Spin>
  );
};
