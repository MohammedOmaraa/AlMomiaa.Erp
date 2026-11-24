import type { AuthenticatedUser } from "@almomiaa/api-client";

export function hasPermission(user: AuthenticatedUser | null, claim: string): boolean {
  if (!user) return false;
  return user.permissions.includes(claim);
}

export function hasAnyPermission(user: AuthenticatedUser | null, claims: string[]): boolean {
  if (!user) return false;
  if (claims.length === 0) return true;
  return claims.some((claim) => user.permissions.includes(claim));
}
