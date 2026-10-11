import { useState, type FormEvent } from 'react';
import { z } from 'zod';

import { Button } from '../../../shared/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '../../../shared/ui/card';
import { Input } from '../../../shared/ui/input';
import {
  changePassword,
  changePasswordSchema,
  parseApiError,
} from '../api/identity-api';

export function ChangePasswordPage() {
  const [values, setValues] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [submitting, setSubmitting] = useState(false);
  const [done, setDone] = useState(false);

  function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const parsed = changePasswordSchema.safeParse(values);
    if (!parsed.success) {
      setErrors(z.flattenError(parsed.error).fieldErrors);
      return;
    }

    setSubmitting(true);
    setErrors({});
    changePassword(parsed.data)
      .then(() => setDone(true))
      .catch((error: unknown) => {
        const problem = parseApiError(error);
        setErrors(
          problem.errors ?? {
            _: [problem.detail ?? 'Error al cambiar la contraseña.'],
          },
        );
      })
      .finally(() => setSubmitting(false));
  }

  return (
    <main style={{ padding: '2rem', maxWidth: '32rem', margin: '0 auto' }}>
      <Card>
        <CardHeader>
          <CardTitle>Cambiar contraseña</CardTitle>
          <CardDescription>
            Define una nueva contraseña para tu cuenta.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {done ? (
            <p style={{ color: 'var(--success)' }}>
              Contraseña actualizada. Vuelve a iniciar sesión.
            </p>
          ) : (
            <form
              style={{
                display: 'flex',
                flexDirection: 'column',
                gap: '0.75rem',
              }}
              onSubmit={onSubmit}
            >
              <label
                style={{
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '0.25rem',
                }}
              >
                <span>Contraseña actual</span>
                <Input
                  autoComplete="current-password"
                  type="password"
                  value={values.currentPassword}
                  onChange={(e) =>
                    setValues((prev) => ({
                      ...prev,
                      currentPassword: e.target.value,
                    }))
                  }
                />
                {errors.currentPassword?.map((msg) => (
                  <span
                    key={msg}
                    style={{ color: 'var(--destructive)', fontSize: '0.75rem' }}
                  >
                    {msg}
                  </span>
                ))}
              </label>
              <label
                style={{
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '0.25rem',
                }}
              >
                <span>Nueva contraseña</span>
                <Input
                  autoComplete="new-password"
                  type="password"
                  value={values.newPassword}
                  onChange={(e) =>
                    setValues((prev) => ({
                      ...prev,
                      newPassword: e.target.value,
                    }))
                  }
                />
                {errors.newPassword?.map((msg) => (
                  <span
                    key={msg}
                    style={{ color: 'var(--destructive)', fontSize: '0.75rem' }}
                  >
                    {msg}
                  </span>
                ))}
              </label>
              <label
                style={{
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '0.25rem',
                }}
              >
                <span>Repetir nueva contraseña</span>
                <Input
                  autoComplete="new-password"
                  type="password"
                  value={values.confirmPassword}
                  onChange={(e) =>
                    setValues((prev) => ({
                      ...prev,
                      confirmPassword: e.target.value,
                    }))
                  }
                />
                {errors.confirmPassword?.map((msg) => (
                  <span
                    key={msg}
                    style={{ color: 'var(--destructive)', fontSize: '0.75rem' }}
                  >
                    {msg}
                  </span>
                ))}
              </label>
              {errors._?.map((msg) => (
                <span
                  key={msg}
                  style={{ color: 'var(--destructive)', fontSize: '0.875rem' }}
                >
                  {msg}
                </span>
              ))}
              <Button disabled={submitting} type="submit">
                {submitting ? 'Guardando…' : 'Guardar'}
              </Button>
            </form>
          )}
        </CardContent>
      </Card>
    </main>
  );
}
