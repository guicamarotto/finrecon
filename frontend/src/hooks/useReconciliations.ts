import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { reconciliationsApi } from '../api/reconciliations';
import type { JobStatus, RecordStatus, ProductType } from '../api/types';

export function useReconciliations(page: number, pageSize: number, status?: JobStatus) {
  return useQuery({
    queryKey: ['reconciliations', page, pageSize, status],
    queryFn: () => reconciliationsApi.list({ page, pageSize, status }),
    staleTime: 30_000,
  });
}

export function useReconciliationDetail(jobId: string) {
  return useQuery({
    queryKey: ['reconciliation', jobId],
    queryFn: () => reconciliationsApi.get(jobId),
    // Poll every 3 seconds while job is in-flight; stop when done
    refetchInterval: (query) => {
      const status = query.state.data?.job.status;
      return status === 'Pending' || status === 'Processing' ? 3000 : false;
    },
  });
}

export function useReconciliationRecords(
  jobId: string,
  filters?: { status?: RecordStatus; productType?: ProductType }
) {
  return useQuery({
    queryKey: ['reconciliation-records', jobId, filters],
    queryFn: () => reconciliationsApi.getRecords(jobId, filters),
  });
}

export function useUploadReconciliation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ file, referenceDate }: { file: File; referenceDate: string }) =>
      reconciliationsApi.upload(file, referenceDate),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['reconciliations'] });
    },
  });
}
