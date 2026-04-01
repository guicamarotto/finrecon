import { useState } from 'react';
import { Upload, DatePicker, Button, Card, Typography, Alert, Space } from 'antd';
import { InboxOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useUploadReconciliation } from '../../hooks/useReconciliations';
import type { UploadFile } from 'antd';
import dayjs from 'dayjs';

const { Dragger } = Upload;
const { Title, Text } = Typography;

export function UploadPage() {
  const navigate = useNavigate();
  const { mutateAsync, isPending, error } = useUploadReconciliation();
  const [file, setFile] = useState<File | null>(null);
  const [referenceDate, setReferenceDate] = useState<string | null>(null);

  const handleUpload = async () => {
    if (!file || !referenceDate) return;
    const result = await mutateAsync({ file, referenceDate });
    navigate(`/reconciliations/${result.jobId}`);
  };

  const beforeUpload = (f: File) => {
    setFile(f);
    return false; // prevent auto-upload
  };

  const onRemove = () => setFile(null);

  const apiError = error as { response?: { data?: { code?: string; message?: string } } } | null;

  return (
    <Card>
      <Title level={4}>Upload Reconciliation File</Title>
      <Text type="secondary">
        Upload a CSV or JSON file containing financial product snapshots.
        The system will compare it with the most recent completed reconciliation.
      </Text>

      <Space direction="vertical" size="large" style={{ width: '100%', marginTop: 24 }}>
        {apiError && (
          <Alert
            type="error"
            message={apiError.response?.data?.message ?? 'Upload failed. Please try again.'}
          />
        )}

        <Dragger
          accept=".csv,.json"
          beforeUpload={beforeUpload}
          onRemove={onRemove}
          maxCount={1}
          fileList={file ? [{ uid: '1', name: file.name, status: 'done' } as UploadFile] : []}
        >
          <p className="ant-upload-drag-icon">
            <InboxOutlined />
          </p>
          <p className="ant-upload-text">Click or drag a CSV or JSON file here</p>
          <p className="ant-upload-hint">Max size: 10MB. Supported formats: .csv, .json</p>
        </Dragger>

        <div>
          <Text>Reference Date</Text>
          <br />
          <DatePicker
            style={{ marginTop: 8, width: 240 }}
            disabledDate={(d) => d.isAfter(dayjs())}
            onChange={(_, dateStr) => setReferenceDate(typeof dateStr === 'string' ? dateStr : null)}
          />
        </div>

        <Button
          type="primary"
          size="large"
          onClick={handleUpload}
          loading={isPending}
          disabled={!file || !referenceDate}
        >
          Start Reconciliation
        </Button>
      </Space>
    </Card>
  );
}
