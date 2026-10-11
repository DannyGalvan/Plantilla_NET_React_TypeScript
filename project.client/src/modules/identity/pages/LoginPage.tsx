import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router';
import { z } from 'zod';

import { useAuthStore } from '../../../shared/auth/auth-store';
import { Button } from '../../../shared/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '../../../shared/ui/card';
import { Input } from '../../../shared/ui/input';
import { login, loginSchema, parseApiError } from '../api/identity-api';

export function LoginPage() {
  const [values, setValues] = useState({ userName: '', password: '' });
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [submitting, setSubmitting] = useState(false);
  const navigate = useNavigate();
  const setSession = useAuthStore((s) => s.setSession);

  function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const parsed = loginSchema.safeParse(values);
    if (!parsed.success) {
      setErrors(z.flattenError(parsed.error).fieldErrors);
      return;
    }

    setSubmitting(true);
    setErrors({});
    login(parsed.data)
      .then((session) => {
        setSession(session);
        navigate('/');
      })
      .catch((error: unknown) => {
        const problem = parseApiError(error);
        if (problem.errors) {
          setErrors(problem.errors);
        } else {
          setErrors({ _: [problem.detail ?? 'No se pudo iniciar sesión.'] });
        }
      })
      .finally(() => setSubmitting(false));
  }

  return (
    <main
      style={{
        minHeight: '100vh',
        display: 'grid',
        placeItems: 'center',
        backgroundColor: 'var(--background)',
        padding: '1rem',
      }}
    >
      <Card style={{ width: '24rem' }}>
        <CardHeader>
          <CardTitle>Iniciar sesión</CardTitle>
          <CardDescription>Accede con tu usuario y contraseña.</CardDescription>
        </CardHeader>
        <CardContent>
          <form
            style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}
            onSubmit={onSubmit}
          >
            <label
              style={{
                display: 'flex',
                flexDirection: 'column',
                gap: '0.25rem',
              }}
            >
              <span>Usuario</span>
              <Input
                autoComplete="username"
                value={values.userName}
                onChange={(e) =>
                  setValues((prev) => ({ ...prev, userName: e.target.value }))
                }
              />
              {errors.userName?.map((msg) => (
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
              <span>Contraseña</span>
              <Input
                autoComplete="current-password"
                type="password"
                value={values.password}
                onChange={(e) =>
                  setValues((prev) => ({ ...prev, password: e.target.value }))
                }
              />
              {errors.password?.map((msg) => (
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
              {submitting ? 'Entrando…' : 'Entrar'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </main>
  );
}
