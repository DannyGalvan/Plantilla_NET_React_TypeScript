import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { DownloadIcon, PaperclipIcon, Trash2Icon, UploadIcon } from 'lucide-react';
import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { filesApi, formatBytes } from '@/shared/api/files-api';
import { ApiError } from '@/shared/api/problem-details';
import { useQueryScope } from '@/shared/api/query-keys';
import { formatDate } from '@/shared/lib/format';
import { getActiveModules } from '@/shared/modules/active-modules';
import { Button } from '@/shared/ui';

/**
 * Files attached to a record. Drop it in any edit page:
 * `<Attachments entityType="catalog.product" entityId={product.id} canWrite={canUpdate} />`.
 * The backend applies the permissions and the tenant/unit scope of the record; this component renders nothing when
 * the Files module is not active.
 */
export function Attachments({
  entityType,
  entityId,
  canWrite,
}: {
  entityType: string;
  entityId: string;
  canWrite: boolean;
}) {
  const { t } = useTranslation();
  const input = useRef<HTMLInputElement>(null);
  const queryClient = useQueryClient();
  const enabled = getActiveModules().some((module) => module.id === 'files');
  const key = [...useQueryScope('files', 'attachments'), entityType, entityId];
  const files = useQuery({ queryKey: key, queryFn: () => filesApi.list({ entityType, entityId }), enabled });
  const refresh = () => queryClient.invalidateQueries({ queryKey: key });
  const onError = (error: unknown) => toast.error(ApiError.from(error).message);
  const upload = useMutation({
    mutationFn: (file: File) => filesApi.upload(file, entityType, entityId),
    onSuccess: refresh,
    onError,
  });
  const remove = useMutation({ mutationFn: filesApi.remove, onSuccess: refresh, onError });

  if (!enabled) {
    return null;
  }

  return (
    <section className="space-y-2" aria-label={t('attachments.title')}>
      <div className="flex items-center justify-between">
        <h3 className="flex items-center gap-2 text-sm font-medium">
          <PaperclipIcon className="size-4" aria-hidden />
          {t('attachments.title')}
        </h3>
        {canWrite && (
          <>
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
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={upload.isPending}
              onClick={() => input.current?.click()}
            >
              <UploadIcon aria-hidden />
              {upload.isPending ? t('attachments.uploading') : t('attachments.add')}
            </Button>
          </>
        )}
      </div>
      {files.data?.items.length ? (
        <ul className="divide-y rounded-md border text-sm">
          {files.data.items.map((file) => (
            <li key={file.id} className="flex items-center justify-between gap-2 px-3 py-2">
              <div className="min-w-0">
                <p className="truncate font-medium">{file.fileName}</p>
                <p className="text-xs text-muted-foreground">
                  {formatBytes(file.length)} · {formatDate(file.createdAt)}
                </p>
              </div>
              <div className="flex shrink-0 gap-1">
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  aria-label={t('attachments.download')}
                  onClick={() => void filesApi.download(file).catch(onError)}
                >
                  <DownloadIcon aria-hidden />
                </Button>
                {canWrite && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    aria-label={t('actions.delete')}
                    onClick={() => remove.mutate(file.id)}
                  >
                    <Trash2Icon aria-hidden />
                  </Button>
                )}
              </div>
            </li>
          ))}
        </ul>
      ) : (
        <p className="text-sm text-muted-foreground">
          {files.isLoading ? t('common.loading') : t('attachments.empty')}
        </p>
      )}
    </section>
  );
}
