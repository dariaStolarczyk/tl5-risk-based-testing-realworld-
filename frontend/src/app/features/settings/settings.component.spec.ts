import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

import SettingsComponent from './settings.component';
import { UserService } from '../../core/auth/services/user.service';
import { User } from '../../core/auth/user.model';
import { Errors } from '../../core/models/errors.model';

describe('SettingsComponent', () => {
  let fixture: ComponentFixture<SettingsComponent>;
  let component: SettingsComponent;

  let userServiceMock: {
    getCurrentUserSync: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
  };

  let routerMock: {
    navigate: ReturnType<typeof vi.fn>;
  };

  const mockUser: User = {
    email: 'demo@example.com',
    token: 'jwt-token',
    username: 'demo-user',
    bio: 'Demo bio',
    image: 'https://example.com/avatar.jpg',
  };

  const updatedUser: User = {
    ...mockUser,
    username: 'updated-user',
    email: 'updated@example.com',
    bio: 'Updated bio',
    image: 'https://example.com/updated-avatar.jpg',
  };

  beforeEach(async () => {
    userServiceMock = {
      getCurrentUserSync: vi.fn(),
      update: vi.fn(),
      logout: vi.fn(),
    };

    routerMock = {
      navigate: vi.fn().mockResolvedValue(true),
    };

    await TestBed.configureTestingModule({
      imports: [SettingsComponent],
      providers: [
        {
          provide: UserService,
          useValue: userServiceMock,
        },
        {
          provide: Router,
          useValue: routerMock,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SettingsComponent);
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

  it('should load the current user into the settings form on init', () => {
    // TL5: R1 Authentifizierung / Benutzerkonto-Zustand korrekt in UI-Formular übernehmen
    userServiceMock.getCurrentUserSync.mockReturnValue(mockUser);

    fixture.detectChanges();

    expect(component.settingsForm.getRawValue()).toEqual({
      image: 'https://example.com/avatar.jpg',
      username: 'demo-user',
      bio: 'Demo bio',
      email: 'demo@example.com',
      password: '',
    });
  });

  it('should convert null image and bio values to empty strings', () => {
    // TL5: R7 Formularverhalten / null-Werte stabil als leere Formularfelder darstellen
    const userWithNullValues: User = {
      ...mockUser,
      image: null,
      bio: null,
    };

    userServiceMock.getCurrentUserSync.mockReturnValue(userWithNullValues);

    fixture.detectChanges();

    expect(component.settingsForm.getRawValue()).toEqual({
      image: '',
      username: 'demo-user',
      bio: '',
      email: 'demo@example.com',
      password: '',
    });
  });

  it('should not patch the form when no current user is available', () => {
    // TL5: R1 Authentifizierungszustand / Komponente bleibt stabil ohne geladenen User
    userServiceMock.getCurrentUserSync.mockReturnValue(null);

    fixture.detectChanges();

    expect(component.settingsForm.getRawValue()).toEqual({
      image: '',
      username: '',
      bio: '',
      email: '',
      password: '',
    });
  });

  it('should remove an empty password from the update payload', () => {
    // TL5: R7 Formularverhalten / leeres Passwort darf nicht als Änderung übertragen werden
    userServiceMock.getCurrentUserSync.mockReturnValue(mockUser);
    userServiceMock.update.mockReturnValue(of({ user: updatedUser }));

    fixture.detectChanges();

    component.settingsForm.setValue({
      image: 'https://example.com/updated-avatar.jpg',
      username: 'updated-user',
      bio: 'Updated bio',
      email: 'updated@example.com',
      password: '',
    });

    component.submitForm();

    expect(userServiceMock.update).toHaveBeenCalledWith({
      image: 'https://example.com/updated-avatar.jpg',
      username: 'updated-user',
      bio: 'Updated bio',
      email: 'updated@example.com',
    });
  });

  it('should include a non-empty password in the update payload', () => {
    // TL5: R4 Datenverarbeitung / explizite Kontoänderung vollständig an UserService übergeben
    userServiceMock.getCurrentUserSync.mockReturnValue(mockUser);
    userServiceMock.update.mockReturnValue(of({ user: updatedUser }));

    fixture.detectChanges();

    component.settingsForm.setValue({
      image: 'https://example.com/updated-avatar.jpg',
      username: 'updated-user',
      bio: 'Updated bio',
      email: 'updated@example.com',
      password: 'new-password-123',
    });

    component.submitForm();

    expect(userServiceMock.update).toHaveBeenCalledWith({
      image: 'https://example.com/updated-avatar.jpg',
      username: 'updated-user',
      bio: 'Updated bio',
      email: 'updated@example.com',
      password: 'new-password-123',
    });
  });

  it('should set submitting state while the update request is pending', () => {
    // TL5: R7 UI-naher Formularzustand / mehrfaches Absenden während Request verhindern
    const pendingUpdate = new Subject<{ user: User }>();

    userServiceMock.getCurrentUserSync.mockReturnValue(mockUser);
    userServiceMock.update.mockReturnValue(pendingUpdate.asObservable());

    fixture.detectChanges();

    component.settingsForm.patchValue({
      username: 'updated-user',
      email: 'updated@example.com',
      password: '',
    });

    component.submitForm();

    expect(component.isSubmitting()).toBe(true);
    expect(component.errors()).toBeNull();

    pendingUpdate.next({ user: updatedUser });
    pendingUpdate.complete();
  });

  it('should navigate to the updated user profile after successful update', () => {
    // TL5: R5 Nutzerfluss / erfolgreiche Kontoänderung führt zum Profil des aktualisierten Users
    userServiceMock.getCurrentUserSync.mockReturnValue(mockUser);
    userServiceMock.update.mockReturnValue(of({ user: updatedUser }));

    fixture.detectChanges();

    component.settingsForm.setValue({
      image: 'https://example.com/updated-avatar.jpg',
      username: 'updated-user',
      bio: 'Updated bio',
      email: 'updated@example.com',
      password: '',
    });

    component.submitForm();

    expect(routerMock.navigate).toHaveBeenCalledWith(['/profile/', 'updated-user']);
  });

  it('should set errors and stop submitting when update fails', () => {
    // TL5: R3 API-Fehlerantwort / R7 Formular zeigt Fehler und wird wieder bedienbar
    const validationError: Errors = {
      errors: {
        email: 'is invalid',
        username: 'is already taken',
      },
    };

    userServiceMock.getCurrentUserSync.mockReturnValue(mockUser);
    userServiceMock.update.mockReturnValue(throwError(() => validationError));

    fixture.detectChanges();

    component.settingsForm.setValue({
      image: '',
      username: 'taken-user',
      bio: '',
      email: 'invalid-email',
      password: '',
    });

    component.submitForm();

    expect(component.errors()).toEqual(validationError);
    expect(component.isSubmitting()).toBe(false);
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it('should call userService.logout when logout is triggered', () => {
    // TL5: R1 Authentifizierung / Logout-Funktion der geschützten Kontoansicht
    userServiceMock.getCurrentUserSync.mockReturnValue(mockUser);

    fixture.detectChanges();

    component.logout();

    expect(userServiceMock.logout).toHaveBeenCalledOnce();
  });
});