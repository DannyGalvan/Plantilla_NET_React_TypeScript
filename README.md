# 🧩 Base Project: .NET Core + React + TypeScript

Este repositorio proporciona una plantilla inicial para construir una aplicación web moderna utilizando **.NET Core (API)** y **React con TypeScript (frontend)**. La solución ya viene configurada con autenticación, mapeo de entidades, inyección de dependencias y acceso a base de datos usando Entity Framework.

---

## 🚀 Características

### Backend (.NET Core)

- 🔐 Autenticación lista con JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- 🔁 Automapeo de entidades con [Mapster](https://github.com/MapsterMapper/Mapster)
- 💡 Inyección de dependencias ya configurada
- 🗃️ Entity Framework Core + SQL Server
- 🧪 Validaciones con [FluentValidation](https://docs.fluentvalidation.net/)
- 🔎 Documentación de API con Swagger (Swashbuckle)
- 🔥 Logging estructurado con Serilog (con almacenamiento en SQL Server)
- ⚛️ Frontend en React + TypeScript
- ⚙️ SPA Proxy configurado para desarrollo en simultáneo

### Frontend (React + TypeScript)

- SPA desarrollada con **Vite**
- Componentes estilizados con **Tailwind CSS**
- Librería visual: **HeroUI**
- Validaciones con **Zod**
- Manejo global de estado con **Zustand**
- Llamadas HTTP con **Axios**
- Caché y control de datos con **React Query**
- ESLint + Prettier configurados

---

## 📁 Estructura del proyecto

### Servidor `.NET Core` – `Project.Server`

Project.Server
├── Configs # Configuraciones de servicios, JWT, CORS, etc.
├── Context # DbContext de EF Core
├── Controllers # Endpoints HTTP (incluye AuthController, etc.)
├── Entities # Entidades del dominio
├── Interceptors # Middleware personalizado
├── Mappers # Configuración de Mapster
├── Services # Lógica de negocio
├── Utils # Utilidades generales
├── Validations # Validaciones con FluentValidation
├── appsettings.json # Configuración de entorno
├── Program.cs # Punto de entrada y configuración general
├── web.config # Configuración para IIS

## Frontend (project.client)
arduino
Copiar
Editar
project.client/
├── public/
├── src/
│   ├── assets/
│   ├── components/
│   ├── configs/
│   ├── containers/
│   ├── hooks/
│   ├── pages/
│   ├── routes/
│   ├── services/
│   ├── stores/
│   ├── styles/
│   ├── types/
│   ├── utils/
│   └── validations/
├── .prettierrc
├── eslint.config.js
├── tailwind.config.js
├── vite.config.ts
├── tsconfig.json
├── package.json
└── README.md

## 🛠️ Requisitos

- [.NET SDK](https://dotnet.microsoft.com/en-us/download)
- [Node.js y npm](https://nodejs.org)

---

## 📦 Instalación del proyecto

Ejecuta los siguientes comandos en las respectivas carpetas:

### 🔧 Backend (.NET Core)

dotnet restore

### 💻 Frontend (React + TypeScript)

npm install

### 📌 Nota
Este proyecto sirve como plantilla base para acelerar el desarrollo de aplicaciones completas sin tener que preocuparse por la configuración inicial. Ideal para proyectos empresariales o personales.

