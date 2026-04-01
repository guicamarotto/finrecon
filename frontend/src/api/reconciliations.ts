import { apiClient } from './client';
import type {
  JobStatus,
  PagedResult,
  ReconciliationDetail,
  ReconciliationJob,
  ReconciliationRecord,
  RecordStatus,
  ProductType,
} from './types';

export const reconciliationsApi = {
  list: async (params: {
    page: number;
    pageSize: number;
    status?: JobStatus;
  }): Promise<PagedResult<ReconciliationJob>> => {
    const { data } = await apiClient.get('/api/reconciliations', { params });
    return data;
  },

  get: async (jobId: string): Promise<ReconciliationDetail> => {
    const { data } = await apiClient.get(`/api/reconciliations/${jobId}`);
    return data;
  },

  getRecords: async (
    jobId: string,
    filters?: { status?: RecordStatus; productType?: ProductType }
  ): Promise<ReconciliationRecord[]> => {
    const { data } = await apiClient.get(`/api/reconciliations/${jobId}/records`, {
      params: filters,
    });
    return data;
  },

  upload: async (file: File, referenceDate: string): Promise<{ jobId: string; status: string }> => {
    const form = new FormData();
    form.append('file', file);
    form.append('referenceDate', referenceDate);
    const { data } = await apiClient.post('/api/reconciliations', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return data;
  },
};

export const authApi = {
  login: async (email: string, password: string): Promise<{ token: string; email: string }> => {
    const { data } = await apiClient.post('/api/auth/login', { email, password });
    return data;
  },

  register: async (email: string, password: string): Promise<{ token: string; email: string }> => {
    const { data } = await apiClient.post('/api/auth/register', { email, password });
    return data;
  },
};
