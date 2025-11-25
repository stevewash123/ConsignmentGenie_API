import { TestBed } from '@angular/core/testing';
import { LoadingService } from './loading.service';

describe('LoadingService', () => {
  let service: LoadingService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LoadingService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should start loading', () => {
    service.start('test');
    expect(service.isLoading('test')).toBeTrue();
  });

  it('should stop loading', () => {
    service.start('test');
    service.stop('test');
    expect(service.isLoading('test')).toBeFalse();
  });

  it('should track multiple keys independently', () => {
    service.start('a');
    service.start('b');

    expect(service.isLoading('a')).toBeTrue();
    expect(service.isLoading('b')).toBeTrue();

    service.stop('a');

    expect(service.isLoading('a')).toBeFalse();
    expect(service.isLoading('b')).toBeTrue();
  });

  it('should report anyLoading correctly', () => {
    expect(service.anyLoading).toBeFalse();

    service.start('test');
    expect(service.anyLoading).toBeTrue();

    service.stop('test');
    expect(service.anyLoading).toBeFalse();
  });

  it('should clear all loading states', () => {
    service.start('a');
    service.start('b');
    service.clear();

    expect(service.anyLoading).toBeFalse();
  });

  it('should provide observable for specific key loading state', (done) => {
    service.isLoading$('test').subscribe(isLoading => {
      if (isLoading) {
        expect(isLoading).toBeTrue();
        done();
      }
    });

    service.start('test');
  });

  it('should provide observable for any loading state', (done) => {
    service.anyLoading$.subscribe(anyLoading => {
      if (anyLoading) {
        expect(anyLoading).toBeTrue();
        done();
      }
    });

    service.start('test');
  });

  it('should return active keys for debugging', () => {
    service.start('key1');
    service.start('key2');

    const activeKeys = service.activeKeys;
    expect(activeKeys).toContain('key1');
    expect(activeKeys).toContain('key2');
    expect(activeKeys.length).toBe(2);
  });

  it('should handle stopping non-existent key gracefully', () => {
    service.stop('nonexistent');
    expect(service.anyLoading).toBeFalse();
  });

  it('should handle duplicate start calls', () => {
    service.start('test');
    service.start('test');

    expect(service.isLoading('test')).toBeTrue();
    expect(service.activeKeys.length).toBe(1);
  });

  it('should handle multiple stop calls', () => {
    service.start('test');
    service.stop('test');
    service.stop('test');

    expect(service.isLoading('test')).toBeFalse();
    expect(service.anyLoading).toBeFalse();
  });
});