import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Card,
  Col,
  Row,
  Statistic,
  Table,
  Select,
  Space,
  Typography,
  Alert,
  Spin,
  Button,
  Tag,
} from 'antd';
import { ArrowLeftOutlined, SyncOutlined } from '@ant-design/icons';
import { useReconciliationDetail, useReconciliationRecords } from '../../hooks/useReconciliations';
import { JobStatusTag } from '../../components/JobStatusTag/JobStatusTag';
import { RecordStatusTag } from '../../components/RecordStatusTag/RecordStatusTag';
import { StatusBreakdownChart } from './StatusBreakdownChart';
import { DeltaOverviewChart } from './DeltaOverviewChart';
import type { ReconciliationRecord, RecordStatus, ProductType } from '../../api/types';
import type { ColumnsType } from 'antd/es/table';

const { Title, Text } = Typography;

const PRODUCT_TYPES: ProductType[] = ['Equity', 'Fund', 'Crypto', 'Bond'];
const RECORD_STATUSES: RecordStatus[] = ['Matched', 'Discrepant', 'New', 'Missing'];

export function DetailPage() {
  const { jobId } = useParams<{ jobId: string }>();
  const navigate = useNavigate();
  const [statusFilter, setStatusFilter] = useState<RecordStatus | undefined>();
  const [productFilter, setProductFilter] = useState<ProductType | undefined>();

  const { data: detail, isLoading, error } = useReconciliationDetail(jobId!);
  const { data: records = [] } = useReconciliationRecords(jobId!, {
    status: statusFilter,
    productType: productFilter,
  });

  const columns: ColumnsType<ReconciliationRecord> = [
    { title: 'Client ID', dataIndex: 'clientId', width: 120 },
    {
      title: 'Product',
      dataIndex: 'productType',
      width: 100,
      render: (v: ProductType) => <Tag>{v}</Tag>,
    },
    {
      title: 'Current Value',
      dataIndex: 'currentValue',
      width: 140,
      align: 'right',
      render: (v: number) => `$${v.toLocaleString('en-US', { minimumFractionDigits: 2 })}`,
    },
    {
      title: 'Previous Value',
      dataIndex: 'previousValue',
      width: 140,
      align: 'right',
      render: (v: number | null) =>
        v != null ? `$${v.toLocaleString('en-US', { minimumFractionDigits: 2 })}` : '—',
    },
    {
      title: 'Delta',
      dataIndex: 'delta',
      width: 120,
      align: 'right',
      render: (v: number | null) => {
        if (v == null) return '—';
        const color = v > 0 ? '#52c41a' : v < 0 ? '#ff4d4f' : undefined;
        const prefix = v > 0 ? '+' : '';
        return (
          <span style={{ color, fontWeight: 500 }}>
            {prefix}${v.toLocaleString('en-US', { minimumFractionDigits: 2 })}
          </span>
        );
      },
    },
    {
      title: 'Status',
      dataIndex: 'status',
      width: 120,
      render: (s: RecordStatus) => <RecordStatusTag status={s} />,
    },
  ];

  if (isLoading) return <Spin size="large" style={{ display: 'block', marginTop: 80 }} />;
  if (error || !detail) return <Alert type="error" message="Failed to load reconciliation details." />;

  const { job, report } = detail;
  const isInFlight = job.status === 'Pending' || job.status === 'Processing';

  const processingDuration = (() => {
    if (job.status !== 'Completed' || !job.completedAt) return null;
    const ms = new Date(job.completedAt).getTime() - new Date(job.createdAt).getTime();
    const totalSeconds = Math.round(ms / 1000);
    if (totalSeconds < 60) return `${totalSeconds}s`;
    const m = Math.floor(totalSeconds / 60);
    const s = totalSeconds % 60;
    return s > 0 ? `${m}m ${s}s` : `${m}m`;
  })();

  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      {/* Header */}
      <Card>
        <Row align="middle" gutter={16}>
          <Col>
            <Button icon={<ArrowLeftOutlined />} type="text" onClick={() => navigate('/')} />
          </Col>
          <Col flex={1}>
            <Title level={4} style={{ margin: 0 }}>{job.filename}</Title>
            <Space size="large">
              <Text type="secondary">Reference date: {job.referenceDate}</Text>
              {processingDuration && (
                <Text type="secondary">Processed in <Text strong>{processingDuration}</Text></Text>
              )}
            </Space>
          </Col>
          <Col>
            <Space>
              {isInFlight && <SyncOutlined spin style={{ color: '#1677ff' }} />}
              <JobStatusTag status={job.status} />
            </Space>
          </Col>
        </Row>
      </Card>

      {/* Processing state */}
      {isInFlight && (
        <Alert
          type="info"
          showIcon
          message="Processing in progress"
          description="The reconciliation engine is analysing this file. This page updates automatically."
        />
      )}

      {/* Summary stats */}
      {report && (
        <Row gutter={16}>
          {[
            { label: 'Total Records', value: report.totalRecords, color: undefined, fontSize: 20 },
            { label: 'Matched', value: report.matched, color: '#52c41a', fontSize: 20 },
            { label: 'Discrepant', value: report.discrepant, color: '#faad14', fontSize: 20 },
            { label: 'New', value: report.newRecords, color: '#1677ff', fontSize: 20 },
            { label: 'Missing', value: report.missingRecords, color: '#ff4d4f', fontSize: 20 },
            {
              label: 'Total Delta',
              value: `$${report.totalDelta.toLocaleString('en-US', { maximumFractionDigits: 0 })}`,
              color: report.totalDelta >= 0 ? '#52c41a' : '#ff4d4f',
              fontSize: 14,
            },
          ].map(({ label, value, color, fontSize }) => (
            <Col xs={12} sm={8} md={4} key={label}>
              <Card size="small">
                <Statistic title={label} value={value} valueStyle={{ color, fontSize }} />
              </Card>
            </Col>
          ))}
        </Row>
      )}

      {/* Charts */}
      {report && (
        <Row gutter={16}>
          <Col xs={24} md={12}>
            <Card title="Status Breakdown">
              <StatusBreakdownChart report={report} />
            </Card>
          </Col>
          <Col xs={24} md={12}>
            <Card title="Delta by Product Type">
              <DeltaOverviewChart records={records} />
            </Card>
          </Col>
        </Row>
      )}

      {/* Records table */}
      <Card
        title="Records"
        extra={
          <Space>
            <Select
              allowClear
              placeholder="Filter status"
              style={{ width: 140 }}
              onChange={(v) => setStatusFilter(v)}
              options={RECORD_STATUSES.map((s) => ({ value: s, label: s }))}
            />
            <Select
              allowClear
              placeholder="Filter product"
              style={{ width: 140 }}
              onChange={(v) => setProductFilter(v)}
              options={PRODUCT_TYPES.map((p) => ({ value: p, label: p }))}
            />
          </Space>
        }
      >
        <Table
          rowKey="id"
          columns={columns}
          dataSource={records}
          size="small"
          pagination={{ pageSize: 50, showTotal: (t) => `${t} records` }}
          scroll={{ x: 700 }}
        />
      </Card>
    </Space>
  );
}
