import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';

import { ListErrorsComponent } from './list-errors.component';
import { Errors } from '../../core/models/errors.model';

describe('ListErrorsComponent', () => {
  let fixture: ComponentFixture<ListErrorsComponent>;
  let component: ListErrorsComponent;

  const setErrors = (errors: Errors | null): void => {
    fixture.componentRef.setInput('errors', errors);
    fixture.detectChanges();
  };

  const getRenderedErrors = (): string[] =>
    Array.from(fixture.nativeElement.querySelectorAll('li')).map(
      (element: Element) => element.textContent?.trim() ?? '',
    );

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListErrorsComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ListErrorsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture?.destroy();
    TestBed.resetTestingModule();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should expose an empty error list when errors is null', () => {
    setErrors(null);

    expect(component.errorList).toEqual([]);
    expect(getRenderedErrors()).toEqual([]);
  });

  it('should map multiple server validation errors to readable strings', () => {
    setErrors({
      errors: {
        email: 'is invalid',
        password: 'is too short',
      },
    });

    expect(component.errorList).toEqual(['email is invalid', 'password is too short']);
    expect(getRenderedErrors()).toEqual(['email is invalid', 'password is too short']);
  });

  it('should render no list items when the errors object is empty', () => {
    setErrors({
      errors: {},
    });

    expect(component.errorList).toEqual([]);
    expect(getRenderedErrors()).toEqual([]);
  });

  it('should clear previously rendered errors when errors changes back to null', () => {
    setErrors({
      errors: {
        email: 'is invalid',
      },
    });

    expect(component.errorList).toEqual(['email is invalid']);
    expect(getRenderedErrors()).toEqual(['email is invalid']);

    setErrors(null);

    expect(component.errorList).toEqual([]);
    expect(getRenderedErrors()).toEqual([]);
  });
});