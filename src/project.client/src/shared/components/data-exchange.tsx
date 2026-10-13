import { DownloadIcon, UploadIcon } from 'lucide-react';
import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { z } from 'zod';

import type { ListParams } from '@/shared/api/crud-api';
import { http } from '@/shared/api/http-client';
import { ApiError } from '@/shared/api/problem-details';
import { usePermission } from '@/shared/auth/permissions';
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/shared/ui';

const reportSchema = z.object({
  rows: z.number(),
  created: z.number(),
  dryRun: z.boolean(),
  succeeded: z.boolean(),
  errors: z.array(z.object({ row: z.number(), errors: z.array(z.string()) })),
});

type ImportReport = z.infer<typeof reportSchema>;

async function download(resource: string, format: 'csv' | 'xlsx', params: ListParams) {
  const response = await http.get<Blob>(`${resource}/export`, {
    params: { format, sort: params.sort, filter: params.filter },
    responseType: 'blob',
  });
  const name = /filename="?([^";]+)"?/.exec(String(response.headers['content-disposition'] ?? ''))?.[1];
  const url = URL.createObjectURL(response.data);
  const link = document.createElement('a');
  link.href = url;
  link.download = name ?? `export.${format}`;
  link.click();
  URL.revokeObjectURL(url);
}

async function upload(resource: string, file: File, dryRun: boolean): Promise<ImportReport> {
  const form = new FormData();
  form.append('file', file);
  // 422 carries the per-row report: read it instead of treating it as an error.
  const response = await http.post(`${resource}/import`, form, {
    params: { dryRun },
    validateStatus: (status) => status === 200 || status === 422,
  });
  return reportSchema.parse(response.data);
}

/**
 * Export (CSV or Excel, with the current filter and sort) and import (validated first, then saved all or nothing)
 * for any generic CRUD resource: `<DataExchange resource="/catalog/products" params={params} permission="catalog.products" />`.
 */
export function DataExchange({
  resource,
  params,
  permission,
  onImported,
}: {
  resource: string;
  params: ListParams;
  /** Permission prefix of the resource: export needs `.read`, import needs `.create`. */
  permission: string;
  onImported?: () => void;
}) {
  const { t } = useTranslation();
  const canExport = usePermission(`${permission}.read`);
  const canImport = usePermission(`${permission}.create`);
  const input = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [report, setReport] = useState<ImportReport | null>(null);
  const [busy, setBusy] = useState(false);

  const run = async (dryRun: boolean) => {
    if (!file) {
      return;
    }

    setBusy(true);
    try {
      const result = await upload(resource, file, dryRun);
      setReport(result);
      if (!dryRun && result.succeeded) {
        toast.success(t('dataExchange.imported', { count: result.created }));
        setFile(null);
        onImported?.();
      }
    } catch (error) {
      toast.error(ApiError.from(error).message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      {canExport && (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline">
              <DownloadIcon aria-hidden />
              {t('dataExchange.export')}
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            {(['xlsx', 'csv'] as const).map((format) => (
              <DropdownMenuItem
                key={format}
                onSelect={() =>
                  void download(resource, format, params).catch((error: unknown) =>
                    toast.error(ApiError.from(error).message),
                  )
                }
              >
                {t(`dataExchange.formats.${format}`)}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
      )}
      {canImport && (
        <>
          <input
            ref={input}
            type="file"
            accept=".csv,.xlsx"
            className="hidden"
            onChange={(event) => {
              setFile(event.target.files?.[0] ?? null);
              setReport(null);
              event.target.value = '';
            }}
          />
          <Button variant="outline" onClick={() => input.current?.click()}>
            <UploadIcon aria-hidden />
            {t('dataExchange.import')}
          </Button>
          <Dialog
            open={file !== null}
            onOpenChange={(open) => {
              if (!open) {
                setFile(null);
                setReport(null);
              }
            }}
          >
            <DialogContent>
              <DialogHeader>
                <DialogTitle>{t('dataExchange.import')}</DialogTitle>
                <DialogDescription>{t('dataExchange.hint', { file: file?.name ?? '' })}</DialogDescription>
              </DialogHeader>
              {report && (
                <div className="space-y-2 text-sm">
                  {report.succeeded ? (
                    <p>
                      {report.dryRun
                        ? t('dataExchange.valid', { count: report.rows })
                        : t('dataExchange.imported', { count: report.created })}
                    </p>
                  ) : (
                    <>
                      <p className="text-destructive">{t('dataExchange.invalid', { count: report.errors.length })}</p>
                      <ul className="max-h-60 space-y-1 overflow-y-auto rounded-md border p-2 text-xs">
                        {report.errors.map((error) => (
                          <li key={error.row}>
                            <span className="font-medium">{t('dataExchange.row', { row: error.row })}</span>:{' '}
                            {error.errors.join(' · ')}
                          </li>
                        ))}
                      </ul>
                    </>
                  )}
                </div>
              )}
              <DialogFooter>
                <Button variant="outline" disabled={busy} onClick={() => void run(true)}>
                  {t('dataExchange.validate')}
                </Button>
                <Button disabled={busy || (report !== null && !report.succeeded)} onClick={() => void run(false)}>
                  {t('dataExchange.import')}
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        </>
      )}
    </>
  );
}
