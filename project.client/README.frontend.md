# 💻 Frontend - React + TypeScript + Vite

Este frontend es una aplicación SPA moderna desarrollada con **React** y **TypeScript**, empaquetada con **Vite**, y estilizada utilizando **Tailwind CSS**. Está diseñada para integrarse con un backend a través de API REST protegidas con autenticación JWT.

---

## 🚀 Tecnologías utilizadas

### ⚛️ Core

- **React 19**
- **TypeScript**
- **Vite** como bundler y servidor de desarrollo

### 🎨 Estilos y UI

- **Tailwind CSS**
- **HeroUI** (Botones, Inputs, Cards, etc.)
- **Bootstrap Icons**
- **Framer Motion** para animaciones

### 🔗 Comunicación y Estado

- **Axios** para llamadas HTTP
- **React Query** para caché, carga y sincronización
- **Zustand** para gestión de estado global
- **Zod** para validaciones de formularios

### ⚙️ Linting y Formato

- **ESLint** con múltiples plugins
- **Prettier** con plugins para Tailwind e imports

---

## 🧱 Estructura del proyecto

project.client/
├── public/ # Archivos estáticos
├── src/
│ ├── assets/ # Imágenes, íconos, logos
│ ├── components/ # Componentes reutilizables
│ ├── configs/ # Configuraciones globales (API, rutas)
│ ├── containers/ # Componentes de alto nivel o layout
│ ├── hooks/ # Hooks personalizados
│ ├── pages/ # Páginas del sitio
│ ├── routes/ # Sistema de rutas
│ ├── services/ # Lógica de negocio y llamadas HTTP
│ ├── stores/ # Estado global con Zustand
│ ├── styles/ # Estilos globales o CSS utilitario
│ ├── types/ # Tipos y modelos TypeScript
│ ├── utils/ # Utilidades generales
│ └── validations/ # Validaciones con Zod
├── .prettierrc
├── eslint.config.js
├── tailwind.config.js
├── vite.config.ts
└── tsconfig.json
---

## 📦 Instalación y ejecución

### 🛠️ Requisitos

- Node.js 24.15+ (ver `.nvmrc`)
- pnpm 9.15+ (`corepack enable`)

### ⚙️ Pasos

1. Instala las dependencias:


## Instala todas las dependencias necesarias
### pnpm install
## Ejecuta la aplicación en desarrollo:
### pnpm dev
## Construye la aplicación para producción:
### pnpm build

## 🔍 Scripts disponibles


| Comando           | Descripción                                |
| ----------------- | ------------------------------------------ |
| `pnpm dev`        | Levanta el servidor de desarrollo con Vite |
| `pnpm build`      | Compila la app para producción (`/dist`)   |
| `pnpm lint`       | Ejecuta ESLint sobre el código fuente      |
| `pnpm format`     | Formatea el código con Prettier            |

## 🧪 Buenas prácticas

Uso de componentes desacoplados y reutilizables.

Hooks personalizados para lógica compartida.

Validaciones consistentes con Zod.

Gestión de estado optimizada con Zustand.

Código limpio, tipado y consistente gracias a ESLint + Prettier.
