"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { extractProblemDetails } from "@almomiaa/api-client";
import { BrandLogo, Button, PasswordField, SurfaceCard, TextField } from "@almomiaa/ui";
import { ar } from "../../locales/ar";
import { en } from "../../locales/en";
import { useAuth } from "../../providers/AuthProvider";
import { useLocale } from "../../providers/LocaleProvider";

export function LoginForm() {
  const router = useRouter();
  const { locale } = useLocale();
  const { login } = useAuth();
  const t = locale === "ar-SA" ? ar : en;

  const [identifier, setIdentifier] = useState("");
  const [password, setPassword] = useState("");
  const [identifierError, setIdentifierError] = useState<string | undefined>();
  const [passwordError, setPasswordError] = useState<string | undefined>();
  const [formError, setFormError] = useState<string | undefined>();
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFormError(undefined);

    const trimmedIdentifier = identifier.trim();
    const nextIdentifierError = trimmedIdentifier.length === 0 ? t.auth.identifierRequired : undefined;
    const nextPasswordError = password.length === 0 ? t.auth.passwordRequired : undefined;

    setIdentifierError(nextIdentifierError);
    setPasswordError(nextPasswordError);

    if (nextIdentifierError || nextPasswordError) {
      return;
    }

    setSubmitting(true);
    try {
      await login({ identifier: trimmedIdentifier, password });
      router.replace("/");
    } catch (error: unknown) {
      const problem = extractProblemDetails(error);
      if (problem?.status === 429) {
        setFormError(t.auth.rateLimited);
      } else if (problem?.status === 401 || problem?.status === 400) {
        setFormError(t.auth.invalidCredentials);
      } else {
        setFormError(t.auth.serviceUnavailable);
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <SurfaceCard className="w-full max-w-md p-6 sm:p-8">
      <div className="mb-6 flex flex-col items-center gap-3">
        <BrandLogo label={t.brand} className="w-full" />
        <div className="w-full text-center">
          <h1 className="text-xl font-bold text-text-primary sm:text-2xl">{t.auth.pageTitle}</h1>
          <p className="mt-1 text-sm text-text-secondary">{t.auth.subtitle}</p>
        </div>
      </div>

      {formError && (
        <div
          role="alert"
          className="mb-4 rounded-lg bg-status-danger-bg px-4 py-3 text-start text-sm font-medium text-status-danger"
        >
          {formError}
        </div>
      )}

      <form onSubmit={handleSubmit} noValidate className="space-y-4">
        <TextField
          id="almomiaa-login-identifier"
          name="identifier"
          label={t.auth.identifierLabel}
          placeholder={t.auth.identifierPlaceholder}
          hint={t.auth.identifierHint}
          error={identifierError}
          value={identifier}
          onChange={(event) => {
            setIdentifier(event.currentTarget.value);
            if (identifierError) setIdentifierError(undefined);
            if (formError) setFormError(undefined);
          }}
          autoComplete="username"
          required
          disabled={submitting}
        />

        <PasswordField
          id="almomiaa-login-password"
          name="password"
          label={t.auth.passwordLabel}
          placeholder={t.auth.passwordPlaceholder}
          showPasswordLabel={t.auth.showPassword}
          hidePasswordLabel={t.auth.hidePassword}
          error={passwordError}
          value={password}
          onChange={(event) => {
            setPassword(event.currentTarget.value);
            if (passwordError) setPasswordError(undefined);
            if (formError) setFormError(undefined);
          }}
          autoComplete="current-password"
          required
          disabled={submitting}
        />

        <Button
          type="submit"
          variant="primary"
          size="comfortable"
          loading={submitting}
          className="mt-2 w-full"
        >
          {submitting ? t.auth.submitting : t.auth.submit}
        </Button>
      </form>
    </SurfaceCard>
  );
}
