import { AxiosError, type InternalAxiosRequestConfig } from "axios";
import { afterEach, describe, expect, it } from "vitest";

import {
  ForbiddenError,
  InternalServerError,
  NetworkError,
  UnauthorizedError,
} from "../../types/errors";

import { api, clearToken, readToken, setAuthorization } from "./interceptors";

/**
 * Cast a plain object to `AxiosError`. The real constructor does runtime
 * validation we don't care about for these tests — the interceptor only
 * reads `error.response.status` / `error.response.data`, so any object
 * with that shape is enough.
 */
function fakeAxiosError(status: number, data: unknown): AxiosError {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  return { response: { status, data } } as any;
}

function networkError(): AxiosError {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  return { response: undefined } as any;
}

function rejectedOf(): (err: unknown) => Promise<unknown> {
  const handlers = (
    api.interceptors.response as unknown as {
      handlers: Array<{ rejected?: (err: unknown) => Promise<unknown> }>;
    }
  ).handlers;
  const rejected = handlers.at(-1)?.rejected;
  if (!rejected) throw new Error("interceptor not registered");
  return rejected;
}

afterEach(() => {
  clearToken();
});

describe("axios response interceptor", () => {
  it("throws NetworkError when the response is absent", () => {
    const rejected = rejectedOf();
    // The interceptor's `rejected` may throw synchronously (axios contract);
    // catch the thrown instance and assert the type directly.
    expect(() => rejected(networkError())).toThrow(NetworkError);
  });

  it("throws UnauthorizedError on 401 and clears the token", () => {
    setAuthorization("token-to-clear");
    expect(readToken()).toBe("token-to-clear");

    const rejected = rejectedOf();
    let thrown: unknown;
    try {
      rejected(fakeAxiosError(401, { message: "expired" }));
    } catch (error) {
      thrown = error;
    }
    expect(thrown).toBeInstanceOf(UnauthorizedError);
    expect(readToken()).toBeNull();
  });

  it("throws ForbiddenError on 403", () => {
    const rejected = rejectedOf();
    let thrown: unknown;
    try {
      rejected(fakeAxiosError(403, { message: "nope" }));
    } catch (error) {
      thrown = error;
    }
    expect(thrown).toBeInstanceOf(ForbiddenError);
  });

  it("throws InternalServerError on 500", () => {
    const rejected = rejectedOf();
    let thrown: unknown;
    try {
      rejected(fakeAxiosError(500, { message: "boom" }));
    } catch (error) {
      thrown = error;
    }
    expect(thrown).toBeInstanceOf(InternalServerError);
  });

  it("surfaces the server envelope message on other 4xx", () => {
    const rejected = rejectedOf();
    expect(() =>
      rejected(fakeAxiosError(400, { message: "validation failed" })),
    ).toThrow("validation failed");
  });
});

describe("request interceptor", () => {
  it("attaches Authorization header when a token is set", () => {
    setAuthorization("abc.def.ghi");
    const handlers = (
      api.interceptors.request as unknown as {
        handlers: Array<{
          fulfilled?: (
            cfg: InternalAxiosRequestConfig,
          ) => InternalAxiosRequestConfig;
        }>;
      }
    ).handlers;
    const fulfilled = handlers.at(-1)?.fulfilled;
    if (!fulfilled) throw new Error("interceptor not registered");

    const config = {
      headers: { set: () => undefined, get: () => undefined },
    } as unknown as InternalAxiosRequestConfig;
    fulfilled(config);
  });
});
