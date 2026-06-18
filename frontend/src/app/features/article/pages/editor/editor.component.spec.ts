// src/app/features/article/pages/editor/editor.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

import EditorComponent from './editor.component';
import { ArticlesService } from '../../services/articles.service';
import { Article } from '../../models/article.model';
import { UserService } from '../../../../core/auth/services/user.service';
import { Errors } from '../../../../core/models/errors.model';

describe('EditorComponent', () => {
  let fixture: ComponentFixture<EditorComponent>;
  let component: EditorComponent;

  let articleServiceMock: {
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    get: ReturnType<typeof vi.fn>;
  };

  let userServiceMock: {
    getCurrentUser: ReturnType<typeof vi.fn>;
  };

  let routerMock: {
    navigate: ReturnType<typeof vi.fn>;
  };

  let activatedRouteMock: {
    snapshot: {
      params: Record<string, string>;
    };
  };

  const mockArticle: Article = {
    slug: 'existing-article',
    title: 'Existing Article',
    description: 'Existing description',
    body: 'Existing body',
    tagList: ['angular', 'testing'],
    createdAt: '2026-01-01T00:00:00.000Z',
    updatedAt: '2026-01-01T00:00:00.000Z',
    favorited: false,
    favoritesCount: 0,
    author: {
      username: 'demo-user',
      bio: null,
      image: null,
      following: false,
    },
  };

  beforeEach(async () => {
    activatedRouteMock = {
      snapshot: {
        params: {},
      },
    };

    routerMock = {
      navigate: vi.fn().mockResolvedValue(true),
    };

    articleServiceMock = {
      create: vi.fn(),
      update: vi.fn(),
      get: vi.fn(),
    };

    userServiceMock = {
      getCurrentUser: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [EditorComponent],
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
          provide: ArticlesService,
          useValue: articleServiceMock,
        },
        {
          provide: UserService,
          useValue: userServiceMock,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(EditorComponent);
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

  it('should add a non-empty tag and clear the tag input', () => {
    // TL5: R5 Nutzerfluss / R4 korrekte Datenübergabe an Artikelerstellung
    component.tagField.setValue('testing');

    component.addTag();

    expect(component.tagList()).toEqual(['testing']);
    expect(component.tagField.value).toBe('');
  });

  it('should not add empty tags', () => {
    // TL5: R7 Frontend-Validierung
    component.tagField.setValue('   ');

    component.addTag();

    expect(component.tagList()).toEqual([]);
    expect(component.tagField.value).toBe('');
  });

  it('should not add duplicate tags', () => {
    // TL5: R7 Frontend-Validierung
    component.tagList.set(['angular']);
    component.tagField.setValue('angular');

    component.addTag();

    expect(component.tagList()).toEqual(['angular']);
    expect(component.tagField.value).toBe('');
  });

  it('should remove an existing tag', () => {
    // TL5: R7 Frontend-Validierung
    component.tagList.set(['angular', 'testing', 'realworld']);

    component.removeTag('testing');

    expect(component.tagList()).toEqual(['angular', 'realworld']);
  });

  it('should create a new article with form values and tags', () => {
    // TL5: R5 zentraler Nutzerfluss Artikelerstellung
    const createdArticle: Article = {
      ...mockArticle,
      slug: 'new-article',
      title: 'New Article',
      description: 'New description',
      body: 'New body',
      tagList: ['frontend', 'testing'],
    };

    articleServiceMock.create.mockReturnValue(of(createdArticle));

    component.articleForm.setValue({
      title: 'New Article',
      description: 'New description',
      body: 'New body',
    });
    component.tagList.set(['frontend']);
    component.tagField.setValue('testing');

    component.submitForm();

    expect(articleServiceMock.create).toHaveBeenCalledWith({
      title: 'New Article',
      description: 'New description',
      body: 'New body',
      tagList: ['frontend', 'testing'],
    });
    expect(articleServiceMock.update).not.toHaveBeenCalled();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/article/', 'new-article']);
  });

  it('should set errors and stop submitting when article creation fails', () => {
    // TL5: R3 API-Fehlerantwort / R5 Nutzerfluss bricht kontrolliert ab
    const validationError: Errors = {
      errors: {
        title: 'cannot be blank',
        body: 'cannot be blank',
      },
    };

    articleServiceMock.create.mockReturnValue(throwError(() => validationError));

    component.articleForm.setValue({
      title: '',
      description: '',
      body: '',
    });

    component.submitForm();

    expect(component.errors()).toEqual(validationError);
    expect(component.isSubmitting()).toBe(false);
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it('should update an existing article when a slug is present', () => {
    // TL5: R4 Datenverarbeitung / Änderung bestehender Artikel
    activatedRouteMock.snapshot.params = {
      slug: 'existing-article',
    };

    const updatedArticle: Article = {
      ...mockArticle,
      title: 'Updated Article',
      description: 'Updated description',
      body: 'Updated body',
      tagList: ['updated', 'angular'],
    };

    articleServiceMock.update.mockReturnValue(of(updatedArticle));

    component.articleForm.setValue({
      title: 'Updated Article',
      description: 'Updated description',
      body: 'Updated body',
    });
    component.tagList.set(['updated']);
    component.tagField.setValue('angular');

    component.submitForm();

    expect(articleServiceMock.update).toHaveBeenCalledWith({
      title: 'Updated Article',
      description: 'Updated description',
      body: 'Updated body',
      tagList: ['updated', 'angular'],
      slug: 'existing-article',
    });
    expect(articleServiceMock.create).not.toHaveBeenCalled();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/article/', 'existing-article']);
  });

  it('should load an existing article into the form when the current user is the author', () => {
    // TL5: R1 Authentifizierung / R5 Bearbeitungsfluss nur für Autor
    activatedRouteMock.snapshot.params = {
      slug: 'existing-article',
    };

    articleServiceMock.get.mockReturnValue(of(mockArticle));
    userServiceMock.getCurrentUser.mockReturnValue(
      of({
        user: {
          email: 'demo@example.com',
          token: 'jwt-token',
          username: 'demo-user',
          bio: null,
          image: null,
        },
      }),
    );

    component.ngOnInit();

    expect(articleServiceMock.get).toHaveBeenCalledWith('existing-article');
    expect(component.articleForm.value).toEqual({
      title: 'Existing Article',
      description: 'Existing description',
      body: 'Existing body',
    });
    expect(component.tagList()).toEqual(['angular', 'testing']);
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it('should redirect to home when a different user tries to edit the article', () => {
    // TL5: R1 Authentifizierung / Zugriffsschutz im Frontend
    activatedRouteMock.snapshot.params = {
      slug: 'existing-article',
    };

    articleServiceMock.get.mockReturnValue(of(mockArticle));
    userServiceMock.getCurrentUser.mockReturnValue(
      of({
        user: {
          email: 'other@example.com',
          token: 'jwt-token',
          username: 'other-user',
          bio: null,
          image: null,
        },
      }),
    );

    component.ngOnInit();

    expect(routerMock.navigate).toHaveBeenCalledWith(['/']);
  });
});