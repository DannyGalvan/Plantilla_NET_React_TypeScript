import { forwardRef, type ButtonHTMLAttributes } from 'react';

type Variant =
  | 'default'
  | 'destructive'
  | 'outline'
  | 'secondary'
  | 'ghost'
  | 'link';
type Size = 'default' | 'sm' | 'lg' | 'icon';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  size?: Size;
}

const variantClass: Record<Variant, string> = {
  default: 'btn-default',
  destructive: 'btn-destructive',
  outline: 'btn-outline',
  secondary: 'btn-secondary',
  ghost: 'btn-ghost',
  link: 'btn-link',
};

const sizeClass: Record<Size, string> = {
  default: 'btn-md',
  sm: 'btn-sm',
  lg: 'btn-lg',
  icon: 'btn-icon',
};

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  function Button(
    {
      className,
      variant = 'default',
      size = 'default',
      type = 'button',
      ...rest
    },
    ref,
  ) {
    const classes = ['btn', variantClass[variant], sizeClass[size], className]
      .filter(Boolean)
      .join(' ');
    return <button ref={ref} className={classes} type={type} {...rest} />;
  },
);
