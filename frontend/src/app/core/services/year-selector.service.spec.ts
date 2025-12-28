import { TestBed } from '@angular/core/testing';
import { YearSelectorService } from './year-selector.service';

describe('YearSelectorService', () => {
  let service: YearSelectorService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [YearSelectorService]
    });
    service = TestBed.inject(YearSelectorService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('selectedYear', () => {
    it('should default to current year', () => {
      const currentYear = new Date().getFullYear();
      expect(service.selectedYear()).toBe(currentYear);
    });
  });

  describe('availableYears', () => {
    it('should return 6 years', () => {
      const years = service.availableYears();
      expect(years.length).toBe(6);
    });

    it('should start with current year', () => {
      const currentYear = new Date().getFullYear();
      const years = service.availableYears();
      expect(years[0]).toBe(currentYear);
    });

    it('should be in descending order', () => {
      const years = service.availableYears();
      for (let i = 1; i < years.length; i++) {
        expect(years[i]).toBe(years[i - 1] - 1);
      }
    });

    it('should include 5 previous years', () => {
      const currentYear = new Date().getFullYear();
      const years = service.availableYears();
      expect(years[5]).toBe(currentYear - 5);
    });
  });

  describe('setYear', () => {
    it('should update selectedYear', () => {
      const testYear = 2022;
      service.setYear(testYear);
      expect(service.selectedYear()).toBe(testYear);
    });
  });

  describe('reset', () => {
    it('should reset to current year', () => {
      const currentYear = new Date().getFullYear();
      service.setYear(2020);
      service.reset();
      expect(service.selectedYear()).toBe(currentYear);
    });
  });
});
