import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ReferenceLine,
  ResponsiveContainer,
  Cell,
} from 'recharts';
import type { ReconciliationRecord } from '../../api/types';

interface Props {
  records: ReconciliationRecord[];
}

export function DeltaOverviewChart({ records }: Props) {
  // Aggregate total delta per product type
  const deltaByProduct = records.reduce<Record<string, number>>((acc, r) => {
    if (r.delta != null) {
      acc[r.productType] = (acc[r.productType] ?? 0) + r.delta;
    }
    return acc;
  }, {});

  const data = Object.entries(deltaByProduct).map(([productType, delta]) => ({
    productType,
    delta: Math.round(delta * 100) / 100,
  }));

  if (data.length === 0) {
    return <div style={{ textAlign: 'center', color: '#999', padding: 32 }}>No delta data available</div>;
  }

  return (
    <ResponsiveContainer width="100%" height={260}>
      <BarChart data={data} margin={{ top: 8, right: 16, left: 8, bottom: 8 }}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="productType" />
        <YAxis tickFormatter={(v) => `$${v.toLocaleString()}`} />
        <Tooltip formatter={(v) => [`$${Number(v).toLocaleString()}`, 'Delta']} />
        <ReferenceLine y={0} stroke="#666" />
        <Bar dataKey="delta" name="Delta">
          {data.map((entry) => (
            <Cell key={entry.productType} fill={entry.delta >= 0 ? '#52c41a' : '#ff4d4f'} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}
