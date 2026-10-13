import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PlayIcon } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { ApiError } from '@/shared/api/problem-details';
import { useQueryScope } from '@/shared/api/query-keys';
import { PageHeader } from '@/shared/components/page';
import { formatDate } from '@/shared/lib/format';
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Input,
  Label,
  Switch,
} from '@/shared/ui';

import { jobsApi, type Job } from '../api';

export function JobsPage() {
  const { t } = useTranslation('jobs');
  const scope = useQueryScope('jobs');
  const jobs = useQuery({ queryKey: scope, queryFn: jobsApi.list, refetchInterval: 15_000 });

  return (
    <div className="space-y-6">
      <PageHeader title={t('title')} description={t('description')} />
      <div className="grid gap-4 lg:grid-cols-2">
        {jobs.data?.map((job) => (
          <JobCard key={job.name} job={job} scope={scope} />
        ))}
      </div>
    </div>
  );
}

function JobCard({ job, scope }: { job: Job; scope: readonly unknown[] }) {
  const { t } = useTranslation('jobs');
  const queryClient = useQueryClient();
  const [cron, setCron] = useState(job.cron ?? '');
  const [showRuns, setShowRuns] = useState(false);
  const refresh = () => queryClient.invalidateQueries({ queryKey: scope });
  const onError = (error: unknown) => toast.error(ApiError.from(error).message);
  const update = useMutation({
    mutationFn: (values: { enabled: boolean; cron: string }) =>
      jobsApi.update(job.name, values.enabled, values.cron || null),
    onSuccess: async () => {
      toast.success(t('common:common.saved'));
      await refresh();
    },
    onError,
  });
  const run = useMutation({
    mutationFn: () => jobsApi.run(job.name),
    onSuccess: () => toast.success(t('queued')),
    onError,
  });
  const runs = useQuery({
    queryKey: [...scope, job.name, 'runs'],
    queryFn: () => jobsApi.runs(job.name),
    enabled: showRuns,
  });
  const id = `job-${job.name.replace(/\W/g, '-')}`;

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between gap-2">
        <div className="min-w-0 space-y-1">
          <CardTitle className="font-mono text-sm">{job.name}</CardTitle>
          {job.description && <CardDescription>{job.description}</CardDescription>}
        </div>
        {job.lastStatus && (
          <Badge variant={job.lastStatus === 'failed' ? 'destructive' : 'secondary'}>
            {t(`statuses.${job.lastStatus}`)}
          </Badge>
        )}
      </CardHeader>
      <CardContent className="space-y-3 text-sm">
        <dl className="grid grid-cols-2 gap-2 text-xs">
          <dt className="text-muted-foreground">{t('nextRun')}</dt>
          <dd>{job.enabled && job.nextRunAt ? formatDate(job.nextRunAt) : '-'}</dd>
          <dt className="text-muted-foreground">{t('lastRun')}</dt>
          <dd>{job.lastFinishedAt ? formatDate(job.lastFinishedAt) : '-'}</dd>
        </dl>
        {job.lastError && <p className="text-xs break-words text-destructive">{job.lastError}</p>}
        <form
          className="space-y-2"
          onSubmit={(event) => {
            event.preventDefault();
            update.mutate({ enabled: job.enabled, cron });
          }}
        >
          <Label htmlFor={`${id}-cron`}>{t('schedule')}</Label>
          <div className="flex gap-2">
            <Input
              id={`${id}-cron`}
              className="font-mono"
              placeholder={job.defaultCron}
              value={cron}
              onChange={(event) => setCron(event.target.value)}
            />
            <Button type="submit" variant="outline" disabled={update.isPending}>
              {t('common:actions.save')}
            </Button>
          </div>
          <p className="text-xs text-muted-foreground">{t('scheduleHint', { cron: job.defaultCron })}</p>
        </form>
        <div className="flex flex-wrap items-center justify-between gap-2">
          <Label className="flex items-center gap-2 font-normal">
            <Switch checked={job.enabled} onCheckedChange={(enabled) => update.mutate({ enabled, cron })} />
            {t('enabled')}
          </Label>
          <div className="flex gap-2">
            <Button variant="ghost" size="sm" onClick={() => setShowRuns((value) => !value)}>
              {t('history')}
            </Button>
            <Button size="sm" disabled={run.isPending} onClick={() => run.mutate()}>
              <PlayIcon aria-hidden />
              {t('runNow')}
            </Button>
          </div>
        </div>
        {showRuns && (
          <ul className="divide-y rounded-md border text-xs">
            {runs.data?.length ? (
              runs.data.map((entry) => (
                <li key={entry.id} className="flex items-center justify-between gap-2 px-3 py-2">
                  <span>{formatDate(entry.startedAt)}</span>
                  <span className={entry.status === 'failed' ? 'text-destructive' : 'text-muted-foreground'}>
                    {t(`statuses.${entry.status}`)}
                  </span>
                </li>
              ))
            ) : (
              <li className="px-3 py-2 text-muted-foreground">{t('noRuns')}</li>
            )}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
