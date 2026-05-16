import React, { useState } from 'react';
import { Button, message } from 'antd';
import { DownloadOutlined } from '@ant-design/icons';
import { bimlService } from '@/api/services';

export const BimlDownloadButton: React.FC = () => {
  const [loading, setLoading] = useState(false);

  const handleDownload = async () => {
    setLoading(true);
    try {
      const response = await bimlService.download();
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', 'MasterTemplate.biml');
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
      message.success('File BIML scaricato con successo');
    } catch (error: any) {
      console.error('BIML download error:', error);
      message.error(error.response?.data?.message || 'Errore durante il download del BIML');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Button type="default" icon={<DownloadOutlined />} onClick={handleDownload} loading={loading}>
      Genera e Scarica BIML
    </Button>
  );
};
