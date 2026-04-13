# Global CSS Architecture Guide

## Overview

The Payzen frontend has been refactored to use a **centralized global CSS class system** instead of component-scoped CSS files. This ensures that:
- Design changes cascade automatically to all components using those classes
- No code duplication across components
- Consistent styling patterns across the application
- Easy maintenance and updates

## File Structure

```
src/styles/
├── design-system.css        # Design tokens (colors, spacing, radius, shadows, typography)
├── layouts.css              # Reusable layout classes (.page-shell, .hero, .card, etc.)
├── utilities.css            # Utility classes (.alert, .btn, etc.)
├── components.css           # Page-specific component classes (ALL page styles)
├── styles.css               # Main import file (orchestrates all above)
├── DESIGN_SYSTEM.md         # Token documentation
└── GLOBAL_CSS_GUIDE.md      # This file
```

## CSS File Purpose

### 1. **design-system.css**
- **Purpose**: Single source of truth for all design tokens
- **Contains**:
  - Color variables (primary, neutral, semantic)
  - Spacing scale (rem-based: 4px, 8px, 12px, 16px, etc.)
  - Border radius scale
  - Box shadows
  - Typography scale
- **Usage**: Referenced by all other CSS files via `var(--token-name)`
- **When to Edit**: When design tokens change
- **Example**: `color: var(--primary);` `border-radius: var(--radius-md);`

### 2. **layouts.css**
- **Purpose**: Reusable page layout structures
- **Contains**:
  - `.page-shell` - Main page container with max-width and padding
  - `.hero` - Hero section styling (background, border, shadow)
  - `.card` - Card component styling
  - Generic patterns used across multiple pages
- **When to Edit**: When adding new layout patterns that will be reused
- **Naming**: Generic prefixes, no page-specific names
- **Example**: `.page-shell`, `.hero__copy`, `.hero__actions`

### 3. **utilities.css**
- **Purpose**: Reusable utility and component patterns
- **Contains**:
  - Alert message styles (`.alert`, `.alert--success`, `.alert--error`)
  - Form controls
  - Common UI patterns
- **When to Edit**: When adding new utility patterns
- **Naming**: Generic utility names
- **Example**: `.alert`, `.alert--success`, `.alert i`

### 4. **components.css** ⭐ MAIN STYLES FILE
- **Purpose**: ALL page-specific component styles (payslip, leave-requests, etc.)
- **Contains**:
  - `.payslip-*` classes for payslip page
  - `.leave-requests-*` classes for leave requests page
  - `.employee-dashboard-*` classes for dashboard
  - Any page-specific styling variations
- **When to Edit**: When creating new pages or updating existing page styles
- **Naming Convention**: `.page-name-*` prefix for all classes in that page
  - Example: `.payslip-page`, `.payslip-hero`, `.payslip-form__group`
  - Example: `.leave-requests-shell`, `.leave-requests-stats`
- **Structure**: Organized by page section comments
- **No Component CSS Files**: This file replaces ALL `.component.css` files

### 5. **styles.css**
- **Purpose**: Main entry point - orchestrates import order
- **Contains**:
  - Import statements for design-system → layouts → utilities → components → Tailwind → PrimeNG
  - Global resets (if any)
  - Host styling
- **When to Edit**: When changing import order or adding new global files
- **Critical**: Import order matters! Design tokens must come first

## How to Add Styles for a New Page

### Step 1: Create HTML Template
Create your component with standard HTML and class names:
```html
<section class="mypage-page">
  <div class="mypage-shell">
    <header class="mypage-hero">
      <h1 class="mypage-title">Page Title</h1>
    </header>
    <!-- content -->
  </div>
</section>
```

### Step 2: Use Global Classes in HTML
Reference only the CSS class names - NO styleUrl in component decorator:
```typescript
@Component({
  selector: 'app-mypage',
  standalone: true,
  imports: [CommonModule, /* other imports */],
  templateUrl: './mypage.component.html'
  // NO styleUrls or styleUrl!
})
```

### Step 3: Add Styles to Global CSS
Add ALL styles for your page to `src/styles/components.css`:
```css
/* ── MYPAGE PAGE ────────────────────────────────────────────────────── */

.mypage-page {
  min-height: 100%;
  background: radial-gradient(...);
  color: var(--text-primary);
}

.mypage-shell {
  max-width: 1280px;
  margin: 0 auto;
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.mypage-hero {
  background: var(--bg-element);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-xl);
  padding: 24px;
}

.mypage-title {
  margin: 0;
  color: var(--text-primary);
  font-size: 30px;
  font-weight: 700;
}

/* Add all page-specific styles here */
```

### Step 4: Test
- Run `npm run build` to verify all styles are included
- Check for any CSS budget warnings
- Test the page in the browser

## Naming Conventions

### CSS Class Naming Pattern
```
.{page-name}-{element}[__{modifier}][--{variant}]
```

