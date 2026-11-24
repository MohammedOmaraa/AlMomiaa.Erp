import { useState, type FormEvent } from "react";
import { Navigate, useNavigate, useSearchParams } from "react-router-dom";
import {
  AlertBanner,
  BrandLogo,
  Button,
  LocaleSwitcher,
  PasswordField,
  SurfaceCard,
  TextField,
  ThemeToggle,
} from "@almomiaa/ui";
import { formatApiError } from "../lib/api-client";
import { useApp } from "../providers/AppProviders";

export function LoginPage() {
  const { locale, setLocale, theme, setTheme, t, authStatus, login } = useApp();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const passwordChanged = searchParams.get("passwordChanged") === "1";

  const [identifier, setIdentifier] = useState("");
  const [password, setPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (authStatus === "authenticated") {
    return <Navigate to="/" replace />;
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await login({ identifier: identifier.trim(), password });
      navigate("/", { replace: true });
    } catch (err) {
      setError(formatApiError(err, t.auth.invalidCredentials));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="flex min-h-screen flex-col justify-between bg-surface-canvas px-4 py-6 text-text-primary">
      <div className="mx-auto flex w-full max-w-md items-center justify-end gap-2">
        <LocaleSwitcher value={locale} onChange={setLocale} />
        <ThemeToggle value={theme} onChange={setTheme} />
      </div>

      <div className="mx-auto my-auto w-full max-w-md space-y-6 py-8">
        <div className="flex flex-col items-center gap-3 text-center">
          <BrandLogo label={t.appName} />
          <div>
            <h1 className="text-xl font-bold text-text-primary">{t.auth.title}</h1>
            <p className="mt-1 text-sm text-text-secondary">{t.auth.subtitle}</p>
          </div>
        </div>

        <SurfaceCard title={t.auth.signIn}>
          <form onSubmit={handleSubmit} className="space-y-4">
            {passwordChanged ? (
              <AlertBanner tone="success" message={t.auth.passwordChangedBanner} />
            ) : null}
            {error ? <AlertBanner tone="danger" message={error} /> : null}

            <TextField
              id="rep-identifier"
              label={t.auth.identifierLabel}
              placeholder={t.auth.identifierPlaceholder}
              value={identifier}
              onChange={(e) => setIdentifier(e.target.value)}
              autoComplete="username"
              required
            />

            <PasswordField
              id="rep-password"
              label={t.auth.passwordLabel}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
              required
            />

            <Button type="submit" className="w-full" loading={submitting}>
              {submitting ? t.auth.signingIn : t.auth.signIn}
            </Button>
          </form>
        </SurfaceCard>
      </div>

      <footer className="text-center text-xs text-text-secondary">
        AlMomiaa Trading Co. — Field Sales PWA
      </footer>
    </div>
  );
}
