// src/app/features/article/pages/home/home.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

import HomeComponent from './home.component';
import { TagsService } from '../../services/tags.service';
import { UserService } from '../../../../core/auth/services/user.service';

describe('HomeComponent', () => {
  let fixture: ComponentFixture<HomeComponent>;
  let component: HomeComponent;

  let paramsSubject: BehaviorSubject<Record<string, string>>;
  let queryParamsSubject: BehaviorSubject<Record<string, string>>;
  let isAuthenticatedSubject: BehaviorSubject<boolean>;

  let routerMock: {
    navigate: ReturnType<typeof vi.fn>;
  };

  let activatedRouteMock: {
    params: ReturnType<BehaviorSubject<Record<string, string>>['asObservable']>;
    queryParams: ReturnType<BehaviorSubject<Record<string, string>>['asObservable']>;
    snapshot: {
      queryParams: Record<string, string>;
    };
  };

  beforeEach(async () => {
    paramsSubject = new BehaviorSubject<Record<string, string>>({});
    queryParamsSubject = new BehaviorSubject<Record<string, string>>({});
    isAuthenticatedSubject = new BehaviorSubject<boolean>(false);

    routerMock = {
      navigate: vi.fn().mockResolvedValue(true),
    };

    activatedRouteMock = {
      params: paramsSubject.asObservable(),
      queryParams: queryParamsSubject.asObservable(),
      snapshot: {
        queryParams: {},
      },
    };

    await TestBed.configureTestingModule({
      imports: [HomeComponent],
      providers: [
        {
          provide: Router,
          useValue: routerMock,
        },
        {
          provide: ActivatedRoute,
          useValue: activatedRouteMock,
        },
        {
          provide: UserService,
          useValue: {
            isAuthenticated: isAuthenticatedSubject.asObservable(),
          },
        },
        {
          provide: TagsService,
          useValue: {
            getAll: vi.fn(() => of(['angular', 'testing', 'realworld'])),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
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

  it('should redirect unauthenticated users from the following feed to login', () => {
    // TL5 risk reference:
    // R1 authentication / R5 protected user flow
    isAuthenticatedSubject.next(false);
    queryParamsSubject.next({ feed: 'following' });

    component.ngOnInit();

    expect(routerMock.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should configure following feed when the user is authenticated', () => {
    // TL5 risk reference:
    // R1 authentication / R5 protected user flow
    isAuthenticatedSubject.next(true);
    queryParamsSubject.next({ feed: 'following' });

    component.ngOnInit();

    expect(routerMock.navigate).not.toHaveBeenCalled();
    expect(component.isAuthenticated()).toBe(true);
    expect(component.listConfig()).toEqual({
      type: 'feed',
      filters: {},
    });
    expect(component.isFollowingFeed()).toBe(true);
  });

  it('should configure global feed by default for unauthenticated users', () => {
    // TL5 risk reference:
    // R6 article list configuration
    isAuthenticatedSubject.next(false);
    paramsSubject.next({});
    queryParamsSubject.next({});

    component.ngOnInit();

    expect(component.currentPage()).toBe(1);
    expect(component.listConfig()).toEqual({
      type: 'all',
      filters: {},
    });
    expect(component.isFollowingFeed()).toBe(false);
  });

  it('should configure a tag filter from route params', () => {
    // TL5 risk reference:
    // R6 article list and filter handling
    isAuthenticatedSubject.next(false);
    paramsSubject.next({ tag: 'angular' });
    queryParamsSubject.next({ page: '2' });

    component.ngOnInit();

    expect(component.currentPage()).toBe(2);
    expect(component.listConfig()).toEqual({
      type: 'all',
      filters: {
        tag: 'angular',
      },
    });
    expect(component.isFollowingFeed()).toBe(false);
  });

  it('should preserve following feed when changing to another page', () => {
    // TL5 risk reference:
    // R6 pagination / R5 user flow continuity
    activatedRouteMock.snapshot.queryParams = {
      feed: 'following',
    };

    component.onPageChange(3);

    expect(routerMock.navigate).toHaveBeenCalledWith([], {
      relativeTo: activatedRouteMock,
      queryParams: {
        feed: 'following',
        page: 3,
      },
    });
  });

  it('should omit page query parameter when changing back to page one', () => {
    // TL5 risk reference:
    // R6 pagination URL handling
    activatedRouteMock.snapshot.queryParams = {
      feed: 'following',
    };

    component.onPageChange(1);

    expect(routerMock.navigate).toHaveBeenCalledWith([], {
      relativeTo: activatedRouteMock,
      queryParams: {
        feed: 'following',
      },
    });
  });
});