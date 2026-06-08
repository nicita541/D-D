import { useState, type FormEvent, type ReactNode } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { LogIn, UserPlus } from 'lucide-react';
import { authApi } from '../shared/api/endpoints';
import { Button, Field, Panel } from '../shared/components/ui';
import { getErrorMessage } from '../shared/api/errors';
import { useAuth } from './useAuth';

export function LoginPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [emailOrUsername, setEmailOrUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [pending, setPending] = useState(false);

  const target = getAuthReturnTarget(location.state);

  if (auth.isAuthenticated) {
    return <Navigate to={target} replace />;
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError('');
    setPending(true);
    try {
      await authApi.login({ emailOrUsername, password });
      navigate(target, { replace: true });
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setPending(false);
    }
  }

  return (
    <AuthLayout title="Вход в игру" subtitle="Продолжи одиночную RPG-кампанию.">
      <form className="form-stack" onSubmit={submit}>
        <Field label="Email или username">
          <input value={emailOrUsername} onChange={(event) => setEmailOrUsername(event.target.value)} required />
        </Field>
        <Field label="Пароль">
          <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} required />
        </Field>
        {error ? <p className="form-error">{error}</p> : null}
        <Button disabled={pending} type="submit">
          <LogIn size={18} /> {pending ? 'Входим...' : 'Войти'}
        </Button>
        <p className="muted">
          Нет аккаунта? <Link to="/register">Зарегистрироваться</Link>
        </p>
      </form>
    </AuthLayout>
  );
}

export function RegisterPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState('');
  const [username, setUsername] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [pending, setPending] = useState(false);

  const target = getAuthReturnTarget(location.state);

  if (auth.isAuthenticated) {
    return <Navigate to={target} replace />;
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError('');
    setPending(true);
    try {
      await authApi.register({ email, username, password, displayName });
      navigate(target, { replace: true });
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setPending(false);
    }
  }

  return (
    <AuthLayout title="Новый герой" subtitle="Создай аккаунт для долгой кампании.">
      <form className="form-stack" onSubmit={submit}>
        <Field label="Email">
          <input type="email" value={email} onChange={(event) => setEmail(event.target.value)} required />
        </Field>
        <Field label="Username">
          <input value={username} onChange={(event) => setUsername(event.target.value)} required />
        </Field>
        <Field label="Имя за столом">
          <input value={displayName} onChange={(event) => setDisplayName(event.target.value)} />
        </Field>
        <Field label="Пароль">
          <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} required />
        </Field>
        {error ? <p className="form-error">{error}</p> : null}
        <Button disabled={pending} type="submit">
          <UserPlus size={18} /> {pending ? 'Создаём...' : 'Создать аккаунт'}
        </Button>
        <p className="muted">
          Уже есть аккаунт? <Link to="/login">Войти</Link>
        </p>
      </form>
    </AuthLayout>
  );
}

function AuthLayout({ title, subtitle, children }: { title: string; subtitle: string; children: ReactNode }) {
  return (
    <main className="auth-screen">
      <Panel className="auth-card">
        <p className="eyebrow">D&D Solo RPG</p>
        <h1>{title}</h1>
        <p className="muted">{subtitle}</p>
        {children}
      </Panel>
    </main>
  );
}

function getAuthReturnTarget(state: unknown) {
  const from = (state as { from?: { pathname?: string; search?: string; hash?: string } } | null)?.from;
  if (!from?.pathname) {
    return '/games';
  }

  return `${from.pathname}${from.search ?? ''}${from.hash ?? ''}`;
}
