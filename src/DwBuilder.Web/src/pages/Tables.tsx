import React, { useState, useEffect } from 'react';
import { Card, Table, Button, Space, message, Input, Switch, Spin } from 'antd';
import { SaveOutlined, ReloadOutlined, ArrowLeftOutlined } from '@ant-design/icons';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sourceService, sourceTableService } from '@/api/services';
import { useParams, useNavigate } from 'react-router-dom';
import type { AvailableTable, SourceTable, SourceTableUpsertDto } from '@/types/api';
import type { ColumnsType } from 'antd/es/table';

interface TableRow extends AvailableTable {
  key: string;
  id?: number;
  isActive: boolean;
  landingTableName: string;
  isSelected: boolean;
}

export const Tables: React.FC = () => {
  const { sourceId } = useParams<{ sourceId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [tableRows, setTableRows] = useState<TableRow[]>([]);
  const [saving, setSaving] = useState(false);

  const { data: source, isLoading: sourceLoading } = useQuery({
    queryKey: ['source', sourceId],
    queryFn: async () => {
      const response = await sourceService.getById(Number(sourceId));
      return response.data;
    },
    enabled: !!sourceId,
  });

  const { data: availableTables, isLoading: availableLoading } = useQuery({
    queryKey: ['available-tables', sourceId],
    queryFn: async () => {
      const response = await sourceService.getAvailableTables(Number(sourceId));
      return response.data;
    },
    enabled: !!sourceId,
  });

  const { data: configuredTables, isLoading: configuredLoading } = useQuery({
    queryKey: ['source-tables', sourceId],
    queryFn: async () => {
      const response = await sourceTableService.getBySourceId(Number(sourceId));
      return response.data;
    },
    enabled: !!sourceId,
  });

  useEffect(() => {
    if (availableTables && configuredTables) {
      const rows: TableRow[] = availableTables.map((availTable) => {
        const configured = configuredTables.find(
          (ct) => ct.schemaName === availTable.schemaName && ct.tableName === availTable.tableName
        );
        return {
          key: `${availTable.schemaName}.${availTable.tableName}`,
          schemaName: availTable.schemaName,
          tableName: availTable.tableName,
          id: configured?.id,
          isActive: configured?.isActive ?? true,
          landingTableName: configured?.landingTableName || availTable.tableName,
          isSelected: !!configured,
        };
      });
      setTableRows(rows);
    }
  }, [availableTables, configuredTables]);

  const bulkUpsertMutation = useMutation({
    mutationFn: (tables: SourceTableUpsertDto[]) => sourceTableService.bulkUpsert(Number(sourceId), tables),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['source-tables', sourceId] });
      message.success('Configurazione tabelle salvata con successo');
      setSaving(false);
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || 'Errore durante il salvataggio');
      setSaving(false);
    },
  });

  const handleSelectionChange = (key: string, selected: boolean) => {
    setTableRows((prev) =>
      prev.map((row) => (row.key === key ? { ...row, isSelected: selected } : row))
    );
  };

  const handleLandingNameChange = (key: string, landingName: string) => {
    setTableRows((prev) =>
      prev.map((row) => (row.key === key ? { ...row, landingTableName: landingName } : row))
    );
  };

  const handleActiveChange = (key: string, active: boolean) => {
    setTableRows((prev) =>
      prev.map((row) => (row.key === key ? { ...row, isActive: active } : row))
    );
  };

  const handleSave = () => {
    const selectedTables: SourceTableUpsertDto[] = tableRows
      .filter((row) => row.isSelected)
      .map((row) => ({
        id: row.id,
        schemaName: row.schemaName,
        tableName: row.tableName,
        landingTableName: row.landingTableName,
        isActive: row.isActive,
      }));

    if (selectedTables.length === 0) {
      message.warning('Seleziona almeno una tabella');
      return;
    }

    setSaving(true);
    bulkUpsertMutation.mutate(selectedTables);
  };

  const handleRefresh = () => {
    queryClient.invalidateQueries({ queryKey: ['available-tables', sourceId] });
    queryClient.invalidateQueries({ queryKey: ['source-tables', sourceId] });
  };

  const columns: ColumnsType<TableRow> = [
    {
      title: 'Seleziona',
      key: 'select',
      width: 80,
      render: (_: any, record: TableRow) => (
        <Switch
          checked={record.isSelected}
          onChange={(checked) => handleSelectionChange(record.key, checked)}
        />
      ),
    },
    { title: 'Schema', dataIndex: 'schemaName', key: 'schemaName', width: 150 },
    { title: 'Tabella Sorgente', dataIndex: 'tableName', key: 'tableName', width: 200 },
    {
      title: 'Nome Tabella Landing',
      key: 'landingTableName',
      width: 250,
      render: (_: any, record: TableRow) => (
        <Input
          value={record.landingTableName}
          onChange={(e) => handleLandingNameChange(record.key, e.target.value)}
          disabled={!record.isSelected}
          placeholder="Nome tabella landing"
        />
      ),
    },
    {
      title: 'Attiva',
      key: 'isActive',
      width: 80,
      render: (_: any, record: TableRow) => (
        <Switch
          checked={record.isActive}
          onChange={(checked) => handleActiveChange(record.key, checked)}
          disabled={!record.isSelected}
        />
      ),
    },
    {
      title: 'Azioni',
      key: 'actions',
      width: 150,
      render: (_: any, record: TableRow) => (
        record.id && record.isSelected ? (
          <Button
            type="link"
            size="small"
            onClick={() => navigate(`/sources/${sourceId}/tables/${record.id}/fields`)}
          >
            Configura Campi
          </Button>
        ) : null
      ),
    },
  ];

  const isLoading = sourceLoading || availableLoading || configuredLoading;

  return (
    <Spin spinning={isLoading}>
      <Card
        title={`Selezione Tabelle - ${source?.name || 'Caricamento...'}`}
        extra={
          <Space>
            <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/sources')}>
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
          dataSource={tableRows}
          rowKey="key"
          pagination={{ pageSize: 20 }}
          scroll={{ y: 500 }}
        />
      </Card>
    </Spin>
  );
};
