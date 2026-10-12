import { zodResolver } from '@hookform/resolvers/zod';
import { useState, type ReactNode } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useSearchParams } from 'react-router';
import { toast } from 'sonner';
import { z } from 'zod';

import { ApiError } from '@/shared/api/problem-details';
import { useAuthStore } from '@/shared/auth/auth-store';
import { authApi } from '@/shared/auth/session';
import { applyServerErrors, fieldProps, FormField } from '@/shared/components/form';
import { usePlatformStore } from '@/shared/platform/platform-config';
import { ThemeCustomizer } from '@/shared/theme/theme-customizer';
import { Alert, Button, Card, CardContent, CardDescription, CardHeader, CardTitle, Input } from '@/shared/ui';

/** Two-column layout with the brand panel, used by every public authentication page. */
function AuthLayout({ children }: { children: ReactNode }) {
  const { t } = useTranslation('identity');
  const branding = usePlatformStore((state) => state.config.branding);

  return (
    <div className="grid min-h-screen lg:grid-cols-2">
      <aside className="hidden flex-col justify-between bg-primary p-10 text-primary-foreground lg:flex">
        <div className="flex items-center gap-2 text-lg font-semibold">
          {branding.logoUrl && <img src={branding.logoUrl} alt="" className="size-8 rounded-md" />}
          {branding.displayName}
        </div>
        <p className="max-w-md text-2xl leading-snug font-medium">{t('login.tagline')}</p>
        <span className="text-sm opacity-80">{new Date().getFullYear()}</span>
      </aside>
      <main className="relative flex items-center justify-center p-6">
        <div className="absolute top-4 right-4">
          <ThemeCustomizer />
        </div>
        <div className="w-full max-w-sm">{children}</div>
      </main>
    </div>
  );
}

const loginSchema = z.object({ userName: z.string().trim().min(1), password: z.string().min(1) });

export function LoginPage() {
  const { t } = useTranslation('identity');
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [error, setError] = useState<string | null>(null);
  const form = useForm<z.infer<typeof loginSchema>>({
    resolver: zodResolver(loginSchema),
    defaultValues: { userName: '', password: '' },
  });
  const errors = form.formState.errors;

  const submit = form.handleSubmit(async (values) => {
    setError(null);
    try {
      const session = await authApi.login(values.userName, values.password);
      await authApi.loadProfile();
      const returnTo = params.get('returnTo');
      await navigate(session.mustChangePassword ? '/change-password' : returnTo?.startsWith('/') ? returnTo : '/', {
        replace: true,
      });
    } catch (caught) {
      setError(ApiError.from(caught).message);
    }
  });

  return (
    <AuthLayout>
      <Card>
        <CardHeader>
          <CardTitle className="text-xl">{t('login.title')}</CardTitle>
          <CardDescription>{t('login.subtitle')}</CardDescription>
        </CardHeader>
        <CardContent>
          <form className="grid gap-4" onSubmit={(event) => void submit(event)} noValidate>
            {error && <Alert variant="destructive">{error}</Alert>}
            <FormField id="userName" label={t('login.userName')} error={errors.userName && ' '}>
              <Input
                autoComplete="username"

                {...fieldProps('userName', errors.userName?.message)}
                {...form.register('userName')}
              />
            </FormField>
            <FormField id="password" label={t('login.password')} error={errors.password && ' '}>
              <Input
                type="password"
                autoComplete="current-password"
                {...fieldProps('password', errors.password?.message)}
                {...form.register('password')}
              />
            </FormField>
            <Button type="submit" disabled={form.formState.isSubmitting}>
              {t('login.submit')}
            </Button>
            <Link to="/forgot-password" className="text-center text-sm text-primary hover:underline">
              {t('login.forgot')}
            </Link>
          </form>
        </CardContent>
      </Card>
    </AuthLayout>
  );
}

const changeSchema = z
  .object({ currentPassword: z.string().min(1), newPassword: z.string().min(10), confirm: z.string().min(1) })
  .refine((values) => values.newPassword === values.confirm, { path: ['confirm'], message: 'mismatch' });

