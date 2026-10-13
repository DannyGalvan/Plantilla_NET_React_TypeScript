import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PlusIcon, RotateCcwIcon, Trash2Icon } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { ApiError } from '@/shared/api/problem-details';
import { useQueryScope } from '@/shared/api/query-keys';
import { PageHeader } from '@/shared/components/page';
import { cn } from '@/shared/lib/cn';
import {
  Alert,
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
  Textarea,
} from '@/shared/ui';

import { configurationApi, type ConfigField, type ConfigSection } from '../api';

type Changes = Record<string, unknown>;

/** Text of a scalar value; objects and empty values become an empty string. */
function text(value: unknown): string {
  return typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean' ? String(value) : '';
}

/**
 * Generic editor of the platform configuration: every section declared by a module (AddConfigurableSection) is rendered
 * from its schema, so a new section needs no new UI.
 */
export function ConfigurationPage() {
  const { t } = useTranslation('configuration');
  const scope = useQueryScope('configuration');
  const overview = useQuery({ queryKey: scope, queryFn: configurationApi.get });
  const [selected, setSelected] = useState<string | null>(null);
  const sections = overview.data?.sections ?? [];
  const current = sections.find((section) => section.section === selected) ?? sections[0];

  return (
    <div className="space-y-6">
      <PageHeader title={t('title')} description={t('description')} />
      {overview.data && !overview.data.secretsAvailable && <Alert variant="destructive">{t('noMasterKey')}</Alert>}
      <div className="grid gap-6 lg:grid-cols-[14rem_1fr]">
        <nav aria-label={t('title')} className="flex gap-1 overflow-x-auto lg:flex-col">
          {sections.map((section) => (
            <Button
              key={section.section}
              variant={section.section === current?.section ? 'secondary' : 'ghost'}
              className="justify-start"
              onClick={() => setSelected(section.section)}
            >
              {section.section}
            </Button>
          ))}
        </nav>
        {current && <SectionForm key={current.section} section={current} scope={scope} />}
      </div>
    </div>
  );
}

