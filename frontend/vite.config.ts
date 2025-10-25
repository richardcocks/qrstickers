import { defineConfig } from 'vite';
import { resolve } from 'path';

export default defineConfig({
  build: {
    // Output to ASP.NET wwwroot
    outDir: resolve(__dirname, '../src/wwwroot/dist'),
    emptyOutDir: true,

    // Generate source maps for debugging
    sourcemap: true,

    // Multiple entry points for different pages
    rollupOptions: {
      input: {
        designer: resolve(__dirname, 'src/pages/designer/designer.entry.ts'),
        devices: resolve(__dirname, 'src/pages/devices/devices.entry.ts'),
      }
    },

    // Generate manifest.json for cache-busting
    manifest: true,
  },

  // Development server
  server: {
    port: 5173,
    strictPort: true,
    cors: true,

    hmr: {
      protocol: 'ws',
      host: 'localhost',
      port: 5173,
    }
  },

  // Pre-bundle dependencies
  optimizeDeps: {
    include: ['fabric', 'jszip']
  }
});
