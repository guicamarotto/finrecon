import { useState } from 'react';
import { Table, Select, Card, Typography, Button, Space } from 'antd';
import { EyeOutlined, PlusOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useReconciliations } from '../../hooks/useReconciliations';
import { JobStatusTag } from '../../components/JobStatusTag/JobStatusTag';
import type { ReconciliationJob } from '../../api/types';
import type { JobStatus } from '../../api/types';
import type { ColumnsType } from 'antd/es/table';

const { Title } = Typography;

export function ListPage() {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<JobStatus | undefined>();

  const { data, isLoading } = useReconciliations(page, 20, status);

  const columns: ColumnsType<ReconciliationJob> = [
    {
      title: 'Filename',
      dataIndex: 'filename',
      ellipsis: true,
    },
    {
      title: 'Reference Date',
      dataIndex: 'referenceDate',
      width: 140,
    },
    {
      title: 'Status',
      dataIndex: 'status',
      width: 120,
      render: (s: JobStatus) => <JobStatusTag status={s} />,
    },
    {
      title: 'Created At',
      dataIndex: 'createdAt',
      width: 180,
      render: (v: string) => new Date(v).toLocaleString(),
    },
    {
      title: '',
      key: 'actions',
      width: 80,
      render: (_, record) => (
        <Button
          icon={<EyeOutlined />}
          size="small"
          onClick={() => navigate(`/reconciliations/${record.id}`)}
        />
      ),
    },
  ];

  return (
    <Card>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={4} style={{ margin: 0 }}>Reconciliations</Title>
        <Space>
          <Select
            allowClear
            placeholder="Filter by status"
            style={{ width: 180 }}
            onChange={(v) => { setStatus(v); setPage(1); }}
            options={[
              { value: 'Pending', label: 'Pending' },
              { value: 'Processing', label: 'Processing' },
              { value: 'Completed', label: 'Completed' },
              { value: 'Failed', label: 'Failed' },
            ]}
          />
          <Button type="primary" icon={<PlusOutlined />} onClick={() => navigate('/upload')}>
            New Upload
          </Button>
        </Space>
      </div>

      <Table
        rowKey="id"
        columns={columns}
        dataSource={data?.items}
        loading={isLoading}
        pagination={{
          current: page,
          pageSize: 20,
          total: data?.total,
          onChange: setPage,
          showTotal: (total) => `${total} reconciliations`,
        }}
      />
    </Card>
  );
}
