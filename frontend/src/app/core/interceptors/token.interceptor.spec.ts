import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { JwtService } from '../auth/services/jwt.service';
import { tokenInterceptor } from './token.interceptor';

describe('tokenInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let jwtService: { getToken: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    jwtService = {
      getToken: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([tokenInterceptor])),
        provideHttpClientTesting(),
        { provide: JwtService, useValue: jwtService },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    TestBed.resetTestingModule();
  });

  it('should add Authorization header when token exists', () => {
    jwtService.getToken.mockReturnValue('test-jwt-token');

    http.get('/articles').subscribe();

    const req = httpMock.expectOne('/articles');

    expect(req.request.method).toBe('GET');
    expect(req.request.headers.get('Authorization')).toBe('Token test-jwt-token');

    req.flush({ articles: [], articlesCount: 0 });
  });

  it('should not add Authorization header when no token exists', () => {
    jwtService.getToken.mockReturnValue('');

    http.get('/articles').subscribe();

    const req = httpMock.expectOne('/articles');

    expect(req.request.method).toBe('GET');
    expect(req.request.headers.has('Authorization')).toBe(false);

    req.flush({ articles: [], articlesCount: 0 });
  });
});