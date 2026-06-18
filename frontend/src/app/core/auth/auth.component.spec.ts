import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

import AuthComponent from './auth.component';
import { UserService } from './services/user.service';
import { Errors } from '../models/errors.model';

describe('AuthComponent', () => {
  let fixture: ComponentFixture<AuthComponent>;
  let component: AuthComponent;

  let activatedRouteMock: {
    snapshot: {
      url: Array<{ path: string }>;
    };
  };

  let routerMock: {
    navigate: ReturnType<typeof vi.fn>;
  };

  let userServiceMock: {
    login: ReturnType<typeof vi.fn>;
    register: ReturnType<typeof vi.fn>;
  };

  const mockAuthResponse = {
    user: {
      email: 'test@example.com',
      token: 'jwt-token',
      username: 'testuser',
      bio: null,
      image: null,
    },
  };

  const setRoutePath = (path: 'login' | 'register'): void => {
    activatedRouteMock.snapshot.url = [{ path }];
  };

  beforeEach(async () => {
    activatedRouteMock = {
      snapshot: {
        url: [{ path: 'login' }],
      },
    };

    routerMock = {
      navigate: vi.fn().mockResolvedValue(true),
    };

    userServiceMock = {
      login: vi.fn(),
      register: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [AuthComponent],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: activatedRouteMock,
        },
        {
          provide: Router,
          useValue: routerMock,
        },
        {
          provide: UserService,
          useValue: userServiceMock,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AuthComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    fixture.destroy();
    TestBed.resetTestingModule();
    vi.clearAllMocks();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should configure the login form from the login route', () => {
    // TL5: R1 Authentifizierung / R7 Formularverhalten
    setRoutePath('login');

    component.ngOnInit();

    expect(component.authType).toBe('login');
    expect(component.title).toBe('Sign in');
    expect(component.authForm.contains('email')).toBe(true);
    expect(component.authForm.contains('password')).toBe(true);
    expect(component.authForm.contains('username')).toBe(false);
  });

  it('should configure the register form from the register route and add username control', () => {
    // TL5: R1 Registrierung / R7 Formularverhalten
    setRoutePath('register');

    component.ngOnInit();

    expect(component.authType).toBe('register');
    expect(component.title).toBe('Sign up');
    expect(component.authForm.contains('email')).toBe(true);
    expect(component.authForm.contains('password')).toBe(true);
    expect(component.authForm.contains('username')).toBe(true);
    expect(component.authForm.get('username')?.valid).toBe(false);
  });

  it('should call userService.login with email and password and navigate home on success', () => {
    // TL5: R1 Login / R5 zentraler Nutzerfluss startet nach erfolgreicher Anmeldung
    setRoutePath('login');
    userServiceMock.login.mockReturnValue(of(mockAuthResponse));
    component.ngOnInit();

    component.authForm.patchValue({
      email: 'login@example.com',
      password: 'secret123',
    });

    component.submitForm();

    expect(userServiceMock.login).toHaveBeenCalledWith({
      email: 'login@example.com',
      password: 'secret123',
    });
    expect(userServiceMock.register).not.toHaveBeenCalled();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/']);
  });

  it('should call userService.register with username, email and password and navigate home on success', () => {
    // TL5: R1 Registrierung / R5 zentraler Nutzerfluss startet nach erfolgreicher Registrierung
    setRoutePath('register');
    userServiceMock.register.mockReturnValue(of(mockAuthResponse));
    component.ngOnInit();

    component.authForm.patchValue({
      username: 'newuser',
      email: 'register@example.com',
      password: 'secret123',
    });

    component.submitForm();

    expect(userServiceMock.register).toHaveBeenCalledWith({
      username: 'newuser',
      email: 'register@example.com',
      password: 'secret123',
    });
    expect(userServiceMock.login).not.toHaveBeenCalled();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/']);
  });

  it('should set errors and reset submitting state when login fails', () => {
    // TL5: R3 API-Fehlerantwort / R7 Formularfehler wird UI-nah verarbeitet
    const validationError: Errors = {
      errors: {
        email: 'or password is invalid',
      },
    };

    setRoutePath('login');
    userServiceMock.login.mockReturnValue(throwError(() => validationError));
    component.ngOnInit();

    component.authForm.patchValue({
      email: 'wrong@example.com',
      password: 'wrong-password',
    });

    component.submitForm();

    expect(component.errors()).toEqual(validationError);
    expect(component.isSubmitting()).toBe(false);
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it('should clear previous errors and set submitting state before a register request', () => {
    // TL5: R7 Formularzustand bleibt zwischen zwei Submit-Versuchen konsistent
    const previousError: Errors = {
      errors: {
        username: 'is already taken',
      },
    };

    setRoutePath('register');
    userServiceMock.register.mockReturnValue(of(mockAuthResponse));
    component.ngOnInit();
    component.errors.set(previousError);
    component.isSubmitting.set(false);

    component.authForm.patchValue({
      username: 'newuser',
      email: 'register@example.com',
      password: 'secret123',
    });

    component.submitForm();

    expect(component.errors()).toEqual({ errors: {} });
    expect(component.isSubmitting()).toBe(true);
    expect(routerMock.navigate).toHaveBeenCalledWith(['/']);
  });

  it('should set errors and reset submitting state when registration fails', () => {
    // TL5: R3 API-Fehlerantwort / R7 Registrierungsformular bleibt bedienbar
    const validationError: Errors = {
      errors: {
        username: 'is already taken',
        email: 'is already taken',
      },
    };

    setRoutePath('register');
    userServiceMock.register.mockReturnValue(throwError(() => validationError));
    component.ngOnInit();

    component.authForm.patchValue({
      username: 'existinguser',
      email: 'existing@example.com',
      password: 'secret123',
    });

    component.submitForm();

    expect(component.errors()).toEqual(validationError);
    expect(component.isSubmitting()).toBe(false);
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });
});