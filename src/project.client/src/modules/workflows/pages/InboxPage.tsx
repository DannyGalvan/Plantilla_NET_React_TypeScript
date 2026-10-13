import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { toast } from 'sonner';

import { ApiError } from '@/shared/api/problem-details';
import { useQueryScope } from '@/shared/api/query-keys';
import { EmptyState, PageHeader } from '@/shared/components/page';
import { formatDate, formatNumber } from '@/shared/lib/format';
import { Badge, Button, Card, CardContent, CardHeader, CardTitle, Textarea } from '@/shared/ui';

import { workflowsApi, type ApprovalRequest } from '../api';

/** Requests waiting for the user's decision, and the requests he submitted. */
export function InboxPage() {
  const { t } = useTranslation('workflows');
  const scope = useQueryScope('workflows', 'requests');
  const inbox = useQuery({ queryKey: [...scope, 'inbox'], queryFn: workflowsApi.inbox, refetchInterval: 30_000 });
  const mine = useQuery({ queryKey: [...scope, 'mine'], queryFn: workflowsApi.mine });

  return (
    <div className="space-y-6">
      <PageHeader title={t('inbox.title')} description={t('inbox.description')} />
      <section className="space-y-3">
        <h2 className="text-lg font-semibold">{t('inbox.pending')}</h2>
        {inbox.data?.length ? (
          inbox.data.map((request) => <RequestCard key={request.id} request={request} scope={scope} />)
        ) : (
          <EmptyState title={t('inbox.empty')} />
        )}
      </section>
      <section className="space-y-3">
        <h2 className="text-lg font-semibold">{t('inbox.mine')}</h2>
        {mine.data?.length ? (
          mine.data.map((request) => <RequestCard key={request.id} request={request} scope={scope} own />)
        ) : (
          <EmptyState title={t('inbox.empty')} />
        )}
      </section>
    </div>
  );
}

function RequestCard({
  request,
  scope,
  own = false,
}: {
  request: ApprovalRequest;
  scope: readonly unknown[];
  own?: boolean;
}) {
  const { t } = useTranslation('workflows');
  const queryClient = useQueryClient();
  const [comment, setComment] = useState('');
  const step = request.steps[request.currentStep];
  const decide = useMutation({
    mutationFn: async (decision: 'approve' | 'reject' | 'cancel') => {
      if (decision === 'reject' && !comment.trim()) {
        throw new Error(t('inbox.commentRequired'));
      }

      if (decision === 'approve') {
        await workflowsApi.approve(request.id, comment);
      } else if (decision === 'reject') {
        await workflowsApi.reject(request.id, comment);
      } else {
        await workflowsApi.cancel(request.id);
      }
    },
    onSuccess: async () => {
      toast.success(t('inbox.done'));
      setComment('');
      await queryClient.invalidateQueries({ queryKey: scope });
    },
    onError: (error) =>
      toast.error(
        error instanceof ApiError || !(error instanceof Error) ? ApiError.from(error).message : error.message,
      ),
  });

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between gap-2">
        <div className="min-w-0 space-y-1">
          <CardTitle className="truncate text-base">{request.title}</CardTitle>
          <p className="text-xs text-muted-foreground">
            {request.workflow} · {formatDate(request.requestedAt)}
            {request.amount !== null && ` · ${t('inbox.amount')}: ${formatNumber(request.amount)}`}
          </p>
        </div>
        <Badge variant={request.status === 'Rejected' ? 'destructive' : 'secondary'}>
          {t(`statuses.${request.status}`)}
        </Badge>
      </CardHeader>
      <CardContent className="space-y-3 text-sm">
        {request.status === 'Pending' && step && (
          <p>{t('inbox.step', { current: request.currentStep + 1, total: request.steps.length, name: step.name })}</p>
        )}
        {request.decisions.length > 0 && (
          <ul className="space-y-1 text-xs text-muted-foreground">
            {request.decisions.map((decision) => (
              <li key={`${decision.step}-${decision.at}`}>
                {request.steps[decision.step]?.name}: {t(`statuses.${decision.decision}`)} · {formatDate(decision.at)}
                {decision.comment && ` · "${decision.comment}"`}
              </li>
            ))}
          </ul>
        )}
        {request.canDecide && (
          <div className="space-y-2">
            <Textarea
              aria-label={t('inbox.comment')}
              placeholder={t('inbox.comment')}
              rows={2}
              maxLength={1000}
              value={comment}
              onChange={(event) => setComment(event.target.value)}
            />
            <div className="flex justify-end gap-2">
              <Button variant="outline" disabled={decide.isPending} onClick={() => decide.mutate('reject')}>
                {t('inbox.reject')}
              </Button>
              <Button disabled={decide.isPending} onClick={() => decide.mutate('approve')}>
                {t('inbox.approve')}
              </Button>
            </div>
          </div>
        )}
        <div className="flex justify-end gap-2">
          {request.link && (
            <Button variant="link" size="sm" asChild>
              <Link to={request.link}>{t('inbox.open')}</Link>
            </Button>
          )}
          {own && request.status === 'Pending' && (
            <Button variant="ghost" size="sm" disabled={decide.isPending} onClick={() => decide.mutate('cancel')}>
              {t('inbox.cancel')}
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