function SectionForm({ section, scope }: { section: ConfigSection; scope: readonly unknown[] }) {
  const { t } = useTranslation('configuration');
  const queryClient = useQueryClient();
  const [changes, setChanges] = useState<Changes>({});
  const save = useMutation({
    mutationFn: () => configurationApi.update(section.section, changes),
    onSuccess: async () => {
      toast.success(t('saved'));
      setChanges({});
      await queryClient.invalidateQueries({ queryKey: scope });
    },
    onError: (error) => toast.error(ApiError.from(error).message),
  });
  const set = (key: string, value: unknown) => setChanges((current) => ({ ...current, [key]: value }));

  return (
    <Card>
      <CardHeader>
        <CardTitle>{section.section}</CardTitle>
        {section.description && <CardDescription>{section.description}</CardDescription>}
      </CardHeader>
      <CardContent>
        <form
          className="space-y-6"
          onSubmit={(event) => {
            event.preventDefault();
            save.mutate();
          }}
        >
          {section.fields.map((field) => (
            <FieldEditor
              key={field.key}
              field={field}
              label={
                field.key === section.section
                  ? section.section
                  : field.key.slice(section.section.length + 1).replaceAll(':', ' / ')
              }
              change={changes[field.key]}
              changed={field.key in changes}
              onChange={(value) => set(field.key, value)}
            />
          ))}
          <div className="flex justify-end gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={Object.keys(changes).length === 0}
              onClick={() => setChanges({})}
            >
              {t('common:actions.cancel')}
            </Button>
            <Button type="submit" disabled={save.isPending || Object.keys(changes).length === 0}>
              {t('common:actions.save')}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}

function FieldEditor({
  field,
  label,
  change,
  changed,
  onChange,
}: {
  field: ConfigField;
  label: string;
  change: unknown;
  changed: boolean;
  onChange: (value: unknown) => void;
}) {
  const { t } = useTranslation('configuration');
  const id = `config-${field.key.replaceAll(':', '-')}`;
  const current = changed ? change : field.value;
  const defaultText =
    field.default === null || field.default === undefined || typeof field.default === 'object'
      ? ''
      : text(field.default);

  return (
    <div className={cn('space-y-2', changed && 'rounded-md border-l-2 border-primary pl-3')}>
      <div className="flex flex-wrap items-center gap-2">
        <Label htmlFor={id} className="font-mono text-xs">
          {label}
        </Label>
        <Badge variant="secondary">{field.source ? t(`sources.${field.source}`) : t('defaultSource')}</Badge>
        {field.requiresRestart && <Badge variant="outline">{t('restart')}</Badge>}
        {field.hasValue && field.source === 'database' && (
          <Button type="button" variant="ghost" size="sm" className="h-6 px-2 text-xs" onClick={() => onChange(null)}>
            <RotateCcwIcon aria-hidden />
            {t('reset')}
          </Button>
        )}
      </div>

      {field.kind === 'Boolean' && (
        <Switch
          id={id}
          checked={text(current ?? field.default) === 'true'}
          onCheckedChange={(checked) => onChange(checked)}
        />
      )}

      {(field.kind === 'Text' || field.kind === 'Number') && (
        <Input
          id={id}
          type={field.secret ? 'password' : field.kind === 'Number' ? 'number' : 'text'}
          autoComplete="off"
          placeholder={field.secret ? (field.hasValue ? t('secretSet') : t('secretEmpty')) : defaultText}
          value={changed ? text(change) : field.secret ? '' : text(field.value)}
          onChange={(event) => onChange(event.target.value === '' && !field.secret ? null : event.target.value)}
        />
      )}

      {field.kind === 'List' && (
        <Textarea
          id={id}
          rows={3}
          className="font-mono text-xs"
          value={((current as string[] | null) ?? []).join('\n')}
          onChange={(event) => onChange(event.target.value.split('\n'))}
        />
      )}

      {field.kind === 'Map' && (
        <MapEditor
          field={field}
          id={id}
          change={change as Record<string, string | null> | undefined}
          onChange={onChange}
        />
      )}

      <p className="text-xs text-muted-foreground">
        {[
          field.help,
          field.secret ? t('secretHint') : null,
          field.kind === 'List' ? t('listHint') : null,
          defaultText && !field.secret ? t('defaultValue', { value: defaultText }) : null,
        ]
          .filter(Boolean)
          .join(' ')}
      </p>
      {field.source === 'environment' && <p className="text-xs text-destructive">{t('envOverride')}</p>}
    </div>
  );
}

/** Names and values; existing secret values are never shown, only replaced. */
function MapEditor({
  field,
  id,
  change,
  onChange,
}: {
  field: ConfigField;
  id: string;
  change: Record<string, string | null> | undefined;
  onChange: (value: Record<string, string | null>) => void;
}) {
  const { t } = useTranslation('configuration');
  const existing = (field.value as Record<string, string | null> | null) ?? {};
  const edits = change ?? {};
  const [newName, setNewName] = useState('');
  const names = [...new Set([...Object.keys(existing), ...Object.keys(edits)])].filter((name) => edits[name] !== null);

  return (
    <div id={id} className="space-y-2">
      {names.map((name) => (
        <div key={name} className="flex gap-2">
          <Input aria-label={t('name')} className="max-w-48 font-mono text-xs" value={name} readOnly />
          <Input
            aria-label={`${name} ${t('value')}`}
            type={field.secret ? 'password' : 'text'}
            autoComplete="off"
            placeholder={field.secret && name in existing ? t('secretSet') : ''}
            value={edits[name] ?? (field.secret ? '' : (existing[name] ?? ''))}
            onChange={(event) => onChange({ ...edits, [name]: event.target.value })}
          />
          <Button
            type="button"
            variant="ghost"
            size="icon"
            aria-label={t('common:actions.delete')}
            onClick={() => onChange({ ...edits, [name]: null })}
          >
            <Trash2Icon aria-hidden />
          </Button>
        </div>
      ))}
      <div className="flex gap-2">
        <Input
          aria-label={t('name')}
          className="max-w-48 font-mono text-xs"
          placeholder={t('name')}
          value={newName}
          onChange={(event) => setNewName(event.target.value.replace(/[:\s]/g, ''))}
        />
        <Button
          type="button"
          variant="outline"
          disabled={!newName || names.includes(newName)}
          onClick={() => {
            onChange({ ...edits, [newName]: '' });
            setNewName('');
          }}
        >
          <PlusIcon aria-hidden />
          {t('addEntry')}
        </Button>
      </div>
    </div>
  );
}
