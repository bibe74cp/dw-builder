import React, { useState } from 'react';
import { Card, Button, Space, message, Tabs, Input, Spin, Checkbox } from 'antd';
import { DownloadOutlined, ThunderboltOutlined, ArrowLeftOutlined, ReloadOutlined } from '@ant-design/icons';
import { useQuery, useMutation } from '@tanstack/react-query';
import { ddlService } from '@/api/services';
import { useParams, useNavigate } from 'react-router-dom';
import type { ApplyDdlRequest } from '@/types/api';

const { TextArea } = Input;

export const Ddl: React.FC = () => {
  const { sourceId, tableId } = useParams<{ sourceId: string; tableId: string }>();
  const navigate = useNavigate();
  const [applyOptions, setApplyOptions] = useState<ApplyDdlRequest>({
    applyLanding: true,
    applyStaging: true,
    applyAlter: false,
  });

  const { data: ddlResult, isLoading, refetch } = useQuery({
    queryKey: ['ddl', sourceId, tableId],
    queryFn: async () => {
      const response = await ddlService.generate(Number(sourceId), Number(tableId));
      return response.data;
    },
    enabled: !!sourceId && !!tableId,
  });

  const applyMutation = useMutation({
    mutationFn: (options: ApplyDdlRequest) => ddlService.apply(Number(sourceId), Number(tableId), options),
    onSuccess: (response) => {
      message.success(response.data.message);
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || 'Errore durante l\'applicazione del DDL');
    },
  });

  const handleDownload = () => {
    if (!ddlResult) return;

    const blob = new Blob(
      [
        '-- CREATE LANDING TABLE\n',
        ddlResult.createLandingTableDdl,
        '\n\n-- CREATE STAGING TABLE\n',
        ddlResult.createStagingTableDdl,
        ddlResult.alterLandingTableDdl ? '\n\n-- ALTER LANDING TABLE\n' + ddlResult.alterLandingTableDdl : '',
      ],
      { type: 'text/plain' }
    );
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `ddl_table_${tableId}.sql`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    message.success('File DDL scaricato con successo');
  };

  const handleApply = () => {
    if (!applyOptions.applyLanding && !applyOptions.applyStaging && !applyOptions.applyAlter) {
      message.warning('Seleziona almeno uno script da applicare');
      return;
    }
    applyMutation.mutate(applyOptions);
  };

  const tabItems = [
    {
      key: 'landing',
      label: 'CREATE Landing Table',
      children: (
        <TextArea
          value={ddlResult?.createLandingTableDdl || ''}
          readOnly
          rows={20}
          style={{ fontFamily: 'monospace', fontSize: 12 }}
        />
      ),
    },
    {
      key: 'staging',
      label: 'CREATE Staging Table',
      children: (
        <TextArea
          value={ddlResult?.createStagingTableDdl || ''}
          readOnly
          rows={20}
          style={{ fontFamily: 'monospace', fontSize: 12 }}
        />
      ),
    },
  ];

  if (ddlResult?.alterLandingTableDdl) {
    tabItems.push({
      key: 'alter',
      label: 'ALTER Landing Table',
      children: (
        <TextArea
          value={ddlResult.alterLandingTableDdl}
          readOnly
          rows={20}
          style={{ fontFamily: 'monospace', fontSize: 12 }}
        />
      ),
    });
  }

  return (
    <Spin spinning={isLoading}>
      <Card
        title="Preview e Applicazione DDL"
        extra={
          <Space>
            <Button icon={<ArrowLeftOutlined />} onClick={() => navigate(`/sources/${sourceId}/tables/${tableId}/fields`)}>
              Indietro
            </Button>
            <Button icon={<ReloadOutlined />} onClick={() => refetch()}>
              Rigenera
            </Button>
            <Button icon={<DownloadOutlined />} onClick={handleDownload}>
              Scarica SQL
            </Button>
            <Button
              type="primary"
              icon={<ThunderboltOutlined />}
              onClick={handleApply}
              loading={applyMutation.isPending}
              danger
            >
              Applica DDL al DW
            </Button>
          </Space>
        }
      >
        <Space direction="vertical" style={{ width: '100%', marginBottom: 16 }}>
          <div style={{ background: '#f5f5f5', padding: 12, borderRadius: 4 }}>
            <strong>Seleziona script da applicare al Data Warehouse:</strong>
            <div style={{ marginTop: 8 }}>
              <Space>
                <Checkbox
                  checked={applyOptions.applyLanding}
                  onChange={(e) => setApplyOptions({ ...applyOptions, applyLanding: e.target.checked })}
                >
                  Applica CREATE Landing Table
                </Checkbox>
                <Checkbox
                  checked={applyOptions.applyStaging}
                  onChange={(e) => setApplyOptions({ ...applyOptions, applyStaging: e.target.checked })}
                >
                  Applica CREATE Staging Table
                </Checkbox>
                {ddlResult?.alterLandingTableDdl && (
                  <Checkbox
                    checked={applyOptions.applyAlter}
                    onChange={(e) => setApplyOptions({ ...applyOptions, applyAlter: e.target.checked })}
                  >
                    Applica ALTER Landing Table
                  </Checkbox>
                )}
              </Space>
            </div>
          </div>
        </Space>

        <Tabs items={tabItems} defaultActiveKey="landing" />
      </Card>
    </Spin>
  );
};
