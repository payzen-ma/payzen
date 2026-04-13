/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./src/**/*.{html,ts,tsx}",
        "./src/app/**/*.{html,ts,tsx}",
    ],
    theme: {
        extend: {
            /* ============================================
               COLORS - Integrated with CSS variables
               ============================================ */
            colors: {
                /* Primary - Blue PayZen */
                primary: {
                    50: '#ebf5ff',
                    100: '#d6ebff',
                    200: '#aed6ff',
                    300: '#85c2ff',
                    400: '#5cadff',
                    500: '#1a73e8',
                    600: '#1557b0',
                    700: '#0f4187',
                    800: '#0a2c5e',
                    900: '#051835',
                    950: '#020c1a',
                },
                /* Neutral - Grays */
                neutral: {
                    white: '#ffffff',
                    50: '#f8fafc',
                    100: '#f1f5f9',
                    200: '#e2e8f0',
                    300: '#cbd5e1',
                    400: '#94a3b8',
                    500: '#64748b',
                    600: '#475569',
                    700: '#344155',
                    800: '#1e293b',
                },
                /* Semantic Status Colors */
                success: '#16a34a',
                'success-light': '#d1fae5',
                'success-dark': '#065f46',

                warning: '#b35109',
                'warning-light': '#fcd44d',
                'warning-dark': '#92400e',

                danger: '#dc2626',
                'danger-light': '#fee2e2',
                'danger-dark': '#991b1b',

                info: '#3b82f6',
                'info-light': '#dbeafe',
                'info-dark': '#0369a1',
            },

            /* ============================================
               SPACING - 4px base unit
               ============================================ */
            spacing: {
                '1': '4px',
                '2': '8px',
                '3': '12px',
                '4': '16px',
                '5': '20px',
                '6': '24px',
                '8': '32px',
                '10': '40px',
                '12': '48px',
                '16': '64px',
                '20': '80px',
            },

            /* ============================================
               BORDER RADIUS
               ============================================ */
            borderRadius: {
                'sm': '4px',
                'md': '6px',
                'lg': '8px',
                'xl': '12px',
                '2xl': '16px',
                'full': '9999px',
            },

            /* ============================================
               SHADOWS / ELEVATIONS
               ============================================ */
            boxShadow: {
                'xs': '0px 1px 2px 0px rgba(0, 0, 0, 0.05)',
                'sm': '0px 1px 3px 0px rgba(0, 0, 0, 0.1)',
                'md': '0px 4px 6px 0px rgba(0, 0, 0, 0.1)',
                'lg': '0px 10px 15px 0px rgba(0, 0, 0, 0.1)',
                'xl': '0px 20px 25px 0px rgba(0, 0, 0, 0.1)',
            },

            /* ============================================
               TYPOGRAPHY
               ============================================ */
            fontSize: {
                'xs': ['12px', { lineHeight: '1.4', fontWeight: '500' }],  // Caption/Badge
                'sm': ['14px', { lineHeight: '1.5', fontWeight: '400' }],  // Small body
                'base': ['16px', { lineHeight: '1.5', fontWeight: '400' }], // Body
                'lg': ['18px', { lineHeight: '1.4', fontWeight: '500' }],  // Subheading
                'xl': ['20px', { lineHeight: '1.3', fontWeight: '600' }],  // Card Heading
                '2xl': ['24px', { lineHeight: '1.3', fontWeight: '600' }], // Section Title
                '3xl': ['30px', { lineHeight: '1.2', fontWeight: '700' }], // Page Title
                '4xl': ['36px', { lineHeight: '1.2', fontWeight: '700' }], // Display Heading
            },

            fontFamily: {
                inter: ['Inter', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'sans-serif'],
            },

            /* ============================================
               ANIMATION / TRANSITIONS
               ============================================ */
            transitionDuration: {
                '200': '200ms',
            },

            /* ============================================
               Z-INDEX LAYERS
               ============================================ */
            zIndex: {
                'tooltip': '1000',
                'modal': '1040',
                'popover': '1030',
                'dropdown': '1020',
            },
        },
    },
    plugins: [],
    safelist: [
        /* Ensure badge colors are available */
        'bg-success-light', 'text-success-dark',
        'bg-warning-light', 'text-warning-dark',
        'bg-danger-light', 'text-danger-dark',
        'bg-info-light', 'text-info-dark',
    ],
};
