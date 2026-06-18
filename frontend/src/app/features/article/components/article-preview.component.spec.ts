import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

import { ArticlePreviewComponent } from './article-preview.component';
import { FavoriteButtonComponent } from './favorite-button.component';
import { ArticlesService } from '../services/articles.service';
import { UserService } from '../../../core/auth/services/user.service';
import { Article } from '../models/article.model';

describe('ArticlePreviewComponent', () => {
  let fixture: ComponentFixture<ArticlePreviewComponent>;
  let component: ArticlePreviewComponent;

  let articlesServiceMock: {
    favorite: ReturnType<typeof vi.fn>;
    unfavorite: ReturnType<typeof vi.fn>;
  };

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
      favorite: vi.fn(),
      unfavorite: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [ArticlePreviewComponent],
      providers: [
        provideRouter([]),
        {
          provide: ArticlesService,
          useValue: articlesServiceMock,
        },
        {
          provide: UserService,
          useValue: {
            isAuthenticated: of(true),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ArticlePreviewComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    fixture.destroy();
    TestBed.resetTestingModule();
    vi.clearAllMocks();
  });

  function renderArticle(article: Article = mockArticle): void {
    fixture.componentRef.setInput('articleInput', article);
    fixture.detectChanges();
  }

  it('should create the component', () => {
    renderArticle();

    expect(component).toBeTruthy();
  });

  it('should render title, description, author, favorite count and tags', () => {
    // TL5: R7 Frontend-Darstellung / UI-nahe Zustände
    renderArticle();

    const nativeElement: HTMLElement = fixture.nativeElement;

    expect(nativeElement.querySelector('h1')?.textContent?.trim()).toBe('Test Article');
    expect(nativeElement.querySelector('p')?.textContent?.trim()).toBe('Test description');
    expect(nativeElement.querySelector('.author')?.textContent?.trim()).toBe('demo-user');

    const favoriteButton = nativeElement.querySelector('app-favorite-button button') as HTMLButtonElement;
    expect(favoriteButton.textContent).toContain('3');

    const tags = Array.from(nativeElement.querySelectorAll('.tag-list li')).map(tag => tag.textContent?.trim());

    expect(tags).toEqual(['angular', 'testing']);
  });

  it('should pass the article slug into the preview router link', () => {
    // TL5: R7 Frontend-Darstellung / Navigation aus Artikelliste
    renderArticle();

    const previewLink = fixture.nativeElement.querySelector('a.preview-link') as HTMLAnchorElement;

    expect(previewLink.getAttribute('href')).toBe('/article/test-article');
  });

  it('should set favorited to true and increment favoritesCount when toggleFavorite receives true', () => {
    // TL5: R4 Datenverarbeitung / Parent-State nach Favorisieren
    renderArticle();

    component.toggleFavorite(true);
    fixture.detectChanges();

    expect(component.article().favorited).toBe(true);
    expect(component.article().favoritesCount).toBe(4);

    const favoriteButton = fixture.nativeElement.querySelector('app-favorite-button button') as HTMLButtonElement;
    expect(favoriteButton.textContent).toContain('4');
  });

  it('should set favorited to false and decrement favoritesCount when toggleFavorite receives false', () => {
    // TL5: R4 Datenverarbeitung / Parent-State nach Entfernen des Favoriten
    renderArticle({
      ...mockArticle,
      favorited: true,
      favoritesCount: 4,
    });

    component.toggleFavorite(false);
    fixture.detectChanges();

    expect(component.article().favorited).toBe(false);
    expect(component.article().favoritesCount).toBe(3);

    const favoriteButton = fixture.nativeElement.querySelector('app-favorite-button button') as HTMLButtonElement;
    expect(favoriteButton.textContent).toContain('3');
  });

  it('should update the parent state when the child favorite button emits true', () => {
    // TL5: R4 Favoriten-Parent-State / Output-Binding vom Child
    renderArticle();

    const favoriteButtonComponent = fixture.debugElement.query(By.directive(FavoriteButtonComponent))
      .componentInstance as FavoriteButtonComponent;

    favoriteButtonComponent.toggle.emit(true);
    fixture.detectChanges();

    expect(component.article().favorited).toBe(true);
    expect(component.article().favoritesCount).toBe(4);
  });

  it('should not mutate the original article input object when updating favorite state', () => {
    // TL5: R7 UI-State stabil halten / keine unerwartete Input-Mutation
    const articleInput: Article = {
      ...mockArticle,
      favorited: false,
      favoritesCount: 3,
    };

    renderArticle(articleInput);

    component.toggleFavorite(true);

    expect(articleInput.favorited).toBe(false);
    expect(articleInput.favoritesCount).toBe(3);
    expect(component.article()).not.toBe(articleInput);
  });
});