import type { HTMLAttributes } from 'react';

export function Card(props: HTMLAttributes<HTMLDivElement>) {
  const { className, ...rest } = props;
  return (
    <div className={['card', className].filter(Boolean).join(' ')} {...rest} />
  );
}

export function CardHeader(props: HTMLAttributes<HTMLDivElement>) {
  const { className, ...rest } = props;
  return (
    <div
      className={['card-header', className].filter(Boolean).join(' ')}
      {...rest}
      style={{ marginBottom: '1rem' }}
    />
  );
}

export function CardTitle(props: HTMLAttributes<HTMLHeadingElement>) {
  const { className, children, ...rest } = props;
  return (
    <h3
      className={['card-title', className].filter(Boolean).join(' ')}
      style={{ fontSize: '1.125rem', fontWeight: 600, lineHeight: 1.25 }}
      {...rest}
    >
      {children}
    </h3>
  );
}

export function CardDescription(props: HTMLAttributes<HTMLParagraphElement>) {
  const { className, ...rest } = props;
  return (
    <p
      className={['card-description', className].filter(Boolean).join(' ')}
      {...rest}
      style={{ color: 'var(--muted-foreground)', fontSize: '0.875rem' }}
    />
  );
}

export function CardContent(props: HTMLAttributes<HTMLDivElement>) {
  const { className, ...rest } = props;
  return (
    <div
      className={['card-content', className].filter(Boolean).join(' ')}
      {...rest}
    />
  );
}

export function CardFooter(props: HTMLAttributes<HTMLDivElement>) {
  const { className, ...rest } = props;
  return (
    <div
      className={['card-footer', className].filter(Boolean).join(' ')}
      {...rest}
      style={{
        marginTop: '1rem',
        display: 'flex',
        alignItems: 'center',
        gap: '0.5rem',
      }}
    />
  );
}
