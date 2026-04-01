import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import type { ReconciliationReport } from '../../api/types';

const COLORS = {
  Matched: '#52c41a',
  Discrepant: '#faad14',
  New: '#1677ff',
  Missing: '#ff4d4f',
};

export function StatusBreakdownChart({ report }: { report: ReconciliationReport }) {
  const data = [
    { name: 'Matched', value: report.matched },
    { name: 'Discrepant', value: report.discrepant },
    { name: 'New', value: report.newRecords },
    { name: 'Missing', value: report.missingRecords },
  ].filter((d) => d.value > 0);

  return (
    <ResponsiveContainer width="100%" height={260}>
      <PieChart>
        <Pie data={data} dataKey="value" nameKey="name" outerRadius={80} label>
          {data.map((entry) => (
            <Cell key={entry.name} fill={COLORS[entry.name as keyof typeof COLORS]} />
          ))}
        </Pie>
        <Tooltip formatter={(v) => [`${v} records`, '']} />
        <Legend />
      </PieChart>
    </ResponsiveContainer>
  );
}
