import { Directive, Input, TemplateRef, ViewContainerRef, OnInit, OnDestroy } from '@angular/core';
import { AuthService } from '@app/core/services/auth.service';
import { Subject, takeUntil } from 'rxjs';

/**
 * Structural directive to show/hide elements based on user permissions
 * Usage: *hasPermission="'createEmployee'"
 */
@Directive({
  selector: '[hasPermission]',
  standalone: true
})
export class HasPermissionDirective implements OnInit, OnDestroy {
  private permission: string = '';
  private destroy$ = new Subject<void>();
  private hasView = false;

  constructor(
    private templateRef: TemplateRef<any>,
    private viewContainer: ViewContainerRef,
    private authService: AuthService
  ) {}

  @Input() set hasPermission(permission: string) {
    this.permission = permission;
    this.updateView();
  }

  ngOnInit() {
    // Listen to auth state changes
    this.authService.authState$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.updateView();
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private updateView() {
    const hasPermission = this.authService.hasPermission(this.permission as any);

    if (hasPermission && !this.hasView) {
      this.viewContainer.createEmbeddedView(this.templateRef);
      this.hasView = true;
    } else if (!hasPermission && this.hasView) {
      this.viewContainer.clear();
      this.hasView = false;
    }
  }
}

/**
 * Structural directive to show/hide elements based on user roles
 * Usage: *hasRole="['admin', 'rh']"
 */
@Directive({
  selector: '[hasRole]',
  standalone: true
})
export class HasRoleDirective implements OnInit, OnDestroy {
  private roles: string[] = [];
  private destroy$ = new Subject<void>();
  private hasView = false;

  constructor(
    private templateRef: TemplateRef<any>,
    private viewContainer: ViewContainerRef,
    private authService: AuthService
  ) {}

  @Input() set hasRole(roles: string | string[]) {
    this.roles = Array.isArray(roles) ? roles : [roles];
    this.updateView();
  }

  ngOnInit() {
    this.authService.authState$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.updateView();
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private updateView() {
    const hasRole = this.authService.hasRole(this.roles as any);

    if (hasRole && !this.hasView) {
      this.viewContainer.createEmbeddedView(this.templateRef);
      this.hasView = true;
    } else if (!hasRole && this.hasView) {
      this.viewContainer.clear();
      this.hasView = false;
    }
  }
}
