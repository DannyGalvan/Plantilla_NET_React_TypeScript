import { z } from "zod";

/**
 * Phase 4.9 — Zod schemas for API responses.
 *
 * The plan calls for validating every API response shape instead of the
 * type-cast `api.get<unknown, ApiResponse<T>>(url)` pattern that trusts the
 * server blindly. These schemas describe the wire format and are the
 * single source of truth for the response types.
 *
 * Each entity's full schema lives next to the type and is exported for use
 * by both the validator helper and any test that exercises the contract.
 */

/**
 * Envelope every backend endpoint returns. `data` is opaque here — callers
 * refine it with their own schema (`zodApi.get(..., schema)`).
 */
export const apiEnvelopeSchema = z.object({
  success: z.boolean(),
  message: z.string(),
  totalResults: z.number().int().nonnegative().optional(),
  data: z.unknown(),
});

export type ApiEnvelope = z.infer<typeof apiEnvelopeSchema>;

// ----------------------------------------------------------------
// User
// ----------------------------------------------------------------

export const userResponseSchema = z.object({
  id: z.number().int().nullable().optional(),
  rolId: z.number().int().nullable().optional(),
  email: z.string().nullable(),
  name: z.string().nullable(),
  userName: z.string().nullable(),
  identificationDocument: z.string().nullable(),
  number: z.string().nullable(),
  state: z.number().int().nullable(),
  createdBy: z.number().int().nullable().optional(),
  updatedBy: z.number().int().nullable().optional(),
});
export type UserResponseZ = z.infer<typeof userResponseSchema>;

// ----------------------------------------------------------------
// Role
// ----------------------------------------------------------------

export const rolResponseSchema = z.object({
  id: z.number().int().nullable().optional(),
  name: z.string().nullable(),
  state: z.number().int().nullable(),
  createdBy: z.number().int().nullable().optional(),
  updatedBy: z.number().int().nullable().optional(),
});
export type RolResponseZ = z.infer<typeof rolResponseSchema>;

// ----------------------------------------------------------------
// Operation
// ----------------------------------------------------------------

export const operationResponseSchema = z.object({
  id: z.number().int().nullable().optional(),
  moduleId: z.number().int().nullable().optional(),
  operationKey: z.string().nullable(),
  guid: z.string().nullable(),
  name: z.string().nullable(),
  description: z.string().nullable(),
  policy: z.string().nullable(),
  icon: z.string().nullable(),
  path: z.string().nullable(),
  controllerName: z.string().nullable(),
  actionName: z.string().nullable(),
  httpMethod: z.string().nullable(),
  routeTemplate: z.string().nullable(),
  isVisible: z.boolean().nullable(),
  state: z.number().int().nullable(),
  createdBy: z.number().int().nullable().optional(),
  updatedBy: z.number().int().nullable().optional(),
});
export type OperationResponseZ = z.infer<typeof operationResponseSchema>;

// ----------------------------------------------------------------
// RolOperation
// ----------------------------------------------------------------

export const rolOperationResponseSchema = z.object({
  id: z.number().int().nullable().optional(),
  rolId: z.number().int().nullable().optional(),
  operationId: z.number().int().nullable().optional(),
  state: z.number().int().nullable(),
  createdBy: z.number().int().nullable().optional(),
  updatedBy: z.number().int().nullable().optional(),
});
export type RolOperationResponseZ = z.infer<typeof rolOperationResponseSchema>;

// ----------------------------------------------------------------
// Auth
// ----------------------------------------------------------------

export const moduleResponseSchema = z.object({
  id: z.number().int().nullable().optional(),
  name: z.string().nullable(),
  description: z.string().nullable(),
  image: z.string().nullable(),
  path: z.string().nullable(),
  state: z.number().int().nullable(),
  order: z.number().int().nullable(),
  isVisible: z.boolean().nullable(),
  createdBy: z.number().int().nullable().optional(),
  updatedBy: z.number().int().nullable().optional(),
});

export const operationAuthItemSchema = z.object({
  id: z.number().int().nullable().optional(),
  operationKey: z.string().nullable(),
  name: z.string().nullable(),
  description: z.string().nullable(),
  icon: z.string().nullable(),
  path: z.string().nullable(),
  controllerName: z.string().nullable(),
  actionName: z.string().nullable(),
  httpMethod: z.string().nullable(),
  routeTemplate: z.string().nullable(),
  moduleId: z.number().int().nullable().optional(),
  isVisible: z.boolean().nullable(),
  state: z.number().int().nullable(),
});

export const authorizationsSchema = z.object({
  module: moduleResponseSchema,
  operations: z.array(operationAuthItemSchema),
});

export const authWithRefreshResponseSchema = z.object({
  token: z.string().nullable(),
  refreshToken: z.string().nullable().optional(),
  email: z.string().nullable(),
  userName: z.string().nullable(),
  name: z.string().nullable(),
  userId: z.number().int().nullable(),
  expiresInSeconds: z.number().int().nullable().optional(),
  operations: z.array(authorizationsSchema).nullable().optional(),
});
export type AuthWithRefreshResponseZ = z.infer<
  typeof authWithRefreshResponseSchema
>;

// ----------------------------------------------------------------
// Catalogue
// ----------------------------------------------------------------

export const catalogueResponseItemSchema = z.object({
  id: z.number().int().nullable().optional(),
  value: z.string().nullable().optional(),
  label: z.string().nullable().optional(),
  name: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
});
export type CatalogueResponseItemZ = z.infer<
  typeof catalogueResponseItemSchema
>;

// ----------------------------------------------------------------
// Validation failures
// ----------------------------------------------------------------

export const validationFailureSchema = z.object({
  propertyName: z.string(),
  errorMessage: z.string(),
  attemptedValue: z.unknown().optional(),
});
