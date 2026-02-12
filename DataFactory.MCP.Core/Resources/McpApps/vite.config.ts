import { defineConfig } from "vite";
import { viteSingleFile } from "vite-plugin-singlefile";
import react from "@vitejs/plugin-react";
import { resolve } from "node:path";
import { readdirSync, existsSync, renameSync, mkdirSync, rmSync } from "node:fs";

// Automatically discover all apps in src/ that have an index.html
function discoverApps(): Record<string, string> {
  const srcDir = resolve(__dirname, "src");
  const apps: Record<string, string> = {};

  if (!existsSync(srcDir)) {
    return apps;
  }

  const dirs = readdirSync(srcDir, { withFileTypes: true })
    .filter((dirent) => dirent.isDirectory() && dirent.name !== "shared")
    .map((dirent) => dirent.name);

  for (const dir of dirs) {
    const indexPath = resolve(srcDir, dir, "index.html");
    if (existsSync(indexPath)) {
      apps[dir] = indexPath;
    }
  }

  return apps;
}

const apps = discoverApps();

export default defineConfig({
  root: __dirname,
  build: {
    outDir: "dist",
    emptyOutDir: true,
    rollupOptions: {
      input: apps,
    },
  },
  plugins: [
    react(),
    viteSingleFile(),
    {
      // Post-build plugin to flatten output structure
      name: "flatten-output",
      closeBundle() {
        const distDir = resolve(__dirname, "dist");
        const srcDir = resolve(distDir, "src");
        
        if (!existsSync(srcDir)) return;

        // Move each app's index.html to dist/{appName}.html
        const appDirs = readdirSync(srcDir, { withFileTypes: true })
          .filter((d) => d.isDirectory())
          .map((d) => d.name);

        for (const appName of appDirs) {
          const srcPath = resolve(srcDir, appName, "index.html");
          const destPath = resolve(distDir, `${appName}.html`);
          if (existsSync(srcPath)) {
            renameSync(srcPath, destPath);
            console.log(`  Moved: ${appName}/index.html -> ${appName}.html`);
          }
        }

        // Clean up src folder
        rmSync(srcDir, { recursive: true, force: true });
      },
    },
  ],
});
