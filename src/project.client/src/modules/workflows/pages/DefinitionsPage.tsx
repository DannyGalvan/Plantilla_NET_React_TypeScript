import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PlusIcon, Trash2Icon } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { ApiError } from '@/shared/api/problem-details';
import { useQueryScope } from '@/shared/api/query-keys';
import { PageHeader } from '@/shared/components/page';
import { Badge, Button, Card, CardContent, CardHeader, CardTitle, Input, NativeSelect } from '@/shared/ui';

import { workflowsApi, type ApprovalStep, type Workflow } from '../api';

/** Tenant administrators adjust the steps of the flows declared by the modules. */
export function DefinitionsPage() {
  const { t } = useTranslation('workflows');
  const scope = useQueryScope('workflows', 'definitions');
  const definitions = useQuery({ queryKey: scope, queryFn: workflowsApi.definitions });

  return (
    <div className="space-y-6">
      <PageHeader title={t('definitions.title')} description={t('definitions.description')} />
      {definitions.data?.map((workflow) => (
        <WorkflowEditor key={workflow.key} workflow={workflow} scope={scope} />
      ))}
    </div>
  );
}

function WorkflowEditor({ workflow, scope }: { workflow: Workflow; scope: readonly unknown[] }) {
  const { t } = useTranslation('workflows');
  const queryClient = useQueryClient();
  const [steps, setSteps] = useState<ApprovalStep[]>(workflow.steps);
  const save = useMutation({
    mutationFn: (value: ApprovalStep[] | null) => workflowsApi.updateDefinition(workflow.key, value),
    onSuccess: async () => {
      toast.success(t('common:common.saved'));
      await queryClient.invalidateQueries({ queryKey: scope });
    },
    onError: (error) => toast.error(ApiError.from(error).message),
  });
  const change = (index: number, patch: Partial<ApprovalStep>) =>
    setSteps((current) => current.map((step, i) => (i === index ? { ...step, ...patch } : step)));

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between gap-2">
        <CardTitle className="text-base">
          {workflow.name} <span className="font-mono text-xs text-muted-foreground">{workflow.key}</span>
        </CardTitle>
        {workflow.customized && <Badge variant="secondary">{t('definitions.customized')}</Badge>}
      </CardHeader>
      <CardContent className="space-y-3">
        {steps.map((step, index) => (
          <div key={index} className="grid gap-2 sm:grid-cols-[1fr_9rem_1fr_8rem_auto]">
            <Input
              aria-label={t('definitions.name')}
              placeholder={t('definitions.name')}
              value={step.name}
              onChange={(event) => change(index, { name: event.target.value })}
            />
            <NativeSelect
              aria-label={t('definitions.kind')}
              value={step.kind}
              onChange={(event) => change(index, { kind: event.target.value as ApprovalStep['kind'] })}
            >
              {(['Permission', 'Role', 'User'] as const).map((kind) => (
                <option key={kind} value={kind}>
                  {t(`definitions.kinds.${kind}`)}
                </option>
              ))}
            </NativeSelect>
            <Input
              aria-label={t('definitions.approver')}
              placeholder={t('definitions.approver')}
              className="font-mono"
              value={step.approver}
              onChange={(event) => change(index, { approver: event.target.value })}
            />
            <Input
              aria-label={t('definitions.minAmount')}
              placeholder={t('definitions.minAmount')}
              type="number"
              min={0}
              value={step.minAmount ?? ''}
              onChange={(event) =>
                change(index, { minAmount: event.target.value === '' ? null : Number(event.target.value) })
              }
            />
            <Button
              variant="ghost"
              size="icon"
              aria-label={t('common:actions.delete')}
              disabled={steps.length === 1}
              onClick={() => setSteps((current) => current.filter((_, i) => i !== index))}
            >
              <Trash2Icon aria-hidden />
            </Button>
          </div>
        ))}
        <div className="flex flex-wrap justify-between gap-2">
          <Button
            variant="outline"
            size="sm"
            disabled={steps.length >= 10}
            onClick={() =>
              setSteps((current) => [...current, { name: '', kind: 'Permission', approver: '', minAmount: null }])
            }
          >
            <PlusIcon aria-hidden />
            {t('definitions.addStep')}
          </Button>
          <div className="flex gap-2">
            {workflow.customized && (
              <Button
                variant="ghost"
                onClick={() => {
                  setSteps(workflow.defaultSteps);
                  save.mutate(null);
                }}
              >
                {t('definitions.restore')}
              </Button>
            )}
            <Button disabled={save.isPending} onClick={() => save.mutate(steps)}>
              {t('common:actions.save')}
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
