import eslintPluginReactQuery from "@tanstack/eslint-plugin-query";
import eslintPluginImport from "eslint-plugin-import";
import pkg from "eslint-plugin-perfectionist";
import eslintPluginPrettier from "eslint-plugin-prettier";
import eslintPluginReact from "eslint-plugin-react";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import eslintPluginTailwindCSS from "eslint-plugin-tailwindcss";
import eslintUnusedImports from "eslint-plugin-unused-imports";
import globals from "globals";
import tseslint from "typescript-eslint";

export default tseslint.config(
  { ignores: ["dist"] },
  ...tseslint.configs.recommended,
  {
    files: ["**/*.{ts,tsx}"],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    plugins: {
      "react-hooks": reactHooks,
      "react-refresh": reactRefresh,
      prettier: eslintPluginPrettier,
      react: eslintPluginReact,
      "unused-imports": eslintUnusedImports,
      import: eslintPluginImport,
      tailwindcss: eslintPluginTailwindCSS,
      "react-query": eslintPluginReactQuery,
      perfectionist: pkg,
    },
    settings: {
      react: {
        version: "detect",
      },
    },
    rules: {
      ...(reactHooks.configs?.recommended?.rules ?? {}),
      ...(eslintPluginReact.configs?.all?.rules ?? {}),
      ...(eslintPluginTailwindCSS.configs?.all?.rules ?? {}),
      ...(eslintPluginImport.configs?.all?.rules ?? {}),
      ...(eslintPluginPrettier.configs?.recommended?.rules ?? {}),
      "react-refresh/only-export-components": [
        "warn",
        { allowConstantExport: true },
      ],
      "react/jsx-indent": ["error", 2],
      "react/react-in-jsx-scope": "off",
      "react/jsx-filename-extension": [
        "error",
        { extensions: [".jsx", ".tsx"] },
      ],
      "react/jsx-no-literals": "off",
      "arrow-body-style": "off",
      "prefer-arrow-callback": "off",
      "max-len": "off",
      "react/jsx-indent": "off",
      "react/jsx-indent-props": "off",
      "react/jsx-one-expression-per-line": "off",
      "react/jsx-wrap-multilines": "off",
      "react/jsx-one-expression-per-line": "off",
      "react/jsx-tag-spacing": "off",
      "react/jsx-child-element-spacing": "off",
      "react/jsx-closing-bracket-location": "off",
      "react/jsx-closing-tag-location": "off",
      "react/jsx-curly-newline": "off",
      "react/jsx-curly-spacing": "off",
      "react/jsx-equals-spacing": "off",
      "react/jsx-first-prop-new-line": "off",
      "react/jsx-max-props-per-line": "off",
      "react/jsx-props-no-multi-spaces": "off",
      "react/jsx-space-before-closing": "off",
      "react/jsx-tag-spacing": "off",
      "react/jsx-newline": "off",
      "react/require-default-props": "off",
      "react/jsx-max-depth": ["warn", { max: 6 }],
      "react/forbid-component-props": "off",
      "react/jsx-sort-props": [
        "warn",
        {
          callbacksLast: true,
          shorthandFirst: true,
          noSortAlphabetically: false,
          reservedFirst: true,
        },
      ],
    },
  },
);
