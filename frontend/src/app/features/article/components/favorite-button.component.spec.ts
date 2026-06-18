import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

import { FavoriteButtonComponent } from './favorite-button.component';
import { ArticlesService } from '../services/articles.service';
import { UserService } from '../../../core/auth/services/user.service';
import { Article } from '../models/article.model';

describe('FavoriteButtonComponent', () => {
  let fixture: ComponentFixture<FavoriteButtonComponent>;
  let component: FavoriteButtonComponent;

  let isAuthenticatedSubject: BehaviorSubject<boolean>;

  let articlesServiceMock: {
    favorite: ReturnType<typeof vi.fn>;
    unfavorite: ReturnType<typeof vi.fn>;
  };

  let routerMock: {
    navigate: ReturnType<typeof vi.fn>;
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
    isAuthenticatedSubject = new BehaviorSubject<boolean>(true);

    articlesServiceMock = {
      favorite: vi.fn(),
      unfavorite: vi.fn(),
    };

    routerMock = {
      navigate: vi.fn().mockResolvedValue(true),
    };

    await TestBed.configureTestingModule({
      imports: [FavoriteButtonComponent],
      providers: [
        {
          provide: ArticlesService,
          useValue: articlesServiceMock,
        },
        {
          provide: Router,
          useValue: routerMock,
        },
        {
          provide: UserService,
          useValue: {
            isAuthenticated: isAuthenticatedSubject.asObservable(),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(FavoriteButtonComponent);
    component = fixture.componentInstance;
    component.article = { ...mockArticle };
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture.destroy();
    TestBed.resetTestingModule();
    vi.clearAllMocks();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should redirect unauthenticated users to register and not call the API', () => {
    // TL5: R1 Authentifizierung / geschützte Aktion Favorisieren
    isAuthenticatedSubject.next(false);
    const emitSpy = vi.spyOn(component.toggle, 'emit');

    component.toggleFavorite();

    expect(routerMock.navigate).toHaveBeenCalledWith(['/register']);
    expect(articlesServiceMock.favorite).not.toHaveBeenCalled();
    expect(articlesServiceMock.unfavorite).not.toHaveBeenCalled();
    expect(emitSpy).not.toHaveBeenCalled();
  });

  it('should favorite an article when the user is authenticated and the article is not favorited', () => {
    // TL5: R1 Authentifizierung / R4 Statusänderung Favorit
    isAuthenticatedSubject.next(true);
    const emitSpy = vi.spyOn(component.toggle, 'emit');

    articlesServiceMock.favorite.mockReturnValue(
      of({
        ...mockArticle,
        favorited: true,
        favoritesCount: 4,
      }),
    );

    component.article = {
      ...mockArticle,
      favorited: false,
    };

    component.toggleFavorite();

    expect(articlesServiceMock.favorite).toHaveBeenCalledWith('test-article');
    expect(articlesServiceMock.unfavorite).not.toHaveBeenCalled();
    expect(emitSpy).toHaveBeenCalledWith(true);
    expect(component.isSubmitting()).toBe(false);
  });

  it('should unfavorite an article when the user is authenticated and the article is already favorited', () => {
    // TL5: R1 Authentifizierung / R4 Statusänderung Favorit
    isAuthenticatedSubject.next(true);
    const emitSpy = vi.spyOn(component.toggle, 'emit');

    articlesServiceMock.unfavorite.mockReturnValue(of(undefined));

    component.article = {
      ...mockArticle,
      favorited: true,
      favoritesCount: 4,
    };

    component.toggleFavorite();

    expect(articlesServiceMock.unfavorite).toHaveBeenCalledWith('test-article');
    expect(articlesServiceMock.favorite).not.toHaveBeenCalled();
    expect(emitSpy).toHaveBeenCalledWith(false);
    expect(component.isSubmitting()).toBe(false);
  });

  it('should reset submitting state when favoriting fails', () => {
    // TL5: R3 API-Fehlerantwort / kontrollierte Fehlerbehandlung
    isAuthenticatedSubject.next(true);
    const emitSpy = vi.spyOn(component.toggle, 'emit');

    articlesServiceMock.favorite.mockReturnValue(throwError(() => new Error('favorite failed')));

    component.article = {
      ...mockArticle,
      favorited: false,
    };

    component.toggleFavorite();

    expect(articlesServiceMock.favorite).toHaveBeenCalledWith('test-article');
    expect(component.isSubmitting()).toBe(false);
    expect(emitSpy).not.toHaveBeenCalled();
  });

  it('should reset submitting state when unfavoriting fails', () => {
    // TL5: R3 API-Fehlerantwort / kontrollierte Fehlerbehandlung
    isAuthenticatedSubject.next(true);
    const emitSpy = vi.spyOn(component.toggle, 'emit');

    articlesServiceMock.unfavorite.mockReturnValue(throwError(() => new Error('unfavorite failed')));

    component.article = {
      ...mockArticle,
      favorited: true,
    };

    component.toggleFavorite();

    expect(articlesServiceMock.unfavorite).toHaveBeenCalledWith('test-article');
    expect(component.isSubmitting()).toBe(false);
    expect(emitSpy).not.toHaveBeenCalled();
  });

    it('should apply outline styling when the article is not favorited', () => {
    // TL5: R7 Frontend-Darstellung
    fixture.componentRef.setInput('article', {
        ...mockArticle,
        favorited: false,
    });

    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');

    expect(button.classList.contains('btn-outline-primary')).toBe(true);
    expect(button.classList.contains('btn-primary')).toBe(false);
    });

    it('should apply primary styling when the article is favorited', () => {
    // TL5: R7 Frontend-Darstellung
    fixture.componentRef.setInput('article', {
        ...mockArticle,
        favorited: true,
    });

    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');

    expect(button.classList.contains('btn-primary')).toBe(true);
    expect(button.classList.contains('btn-outline-primary')).toBe(false);
    });
});