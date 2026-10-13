import type { ModuleResources } from '@/shared/i18n';

export const resources: ModuleResources = {
  es: {
    library: {
      title: 'Archivos',
      description: 'Documentos compartidos de la organización. Los adjuntos de cada registro se ven en su ficha.',
      upload: 'Subir archivo',
      size: 'Tamaño',
      provider: 'Almacenamiento',
    },
    storage: {
      title: 'Almacenamiento',
      description:
        'Elige dónde se guardan los archivos de tu organización: el almacenamiento de la plataforma o tu propia cuenta.',
      platform: 'Almacenamiento de la plataforma',
      platformHint: 'Se usa mientras no elijas una conexión propia como predeterminada.',
      connect: 'Conectar',
      connectWith: 'Conectar {{provider}}',
      notConfigured: 'La plataforma aún no tiene configurada esta integración.',
      default: 'Predeterminado',
      makeDefault: 'Usar como predeterminado',
      account: 'Cuenta',
      name: 'Nombre de la conexión',
      connected: 'Cuenta conectada.',
      failed: 'No se pudo completar la conexión.',
      secretHint: 'Las credenciales se guardan cifradas y nunca se vuelven a mostrar.',
      providers: {
        'google-drive': 'Google Drive',
        onedrive: 'OneDrive',
        s3: 'Amazon S3',
        'azure-blob': 'Azure Blob Storage',
        local: 'Disco del servidor',
      },
    },
  },
  en: {
    library: {
      title: 'Files',
      description: 'Shared documents of the organization. Attachments of each record are shown on the record.',
      upload: 'Upload file',
      size: 'Size',
      provider: 'Storage',
    },
    storage: {
      title: 'Storage',
      description: 'Choose where the files of your organization are stored: the platform storage or your own account.',
      platform: 'Platform storage',
      platformHint: 'Used until you choose one of your connections as the default.',
      connect: 'Connect',
      connectWith: 'Connect {{provider}}',
      notConfigured: 'The platform has not configured this integration yet.',
      default: 'Default',
      makeDefault: 'Use as default',
      account: 'Account',
      name: 'Connection name',
      connected: 'Account connected.',
      failed: 'The connection could not be completed.',
      secretHint: 'Credentials are stored encrypted and never shown again.',
      providers: {
        'google-drive': 'Google Drive',
        onedrive: 'OneDrive',
        s3: 'Amazon S3',
        'azure-blob': 'Azure Blob Storage',
        local: 'Server disk',
      },
    },
  },
};
