import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnDef } from '@tanstack/react-table';
import { DownloadIcon, Trash2Icon, UploadIcon } from 'lucide-react';
import { useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { filesApi, formatBytes, type StoredFile } from '@/shared/api/files-api';
import { ApiError } from '@/shared/api/problem-details';
import { useQueryScope } from '@/shared/api/query-keys';
import { Can, usePermission } from '@/shared/auth/permissions';
import { ConfirmDialog } from '@/shared/components/confirm-dialog';
import { DataTable, useTableState } from '@/shared/components/data-table';
import { PageHeader } from '@/shared/components/page';
import { formatDate } from '@/shared/lib/format';
import { Button } from '@/shared/ui';

/** Library of the tenant: files that do not belong to a specific record. */
export function FilesPage() {
  const { t } = useTranslation('files');
  const input = useRef<HTMLInputElement>(null);
  const queryClient = useQueryClient();
  const scope = useQueryScope('files', 'library');
  const [state, setState] = useTableState();
  const [deleting, setDeleting] = useState<StoredFile | null>(null);
  const canDelete = usePermission('files.library.delete');
  const list = useQuery({
    queryKey: [...scope, state],
    queryFn: () => filesApi.list({ page: state.page, pageSize: state.pageSize, search: state.q }),
    placeholderData: keepPreviousData,
  });
  const onError = (error: unknown) => toast.error(ApiError.from(error).message);
  const refresh = () => queryClient.invalidateQueries({ queryKey: scope });
  const upload = useMutation({ mutationFn: (file: File) => filesApi.upload(file), onSuccess: refresh, onError });
  const remove = useMutation({
    mutationFn: filesApi.remove,
    onSuccess: async () => {
      toast.success(t('common:common.deleted'));
      await refresh();
    },
    onError,
  });

  const columns = useMemo<ColumnDef<StoredFile, unknown>[]>(
    () => [
      { accessorKey: 'fileName', header: t('common:common.name') },
      { id: 'size', header: t('library.size'), cell: ({ row }) => formatBytes(row.original.length) },
      {
        id: 'provider',
        header: t('library.provider'),
        cell: ({ row }) => t(`storage.providers.${row.original.provider}`),
      },
      {
        accessorKey: 'createdAt',
        header: t('common:common.createdAt'),
        cell: ({ row }) => formatDate(row.original.createdAt),
      },
      {
        id: 'actions',
        header: '',
        cell: ({ row }) => (
          <div className="flex justify-end gap-1">
            <Button
              variant="ghost"
              size="icon"
              aria-label={t('common:attachments.download')}
              onClick={() => void filesApi.download(row.original).catch(onError)}
            >
              <DownloadIcon aria-hidden />
            </Button>
            {canDelete && (
              <Button
                variant="ghost"
                size="icon"
                aria-label={t('common:actions.delete')}
                onClick={() => setDeleting(row.original)}
              >
                <Trash2Icon aria-hidden />
              </Button>
            )}
          </div>
        ),
      },
    ],
    [t, canDelete],
  );

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('library.title')}
        description={t('library.description')}
        actions={
          <Can permission="files.library.upload">
            <input
              ref={input}
              type="file"
              className="hidden"
              onChange={(event) => {
                const file = event.target.files?.[0];
                if (file) {
                  upload.mutate(file);
                }

                event.target.value = '';
              }}
            />
            <Button disabled={upload.isPending} onClick={() => input.current?.click()}>
              <UploadIcon aria-hidden />
              {t('library.upload')}
            </Button>
          </Can>
        }
      />
      <DataTable
        id="files-library"
        columns={columns}
        data={list.data}
        isLoading={list.isLoading}
        error={list.error}
        onRetry={() => void list.refetch()}
        state={state}
        onStateChange={setState}
        searchable
      />
      <ConfirmDialog
        open={deleting !== null}
        onOpenChange={(open) => !open && setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
      />
    </div>
  );
}
