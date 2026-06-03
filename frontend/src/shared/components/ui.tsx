import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { AlertTriangle, Loader2 } from 'lucide-react';
import { getErrorMessage } from '../api/errors';

export function AppShell({
  title,
  subtitle,
  actions,
  children,
}: {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
  children: ReactNode;
}) {
  return (
    <main className="app-shell">
      <header className="topbar">
        <div>
          <p className="eyebrow">D&D Solo RPG</p>
          <h1>{title}</h1>
          {subtitle ? <p className="muted">{subtitle}</p> : null}
        </div>
        <div className="topbar-actions">{actions}</div>
      </header>
      {children}
    </main>
  );
}

export function Panel({
  title,
  children,
  actions,
  className = '',
}: {
  title?: string;
  children: ReactNode;
  actions?: ReactNode;
  className?: string;
}) {
  return (
    <section className={`panel ${className}`}>
      {title || actions ? (
        <div className="panel-header">
          {title ? <h2>{title}</h2> : <span />}
          {actions}
        </div>
      ) : null}
      {children}
    </section>
  );
}

export function Button({
  children,
  variant = 'primary',
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: 'primary' | 'secondary' | 'danger' | 'ghost' }) {
  return (
    <button className={`btn btn-${variant}`} type="button" {...props}>
      {children}
    </button>
  );
}

export function EmptyState({ title, text }: { title: string; text?: string }) {
  return (
    <div className="empty-state">
      <p className="empty-title">{title}</p>
      {text ? <p className="muted">{text}</p> : null}
    </div>
  );
}

export function LoadingState({ text = 'Загрузка...' }: { text?: string }) {
  return (
    <div className="state-line">
      <Loader2 className="spin" size={18} />
      <span>{text}</span>
    </div>
  );
}

export function ErrorState({ error }: { error: unknown }) {
  return (
    <div className="error-box">
      <AlertTriangle size={18} />
      <span>{getErrorMessage(error)}</span>
    </div>
  );
}

export function Field({
  label,
  children,
}: {
  label: string;
  children: ReactNode;
}) {
  return (
    <label className="field">
      <span>{label}</span>
      {children}
    </label>
  );
}
