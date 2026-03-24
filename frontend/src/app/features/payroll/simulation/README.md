# Simulateur de Paie (SimPaie)

A modern, interactive payroll simulation component that generates salary composition suggestions based on natural language input.

## Features

- **Natural Language Input**: Users can describe their payroll needs in French
- **3 Composition Variations**: Automatically generates 3 different salary structures:
  - Standard: Classic structure with base salary and legal contributions
  - Optimized: Maximizes net pay with benefits and tax-optimized bonuses
  - Balanced: Balance between employer cost and competitive net salary
- **Interactive UI**: 
  - Suggestion chips for quick input
  - Real-time loading states with skeleton cards
  - Expandable calculation detail accordions
  - Smooth animations and transitions
- **Responsive Design**: Mobile-first approach with 1-3 column layouts
- **Type Badges**: Color-coded badges for different pay element types:
  - **Base** (green): Base salary components
  - **Prime** (gold): Bonuses and incentives
  - **Deduction** (red): Withholdings and contributions
  - **Avantage** (purple): Benefits in kind
  - **NI** (blue): Non-taxable items

## Design System

This component fully integrates with the PayZen global design system:
- Uses CSS design tokens from `styles.css`
- Follows component patterns (`.card`, `.btn`, `.input`, `.form-label`)
- Consistent with application color palette
- No custom fonts - uses system font stack
- No header/footer - standalone single-page view

## Accessing the Component

Navigate to: `/payroll/simulation`

The route is protected by `authGuard`, requiring user authentication.

## Data Structure

Each composition contains:
```typescript
{
  titre: string;              // Composition title
  description: string;        // Brief description
  elements: [{                // Pay elements array
    nom: string;              // Element name
    type: 'base' | 'prime' | 'deduction' | 'avantage' | 'ni';
    montant: number;          // Amount in MAD
  }];
  brut_imposable: number;     // Taxable gross
  total_retenues: number;     // Total deductions
  cout_employeur: number;     // Total employer cost
  salaire_net: number;        // Net salary to pay
  calcul_steps: [{            // Calculation breakdown
    label: string;
    value: string;
  }];
}
```

## Styling

- **Design Tokens**: Uses global CSS variables (`--primary`, `--success`, `--danger`, etc.)
- **Components**: Leverages global `.card`, `.btn`, `.input` classes
- **Colors**: Consistent with PayZen brand palette
- **Animations**: fadeInUp on card reveal, smooth transitions
- **Number Format**: French locale (10 000,00 DH)

## States

1. **Idle**: Initial state with prompt input visible
2. **Loading**: Shows spinner and skeleton cards (1.5s simulation)
3. **Success**: Displays 3 composition cards with detailed breakdowns
4. **Error**: Shows error banner (5% chance in demo for testing)

## Mock Data

Currently uses hardcoded mock data demonstrating different salary structures around 10,000 MAD. 

**To integrate with actual API**: Replace the `setTimeout` logic in the `generate()` method with your payroll calculation service call.

## Future Enhancements

- [ ] Connect to PAYZEN DSL backend API
- [ ] Save favorite compositions
- [ ] Export compositions as PDF
- [ ] Compare compositions side-by-side
- [ ] Historical simulation tracking
- [ ] Advanced filters (industry, position, experience)
- [ ] Collaborative sharing features
