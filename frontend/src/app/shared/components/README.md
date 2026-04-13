# 🎨 PayZen Design System Components

Angular standalone components implementing the PayZen HR Design System from Figma.

## Components Included

### 1. Button Component

Primary component for all button interactions.

#### Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `variant` | `'primary'\|'secondary'\|'danger'\|'ghost'\|'disabled'` | `'primary'` | Button style variant |
| `size` | `'small'\|'medium'\|'large'` | `'medium'` | Button size |
| `disabled` | `boolean` | `false` | Disable button |
| `type` | `'button'\|'submit'\|'reset'` | `'button'` | HTML button type |
| `clickEvent` | `EventEmitter<void>` | - | Click event emitter |

#### Usage

```typescript
// In template
<app-button variant="primary" size="medium" (clickEvent)="onSave()">
  Save Changes
</app-button>

<app-button variant="secondary" [disabled]="isLoading">
  Cancel
</app-button>

<app-button variant="danger">
  Delete
</app-button>

<app-button variant="ghost">
  Learn More
</app-button>
```

#### Variants

- **primary**: Blue background, white text (default)
- **secondary**: White background, gray text, gray border
- **danger**: Red background, white text
- **ghost**: Light blue background, blue text
- **disabled**: Gray background, muted text (automatically applied when `disabled=true`)

#### Sizes

- **small**: 12px text, 6px vertical, 12px horizontal padding
- **medium**: 14px text, 8px vertical, 16px horizontal padding (default)
- **large**: 16px text, 12px vertical, 24px horizontal padding

---

### 2. Badge Component

Displays status and category information.

#### Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `status` | Badge Status (see table below) | `'active'` | Status type to display |

#### Status Types

| Status | Background | Text | Usage |
|--------|------------|------|-------|
| `active` | Green light | Green dark | Employee is active |
| `on-leave` | Yellow light | Orange dark | Employee on leave |
| `inactive` | Red light | Red dark | Employee inactive |
| `draft` | Gray light | Gray dark | Document in draft |
| `published` | Blue light | Blue dark | Document published |
| `deprecated` | Purple light | Purple dark | Feature deprecated |
| `warning` | Yellow light | Orange dark | Warning status |
| `error` | Red light | Red dark | Error status |
| `cdi` | Blue light | Blue dark | CDI contract type |
| `cdd` | Purple light | Purple dark | CDD contract type |

#### Usage

```html
<!-- Basic usage -->
<app-badge status="active">Active</app-badge>

<!-- In employee card -->
<div class="flex items-center justify-between">
  <h3 class="text-lg font-semibold">John Doe</h3>
  <app-badge status="active">Active</app-badge>
</div>

<!-- Multiple badges -->
<div class="flex gap-2">
  <app-badge status="active">Active</app-badge>
  <app-badge status="cdi">CDI</app-badge>
</div>
```

---

### 3. Card Component

Container for content with consistent styling.

#### Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `size` | `'default'\|'compact'\|'large'` | `'default'` | Padding size |

#### Sizes

- **default**: 24px padding (standard cards)
- **compact**: 20px padding (data cards, stat cards)
- **large**: 32px padding (hero sections, large panels)

#### Usage

```html
<!-- Default card -->
<app-card>
  <h3 class="text-lg font-semibold mb-4">Employee Summary</h3>
  <p class="text-gray-600">Displays key employee metrics and current status.</p>
  <div class="flex justify-end mt-6">
    <app-button variant="primary" size="small">View Details</app-button>
  </div>
</app-card>

<!-- Compact stat card -->
<app-card size="compact">
  <p class="text-gray-500 text-sm font-medium">Total Employees</p>
  <p class="text-4xl font-bold mt-2">247</p>
  <div class="flex items-center gap-1.5 mt-4">
    <app-badge status="active">+12%</app-badge>
    <p class="text-gray-400 text-xs">vs last month</p>
  </div>
</app-card>

<!-- Large panel -->
<app-card size="large">
  <h2 class="text-2xl font-semibold mb-6">Welcome to PayZen HR</h2>
  <p class="text-gray-600 mb-8">Manage your HR operations efficiently.</p>
</app-card>
```

---

### 4. Form Input Component

Text input with integrated validation and state management.

#### Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `type` | Input Type | `'text'` | HTML input type |
| `placeholder` | `string` | `''` | Placeholder text |
| `disabled` | `boolean` | `false` | Disable input |
| `errorMessage` | `string` | `''` | Error message to display |
| `value` | `string \| null` | `null` | Input value |
| `valueChange` | `EventEmitter<string>` | - | Value change emitter |

#### Input Types

Supports all standard HTML input types: `text`, `email`, `password`, `number`, `tel`, `url`, `date`

#### States

- **default**: Gray border, white background
- **focus**: Blue 2px border, white background
- **error**: Red border, light red background, red text
- **disabled**: Gray border, gray background, disabled cursor

#### Usage

```typescript
// In component
import { FormInputComponent } from '@app/shared/components';
import { FormsModule } from '@angular/forms';

export class MyComponent {
  email = '';
  password = '';
  errorMessage = '';

  onSubmit() {
    if (!this.email) {
      this.errorMessage = 'Email is required';
    }
  }
}
```

