import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BehaviorSubject } from 'rxjs';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';

import { IfAuthenticatedDirective } from './if-authenticated.directive';
import { UserService } from './services/user.service';

@Component({
  standalone: true,
  imports: [IfAuthenticatedDirective],
  template: `
    <ng-template [ifAuthenticated]="true">
      <span data-testid="authenticated-content">Authenticated content</span>
    </ng-template>

    <ng-template [ifAuthenticated]="false">
      <span data-testid="unauthenticated-content">Unauthenticated content</span>
    </ng-template>
  `,
})
class TestHostComponent {}

describe('IfAuthenticatedDirective', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let isAuthenticatedSubject: BehaviorSubject<boolean>;

  const authenticatedContent = (): HTMLElement | null =>
    fixture.nativeElement.querySelector('[data-testid="authenticated-content"]');

  const unauthenticatedContent = (): HTMLElement | null =>
    fixture.nativeElement.querySelector('[data-testid="unauthenticated-content"]');

  const setAuthenticationState = (isAuthenticated: boolean): void => {
    isAuthenticatedSubject.next(isAuthenticated);
    fixture.detectChanges();
  };

  beforeEach(async () => {
    isAuthenticatedSubject = new BehaviorSubject<boolean>(false);

    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [
        {
          provide: UserService,
          useValue: {
            isAuthenticated: isAuthenticatedSubject.asObservable(),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture.destroy();
    TestBed.resetTestingModule();
  });

  it('should not render authenticated content when the user is unauthenticated', () => {
    // TL5: R1 Authentifizierungszustand / R7 auth-abhängige UI
    expect(authenticatedContent()).toBeNull();
  });

  it('should render authenticated content when ifAuthenticated is true and the user is authenticated', () => {
    // TL5: R1 Authentifizierungszustand / R7 auth-abhängige UI
    setAuthenticationState(true);

    expect(authenticatedContent()).not.toBeNull();
    expect(authenticatedContent()?.textContent?.trim()).toBe('Authenticated content');
    expect(unauthenticatedContent()).toBeNull();
  });

  it('should render unauthenticated content when ifAuthenticated is false and the user is unauthenticated', () => {
    // TL5: R1 Authentifizierungszustand / R7 auth-abhängige UI
    expect(unauthenticatedContent()).not.toBeNull();
    expect(unauthenticatedContent()?.textContent?.trim()).toBe('Unauthenticated content');
    expect(authenticatedContent()).toBeNull();
  });

  it('should remove the current view when the authentication state no longer matches', () => {
    // TL5: R1 Zustandswechsel / R7 UI-nahe Zustände
    expect(unauthenticatedContent()).not.toBeNull();
    expect(authenticatedContent()).toBeNull();

    setAuthenticationState(true);

    expect(authenticatedContent()).not.toBeNull();
    expect(unauthenticatedContent()).toBeNull();

    setAuthenticationState(false);

    expect(authenticatedContent()).toBeNull();
    expect(unauthenticatedContent()).not.toBeNull();
  });
});