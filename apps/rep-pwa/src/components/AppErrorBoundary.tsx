import { Component, type ErrorInfo, type ReactNode } from "react";

interface AppErrorBoundaryProps {
  children: ReactNode;
}

interface AppErrorBoundaryState {
  hasError: boolean;
  errorMessage: string;
}

export class AppErrorBoundary extends Component<AppErrorBoundaryProps, AppErrorBoundaryState> {
  constructor(props: AppErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false, errorMessage: "" };
  }

  static getDerivedStateFromError(error: Error): AppErrorBoundaryState {
    return {
      hasError: true,
      errorMessage: error.message || "Unexpected application error",
    };
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    console.error("AlMomiaa Rep PWA error:", error.message, info.componentStack);
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="flex min-h-screen w-full items-center justify-center bg-surface-canvas p-4 text-text-primary">
          <div className="w-full max-w-sm space-y-4 rounded-xl border border-surface-border bg-surface-card p-6 text-center shadow-sm">
            <h1 className="text-base font-bold text-text-primary">
              حدث خطأ غير متوقع | Unexpected Error
            </h1>
            <p className="text-xs text-text-secondary">{this.state.errorMessage}</p>
            <div className="flex flex-wrap justify-center gap-2">
              <button
                type="button"
                onClick={() => window.location.reload()}
                className="rounded-lg bg-brand-gold px-4 py-2 text-xs font-semibold text-surface-obsidian"
              >
                إعادة التحميل / Reload
              </button>
              <a
                href="/login"
                className="rounded-lg border border-surface-border bg-surface-canvas px-4 py-2 text-xs font-semibold text-text-primary"
              >
                تسجيل الدخول / Sign In
              </a>
            </div>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