```html
<!-- Basic input -->
<app-form-input
  [(ngModel)]="email"
  type="email"
  placeholder="Enter your email..."
/>

<!-- With error -->
<app-form-input
  [(ngModel)]="email"
  type="email"
  [errorMessage]="errorMessage"
  placeholder="Enter your email..."
/>

<!-- Disabled input -->
<app-form-input
  [(ngModel)]="status"
  placeholder="Status (read-only)"
  [disabled]="true"
/>

<!-- Form with multiple inputs -->
<form (ngSubmit)="onSubmit()" class="space-y-4">
  <div>
    <label class="block text-sm font-medium mb-1">Email</label>
    <app-form-input
      [(ngModel)]="email"
      name="email"
      type="email"
      placeholder="john@example.com"
    />
  </div>

  <div>
    <label class="block text-sm font-medium mb-1">Password</label>
    <app-form-input
      [(ngModel)]="password"
      name="password"
      type="password"
      placeholder="Enter password..."
    />
  </div>

  <app-button type="submit" variant="primary">Login</app-button>
</form>
```

---

## 📋 Complete Example: Employee Card

Combining all components:

```typescript
// employee-card.component.ts
import { Component, Input } from '@angular/core';
import { ButtonComponent, BadgeComponent, CardComponent, FormInputComponent } from '@app/shared/components';

@Component({
  selector: 'app-employee-card',
  standalone: true,
  imports: [ButtonComponent, BadgeComponent, CardComponent, FormInputComponent],
  template: `
    <app-card size="default">
      <!-- Header -->
      <div class="flex items-center justify-between mb-4">
        <h3 class="text-lg font-semibold">{{ employee.name }}</h3>
        <app-badge [status]="employee.status">
          {{ employee.status }}
        </app-badge>
      </div>

      <!-- Description -->
      <p class="text-gray-600 text-sm mb-6">
        {{ employee.description }}
      </p>

      <!-- Edit form -->
      <div class="space-y-4 mb-6">
        <div>
          <label class="block text-sm font-medium mb-1">Email</label>
          <app-form-input
            [(ngModel)]="employee.email"
            type="email"
            placeholder="Enter email..."
          />
        </div>

        <div>
          <label class="block text-sm font-medium mb-1">Department</label>
          <app-form-input
            [(ngModel)]="employee.department"
            type="text"
            placeholder="Enter department..."
          />
        </div>
      </div>

      <!-- Actions -->
      <div class="flex gap-3 justify-end">
        <app-button variant="secondary" (clickEvent)="onCancel()">
          Cancel
        </app-button>
        <app-button variant="primary" (clickEvent)="onSave()">
          Save Changes
        </app-button>
      </div>
    </app-card>
  `
})
export class EmployeeCardComponent {
  @Input() employee = {
    name: 'John Doe',
    status: 'active' as const,
    email: 'john@payzen.com',
    description: 'Senior HR Manager',
    department: 'Human Resources'
  };

  onSave() {
    console.log('Saved:', this.employee);
  }

  onCancel() {
    console.log('Cancelled');
  }
}
```

---

## 🎨 Styling & Customization

### Using CSS Variables

All components use CSS variables from the design system. Customize globally:

```css
/* In your global styles.css */
:root {
  --primary-500: #1a73e8;          /* Change primary color */
  --primary-600: #1557b0;          /* Change hover color */
  --radius-lg: 8px;                /* Change border radius */
  --shadow-md: 0px 4px 6px ...;    /* Change shadows */
}
```

### Extending Components

For custom styling, use TailwindCSS classes:

```html
<!-- Add custom classes -->
<app-button variant="primary" class="w-full">Full Width Button</app-button>

<!-- Combine with utility classes -->
<app-card class="mb-8 hover:shadow-lg transition-shadow">
  <!-- content -->
</app-card>
```

### Dark Mode Support

Components support dark mode. Add to `tailwind.config.js`:

```javascript
darkMode: 'class', // or 'media'
```

---

## 🔄 Component Hierarchy

```
App
├── Layout
├── Card (main container)
│   ├── Badge (status indicator)
│   ├── FormInput (employee data)
│   └── Button (actions)
├── Modal (using Card)
│   ├── FormInput (form fields)
│   └── Button (submit/cancel)
└── DataTable
    ├── Card (row containers)
    ├── Badge (status columns)
    └── Button (row actions)
```

---

## 📚 Design System References

- **Figma File:** [PayZen HR Design System](https://www.figma.com/design/qyN1xWJHA33vgi7ne4DbeU/Payzen-HR)
- **CSS Variables:** `/src/styles/design-system.css`
- **Documentation:** `/src/styles/DESIGN_SYSTEM.md`

---

## ✅ Accessibility

All components follow WCAG 2.1 AA standards:
- ✅ Keyboard navigation support
- ✅ Semantic HTML elements
- ✅ Proper color contrast ratios
- ✅ ARIA labels where needed
- ✅ Focus states for all interactive elements

---

## 🐛 Troubleshooting

### Components not styling correctly?
1. Ensure `design-system.css` is imported in `styles.css`
2. Check that TailwindCSS is properly configured
3. Verify CSS variables are defined in `:root`

### Input values not updating?
Ensure you're using `[(ngModel)]` for two-way binding and `FormsModule` is imported.

### Button click not firing?
Use `(clickEvent)` instead of `(click)` for proper event handling.

---

## 📝 Contributing

When adding new components:
1. Keep them standalone
2. Use CSS variables for colors/sizing
3. Document all @Input/@Output properties
4. Add TypeScript types for props
5. Include usage examples
6. Test accessibility

---

**Last Updated:** April 2026
**Maintained by:** PayZen Development Team