export function ChangePasswordPage() {
  const { t } = useTranslation('identity');
  const navigate = useNavigate();
  const mustChange = useAuthStore((state) => state.profile?.mustChangePassword ?? false);
  const form = useForm<z.infer<typeof changeSchema>>({
    resolver: zodResolver(changeSchema),
    defaultValues: { currentPassword: '', newPassword: '', confirm: '' },
  });
  const errors = form.formState.errors;

  const submit = form.handleSubmit(async (values) => {
    try {
      await authApi.changePassword(values.currentPassword, values.newPassword);
      await authApi.loadProfile();
      toast.success(t('changePassword.done'));
      await navigate('/', { replace: true });
    } catch (error) {
      applyServerErrors(form, error);
    }
  });

  return (
    <div className="mx-auto w-full max-w-md">
      <Card>
        <CardHeader>
          <CardTitle>{t('changePassword.title')}</CardTitle>
          {mustChange && <CardDescription>{t('changePassword.required')}</CardDescription>}
        </CardHeader>
        <CardContent>
          <form className="grid gap-4" onSubmit={(event) => void submit(event)} noValidate>
            <FormField id="currentPassword" label={t('changePassword.current')} error={errors.currentPassword?.message}>
              <Input
                type="password"
                autoComplete="current-password"
                {...fieldProps('currentPassword', errors.currentPassword?.message)}
                {...form.register('currentPassword')}
              />
            </FormField>
            <FormField id="newPassword" label={t('changePassword.new')} error={errors.newPassword?.message}>
              <Input
                type="password"
                autoComplete="new-password"
                {...fieldProps('newPassword', errors.newPassword?.message)}
                {...form.register('newPassword')}
              />
            </FormField>
            <FormField
              id="confirm"
              label={t('changePassword.confirm')}
              error={errors.confirm ? t('changePassword.mismatch') : undefined}
            >
              <Input
                type="password"
                autoComplete="new-password"
                {...fieldProps('confirm', errors.confirm?.message)}
                {...form.register('confirm')}
              />
            </FormField>
            <Button type="submit" disabled={form.formState.isSubmitting}>
              {t('common:actions.save')}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

const forgotSchema = z.object({ email: z.email() });

export function ForgotPasswordPage() {
  const { t } = useTranslation('identity');
  const [sent, setSent] = useState(false);
  const form = useForm<z.infer<typeof forgotSchema>>({
    resolver: zodResolver(forgotSchema),
    defaultValues: { email: '' },
  });
  const errors = form.formState.errors;

  const submit = form.handleSubmit(async (values) => {
    try {
      await authApi.forgotPassword(values.email);
      setSent(true);
    } catch (error) {
      applyServerErrors(form, error);
    }
  });

  return (
    <AuthLayout>
      <Card>
        <CardHeader>
          <CardTitle className="text-xl">{t('forgot.title')}</CardTitle>
          <CardDescription>{t('forgot.subtitle')}</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-4">
          {sent ? (
            <Alert>{t('forgot.sent')}</Alert>
          ) : (
            <form className="grid gap-4" onSubmit={(event) => void submit(event)} noValidate>
              <FormField id="email" label={t('forgot.email')} error={errors.email?.message}>
                <Input
                  type="email"
                  autoComplete="email"
                  {...fieldProps('email', errors.email?.message)}
                  {...form.register('email')}
                />
              </FormField>
              <Button type="submit" disabled={form.formState.isSubmitting}>
                {t('forgot.submit')}
              </Button>
            </form>
          )}
          <Link to="/login" className="text-center text-sm text-primary hover:underline">
            {t('forgot.back')}
          </Link>
        </CardContent>
      </Card>
    </AuthLayout>
  );
}

const resetSchema = z
  .object({ newPassword: z.string().min(10), confirm: z.string().min(1) })
  .refine((values) => values.newPassword === values.confirm, { path: ['confirm'], message: 'mismatch' });

export function ResetPasswordPage() {
  const { t } = useTranslation('identity');
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const form = useForm<z.infer<typeof resetSchema>>({
    resolver: zodResolver(resetSchema),
    defaultValues: { newPassword: '', confirm: '' },
  });
  const errors = form.formState.errors;

  const submit = form.handleSubmit(async (values) => {
    try {
      await authApi.resetPassword(params.get('token') ?? '', values.newPassword);
      toast.success(t('reset.done'));
      await navigate('/login', { replace: true });
    } catch (error) {
      applyServerErrors(form, error);
    }
  });

  return (
    <AuthLayout>
      <Card>
        <CardHeader>
          <CardTitle className="text-xl">{t('reset.title')}</CardTitle>
        </CardHeader>
        <CardContent>
          <form className="grid gap-4" onSubmit={(event) => void submit(event)} noValidate>
            <FormField id="newPassword" label={t('changePassword.new')} error={errors.newPassword?.message}>
              <Input
                type="password"
                autoComplete="new-password"
                {...fieldProps('newPassword', errors.newPassword?.message)}
                {...form.register('newPassword')}
              />
            </FormField>
            <FormField
              id="confirm"
              label={t('changePassword.confirm')}
              error={errors.confirm ? t('changePassword.mismatch') : undefined}
            >
              <Input
                type="password"
                autoComplete="new-password"
                {...fieldProps('confirm', errors.confirm?.message)}
                {...form.register('confirm')}
              />
            </FormField>
            <Button type="submit" disabled={form.formState.isSubmitting}>
              {t('reset.submit')}
            </Button>
          </form>
        </CardContent>
      </Card>
    </AuthLayout>
  );
}
