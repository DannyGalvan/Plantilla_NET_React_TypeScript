import { useTranslation } from 'react-i18next';

import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogTitle,
} from '@/shared/ui';

interface ConfirmDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onConfirm: () => void;
  title?: string;
  description?: string;
  confirmLabel?: string;
}

/** Destructive confirmation built on the accessible AlertDialog (focus trap, Escape, screen readers). */
export function ConfirmDialog({ open, onOpenChange, onConfirm, title, description, confirmLabel }: ConfirmDialogProps) {
  const { t } = useTranslation();
  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogTitle>{title ?? t('common.confirmDelete')}</AlertDialogTitle>
        <AlertDialogDescription>{description ?? t('common.confirmDeleteDescription')}</AlertDialogDescription>
        <div className="flex justify-end gap-2">
          <AlertDialogCancel>{t('actions.cancel')}</AlertDialogCancel>
          <AlertDialogAction onClick={onConfirm}>{confirmLabel ?? t('actions.delete')}</AlertDialogAction>
        </div>
      </AlertDialogContent>
    </AlertDialog>
  );
}
