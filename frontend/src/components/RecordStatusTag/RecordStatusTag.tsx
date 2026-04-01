import { Tag } from 'antd';
import type { RecordStatus } from '../../api/types';

const STATUS_COLORS: Record<RecordStatus, string> = {
  Matched: 'success',
  Discrepant: 'warning',
  New: 'blue',
  Missing: 'error',
};

export function RecordStatusTag({ status }: { status: RecordStatus }) {
  return <Tag color={STATUS_COLORS[status]}>{status}</Tag>;
}
