import { forwardRef, type InputHTMLAttributes } from 'react';

export type InputProps = InputHTMLAttributes<HTMLInputElement>;

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { className, type = 'text', ...rest },
  ref,
) {
  const classes = ['input', className].filter(Boolean).join(' ');
  return <input ref={ref} className={classes} type={type} {...rest} />;
});
