import { z } from "zod";
import type {
  AuthenticatedUser,
  AuthenticationResponse,
  LoginRequest,
  ProblemDetails,
} from "./auth.types.js";

export const loginRequestSchema: z.ZodType<LoginRequest> = z
  .object({
    identifier: z.string().trim().min(1).max(150),
    password: z.string().min(1).max(256),
  })
  .strict();

export const authenticatedUserSchema: z.ZodType<AuthenticatedUser> = z
  .object({
    id: z.string().uuid(),
    employeeCode: z.string().min(1),
    fullNameAr: z.string().min(1),
    fullNameEn: z.string().nullable(),
    roleCodes: z.array(z.string()),
    permissions: z.array(z.string()),
    repType: z.string().nullable(),
    defaultWarehouseId: z.number().int().nullable(),
    assignedWarehouseIds: z.array(z.number().int()),
  })
  .strict();

export const authenticationResponseSchema: z.ZodType<AuthenticationResponse> = z
  .object({
    accessToken: z.string().min(1),
    tokenType: z.literal("Bearer"),
    expiresIn: z.number().int().positive(),
    user: authenticatedUserSchema,
  })
  .strict();

export const problemDetailsSchema: z.ZodType<ProblemDetails> = z
  .object({
    type: z.string().optional(),
    title: z.string().optional(),
    status: z.number().int().optional(),
    detail: z.string().optional(),
    instance: z.string().optional(),
    traceId: z.string().optional(),
    errors: z.record(z.array(z.string())).optional(),
  })
  .passthrough();
