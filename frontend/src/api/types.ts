export type JobStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed';
export type RecordStatus = 'Matched' | 'Discrepant' | 'New' | 'Missing';
export type ProductType = 'Equity' | 'Fund' | 'Crypto' | 'Bond';

export interface ReconciliationJob {
  id: string;
  filename: string;
  status: JobStatus;
  referenceDate: string;
  createdAt: string;
  completedAt: string | null;
}

export interface ReconciliationRecord {
  id: string;
  clientId: string;
  productType: ProductType;
  currentValue: number;
  previousValue: number | null;
  delta: number | null;
  status: RecordStatus;
}

export interface ReconciliationReport {
  id: string;
  jobId: string;
  totalRecords: number;
  matched: number;
  discrepant: number;
  newRecords: number;
  missingRecords: number;
  totalDelta: number;
  generatedAt: string;
}

export interface ReconciliationDetail {
  job: ReconciliationJob;
  report: ReconciliationReport | null;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ApiError {
  code: string;
  message: string;
}
