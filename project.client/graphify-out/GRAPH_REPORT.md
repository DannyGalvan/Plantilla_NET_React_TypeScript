# Graph Report - project.client  (2026-09-17)

## Corpus Check
- 133 files · ~21,659 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 11 file(s) not represented in the graph (top: .css 5, (none) 3, .example 1)

## Summary
- 649 nodes · 1436 edges · 28 communities (17 shown, 11 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 8 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `bc2e6bdc`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- useAuth
- react
- zodApi.ts
- converted.ts
- TableServer.tsx
- useAuthStore.ts
- devDependencies
- package.json
- dependencies
- UpdateRolPage.tsx
- compilerOptions
- 💻 Frontend - React + TypeScript + Vite
- compilerOptions
- Authorizations
- eslint.config.js
- scripts
- vite.config.ts
- CountryResponse.ts
- React + TypeScript + Vite
- @heroui/theme
- tsconfig.json
- pre-commit
- pre-push
- lint-staged
- @testing-library/jest-dom

## God Nodes (most connected - your core abstractions)
1. `react` - 42 edges
2. `useAuth()` - 36 edges
3. `react-router` - 27 edges
4. `ApiResponse` - 25 edges
5. `@heroui/react` - 24 edges
6. `compilerOptions` - 19 edges
7. `compilerOptions` - 17 edges
8. `zod` - 15 edges
9. `nameRoutes` - 14 edges
10. `useErrorsStore` - 14 edges

## Surprising Connections (you probably didn't know these)
- `LoginFormProps` --references--> `ApiResponse`  [EXTRACTED]
  src/components/form/LoginForm.tsx → src/types/ApiResponse.ts
- `OperationButtonProps` --references--> `OperationResponse`  [EXTRACTED]
  src/components/button/OperationButton.tsx → src/types/OperationResponse.ts
- `OperationButton()` --calls--> `validationFailureToString()`  [EXTRACTED]
  src/components/button/OperationButton.tsx → src/utils/converted.ts
- `RolButtonProps` --references--> `RolResponse`  [EXTRACTED]
  src/components/button/RolButton.tsx → src/types/RolResponse.ts
- `UserButtonProps` --references--> `UserResponse`  [EXTRACTED]
  src/components/button/UserButton.tsx → src/types/UserResponse.ts

## Import Cycles
- None detected.

## Communities (28 total, 11 thin omitted)

### Community 0 - "useAuth"
Cohesion: 0.07
Nodes (50): react-router, @testing-library/react, Images, Col(), ColProps, Row(), RowProps, Footer() (+42 more)

### Community 1 - "react"
Cohesion: 0.05
Nodes (49): @heroui/react, next-themes, react, react-select, @tanstack/react-query, RolButton(), RolButtonProps, UserButton() (+41 more)

### Community 2 - "zodApi.ts"
Cohesion: 0.06
Nodes (56): zod, BaseCatalogueSelectProps, CatalogueOption, CatalogueSelect(), CatalogueSelectProps, constructQuery(), CustomOption, AuthResponse (+48 more)

### Community 3 - "converted.ts"
Cohesion: 0.06
Nodes (42): LoginForm(), LoginFormData, LoginFormProps, CreateRolFormProps, EditRolFormProps, RolForm(), RolFormProps, Response() (+34 more)

### Community 4 - "TableServer.tsx"
Cohesion: 0.07
Nodes (34): react-data-table-component, zustand, RolResponseColumns, TableSearch(), InputDateSelector(), InputDateSelectorProps, MesajeNoData(), MesajeNoDataProps (+26 more)

### Community 5 - "useAuthStore.ts"
Cohesion: 0.08
Nodes (25): axios, vitest, api, API_URL, clearToken(), readToken(), setAuthorization(), authInitialState (+17 more)

### Community 6 - "devDependencies"
Cohesion: 0.05
Nodes (44): devDependencies, autoprefixer, eslint, @eslint/compat, eslint-config-prettier, @eslint/eslintrc, @eslint/js, eslint-plugin-import (+36 more)

### Community 7 - "package.json"
Cohesion: 0.05
Nodes (36): name, private, type, version, autoprefixer, bootstrap-icons, eslint, @eslint/compat (+28 more)

### Community 8 - "dependencies"
Cohesion: 0.09
Nodes (23): dependencies, axios, bootstrap-icons, framer-motion, @heroui/react, @heroui/theme, next-themes, project.client (+15 more)

### Community 9 - "UpdateRolPage.tsx"
Cohesion: 0.21
Nodes (14): OperationButton(), OperationButtonProps, OperationResponseColumns, UpdateRolPage(), getOperations(), createRolOperation(), getRolOperations(), updateRolOperation() (+6 more)

### Community 10 - "compilerOptions"
Cohesion: 0.10
Nodes (20): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, jsx, lib, module, moduleDetection, moduleResolution (+12 more)

### Community 11 - "💻 Frontend - React + TypeScript + Vite"
Cohesion: 0.11
Nodes (18): 🧪 Buenas prácticas, 🔗 Comunicación y Estado, Construye la aplicación para producción:, ⚛️ Core, Ejecuta la aplicación en desarrollo:, 🎨 Estilos y UI, 🧱 Estructura del proyecto, 💻 Frontend - React + TypeScript + Vite (+10 more)

### Community 12 - "compilerOptions"
Cohesion: 0.11
Nodes (18): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection, moduleResolution, noEmit (+10 more)

### Community 13 - "Authorizations"
Cohesion: 0.21
Nodes (11): framer-motion, react-dom, SubMenu(), AuthResponse, Authorizations, Catalogue, SearchCatalogue, LoginRequest (+3 more)

### Community 14 - "eslint.config.js"
Cohesion: 0.15
Nodes (12): eslint-plugin-import, eslint-plugin-jsx-a11y, eslint-plugin-perfectionist, eslint-plugin-prettier, eslint-plugin-react, eslint-plugin-react-hooks, eslint-plugin-react-refresh, eslint-plugin-tailwindcss (+4 more)

### Community 15 - "scripts"
Cohesion: 0.17
Nodes (12): scripts, build, dev, format, format:check, lint, lint:fix, prepare (+4 more)

### Community 16 - "vite.config.ts"
Cohesion: 0.22
Nodes (7): @tailwindcss/vite, vite, vite-plugin-compression, vite-tsconfig-paths, @vitejs/plugin-react, certFilePath, keyFilePath

## Knowledge Gaps
- **247 isolated node(s):** `name`, `private`, `version`, `type`, `dev` (+242 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 292 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **11 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `react` connect `react` to `useAuth`, `zodApi.ts`, `converted.ts`, `TableServer.tsx`, `package.json`, `UpdateRolPage.tsx`, `Authorizations`?**
  _High betweenness centrality (0.134) - this node is a cross-community bridge._
- **Why does `devDependencies` connect `devDependencies` to `package.json`?**
  _High betweenness centrality (0.113) - this node is a cross-community bridge._
- **Why does `zod` connect `zodApi.ts` to `converted.ts`, `useAuthStore.ts`, `package.json`?**
  _High betweenness centrality (0.091) - this node is a cross-community bridge._
- **Are the 3 inferred relationships involving `useAuth()` (e.g. with `useCan.test.ts` and `ProtectedPublic.test.tsx`) actually correct?**
  _`useAuth()` has 3 INFERRED edges - model-reasoned connections that need verification._
- **What connects `name`, `private`, `version` to the rest of the system?**
  _247 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `useAuth` be split into smaller, more focused modules?**
  _Cohesion score 0.06677215189873417 - nodes in this community are weakly interconnected._
- **Should `react` be split into smaller, more focused modules?**
  _Cohesion score 0.051929824561403506 - nodes in this community are weakly interconnected._