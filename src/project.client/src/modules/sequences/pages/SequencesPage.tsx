import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { ApiError } from '@/shared/api/problem-details';
import { useQueryScope } from '@/shared/api/query-keys';
import { PageHeader } from '@/shared/components/page';
import { Badge, Button, Card, CardContent, CardDescription, CardHeader, CardTitle, Input, Label } from '@/shared/ui';

import { sequencesApi, type Sequence } from '../api';

export function SequencesPage() {
  const { t } = useTranslation('sequences');
  const scope = useQueryScope('sequences');
  const sequences = useQuery({ queryKey: scope, queryFn: sequencesApi.list });

  return (
    <div className="space-y-6">
      <PageHeader title={t('title')} description={t('description')} />
      <div className="grid gap-4 lg:grid-cols-2">
        {sequences.data?.map((sequence) => (
          <SequenceCard key={sequence.name} sequence={sequence} scope={scope} />
        ))}
      </div>
    </div>
  );
}

function SequenceCard({ sequence, scope }: { sequence: Sequence; scope: readonly unknown[] }) {
  const { t } = useTranslation('sequences');
  const queryClient = useQueryClient();
  const [format, setFormat] = useState(sequence.format);
  const id = `sequence-${sequence.name.replace(/\W/g, '-')}`;
  const save = useMutation({
    mutationFn: (value: string | null) => sequencesApi.update(sequence.name, value),
    onSuccess: async () => {
      toast.success(t('common:common.saved'));
      await queryClient.invalidateQueries({ queryKey: scope });
    },
    onError: (error) => toast.error(ApiError.from(error).message),
  });

  return (
    <Card>
      <CardHeader className="space-y-1">
        <CardTitle className="font-mono text-sm">{sequence.name}</CardTitle>
        {sequence.description && <CardDescription>{sequence.description}</CardDescription>}
      </CardHeader>
      <CardContent className="space-y-3 text-sm">
        <div className="flex flex-wrap gap-2">
          <Badge variant="secondary">
            {t('reset')}: {t(`resets.${sequence.reset}`)}
          </Badge>
          {sequence.perOrganizationUnit && <Badge variant="secondary">{t('perUnit')}</Badge>}
        </div>
        <form
          className="space-y-2"
          onSubmit={(event) => {
            event.preventDefault();
            save.mutate(format);
          }}
        >
          <Label htmlFor={id}>{t('format')}</Label>
          <div className="flex gap-2">
            <Input
              id={id}
              className="font-mono"
              maxLength={64}
              value={format}
              onChange={(event) => setFormat(event.target.value)}
            />
            <Button type="submit" variant="outline" disabled={save.isPending}>
              {t('common:actions.save')}
            </Button>
          </div>
        </form>
        <p className="text-xs text-muted-foreground">
          {t('example')}: <span className="font-mono">{sequence.example}</span>
        </p>
        {sequence.format !== sequence.defaultFormat && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              setFormat(sequence.defaultFormat);
              save.mutate(null);
            }}
          >
            {t('restore')}
          </Button>
        )}
      </CardContent>
    </Card>
  );
}
