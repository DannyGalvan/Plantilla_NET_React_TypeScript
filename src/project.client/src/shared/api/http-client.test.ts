import { http as mock, HttpResponse } from 'msw';
import { describe, expect, it, vi } from 'vitest';

import { useAuthStore } from '@/shared/auth/auth-store';
import { api, server, session } from '@/test/server';

import { http, httpHooks } from './http-client';
import { ApiError } from './problem-details';

describe('http-client', () => {
  it('refreshes the token once for concurrent 401 responses and retries every request', async () => {
    useAuthStore.getState().setSession('expired');
    document.cookie = 'XSRF-TOKEN=csrf-value; path=/';
    let refreshCalls = 0;

    server.use(
      mock.post(api('/identity/auth/refresh'), ({ request }) => {
        refreshCalls++;
        expect(request.headers.get('X-XSRF-TOKEN')).toBe('csrf-value');
        return HttpResponse.json(session('fresh'));
      }),
      mock.get(api('/catalog/products'), ({ request }) =>
        request.headers.get('Authorization') === 'Bearer fresh'
          ? HttpResponse.json({ ok: true })
          : new HttpResponse(null, { status: 401 }),
      ),
    );

    const results = await Promise.all([
      http.get('/catalog/products'),
      http.get('/catalog/products'),
      http.get('/catalog/products'),
    ]);

    expect(refreshCalls).toBe(1);
    expect(results.every((response) => response.status === 200)).toBe(true);
    expect(useAuthStore.getState().accessToken).toBe('fresh');
  });

  it('ends the session when the refresh fails', async () => {
    useAuthStore.getState().setSession('expired');
    const expired = vi.fn();
    httpHooks.onSessionExpired = expired;

    server.use(
      mock.post(api('/identity/auth/refresh'), () =>
        HttpResponse.json({ code: 'auth.invalid_refresh_token' }, { status: 401 }),
      ),
      mock.get(api('/me'), () => new HttpResponse(null, { status: 401 })),
    );

    await expect(http.get('/me')).rejects.toBeInstanceOf(ApiError);
    expect(expired).toHaveBeenCalledOnce();
    expect(useAuthStore.getState().status).toBe('anonymous');
  });

  it('turns ProblemDetails into ApiError with field errors and trace id', async () => {
    server.use(
      mock.post(api('/catalog/products'), () =>
        HttpResponse.json(
          {
            title: 'Validation failed',
            status: 400,
            detail: 'One or more fields are invalid.',
            code: 'validation',
            traceId: 'abc',
            errors: { name: ['Required'] },
          },
          { status: 400 },
        ),
      ),
    );

    const error = await http.post('/catalog/products', {}).catch((caught: unknown) => caught);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 400, code: 'validation', traceId: 'abc', errors: { name: ['Required'] } });
  });

  it('sends the UI language and the bearer token', async () => {
    useAuthStore.getState().setSession('token-1');
    httpHooks.language = () => 'en';
    server.use(
      mock.get(api('/echo'), ({ request }) =>
        HttpResponse.json({
          language: request.headers.get('Accept-Language'),
          authorization: request.headers.get('Authorization'),
        }),
      ),
    );

    const response = await http.get<{ language: string; authorization: string }>('/echo');

    expect(response.data).toEqual({ language: 'en', authorization: 'Bearer token-1' });
  });
});
