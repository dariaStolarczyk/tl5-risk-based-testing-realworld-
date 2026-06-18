import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { apiInterceptor } from './api.interceptor';

describe('apiInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([apiInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    TestBed.resetTestingModule();
  });

  it('should prepend backend base URL to relative requests', () => {
    http.get('/articles').subscribe();

    const req = httpMock.expectOne('http://localhost:8080/articles');

    expect(req.request.method).toBe('GET');
    expect(req.request.url).toBe('http://localhost:8080/articles');

    req.flush({ articles: [], articlesCount: 0 });
  });

  it('should preserve query parameters when backend base URL is prepended', () => {
    http.get('/articles', {
      params: {
        tag: 'testing',
        limit: 10,
        offset: 0,
      },
    }).subscribe();

    const req = httpMock.expectOne(request =>
      request.url === 'http://localhost:8080/articles' &&
      request.params.get('tag') === 'testing' &&
      request.params.get('limit') === '10' &&
      request.params.get('offset') === '0'
    );

    expect(req.request.method).toBe('GET');

    req.flush({ articles: [], articlesCount: 0 });
  });
});