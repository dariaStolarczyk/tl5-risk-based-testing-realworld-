// src/app/features/article/components/article-list.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

import { ArticleListComponent } from './article-list.component';
import { ArticlesService } from '../services/articles.service';
import { UserService } from '../../../core/auth/services/user.service';
import { Article } from '../models/article.model';
import { ArticleListConfig } from '../models/article-list-config.model';
import { LoadingState } from '../../../core/models/loading-state.model';

describe('ArticleListComponent', () => {
  let fixture: ComponentFixture<ArticleListComponent>;
  let component: ArticleListComponent;

  let articlesServiceMock: {
    query: ReturnType<typeof vi.fn>;
    favorite: ReturnType<typeof vi.fn>;
    unfavorite: ReturnType<typeof vi.fn>;
  };

  const isAuthenticatedSubject = new BehaviorSubject<boolean>(true);

  const mockArticle: Article = {
    slug: 'test-article',
    title: 'Test Article',
    description: 'Test description',
    body: 'Test body',
    tagList: ['angular', 'testing'],
    createdAt: '2026-01-01T00:00:00.000Z',
    updatedAt: '2026-01-01T00:00:00.000Z',
    favorited: false,
    favoritesCount: 3,
    author: {
      username: 'demo-user',
      bio: null,
      image: null,
      following: false,
    },
  };

  beforeEach(async () => {
    articlesServiceMock = {
      query: vi.fn(),
      favorite: vi.fn(),
      unfavorite: vi.fn(),
    };

    articlesServiceMock.query.mockReturnValue(
      of({
        articles: [mockArticle],
        articlesCount: 25,
      }),
    );

    await TestBed.configureTestingModule({
      imports: [ArticleListComponent],
      providers: [
        provideRouter([]),
        {
          provide: ArticlesService,
          useValue: articlesServiceMock,
        },
        {
          provide: UserService,
          useValue: {
            isAuthenticated: isAuthenticatedSubject.asObservable(),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ArticleListComponent);
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

  it('should query articles with limit and calculated offset for the current page', () => {
    // TL5: R6 Artikellisten und Pagination
    const config: ArticleListConfig = {
      type: 'all',
      filters: {},
    };

    fixture.componentRef.setInput('limit', 10);
    fixture.componentRef.setInput('currentPage', 2);
    fixture.componentRef.setInput('config', config);

    fixture.detectChanges();

    expect(articlesServiceMock.query).toHaveBeenCalledWith({
      type: 'all',
      filters: {
        limit: 10,
        offset: 10,
      },
    });

    expect(component.page()).toBe(2);
    expect(component.results()).toEqual([mockArticle]);
    expect(component.loading()).toBe(LoadingState.LOADED);
  });

  it('should calculate total pages from articlesCount and limit', () => {
    // TL5: R6 Pagination / korrekte Seitenanzahl
    articlesServiceMock.query.mockReturnValue(
      of({
        articles: [mockArticle],
        articlesCount: 25,
      }),
    );

    fixture.componentRef.setInput('limit', 10);
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('config', {
      type: 'all',
      filters: {},
    });

    fixture.detectChanges();

    expect(component.totalPages()).toEqual([1, 2, 3]);
  });

  it('should emit pageChange and run a new query when changing to another page', () => {
    // TL5: R6 Pagination / Nutzerinteraktion
    const emitSpy = vi.spyOn(component.pageChange, 'emit');

    fixture.componentRef.setInput('limit', 10);
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('config', {
      type: 'all',
      filters: {},
    });

    fixture.detectChanges();
    articlesServiceMock.query.mockClear();

    component.setPageTo(3);

    expect(component.page()).toBe(3);
    expect(emitSpy).toHaveBeenCalledWith(3);
    expect(articlesServiceMock.query).toHaveBeenCalledWith({
      type: 'all',
      filters: {
        limit: 10,
        offset: 20,
      },
    });
  });

  it('should not emit pageChange or query again when selecting the current page', () => {
    // TL5: R6 Pagination / unnötige Requests vermeiden
    const emitSpy = vi.spyOn(component.pageChange, 'emit');

    fixture.componentRef.setInput('limit', 10);
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('config', {
      type: 'all',
      filters: {},
    });

    fixture.detectChanges();
    articlesServiceMock.query.mockClear();

    component.setPageTo(1);

    expect(emitSpy).not.toHaveBeenCalled();
    expect(articlesServiceMock.query).not.toHaveBeenCalled();
  });

  it('should keep tag filters when adding pagination parameters', () => {
    // TL5: R6 Artikellisten und Filter
    const config: ArticleListConfig = {
      type: 'all',
      filters: {
        tag: 'angular',
      },
    };

    fixture.componentRef.setInput('limit', 20);
    fixture.componentRef.setInput('currentPage', 3);
    fixture.componentRef.setInput('config', config);

    fixture.detectChanges();

    expect(articlesServiceMock.query).toHaveBeenCalledWith({
      type: 'all',
      filters: {
        tag: 'angular',
        limit: 20,
        offset: 40,
      },
    });
  });

  it('should show an empty result list when the API returns no articles', () => {
    // TL5: R3 API-Antwort / R6 leere Artikelliste
    articlesServiceMock.query.mockReturnValue(
      of({
        articles: [],
        articlesCount: 0,
      }),
    );

    fixture.componentRef.setInput('limit', 10);
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('config', {
      type: 'all',
      filters: {},
    });

    fixture.detectChanges();

    expect(component.results()).toEqual([]);
    expect(component.totalPages()).toEqual([]);
    expect(component.loading()).toBe(LoadingState.LOADED);
  });
});