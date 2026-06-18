import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';

import { UserService } from '../auth/services/user.service';
import { errorInterceptor } from './error.interceptor';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let userService: { purgeAuth: ReturnType<typeof vi.fn> };

  const networkFallback = {
    network: ['Unable to connect. Please check your internet connection.'],
  };

  beforeEach(() => {
    userService = {
      purgeAuth: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: UserService, useValue: userService },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    TestBed.resetTestingModule();
  });

  it('should call purgeAuth on 401 errors for non-/user requests', async () => {
    const promise = firstValueFrom(http.get('/articles'));

    const req = httpMock.expectOne('/articles');

    req.flush(
      { errors: { auth: ['Token expired'] } },
      { status: 401, statusText: 'Unauthorized' },
    );

    await expect(promise).rejects.toMatchObject({
      status: 401,
      errors: { auth: ['Token expired'] },
    });

    expect(userService.purgeAuth).toHaveBeenCalledTimes(1);
  });

  it('should not call purgeAuth on 401 errors for /user request', async () => {
    const promise = firstValueFrom(http.get('/user'));

    const req = httpMock.expectOne('/user');

    req.flush(
      { errors: { auth: ['Invalid token'] } },
      { status: 401, statusText: 'Unauthorized' },
    );

    await expect(promise).rejects.toMatchObject({
      status: 401,
      errors: { auth: ['Invalid token'] },
    });

    expect(userService.purgeAuth).not.toHaveBeenCalled();
  });

  it('should forward 422 validation errors with errors body and not call purgeAuth', async () => {
    const validationErrors = {
      email: ['is invalid'],
      password: ['is too short'],
    };

    const promise = firstValueFrom(
      http.post('/users', {
        user: {
          email: 'invalid-email',
          password: '123',
        },
      }),
    );

    const req = httpMock.expectOne('/users');

    req.flush(
      { errors: validationErrors },
      { status: 422, statusText: 'Unprocessable Entity' },
    );

    await expect(promise).rejects.toMatchObject({
      status: 422,
      errors: validationErrors,
    });

    expect(userService.purgeAuth).not.toHaveBeenCalled();
  });

  it('should normalize 500 errors and keep server error body', async () => {
    const promise = firstValueFrom(http.get('/articles'));

    const req = httpMock.expectOne('/articles');

    req.flush(
      { errors: { server: ['Internal server error'] } },
      { status: 500, statusText: 'Internal Server Error' },
    );

    await expect(promise).rejects.toMatchObject({
      status: 500,
      errors: { server: ['Internal server error'] },
    });

    expect(userService.purgeAuth).not.toHaveBeenCalled();
  });

  it('should return fallback network error format when error body has no errors object', async () => {
    const promise = firstValueFrom(http.get('/articles'));

    const req = httpMock.expectOne('/articles');

    req.flush('Server is not reachable', {
      status: 500,
      statusText: 'Internal Server Error',
    });

    await expect(promise).rejects.toMatchObject({
      status: 500,
      errors: networkFallback,
    });

    expect(userService.purgeAuth).not.toHaveBeenCalled();
  });

  it('should normalize status 0 network errors to fallback network error format', async () => {
    const promise = firstValueFrom(http.get('/articles'));

    const req = httpMock.expectOne('/articles');

    req.error(new ProgressEvent('error'), {
      status: 0,
      statusText: 'Unknown Error',
    });

    await expect(promise).rejects.toMatchObject({
      status: 0,
      errors: networkFallback,
    });

    expect(userService.purgeAuth).not.toHaveBeenCalled();
  });
});