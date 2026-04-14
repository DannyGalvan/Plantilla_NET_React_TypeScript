import { env } from "node:process";
import { fileURLToPath, URL } from "node:url";

import tailwindcss from "@tailwindcss/vite";
import viteReact from "@vitejs/plugin-react";
import { spawnSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { defineConfig } from "vite";
import compress from "vite-plugin-compression";
import tsconfigPaths from "vite-tsconfig-paths";

const baseFolder =
  env.APPDATA !== undefined && env.APPDATA !== ""
    ? `${env.APPDATA}/ASP.NET/https`
    : `${env.HOME}/.aspnet/https`;

const certificateName = "project.client";
const certFilePath = join(baseFolder, `${certificateName}.pem`);
const keyFilePath = join(baseFolder, `${certificateName}.key`);

if (!existsSync(baseFolder)) {
  mkdirSync(baseFolder, { recursive: true });
}

if (!existsSync(certFilePath) || !existsSync(keyFilePath)) {
  if (
    0 !==
    spawnSync(
      "dotnet",
      [
        "dev-certs",
        "https",
        "--export-path",
        certFilePath,
        "--format",
        "Pem",
        "--no-password",
      ],
      { stdio: "inherit" },
    ).status
  ) {
    throw new Error("Could not create certificate.");
  }
}

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [viteReact(), compress(), tsconfigPaths(), tailwindcss()],
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
  server: {
    proxy: {
      "/api": {
        target: "https://localhost:7266/api",
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path.replace(/^\/api/, ""),
      },
    },
    port: 60263,
    https: {
      key: readFileSync(keyFilePath),
      cert: readFileSync(certFilePath),
    },
  },
});
