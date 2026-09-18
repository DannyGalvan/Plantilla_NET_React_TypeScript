import type { AxiosResponse } from "axios";
import { afterEach, describe, expect, it, vi } from "vitest";
import { z } from "zod";

// Hoisted factory — vi.mock runs the factory when this module is imported.
vi.mock("../configs/axios/interceptors", () => {
  return {
    api: {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
      patch: vi.fn(),
      delete: vi.fn(),
    },
    setAuthorization: vi.fn(),
    clearToken: vi.fn(),
    readToken: vi.fn(),
  };
});

// Import the mocked module AFTER the vi.mock call so the mocks apply.
const interceptors = await import("../configs/axios/interceptors");
const mockedApi = interceptors.api as unknown as {
  get: ReturnType<typeof vi.fn>;
  post: ReturnType<typeof vi.fn>;
  put: ReturnType<typeof vi.fn>;
  patch: ReturnType<typeof vi.fn>;
  delete: ReturnType<typeof vi.fn>;
};

const { SchemaError, parseEnvelope, tryParseEnvelope, zodApi } = await import(
  "./zodApi"
);

afterEach(() => {
  vi.clearAllMocks();
});

const sampleSchema = z.object({ id: z.number(), name: z.string() });

function fakeAxios(data: unknown): AxiosResponse {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  return { data } as any;
}

describe("zodApi envelope validation", () => {
  it("parses a valid envelope and forwards `data`", async () => {
    mockedApi.get.mockResolvedValueOnce(
      fakeAxios({
        success: true,
        message: "ok",
        totalResults: 1,
        data: { id: 1, name: "Ada" },
      }),
    );

    const env = await zodApi.get("/x", sampleSchema);
    expect(env.success).toBe(true);
    if (!env.success) throw new Error("env must be success");
    expect(env.data).toEqual({ id: 1, name: "Ada" });
  });

  it("throws SchemaError when the envelope shape is wrong", async () => {
    mockedApi.get.mockResolvedValueOnce(
      // Missing `success` field.
      fakeAxios({ message: "?", data: { id: 1, name: "x" } }),
    );

    await expect(zodApi.get("/x", sampleSchema)).rejects.toBeInstanceOf(
      SchemaError,
    );
  });

  it("throws SchemaError when the data shape is wrong", async () => {
    mockedApi.get.mockResolvedValueOnce(
      fakeAxios({
        success: true,
        message: "ok",
        data: { id: "not-a-number", name: "x" },
      }),
    );

    await expect(zodApi.get("/x", sampleSchema)).rejects.toBeInstanceOf(
      SchemaError,
    );
  });
});

describe("parseEnvelope (standalone helper)", () => {
  it("parses a raw body", () => {
    const env = parseEnvelope(
      { success: true, message: "ok", data: { id: 7, name: "Bo" } },
      sampleSchema,
    );
    expect(env.success).toBe(true);
    if (!env.success) throw new Error("must succeed");
    expect(env.data).toEqual({ id: 7, name: "Bo" });
  });

  it("throws on a malformed envelope", () => {
    expect(() =>
      parseEnvelope({ data: { id: 1, name: "x" } }, sampleSchema),
    ).toThrow(SchemaError);
  });
});

describe("tryParseEnvelope (non-throwing variant)", () => {
  it("returns null when validation fails instead of throwing", () => {
    expect(
      tryParseEnvelope({ data: { id: 1, name: "x" } }, sampleSchema),
    ).toBeNull();
  });

  it("returns the parsed envelope on success", () => {
    const env = tryParseEnvelope(
      { success: true, message: "ok", data: { id: 1, name: "Ada" } },
      sampleSchema,
    );
    expect(env).not.toBeNull();
    if (!env) throw new Error("must succeed");
    expect(env.data).toEqual({ id: 1, name: "Ada" });
  });
});
