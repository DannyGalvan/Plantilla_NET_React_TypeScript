import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { ApiError } from '@/shared/api/problem-details';
import { useQueryScope } from '@/shared/api/query-keys';
import { PageHeader } from '@/shared/components/page';
import { Badge, Button, Card, CardContent, CardHeader, CardTitle, Input, Label, Textarea } from '@/shared/ui';

import { notificationsApi, type NotificationTemplate } from '../api';

/** Tenant administrators adjust the texts of every template declared by the modules. */
export function TemplatesPage() {
  const { t } = useTranslation('notifications');
  const scope = useQueryScope('notifications', 'templates');
  const templates = useQuery({ queryKey: scope, queryFn: notificationsApi.templates });
  const test = useMutation({
    mutationFn: notificationsApi.sendTest,
    onSuccess: () => toast.success(t('templates.testSent')),
    onError: (error) => toast.error(ApiError.from(error).message),
  });

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('templates.title')}
        description={t('templates.description')}
        actions={
          <Button variant="outline" disabled={test.isPending} onClick={() => test.mutate()}>
            {t('templates.sendTest')}
          </Button>
        }
      />
      <div className="grid gap-4 lg:grid-cols-2">
        {templates.data?.map((template) => (
          <TemplateCard key={`${template.key}:${template.language}`} template={template} scope={scope} />
        ))}
      </div>
    </div>
  );
}

function TemplateCard({ template, scope }: { template: NotificationTemplate; scope: readonly unknown[] }) {
  const { t } = useTranslation('notifications');
  const queryClient = useQueryClient();
  const [subject, setSubject] = useState(template.subject);
  const [body, setBody] = useState(template.body);
  const id = `${template.key}-${template.language}`;
  const save = useMutation({
    mutationFn: (values: { subject: string; body: string }) =>
      notificationsApi.updateTemplate(template.key, template.language, values.subject, values.body),
    onSuccess: async () => {
      toast.success(t('common:common.saved'));
      await queryClient.invalidateQueries({ queryKey: scope });
    },
    onError: (error) => toast.error(ApiError.from(error).message),
  });

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between gap-2">
        <CardTitle className="font-mono text-sm">
          {template.key} · {template.language}
        </CardTitle>
        {template.customized && <Badge variant="secondary">{t('templates.customized')}</Badge>}
      </CardHeader>
      <CardContent>
        <form
          className="space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            save.mutate({ subject, body });
          }}
        >
          <div className="space-y-2">
            <Label htmlFor={`${id}-subject`}>{t('templates.subject')}</Label>
            <Input
              id={`${id}-subject`}
              value={subject}
              maxLength={256}
              onChange={(event) => setSubject(event.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor={`${id}-body`}>{t('templates.body')}</Label>
            <Textarea
              id={`${id}-body`}
              rows={4}
              value={body}
              maxLength={4000}
              onChange={(event) => setBody(event.target.value)}
            />
          </div>
          {template.placeholders.length > 0 && (
            <p className="text-xs text-muted-foreground">
              {t('templates.placeholders')}: {template.placeholders.map((name) => `{{${name}}}`).join(', ')}
            </p>
          )}
          <div className="flex justify-end gap-2">
            {template.customized && (
              <Button type="button" variant="ghost" onClick={() => save.mutate({ subject: '', body: '' })}>
                {t('templates.reset')}
              </Button>
            )}
            <Button type="submit" disabled={save.isPending}>
              {t('common:actions.save')}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