**Examples:**
- `.payslip-page` - Root page container
- `.payslip-shell` - Main content shell
- `.payslip-form` - Form container
- `.payslip-form__group` - Form group (double underscore for nested elements)
- `.payslip-form__group--flex-1` - Form group variant (double dash for variants)
- `.payslip-alert--success` - Alert success variant

### Naming Rules
1. **Always prefix with page name**: `.payslip-*`, `.leave-requests-*`
2. **Use BEM-like structure**:
   - Block: `.payslip-form`
   - Element: `.payslip-form__group` (nested element)
   - Modifier: `.payslip-form__group--flex-1` (variant)
3. **Be descriptive and consistent**
4. **Use lowercase with hyphens**
5. **Avoid single-letter abbreviations unless inevitable**

## Migration from Component CSS

### Before (Old - Don't Use)
```typescript
@Component({
  selector: 'app-payslip',
  templateUrl: './payslip.component.html',
  styleUrls: ['./payslip.component.css']  // ❌ OLD
})
```
With separate `payslip.component.css` file declaring:
```css
.payslip-form { ... }
```

### After (New - Use This)
```typescript
@Component({
  selector: 'app-payslip',
  templateUrl: './payslip.component.html'
  // No styleUrls! ✅
})
```
All styles now in `src/styles/components.css`:
```css
/* ── PAYSLIP PAGE ────────────────────────────────────────────────────── */
.payslip-form { ... }
```

## Benefits of This Approach

✅ **Single Source of Truth**: All page-specific styles in one file (`components.css`)
✅ **Cache Efficiency**: Global CSS file is cached by browser
✅ **Easy Updates**: Change global class, all components using it update automatically
✅ **No Duplication**: Classes defined once, used everywhere
✅ **Scalability**: Adding new pages is straightforward
✅ **Performance**: Fewer HTTP requests, better bundling
✅ **Maintainability**: Clear organization with page-specific sections

## Design System Extension

### Using Design Tokens
Always use design system variables instead of hardcoded values:

```css
/* ✅ GOOD - Uses design tokens */
.payslip-title {
  color: var(--text-primary);
  font-size: 30px;
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-sm);
}

/* ❌ BAD - Hardcoded values */
.payslip-title {
  color: #1f2937;
  font-size: 30px;
  border-radius: 12px;
  box-shadow: 0 1px 2px rgba(0,0,0,0.05);
}
```

### Available Design Tokens
See `src/styles/design-system.css` and `src/styles/DESIGN_SYSTEM.md` for complete list:
- Colors: `--primary`, `--primary-light`, `--success`, `--danger`, `--warning`, `--info`, etc.
- Spacing: `--space-2` (8px), `--space-3` (12px), `--space-4` (16px), etc.
- Radius: `--radius-sm`, `--radius-md`, `--radius-lg`, `--radius-xl`, `--radius-full`
- Shadows: `--shadow-sm`, `--shadow-md`, `--shadow-lg`
- Typography: Font sizes, weights, line heights

## Common Patterns

### Page Shell Pattern
```css
.{page}-shell {
  max-width: 1280px;
  margin: 0 auto;
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 24px;
}
```

### Hero Section Pattern
```css
.{page}-hero {
  background: var(--bg-element);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-sm);
  padding: 24px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
}
```

### Form Group Pattern
```css
.{page}-form__group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
```

### Responsive Pattern
```css
@media (max-width: 860px) {
  .{page}-shell {
    padding: 16px;
    gap: 16px;
  }

  .{page}-hero {
    flex-direction: column;
  }
}
```

## Troubleshooting

### Styles Not Applying
1. Check that the class name matches HTML exactly
2. Verify class is defined in `src/styles/components.css`
3. Ensure global CSS file is imported in `src/styles.css`
4. Run `npm run build` and check for errors
5. Clear browser cache (Ctrl+Shift+Delete)

### CSS Budget Exceeded
1. Run `npm run build` and note which file exceeded budget
2. Check `angular.json` for budget configuration
3. Optimize CSS: remove unused rules, consolidate similar rules
4. Consider if styles can be moved to shared utilities

### Component Styles Not Found
- If a component has `styleUrls: ['./file.css']`, remove it
- Move all styles to `src/styles/components.css`
- Delete the component CSS file
- Rebuild and verify

## Best Practices

1. **Always use design tokens** - Never hardcode values
2. **Follow naming conventions** - Consistency matters
3. **Group related styles** - Use comments to section code
4. **Add media queries** - Ensure responsive design
5. **Test across browsers** - Verify rendering consistency
6. **Keep classes reusable** - But page-specific when needed
7. **Document complex styles** - Add comments for non-obvious CSS
8. **Run build regularly** - Catch errors early

## Current Pages Migrated

✅ Payslip (`/app/payroll/payslip`)
✅ Leave Requests (`/app/my-leave-requests`)
✅ Employee Dashboard (`/app/employee/dashboard`)
✅ Main Dashboard (`/app/dashboard`)
✅ Sidebar (`src/app/shared/sidebar`)

**Note**: All these pages now use global CSS classes. Their component CSS files have been removed, and all styles are consolidated in `src/styles/components.css`.
