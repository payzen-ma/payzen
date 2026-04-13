# 🚀 PayZen Design System - Integration Guide

**Status:** ✅ Implementation Complete
**Last Updated:** April 2026
**Figma Source:** [PayZen HR Design System](https://www.figma.com/design/qyN1xWJHA33vgi7ne4DbeU/Payzen-HR)

---

## 📋 What Has Been Implemented

### ✅ Design System Files

1. **CSS Variables** (`src/styles/design-system.css`)
   - 🎨 Complete color palette (Primary, Neutral, Semantic)
   - 📝 Typography scale (8 levels, Inter font)
   - 📏 Spacing system (4px base unit, 9 values)
   - ⚫ Border radius tokens (6 variants)
   - ✨ Shadow/elevation system (5 levels)

2. **Documentation** (`src/styles/DESIGN_SYSTEM.md`)
   - Complete design system reference
   - Color palette specifications
   - Typography guidelines
   - Component usage examples
   - CSS variables reference
   - Integration instructions

3. **Tailwind Configuration** (`tailwind.config.js`)
   - Extended theme with all design system values
   - Custom colors, spacing, typography
   - Shadow definitions
   - Z-index layers
   - Safelist for dynamic classes

### ✅ Angular Components

Four reusable standalone components following the design system:

1. **Button Component** (`src/app/shared/components/button/`)
   - Variants: Primary, Secondary, Ghost, Danger, Disabled
   - Sizes: Small, Medium, Large
   - Full TypeScript typing
   - Accessibility support

2. **Badge Component** (`src/app/shared/components/badge/`)
   - 10 status types (Active, On Leave, Draft, Published, etc.)
   - Semantic colors
   - Responsive sizing

3. **Card Component** (`src/app/shared/components/card/`)
   - Sizes: Default, Compact, Large
   - Shadow and border styling
   - Flexible content layout

4. **Form Input Component** (`src/app/shared/components/form-input/`)
   - States: Default, Focus, Error, Disabled
   - ControlValueAccessor for form integration
   - Error message display
   - Multiple input types

### ✅ Supporting Files

- **Component Index** (`src/app/shared/components/index.ts`)
  - Barrel exports for easy importing

- **Component Documentation** (`src/app/shared/components/README.md`)
  - Usage examples
  - Props reference
  - Complete API documentation
  - Accessibility notes

---

## 🎯 Quick Start

### 1. Import Design System Globally

Already done in `src/styles.css`:
```css
@import './styles/design-system.css';
@import "tailwindcss";
```

### 2. Using Components

**Option A - Barrel Imports:**
```typescript
import { ButtonComponent, BadgeComponent, CardComponent, FormInputComponent } from '@app/shared/components';
```

**Option B - Individual Imports:**
```typescript
import { ButtonComponent } from '@app/shared/components/button/button.component';
```

### 3. Example Usage

```html
<!-- Button -->
<app-button variant="primary" (clickEvent)="onSave()">
  Save Changes
</app-button>

<!-- Badge -->
<app-badge status="active">Active</app-badge>

<!-- Card -->
<app-card size="default">
  <h3>Card Title</h3>
  <p>Card content here</p>
</app-card>

<!-- Form Input -->
<app-form-input
  [(ngModel)]="email"
  type="email"
  placeholder="Enter email..."
  [errorMessage]="error"
/>
```

---

## 📁 File Structure

```
frontend/
├── src/
│   ├── styles/
│   │   ├── design-system.css          ← Design tokens
│   │   ├── DESIGN_SYSTEM.md           ← Design documentation
│   │   └── (other styles)
│   ├── styles.css                      ← Updated with imports
│   ├── app/
│   │   └── shared/
│   │       └── components/
│   │           ├── button/
│   │           │   └── button.component.ts
│   │           ├── badge/
│   │           │   └── badge.component.ts
│   │           ├── card/
│   │           │   └── card.component.ts
│   │           ├── form-input/
│   │           │   └── form-input.component.ts
│   │           ├── index.ts            ← Barrel exports
│   │           └── README.md           ← Component usage guide
│   └── (app structure)
│
├── tailwind.config.js                 ← Updated with design tokens
└── (project files)
```

---

## 🎨 Color Palette Quick Reference

### Primary (Blue - Brand)
```
#1a73e8 - Main  |  #1557b0 - Hover  |  #0f4187 - Active
```

### Status Colors
```
Success: #16a34a  |  Warning: #b35109  |  Danger: #dc2626  |  Info: #3b82f6
```

### Neutral (Grays)
```
#ffffff - White  |  #f8fafc - Light  |  #1f2937 - Text  |  #1e293b - Dark
```

---

## 💡 Usage Examples

### Creating an Employee Card

```typescript
import { Component } from '@angular/core';
import { ButtonComponent, BadgeComponent, CardComponent } from '@app/shared/components';

@Component({
  selector: 'app-employee-card',
  standalone: true,
  imports: [ButtonComponent, BadgeComponent, CardComponent],
  template: `
    <app-card size="default">
      <div class="flex items-center justify-between mb-4">
        <h3 class="text-xl font-semibold">{{ employee.name }}</h3>
        <app-badge [status]="employee.status">
          {{ employee.status }}
        </app-badge>
      </div>

      <p class="text-gray-600 text-sm mb-6">{{ employee.role }}</p>

      <div class="flex gap-3 justify-end">
        <app-button variant="secondary">Cancel</app-button>
        <app-button variant="primary" (clickEvent)="onSave()">
          Save
        </app-button>
      </div>
    </app-card>
  `
})
export class EmployeeCardComponent {
  employee = { name: 'John Doe', status: 'active', role: 'HR Manager' };

  onSave() {
  }
}
```

### Building a Form

```html
<form class="space-y-6">
  <!-- Name Field -->
  <div class="flex flex-col gap-2">
    <label class="text-sm font-medium">Full Name</label>
    <app-form-input
      [(ngModel)]="formData.name"
      name="name"
      placeholder="Enter full name..."
      [errorMessage]="getError('name')"
    />
  </div>

  <!-- Email Field -->
  <div class="flex flex-col gap-2">
    <label class="text-sm font-medium">Email Address</label>
    <app-form-input
      [(ngModel)]="formData.email"
      name="email"
      type="email"
      placeholder="Enter email..."
      [errorMessage]="getError('email')"
    />
  </div>

  <!-- Actions -->
  <div class="flex gap-3 justify-end pt-4 border-t">
    <app-button variant="secondary">Cancel</app-button>
    <app-button variant="primary" type="submit">
      Submit
    </app-button>
  </div>
</form>
```

---

## 🔧 CSS Variables API

### Using in Component Styles

```css
.custom-element {
  background-color: var(--bg-element);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg);
  padding: var(--space-4);
  box-shadow: var(--shadow-md);
  font-size: var(--font-size-base);
  color: var(--text-primary);
}
```

### All Available Variables

```
Colors:
  --primary-500, --primary-600, --primary-700, ...
  --success, --warning, --danger, --info
  --text-primary, --text-secondary, --text-muted
  --border-subtle, --border-medium, --border-strong

Spacing:
  --space-1 (4px) through --space-12 (48px)

Typography:
  --font-size-xs through --font-size-4xl
  --font-weight-xs through --font-weight-4xl
  --line-height-xs through --line-height-4xl

Borders & Radius:
  --radius-sm (4px) through --radius-full (9999px)

Shadows:
  --shadow-xs through --shadow-xl
```

See `src/styles/design-system.css` for complete reference.

---

## ✅ Ava ilability & Testing

### Browser Support
- ✅ Chrome/Edge (latest)
- ✅ Firefox (latest)
- ✅ Safari (latests)
- ✅ Mobile browsers

### Accessibility
- ✅ Keyboard navigation
- ✅ WCAG 2.1 AA contrast
- ✅ Focus states
- ✅ Semantic HTML
- ✅ ARIA labels

---

## 🚀 Next Steps

### For Developers

1. **Start using the components** in your feature modules
2. **Reference DESIGN_SYSTEM.md** for styling guidelines
3. **Use Tailwind classes** for quick styling
4. **Follow the patterns** in component examples

### For Designers

1. **Keep Figma file updated** with design changes
2. **Export new components** as needed
3. **Document patterns** in the design system
4. **Update CSS variables** when colors/spacing changes

### For the Team

1. **Use components consistently** across the app
2. **Report component issues** in your dev process
3. **Suggest improvements** to the design system
4. **Maintain documentation** as you evolve the system

---

## 📚 Additional Resources

- **Design System Docs:** `src/styles/DESIGN_SYSTEM.md`
- **Component Docs:** `src/app/shared/components/README.md`
- **Figma File:** [PayZen HR](https://www.figma.com/design/qyN1xWJHA33vgi7ne4DbeU/Payzen-HR)
- **Tailwind Docs:** [tailwindcss.com](https://tailwindcss.com)
- **Angular Components:** [angular.io/components](https://angular.io/guide/components)

---

## 🐛 Troubleshooting

### Styles not applying?
1. Check `design-system.css` is imported before Tailwind
2. Verify CSS variables are defined in `:root`
3. Clear build cache: `rm -rf dist/` and rebuild

### Components not rendering?
1. Ensure component is imported in `imports` array
2. Check for TypeScript errors in IDE
3. Verify ng serve is running without errors

### Colors look wrong?
1. Check browser devtools for applied styles
2. Verify CSS variable values are correct
3. Check for conflicting global styles

### Performance issues?
1. Use OnPush change detection
2. Lazy load components where possible
3. Minimize re-renders in templates

---

## 📞 Support

Questions or issues?
1. Check the relevant documentation file
2. Review component examples
3. Check Figma design file for reference
4. Ask your team lead or design system maintainer

---

**Design System Version:** 1.0
**Status:** Production Ready
**Last Update:** April 7, 2026
