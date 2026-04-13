# 🎨 PayZen HR - Design System Implementation Guide

**Source:** Figma Design System
**URL:** https://www.figma.com/design/qyN1xWJHA33vgi7ne4DbeU/Payzen-HR
**Technology Stack:** Angular 20 + TailwindCSS v4 + PrimeNG
**Date:** April 2026

---

## 📋 Table of Contents

1. [Color Palette](#color-palette)
2. [Typography](#typography)
3. [Spacing System](#spacing-system)
4. [Border Radius](#border-radius)
5. [Shadows & Elevation](#shadows--elevation)
6. [Components](#components)
7. [Usage Examples](#usage-examples)
8. [CSS Variables Reference](#css-variables-reference)

---

## 🎨 Color Palette

### Primary Colors (Blue - Brand)

```
#ebf5ff  ← 50  (lightest)
#d6ebff  ← 100
#aed6ff  ← 200
#85c2ff  ← 300
#5cadff  ← 400
#1a73e8  ← 500 ★ (main brand)
#1557b0  ← 600 (hover state)
#0f4187  ← 700 (active state)
#0a2c5e  ← 800
#051835  ← 900 (darkest)
```

**CSS Variables:**
```css
var(--primary-500)        /* #1a73e8 - Main brand */
var(--primary-600)        /* #1557b0 - Hover */
var(--primary-700)        /* #0f4187 - Active */
var(--primary-light)      /* #d6ebff - Light backgrounds */
var(--primary-dark)       /* #0f4187 - Dark text */
```

### Neutral Colors (Grays)

```
#ffffff  ← White (element backgrounds)
#f8fafc  ← 50  (page background)
#f1f5f9  ← 100
#e2e8f0  ← 200
#cbd5e1  ← 300
#94a3b8  ← 400
#64748b  ← 500
#475569  ← 600
#344155  ← 700
#1e293b  ← 800 (darkest)
```

**CSS Variables:**
```css
var(--neutral-white)      /* #ffffff */
var(--neutral-50)         /* #f8fafc */
var(--text-primary)       /* #1f2937 - Main text */
var(--text-secondary)     /* #6b7280 - Secondary text */
var(--text-muted)         /* #9ca3af - Muted text */
var(--border-subtle)      /* #e5e7eb - Light borders */
var(--border-medium)      /* #d1d5db - Medium borders */
```

### Semantic Colors

#### Success (Green)
```css
--success: #16a34a;           /* Main success */
--success-light: #d1fae5;     /* Light background */
--success-hover: #15803d;     /* Hover state */
--success-text: #065f46;      /* Text color */
```

#### Warning (Orange)
```css
--warning: #b35109;           /* Main warning */
--warning-light: #fcd44d;     /* Light background */
--warning-hover: #a84a09;     /* Hover state */
--warning-text: #92400e;      /* Text color */
```

#### Danger (Red)
```css
--danger: #dc2626;            /* Main danger */
--danger-light: #fee2e2;      /* Light background */
--danger-hover: #b91c1c;      /* Hover state */
--danger-text: #991b1b;       /* Text color */
```

#### Info (Blue)
```css
--info: #3b82f6;              /* Main info */
--info-light: #dbeafe;        /* Light background */
--info-hover: #1d4ed8;        /* Hover state */
--info-text: #0369a1;         /* Text color */
```

#### CDI (Contract Type)
```
Background: #e0f2fe  Text: #0369a1
```

#### CDD (Contract Type)
```
Background: #fae8ff  Text: #7e22ce
```

---

## 📝 Typography

All text uses the **Inter** font family.

### Typography Scale

| Level | Size | Weight | Usage | CSS Variable |
|-------|------|--------|-------|--------------|
| 4xl | 36px | Bold (700) | Display Heading | `--font-size-4xl` |
| 3xl | 30px | Bold (700) | Page Title | `--font-size-3xl` |
| 2xl | 24px | Semi Bold (600) | Section Title | `--font-size-2xl` |
| xl | 20px | Semi Bold (600) | Card Heading | `--font-size-xl` |
| lg | 18px | Medium (500) | Subheading text | `--font-size-lg` |
| base | 16px | Regular (400) | Body text | `--font-size-base` |
| sm | 14px | Regular (400) | Small body, labels | `--font-size-sm` |
| xs | 12px | Medium (500) | Caption, Badge | `--font-size-xs` |

### Usage in HTML/CSS

```html
<!-- Using Tailwind classes -->
<h1 class="text-4xl">Display Heading</h1>
<h2 class="text-3xl">Page Title</h2>
<h3 class="text-2xl">Section Title</h3>
<h4 class="text-xl">Card Heading</h4>
<p class="text-lg">Subheading text</p>
<p class="text-base">Body text...</p>
<p class="text-sm">Small body text...</p>
<span class="text-xs">Caption or Badge</span>
```

---

## 📏 Spacing System

**Base Unit:** 4px (1 = 4px, 2 = 8px, etc.)

| Token | Value | CSS Variable | Usage |
|-------|-------|--------------|-------|
| space-1 | 4px | `--space-1` | Extra small gap |
| space-2 | 8px | `--space-2` | Small gap |
| space-3 | 12px | `--space-3` | Small-medium gap |
| space-4 | 16px | `--space-4` | Medium gap (default) |
| space-5 | 20px | `--space-5` | Medium-large gap |
| space-6 | 24px | `--space-6` | Large gap |
| space-8 | 32px | `--space-8` | Extra large gap |
| space-10 | 40px | `--space-10` | 2.5x large gap |
| space-12 | 48px | `--space-12` | 3x large gap |

### Usage in HTML

```html
<!-- Tailwind classes (auto-map to variables) -->
<div class="p-4">Padding 16px</div>
<div class="m-6">Margin 24px</div>
<div class="gap-4">Gap 16px</div>
<div class="px-4 py-2">Horizontal 16px, Vertical 8px</div>
```

---

## ⚫ Border Radius

| Token | Value | CSS Variable | Usage |
|-------|-------|--------------|-------|
| sm | 4px | `--radius-sm` | Subtle rounding |
| md | 6px | `--radius-md` | Default buttons |
| lg | 8px | `--radius-lg` | Cards, panels |
| xl | 12px | `--radius-xl` | Large components |
| 2xl | 16px | `--radius-2xl` | Extra large |
| full | 9999px | `--radius-full` | Circular/Pills |

### Usage

```html
<div class="rounded-md">Button (6px)</div>
<div class="rounded-lg">Card (8px)</div>
<div class="rounded-full">Circular (pill shape)</div>
```

---

## ✨ Shadows & Elevation

| Shadow | CSS Value | Usage |
|--------|-----------|-------|
| shadow-xs | `0px 1px 2px 0px rgba(0,0,0,0.05)` | Subtle hover state |
| shadow-sm | `0px 1px 3px 0px rgba(0,0,0,0.1)` | Buttons, inputs |
| shadow-md | `0px 4px 6px 0px rgba(0,0,0,0.1)` | Cards (default) |
| shadow-lg | `0px 10px 15px 0px rgba(0,0,0,0.1)` | Modals, dropdowns |
| shadow-xl | `0px 20px 25px 0px rgba(0,0,0,0.1)` | Overlays, top-level |

### Usage

```html
<div class="shadow-md">Standard Card</div>
<div class="shadow-lg">Modal/Dropdown</div>
```

---

## 🔘 Components

### 1. Buttons

#### Variants

| Variant | Background | Text | Border | Hover |
|---------|------------|------|--------|-------|
| Primary | #1a73e8 | White | None | #1557b0 |
| Secondary | White | #374151 | #d1d5db | #f9fafb |
| Ghost | #ebf5ff | #1a73e8 | None | More opaque |
| Danger | #ef4444 | White | None | #dc2626 |
| Disabled | #f3f4f6 | #9ca3af | #e5e7eb | No change |

#### Sizes

| Size | Padding | Font Size | Usage |
|------|---------|-----------|-------|
| Small | 6px 12px | 12px | Compact, secondary |
| Medium | 8px 16px | 14px | Default, primary action |
| Large | 12px 24px | 16px | Call-to-action |

#### HTML Example

```html
<!-- Primary Button - Medium (default) -->
<button class="bg-primary-500 text-white px-4 py-2 rounded-md shadow-sm hover:bg-primary-600">
  Primary
</button>

<!-- Secondary Button -->
<button class="bg-white text-gray-700 border border-gray-300 px-4 py-2 rounded-md">
  Secondary
</button>

<!-- Danger Button -->
<button class="bg-danger text-white px-4 py-2 rounded-md">
  Delete
</button>

<!-- Disabled Button -->
<button class="bg-gray-100 text-gray-300 border border-gray-200 px-4 py-2 rounded-md" disabled>
  Disabled
</button>
```

### 2. Badges & Status Chips

**Style:** Rounded (border-radius: full), small padding

| Badge | Background | Text | Usage |
|-------|------------|------|-------|
| Active | #d1fae5 | #065f46 | Employee status |
| On Leave | #fef3c7 | #92400e | Leave status |
| Inactive | #fee2e2 | #991b1b | Inactive status |
| Draft | #f1f5f9 | #475569 | Draft document |
| Published | #eff6ff | #1d4ed8 | Published status |
| Deprecated | #f5f3ff | #6d28d9 | Deprecated |
| Warning | #fef9c3 | #854d0e | Warning status |
| Error | #fee2e2 | #b91c1c | Error status |
| CDI | #e0f2fe | #0369a1 | Contract type |
| CDD | #fae8ff | #7e22ce | Contract type |

#### HTML Example

```html
<span class="bg-success-light text-success-text px-2.5 py-1 rounded-full text-xs font-medium">
  Active
</span>

<span class="bg-warning-light text-warning-text px-2.5 py-1 rounded-full text-xs font-medium">
  Warning
</span>
```

### 3. Cards

**Base Style:**
- Background: White
- Border: 1px solid #e5e7eb
- Padding: 24px (content cards) or 20px (stat cards)
- Border Radius: 8px
- Shadow: shadow-md

#### Content Card (Default)

```html
<div class="bg-white border border-gray-200 rounded-lg p-6 shadow-md">
  <div class="flex items-center justify-between mb-4">
    <h3 class="text-lg font-semibold">Employee Summary</h3>
    <span class="bg-success-light text-success-text px-2.5 py-1 rounded-full text-xs">
      Active
    </span>
  </div>

  <p class="text-gray-600 text-sm mb-6">
    Displays key employee metrics and current status at a glance.
  </p>

  <div class="flex justify-end">
    <button class="bg-primary-500 text-white px-3.5 py-2 rounded-md text-sm">
      View Details
    </button>
  </div>
</div>
```

#### Stat Card (Compact)

```html
<div class="bg-white border border-gray-200 rounded-lg p-5 shadow-md">
  <p class="text-gray-500 text-sm font-medium mb-2">Total Employees</p>
  <p class="text-gray-900 text-4xl font-bold">247</p>

  <div class="flex items-center gap-1.5 mt-4">
    <span class="bg-success-light text-success-text px-1.5 py-0.5 rounded text-xs font-semibold">
      +12%
    </span>
    <p class="text-gray-400 text-xs">vs last month</p>
  </div>
</div>
```

### 4. Form Inputs

**Base Style:**
- Background: White
- Height: 40px
- Padding: 10px 12px
- Border Radius: 6px
- Border: 1px solid

| State | Border Color | Background | Text | Icon |
|-------|--------------|------------|------|------|
| Default | #d1d5db | White | #9ca3af (placeholder) | Gray |
| Focus | #1a73e8 (2px) | White | #1f2937 | Blue |
| Error | #ef4444 | #fef2f2 | #ef4444 | Red |
| Disabled | #e5e7eb | #f9fafb | #d1d5db | Gray |

#### HTML Example

```html
<!-- Default Input -->
<input
  type="text"
  placeholder="Enter value..."
  class="w-full h-10 px-3 py-2.5 border border-gray-300 rounded-md bg-white text-gray-900 placeholder-gray-400"
/>

<!-- Focus State Input -->
<input
  type="text"
  placeholder="Enter value..."
  class="w-full h-10 px-3 py-2.5 border-2 border-primary-500 rounded-md bg-white text-gray-900 shadow-sm"
/>

<!-- Error State Input -->
<div>
  <input
    type="text"
    value="Invalid value"
    class="w-full h-10 px-3 py-2.5 border border-danger rounded-md bg-red-50 text-danger"
  />
  <p class="text-danger text-xs mt-1">⚠ This field is required</p>
</div>

<!-- Disabled Input -->
<input
  type="text"
  placeholder="Disabled..."
  class="w-full h-10 px-3 py-2.5 border border-gray-200 rounded-md bg-gray-50 text-gray-300"
  disabled
/>
```

---

## 💡 Usage Examples

### Creating a Header with Title

```html
<div class="bg-page p-16 mb-8">
  <h1 class="text-4xl font-bold text-primary-500 mb-2">
    PayZen Design System
  </h1>
  <p class="text-gray-600">
    Colors · Typography · Spacing · Radius · Shadows · Components
  </p>
</div>
```

### Creating a Section

```html
<section class="mb-12">
  <h2 class="text-2xl font-semibold text-gray-900 mb-6">Color Palette</h2>
  <div class="flex gap-4 flex-wrap">
    <!-- Color swatches -->
    <div class="flex flex-col items-center gap-2">
      <div class="w-16 h-16 rounded-lg bg-primary-500 shadow-md"></div>
      <p class="text-xs font-medium text-gray-600">Primary</p>
    </div>
  </div>
</section>
```

### Creating a Data Table with Cards

```html
<div class="grid grid-cols-3 gap-6">
  <!-- Employee Summary Card -->
  <div class="bg-white border border-gray-200 rounded-lg p-6 shadow-md">
    <div class="flex justify-between items-start mb-4">
      <h3 class="text-lg font-semibold">Employee Summary</h3>
      <span class="bg-success-light text-success-text px-2.5 py-1 rounded-full text-xs">Active</span>
    </div>
    <p class="text-gray-600 text-sm">Displays key employee metrics at a glance.</p>
  </div>

  <!-- Stat Card -->
  <div class="bg-white border border-gray-200 rounded-lg p-5 shadow-md">
    <p class="text-gray-500 text-sm font-medium">Total Employees</p>
    <p class="text-gray-900 text-4xl font-bold mt-2">247</p>
    <div class="flex items-center gap-1.5 mt-4">
      <span class="bg-success-light text-success-text px-1.5 py-0.5 rounded text-xs font-semibold">+12%</span>
      <p class="text-gray-400 text-xs">vs last month</p>
    </div>
  </div>
</div>
```

---

## 📚 CSS Variables Reference

### Complete List

```css
/* Colors - Primary */
--primary-50 through --primary-950
--primary, --primary-hover, --primary-light, --primary-dark

/* Colors - Neutral */
--neutral-white, --neutral-50 through --neutral-800

/* Colors - Semantic */
--success, --success-light, --success-hover, --success-text
--warning, --warning-light, --warning-hover, --warning-text
--danger, --danger-light, --danger-hover, --danger-text
--info, --info-light, --info-hover, --info-text

/* Text & Border */
--text-primary, --text-secondary, --text-muted, --text-inverse, --text-danger
--border-subtle, --border-medium, --border-strong

/* Background */
--bg-page, --bg-element, --bg-hover, --bg-active, --bg-muted, --bg-disabled

/* Spacing */
--space-1 through --space-12

/* Typography */
--font-family-base
--font-size-4xl through --font-size-xs
--font-weight-4xl through --font-weight-xs
--line-height-4xl through --line-height-xs

/* Borders & Radius */
--radius-sm through --radius-full

/* Shadows */
--shadow-xs through --shadow-xl
```

---

## 🔗 Integration Guide

### 1. Import Design System Stylesheet

In `src/styles.css`:

```css
@import 'url("./styles/design-system.css");';
@import "tailwindcss";
```

### 2. Use in Components

```html
<!-- Typography -->
<h1 class="text-4xl font-bold text-primary-500">Title</h1>

<!-- Spacing -->
<div class="p-6 mb-8">Content</div>

<!-- Components -->
<button class="bg-primary-500 text-white px-4 py-2 rounded-md">
  Click me
</button>

<!-- Semantic Colors -->
<span class="bg-success-light text-success-text">Success</span>
```

### 3. CSS Variables in Custom CSS

```css
.custom-component {
  background-color: var(--bg-element);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg);
  padding: var(--space-4);
  box-shadow: var(--shadow-md);
  font-size: var(--font-size-base);
  color: var(--text-primary);
}
```

---

## 📖 File Structure

```
src/
├── styles/
│   ├── design-system.css         ← Design system tokens
│   └── design-system-docs.md    ← This file
├── styles.css                    ← Main stylesheet
└── ...
```

---

## ✅ Checklist for New Components

When creating a new component, follow this checklist:

- [ ] Use colors from `--primary-*`, `--success`, `--warning`, `--danger`, `--info`
- [ ] Use typography classes `.text-4xl` through `.text-xs`
- [ ] Use spacing from `--space-*` for padding/margins/gaps
- [ ] Use border-radius from `--radius-*`
- [ ] Use shadows from `--shadow-*`
- [ ] Apply hover/active states with appropriate color shifts
- [ ] Ensure text contrast meets WCAG AA standards
- [ ] Use semantic HTML elements
- [ ] Test on light and dark modes (if applicable)

---

## 📞 Support

For questions about the design system:
1. Check this documentation
2. Reference the [Figma Design](https://www.figma.com/design/qyN1xWJHA33vgi7ne4DbeU/Payzen-HR)
3. Review the CSS variables in `design-system.css`

**Last Updated:** April 2026
**Figma File:** PayZen HR Design System
