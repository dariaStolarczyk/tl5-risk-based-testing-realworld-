import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AuthState, UserService } from './user.service';
import { JwtService } from './jwt.service';
import { User } from '../user.model';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

  let jwtService: {
    saveToken: ReturnType<typeof vi.fn>;
    destroyToken: ReturnType<typeof vi.fn>;
    getToken: ReturnType<typeof vi.fn>;
  };

  let router: {
    navigate: ReturnType<typeof vi.fn>;
  };

  const mockUser: User = {
    email: 'test@example.com',
    token: 'test-jwt-token',
    username: 'testuser',
    bio: 'Test bio',
    image: 'https://example.com/avatar.jpg',
  };

  beforeEach(() => {
    jwtService = {
      saveToken: vi.fn(),
      destroyToken: vi.fn(),
      getToken: vi.fn(),
    };

    router = {
      navigate: vi.fn().mockResolvedValue(true),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        UserService,
        { provide: JwtService, useValue: jwtService },
        { provide: Router, useValue: router },
      ],
    });

    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    vi.clearAllTimers();
    vi.useRealTimers();
    TestBed.resetTestingModule();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('currentUser observable', () => {
    it('should emit null initially', async () => {
      const user = await firstValueFrom(service.currentUser);
      expect(user).toBeNull();
    });

    it('should emit distinct values only', () => {
      const emissions: (User | null)[] = [];

      const sub = service.currentUser.subscribe(user => {
        emissions.push(user);
      });

      service.setAuth(mockUser);
      service.setAuth(mockUser);

      expect(emissions).toEqual([null, mockUser]);

      sub.unsubscribe();
    });
  });

  describe('authState observable', () => {
    it('should emit loading initially', async () => {
      const state = await firstValueFrom(service.authState);
      expect(state).toBe('loading');
    });

    it('should set authState to authenticated when setAuth is called', () => {
      const emissions: AuthState[] = [];

      const sub = service.authState.subscribe(state => {
        emissions.push(state);
      });

      service.setAuth(mockUser);

      expect(jwtService.saveToken).toHaveBeenCalledWith(mockUser.token);
      expect(emissions).toEqual(['loading', 'authenticated']);

      sub.unsubscribe();
    });

    it('should set authState to unauthenticated when purgeAuth is called', () => {
      const emissions: AuthState[] = [];

      const sub = service.authState.subscribe(state => {
        emissions.push(state);
      });

      service.setAuth(mockUser);
      service.purgeAuth();

      expect(jwtService.destroyToken).toHaveBeenCalledTimes(1);
      expect(emissions).toEqual(['loading', 'authenticated', 'unauthenticated']);

      sub.unsubscribe();
    });
  });

  describe('isAuthenticated observable', () => {
    it('should emit false when no user is authenticated', async () => {
      const isAuth = await firstValueFrom(service.isAuthenticated);
      expect(isAuth).toBe(false);
    });

    it('should emit true when user is authenticated', () => {
      const emissions: boolean[] = [];

      const sub = service.isAuthenticated.subscribe(isAuth => {
        emissions.push(isAuth);
      });

      service.setAuth(mockUser);

      expect(emissions).toEqual([false, true]);

      sub.unsubscribe();
    });
  });

  describe('getCurrentUserSync', () => {
    it('should return null initially', () => {
      expect(service.getCurrentUserSync()).toBeNull();
    });

    it('should return cached user after setAuth', () => {
      service.setAuth(mockUser);

      expect(service.getCurrentUserSync()).toEqual(mockUser);
    });

    it('should return null after purgeAuth', () => {
      service.setAuth(mockUser);
      service.purgeAuth();

      expect(service.getCurrentUserSync()).toBeNull();
    });
  });

  describe('login', () => {
    it('should send POST request to /users/login', () => {
      const credentials = {
        email: 'test@example.com',
        password: 'password123',
      };

      service.login(credentials).subscribe();

      const req = httpMock.expectOne('/users/login');

      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ user: credentials });

      req.flush({ user: mockUser });
    });

    it('should save token, update currentUser and set authState after successful login', async () => {
      const credentials = {
        email: 'test@example.com',
        password: 'password123',
      };

      const users: (User | null)[] = [];
      const states: AuthState[] = [];

      const userSub = service.currentUser.subscribe(user => users.push(user));
      const stateSub = service.authState.subscribe(state => states.push(state));

      const promise = firstValueFrom(service.login(credentials));

      const req = httpMock.expectOne('/users/login');
      req.flush({ user: mockUser });

      await promise;

      expect(jwtService.saveToken).toHaveBeenCalledWith(mockUser.token);
      expect(users).toEqual([null, mockUser]);
      expect(states).toEqual(['loading', 'authenticated']);

      userSub.unsubscribe();
      stateSub.unsubscribe();
    });

    it('should pass through login errors', async () => {
      const credentials = {
        email: 'test@example.com',
        password: 'wrong-password',
      };

      const promise = firstValueFrom(service.login(credentials));

      const req = httpMock.expectOne('/users/login');

      req.flush(
        { errors: { email: ['or password is invalid'] } },
        { status: 401, statusText: 'Unauthorized' },
      );

      await expect(promise).rejects.toMatchObject({
        status: 401,
      });

      expect(jwtService.saveToken).not.toHaveBeenCalled();
    });
  });

  describe('register', () => {
    it('should send POST request to /users', () => {
      const credentials = {
        username: 'newuser',
        email: 'new@example.com',
        password: 'password123',
      };

      service.register(credentials).subscribe();

      const req = httpMock.expectOne('/users');

      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ user: credentials });

      req.flush({ user: mockUser });
    });

    it('should save token, update currentUser and set authState after successful registration', async () => {
      const credentials = {
        username: 'newuser',
        email: 'new@example.com',
        password: 'password123',
      };

      const users: (User | null)[] = [];
      const states: AuthState[] = [];

      const userSub = service.currentUser.subscribe(user => users.push(user));
      const stateSub = service.authState.subscribe(state => states.push(state));

      const promise = firstValueFrom(service.register(credentials));

      const req = httpMock.expectOne('/users');
      req.flush({ user: mockUser });

      await promise;

      expect(jwtService.saveToken).toHaveBeenCalledWith(mockUser.token);
      expect(users).toEqual([null, mockUser]);
      expect(states).toEqual(['loading', 'authenticated']);

      userSub.unsubscribe();
      stateSub.unsubscribe();
    });

    it('should pass through registration errors', async () => {
      const credentials = {
        username: 'existing',
        email: 'existing@example.com',
        password: 'password123',
      };

      const promise = firstValueFrom(service.register(credentials));

      const req = httpMock.expectOne('/users');

      req.flush(
        { errors: { username: ['is already taken'] } },
        { status: 422, statusText: 'Unprocessable Entity' },
      );

      await expect(promise).rejects.toMatchObject({
        status: 422,
      });

      expect(jwtService.saveToken).not.toHaveBeenCalled();
    });
  });

  describe('logout', () => {
    it('should purge auth and navigate to home page', () => {
      service.logout();

      expect(jwtService.destroyToken).toHaveBeenCalledTimes(1);
      expect(router.navigate).toHaveBeenCalledWith(['/']);
    });

    it('should clear currentUser and set authState to unauthenticated', () => {
      const users: (User | null)[] = [];
      const states: AuthState[] = [];

      const userSub = service.currentUser.subscribe(user => users.push(user));
      const stateSub = service.authState.subscribe(state => states.push(state));

      service.setAuth(mockUser);
      service.logout();

      expect(users).toEqual([null, mockUser, null]);
      expect(states).toEqual(['loading', 'authenticated', 'unauthenticated']);

      userSub.unsubscribe();
      stateSub.unsubscribe();
    });
  });

  describe('getCurrentUser', () => {
    it('should send GET request to /user', () => {
      service.getCurrentUser().subscribe();

      const req = httpMock.expectOne('/user');

      expect(req.request.method).toBe('GET');

      req.flush({ user: mockUser });
    });

    it('should call setAuth on success', async () => {
      const states: AuthState[] = [];
      const stateSub = service.authState.subscribe(state => states.push(state));

      const promise = firstValueFrom(service.getCurrentUser());

      const req = httpMock.expectOne('/user');
      req.flush({ user: mockUser });

      await promise;

      expect(jwtService.saveToken).toHaveBeenCalledWith(mockUser.token);
      expect(service.getCurrentUserSync()).toEqual(mockUser);
      expect(states).toEqual(['loading', 'authenticated']);

      stateSub.unsubscribe();
    });

    it('should share replay the result for multiple subscribers of the same call', () => {
      const observable = service.getCurrentUser();

      observable.subscribe();
      observable.subscribe();

      const requests = httpMock.match('/user');

      expect(requests.length).toBe(1);

      requests[0].flush({ user: mockUser });
    });

    it('should destroy token and set authState to unauthenticated on 401', () => {
      const users: (User | null)[] = [];
      const states: AuthState[] = [];

      const userSub = service.currentUser.subscribe(user => users.push(user));
      const stateSub = service.authState.subscribe(state => states.push(state));

      service.getCurrentUser().subscribe();

      const req = httpMock.expectOne('/user');

      req.flush(
        { errors: { auth: ['Invalid token'] } },
        { status: 401, statusText: 'Unauthorized' },
      );

      expect(jwtService.destroyToken).toHaveBeenCalledTimes(1);
      expect(jwtService.saveToken).not.toHaveBeenCalled();
      expect(users).toEqual([null]);
      expect(states).toEqual(['loading', 'unauthenticated']);

      userSub.unsubscribe();
      stateSub.unsubscribe();
    });

    it('should destroy token and set authState to unauthenticated on any 4xx auth error', () => {
      const states: AuthState[] = [];
      const stateSub = service.authState.subscribe(state => states.push(state));

      service.getCurrentUser().subscribe();

      const req = httpMock.expectOne('/user');

      req.flush(
        { errors: { auth: ['Forbidden'] } },
        { status: 403, statusText: 'Forbidden' },
      );

      expect(jwtService.destroyToken).toHaveBeenCalledTimes(1);
      expect(states).toEqual(['loading', 'unauthenticated']);

      stateSub.unsubscribe();
    });

    it('should keep token and set authState to unavailable on 500', () => {
      vi.useFakeTimers();

      jwtService.getToken.mockReturnValue('still-valid-token');

      const users: (User | null)[] = [];
      const states: AuthState[] = [];

      const userSub = service.currentUser.subscribe(user => users.push(user));
      const stateSub = service.authState.subscribe(state => states.push(state));

      service.getCurrentUser().subscribe();

      const req = httpMock.expectOne('/user');

      req.flush(
        { errors: { server: ['Internal server error'] } },
        { status: 500, statusText: 'Internal Server Error' },
      );

      expect(jwtService.destroyToken).not.toHaveBeenCalled();
      expect(jwtService.saveToken).not.toHaveBeenCalled();
      expect(users).toEqual([null]);
      expect(states).toEqual(['loading', 'unavailable']);

      httpMock.expectNone('/user');

      userSub.unsubscribe();
      stateSub.unsubscribe();
    });

    it('should keep token and set authState to unavailable on status 0 network error', () => {
      jwtService.getToken.mockReturnValue(null);

      const users: (User | null)[] = [];
      const states: AuthState[] = [];

      const userSub = service.currentUser.subscribe(user => users.push(user));
      const stateSub = service.authState.subscribe(state => states.push(state));

      service.getCurrentUser().subscribe();

      const req = httpMock.expectOne('/user');

      req.flush(
        'Network error',
        { status: 0, statusText: 'Unknown Error' },
      );

      expect(jwtService.destroyToken).not.toHaveBeenCalled();
      expect(jwtService.saveToken).not.toHaveBeenCalled();
      expect(users).toEqual([null]);
      expect(states).toEqual(['loading', 'unavailable']);

      userSub.unsubscribe();
      stateSub.unsubscribe();
    });

    it('should not schedule retry after unavailable state when no token exists', () => {
      vi.useFakeTimers();

      jwtService.getToken.mockReturnValue(null);

      const states: AuthState[] = [];
      const stateSub = service.authState.subscribe(state => states.push(state));

      service.getCurrentUser().subscribe();

      const req = httpMock.expectOne('/user');

      req.flush(
        { errors: { server: ['Internal server error'] } },
        { status: 500, statusText: 'Internal Server Error' },
      );

      expect(states).toEqual(['loading', 'unavailable']);
      expect(jwtService.getToken).toHaveBeenCalledTimes(1);

      vi.advanceTimersByTime(16_000);

      httpMock.expectNone('/user');

      stateSub.unsubscribe();
    });

    it('should retry /user after unavailable state when token exists', () => {
      vi.useFakeTimers();

      jwtService.getToken.mockReturnValue('stored-token');

      const states: AuthState[] = [];
      const stateSub = service.authState.subscribe(state => states.push(state));

      service.getCurrentUser().subscribe();

      const failedReq = httpMock.expectOne('/user');

      failedReq.flush(
        { errors: { server: ['Internal server error'] } },
        { status: 500, statusText: 'Internal Server Error' },
      );

      expect(states).toEqual(['loading', 'unavailable']);
      expect(jwtService.destroyToken).not.toHaveBeenCalled();

      vi.advanceTimersByTime(1_999);
      httpMock.expectNone('/user');

      vi.advanceTimersByTime(1);

      const retryReq = httpMock.expectOne('/user');

      expect(states).toEqual(['loading', 'unavailable', 'loading']);

      retryReq.flush({ user: mockUser });

      expect(jwtService.saveToken).toHaveBeenCalledWith(mockUser.token);
      expect(states).toEqual(['loading', 'unavailable', 'loading', 'authenticated']);
      expect(service.getCurrentUserSync()).toEqual(mockUser);

      stateSub.unsubscribe();
    });
  });

  describe('update', () => {
    it('should send PUT request to /user', () => {
      const updates: Partial<User> = {
        bio: 'Updated bio',
        image: 'https://example.com/new-avatar.jpg',
      };

      service.update(updates).subscribe();

      const req = httpMock.expectOne('/user');

      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({ user: updates });

      req.flush({ user: { ...mockUser, ...updates } });
    });

    it('should update currentUser with new values', async () => {
      const updates: Partial<User> = {
        bio: 'Updated bio',
      };

      const updatedUser: User = {
        ...mockUser,
        ...updates,
      };

      const users: (User | null)[] = [];
      const userSub = service.currentUser.subscribe(user => users.push(user));

      const promise = firstValueFrom(service.update(updates));

      const req = httpMock.expectOne('/user');
      req.flush({ user: updatedUser });

      await promise;

      expect(users).toEqual([null, updatedUser]);

      userSub.unsubscribe();
    });

    it('should pass through update errors', async () => {
      const updates: Partial<User> = {
        bio: 'Updated bio',
      };

      const promise = firstValueFrom(service.update(updates));

      const req = httpMock.expectOne('/user');

      req.flush(
        { errors: { bio: ['is too long'] } },
        { status: 422, statusText: 'Unprocessable Entity' },
      );

      await expect(promise).rejects.toMatchObject({
        status: 422,
      });
    });
  });

  describe('Integration scenarios', () => {
    it('should handle complete authentication flow', async () => {
      const credentials = {
        email: 'test@example.com',
        password: 'password123',
      };

      const users: (User | null)[] = [];
      const states: AuthState[] = [];

      const userSub = service.currentUser.subscribe(user => users.push(user));
      const stateSub = service.authState.subscribe(state => states.push(state));

      const loginPromise = firstValueFrom(service.login(credentials));

      const loginReq = httpMock.expectOne('/users/login');
      loginReq.flush({ user: mockUser });

      await loginPromise;

      service.logout();

      expect(users).toEqual([null, mockUser, null]);
      expect(states).toEqual(['loading', 'authenticated', 'unauthenticated']);
      expect(router.navigate).toHaveBeenCalledWith(['/']);

      userSub.unsubscribe();
      stateSub.unsubscribe();
    });

    it('should maintain authenticated state across login and update', async () => {
      const credentials = {
        email: 'test@example.com',
        password: 'password123',
      };

      const loginPromise = firstValueFrom(service.login(credentials));

      const loginReq = httpMock.expectOne('/users/login');
      loginReq.flush({ user: mockUser });

      await loginPromise;

      const isAuthAfterLogin = await firstValueFrom(service.isAuthenticated);
      expect(isAuthAfterLogin).toBe(true);

      const updates: Partial<User> = {
        bio: 'New bio',
      };

      const updatedUser: User = {
        ...mockUser,
        ...updates,
      };

      const updatePromise = firstValueFrom(service.update(updates));

      const updateReq = httpMock.expectOne('/user');
      updateReq.flush({ user: updatedUser });

      await updatePromise;

      const isAuthAfterUpdate = await firstValueFrom(service.isAuthenticated);
      expect(isAuthAfterUpdate).toBe(true);
      expect(service.getCurrentUserSync()).toEqual(updatedUser);
    });
  });
});