# PayZen Custom Theme

Custom PrimeNG Aura theme preset based on the PayZen UI/UX design specifications.

## 📋 Overview

This custom theme extends PrimeNG's Aura preset with PayZen-specific design tokens, ensuring consistency across the entire application.

### Design Philosophy

- **Clarity & Professionalism**: Clean interface without distractions
- **Action-Oriented**: Primary Blue (#1A73E8) guides users to critical actions
- **Modularity**: Card-based layout for easy maintenance and reorganization

## 🎨 Color Palette

### Primary Colors

| Name | HEX | CSS Variable | Usage |
|------|-----|--------------|-------|
| Primary | `#1A73E8` | `--color-primary` | Buttons, links, active menu states |
| Primary Hover | `#1557B0` | `--color-primary-hover` | Hover states (10% darker) |
| Primary Light | `#EBF5FF` | `--color-primary-light` | Active menu backgrounds |

### Background Colors

| Name | HEX | CSS Variable | Usage |
|------|-----|--------------|-------|
| Page Background | `#F8FAFC` | `--color-bg-page` | Main background |
| Element Background | `#FFFFFF` | `--color-bg-element` | Cards, sidebar |
| Hover Background | `#F9FAFB` | `--color-bg-hover` | Interactive elements hover |

### Text Colors

| Name | HEX | CSS Variable | Usage |
|------|-----|--------------|-------|
| Primary Text | `#1F2937` | `--color-text-primary` | Headings, paragraphs |
| Secondary Text | `#6B7280` | `--color-text-secondary` | Supporting text |
| Muted Text | `#9CA3AF` | `--color-text-muted` | Placeholders, disabled |

### Borders

| Name | HEX | CSS Variable | Usage |
|------|-----|--------------|-------|
| Subtle | `#E5E7EB` | `--color-border-subtle` | Card outlines, grid lines |
| Medium | `#D1D5DB` | `--color-border-medium` | Input borders on hover |

### Semantic Colors

| Type | Background | Text | Border |
|------|-----------|------|--------|
| Success | `#DCFCE7` | `#16A34A` | `#16A34A` |
| Warning | `#FEF3C7` | `#D97706` | `#D97706` |
| Danger | `#FEE2E2` | `#DC2626` | `#DC2626` |
| Info | `#E0F2FE` | `#0284C7` | `#0284C7` |

## 📐 Typography

### Font Family

```css
--font-family-base: system-ui, -apple-system, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
```

### Font Sizes

| Usage | Size | CSS Variable |
|-------|------|--------------|
| Grand Titre (Page Heading) | `2rem` (32px) | `--font-size-3xl` |
| Titre de Carte (Card Title) | `1.25rem` (20px) | `--font-size-xl` |
| Texte de Corps (Body Text) | `1rem` (16px) | `--font-size-base` |
| Small Text | `0.875rem` (14px) | `--font-size-sm` |
| Extra Small | `0.75rem` (12px) | `--font-size-xs` |

### Font Weights

| Type | Weight | CSS Variable |
|------|--------|--------------|
| Normal | 400 | `--font-weight-normal` |
| Medium | 500 | `--font-weight-medium` |
| Semi-Bold | 600 | `--font-weight-semibold` |
| Bold | 700 | `--font-weight-bold` |

## 🧱 Components

### Card Component

```html
<div class="card">
  <h3 class="card-title">Card Title</h3>
  <p>Card content goes here</p>
</div>
```

**Properties:**
- Background: `#FFFFFF`
- Border Radius: `8px`
- Padding: `24px` (1.5rem)
- Shadow: Subtle elevation
- Hover: Elevated shadow

**Variants:**
- `.card-compact` - Reduced padding (16px)
- `.card-elevated` - Enhanced shadow

### Stat Card Component

```html
<div class="stat-card">
  <div class="stat-card-icon text-blue-500">
    <i class="pi pi-users"></i>
  </div>
  <div>
    <p class="stat-label">Total Salariés</p>
    <p class="stat-value">150</p>
  </div>
</div>
```

**Properties:**
- Display: Flex with gap
- Icon Size: `3rem` (48px)
- Hover: Slight elevation + transform

### Button Component

```html
<button class="btn btn-primary">
  <i class="pi pi-plus"></i>
  Action Button
</button>
```

**Variants:**
- `.btn-primary` - Main action (Blue background)
- `.btn-secondary` - Secondary action (Gray background)
- `.btn-success` - Success action (Green)
- `.btn-warning` - Warning action (Orange)
- `.btn-danger` - Dangerous action (Red)

**Properties:**
- Border Radius: `6px`
- Padding: `0.75rem 1rem`
- Font Size: `0.875rem` (14px)
- Font Weight: 500 (Medium)
- Hover: 10% darker

### Sidebar Navigation

**Active State:**
- Background: `#EBF5FF` (Primary Light)
- Left Border: `3px solid #1A73E8`
- Text Color: `#1A73E8`
- Icon Color: `#1A73E8`

**Hover State:**
- Background: `#EBF5FF`
- Transition: `0.2s`

### Input Fields

```html
<input type="text" class="input" placeholder="Enter text...">
```

**Properties:**
- Border: `1px solid #E5E7EB`
- Border Radius: `6px`
- Padding: `0.75rem`
- Focus Border: `#1A73E8`
- Focus Ring: `0.2rem rgba(26, 115, 232, 0.2)`

### Alert Component

```html
<div class="alert alert-success">
  <i class="pi pi-check-circle"></i>
  <div>Success message</div>
</div>
```

**Variants:**
- `.alert-success` - Green tint
- `.alert-warning` - Yellow tint
- `.alert-error` - Red tint
- `.alert-info` - Blue tint

## 📱 Responsive Design

### Breakpoints

| Device | Min Width | Max Width | Sidebar | Grid |
|--------|-----------|-----------|---------|------|
| Desktop (L+) | `1024px` | - | `240px` fixed | 2-3 columns |
| Tablet (M) | `641px` | `1023px` | `60px` icons only | 2 columns or stacked |
| Mobile (XS) | - | `640px` | Hidden (hamburger) | Stacked (100% width) |

### Mobile-First Approach

The theme is designed mobile-first:
1. Base styles apply to mobile
2. Progressive enhancement for larger screens
3. Fluid transitions between breakpoints

### Sidebar Behavior

**Desktop (≥1024px):**
- Always visible
- Width: `240px`
- Full text + icons

**Tablet (641-1023px):**
- Compact mode
- Width: `60px`
- Icons only

**Mobile (≤640px):**
- Hidden by default
- Accessible via hamburger menu
- Overlay when open

## 🚀 Usage

### 1. Installation

The theme is automatically configured in `app.config.ts`:

```typescript
import { providePrimeNG } from 'primeng/config';
import { PayZenTheme } from '../assets/themes/payzen-theme';

export const appConfig: ApplicationConfig = {
  providers: [
    providePrimeNG({
      theme: {
        preset: PayZenTheme
      }
    })
  ]
};
```

### 2. Using Design Tokens

**In CSS:**

```css
.my-component {
  background: var(--color-bg-element);
  color: var(--color-text-primary);
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--rads-lg);
  padding: var(--space-6);
}
```

**With Tailwind (v4):**

```html
<div class="bg-white text-gray-900 border border-gray-200 rounded-lg p-6">
  Content
</div>
```

### 3. Component Classes

**Typography:**

```html
<h1 class="heading">Main Title</h1>
<p class="subheading">Subtitle text</p>
<h3 class="card-title">Card Title</h3>
```

**Cards:**

```html
<div class="card">
  <div class="card-header">
    <h3 class="card-title">Title</h3>
    <button class="btn btn-primary">Action</button>
  </div>
  <p>Card content...</p>
</div>
```

**Buttons:**

```html
<button class="btn btn-primary">Primary</button>
<button class="btn btn-secondary">Secondary</button>
<button class="btn btn-danger">Delete</button>
```

**Stat Cards:**

```html
<div class="stat-card">
  <div class="stat-card-icon text-blue-500">
    <i class="pi pi-users"></i>
  </div>
  <div>
    <p class="stat-label">Label</p>
    <p class="stat-value">123</p>
  </div>
</div>
```

## 🎯 Design Principles

### 1. Consistency

All components follow the same design language:
- Consistent spacing (multiples of 4px)
- Unified color palette
- Standard border radius values
- Harmonized shadows

### 2. Accessibility

- Sufficient color contrast (WCAG AA compliant)
- Clear focus states
- Readable font sizes
- Semantic color usage

### 3. Performance

- CSS custom properties for theming
- Minimal specificity
- Efficient selectors
- Reusable utility classes

### 4. Maintainability

- Centralized design tokens
- Documented components
- Modular structure
- Clear naming conventions

## 📂 File Structure

```
src/
├── assets/
│   ├── themes/
│   │   ├── payzen-theme.ts       # PrimeNG theme preset
│   │   └── README.md             # This documentation
│   └── styles/
│       └── design-tokens.css     # Global design tokens & utilities
├── app/
│   └── app.config.ts             # Theme configuration
└── styles.css                     # Global styles (imports design-tokens.css)
```

## 🔄 Updating the Theme

To modify the theme:

1. **Color Changes**: Edit `payzen-theme.ts` semantic colors
2. **Component Styles**: Update component-specific configs in `payzen-theme.ts`
3. **Global Tokens**: Modify `design-tokens.css` for app-wide variables
4. **Utility Classes**: Add new utilities to `design-tokens.css`

## 📚 References

- [PrimeNG Theming Documentation](https://primeng.org/theming)
- [Tailwind CSS v4 Documentation](https://tailwindcss.com/docs)
- PayZen UI/UX Specifications (Cahier des Charges - 24 Novembre 2025)

## ✨ Best Practices

1. **Use design tokens** instead of hardcoded values
2. **Leverage utility classes** for common patterns
3. **Follow component patterns** for consistency
4. **Test responsive behavior** at all breakpoints
5. **Maintain accessibility** standards
6. **Document custom components** as they're added

---

**Version:** 1.0.0
**Last Updated:** December 2025
**Maintained by:** PayZen Development Team
