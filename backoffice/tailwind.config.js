/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}"
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: '#1A73E8',
          50: '#E8F0FE',
          100: '#D2E3FC',
          200: '#AECBFA',
          300: '#8AB4F8',
          400: '#669DF6',
          500: '#1A73E8',
          600: '#1557B0',
          700: '#0D47A1',
          800: '#0A3A82',
          900: '#062E63'
        },
        surface: {
          DEFAULT: '#F8FAFC',
          50: '#FAFBFC',
          100: '#F8FAFC',
          200: '#F1F5F9',
          300: '#E2E8F0',
          400: '#CBD5E1',
          500: '#94A3B8',
          600: '#64748B',
          700: '#475569',
          800: '#334155',
          900: '#1E293B'
        },
        border: '#E5E7EB',
        text: {
          primary: '#1F2937',
          secondary: '#6B7280'
        }
      }
    }
  }
};
