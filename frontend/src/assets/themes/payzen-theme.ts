import { definePreset } from '@primeng/themes';
import Aura from '@primeng/themes/aura';

/**
 * PayZen Custom Theme Preset
 * Based on PrimeNG Aura with custom design tokens
 *
 * Design Philosophy:
 * - Clarity & Professionalism
 * - Action-oriented with Primary Blue
 * - Modular card-based layout
 */
export const PayZenTheme = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#EBF5FF',
      100: '#D6EBFF',
      200: '#AED6FF',
      300: '#85C2FF',
      400: '#5CADFF',
      500: '#1A73E8', // Main primary color
      600: '#1557B0',
      700: '#0F4187',
      800: '#0A2C5E',
      900: '#051835',
      950: '#020C1A',
    },
    colorScheme: {
      light: {
        // Surface colors
        surface: {
          0: '#FFFFFF',        // --color-bg-element (cards, sidebar)
          50: '#F8FAFC',       // --color-bg-page (page background)
          100: '#F1F5F9',
          200: '#E2E8F0',
          300: '#CBD5E1',
          400: '#94A3B8',
          500: '#64748B',
          600: '#475569',
          700: '#334155',
          800: '#1E293B',
          900: '#0F172A',
          950: '#020617',
        },

        // Primary color variations
        primary: {
          color: '#1A73E8',           // --color-primary
          contrastColor: '#FFFFFF',   // White text on primary
          hoverColor: '#1557B0',      // 10% darker for hover state
          activeColor: '#1557B0',
        },

        // Text colors
        text: {
          color: '#1F2937',           // --color-text-primary
          hoverColor: '#111827',
          mutedColor: '#6B7280',      // Secondary text
          hoverMutedColor: '#4B5563',
        },

        // Border & outlines
        content: {
          borderColor: '#E5E7EB',     // --color-border-subtle
          background: '#FFFFFF',
          hoverBackground: '#F9FAFB',
          color: '#1F2937',
          hoverColor: '#111827',
        },

        // Navigation highlights
        navigation: {
          item: {
            focusBackground: '#EBF5FF',   // --color-primary-light
            color: '#475569',
            focusColor: '#1A73E8',
            icon: {
              color: '#64748B',
              focusColor: '#1A73E8',
            },
          },
        },
      },
    },
  },
});
