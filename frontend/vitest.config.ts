import { defineConfig } from 'vitest/config';
import { resolve } from 'path';

export default defineConfig({
  test: {
    globals: true,
    environment: 'happy-dom',
    setupFiles: [resolve(__dirname, 'tests/setup.ts')],

    coverage: {
      provider: 'v8',
      reporter: ['text', 'html'],
      exclude: [
        '**/*.d.ts',
        '**/*.config.ts',
        'tests/**',
        'src/**/*.entry.ts',
      ],
    },

    include: ['tests/**/*.test.ts'],
  },

  resolve: {
    alias: {
      '@': resolve(__dirname, 'src'),
    }
  }
});
