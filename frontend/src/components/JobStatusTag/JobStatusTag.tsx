import { Tag } from 'antd';
import type { JobStatus } from '../../api/types';

const STATUS_COLORS: Record<JobStatus, string> = {
  Pending: 'default',
  Processing: 'processing',
  Completed: 'success',
  Failed: 'error',
};

export function JobStatusTag({ status }: { status: JobStatus }) {
  return <Tag color={STATUS_COLORS[status]}>{status}</Tag>;
}
