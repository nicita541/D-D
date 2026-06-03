import { Component, type ErrorInfo, type ReactNode } from 'react';
import { AlertTriangle } from 'lucide-react';

interface ErrorBoundaryState {
  error: Error | null;
}

export class ErrorBoundary extends Component<{ children: ReactNode }, ErrorBoundaryState> {
  public state: ErrorBoundaryState = { error: null };

  public static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error };
  }

  public componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Frontend render error', error, info.componentStack);
  }

  public render() {
    if (!this.state.error) {
      return this.props.children;
    }

    return (
      <main className="auth-screen">
        <section className="panel auth-card">
          <div className="error-box">
            <AlertTriangle size={18} />
            <span>Интерфейс игры не смог отрисоваться.</span>
          </div>
          <p className="muted">
            Обновите страницу. Если ошибка повторится, backend уже доступен, но frontend получил состояние игры в неожиданном формате.
          </p>
          <details>
            <summary>Техническая ошибка</summary>
            <pre>{this.state.error.message}</pre>
          </details>
        </section>
      </main>
    );
  }
}
