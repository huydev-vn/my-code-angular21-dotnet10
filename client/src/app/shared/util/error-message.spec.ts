import { toErrorMessage } from './error-message';

describe('toErrorMessage', () => {
  it('reads Error.message', () => {
    expect(toErrorMessage(new Error('boom'))).toBe('boom');
  });

  it('falls back for unknown values', () => {
    expect(toErrorMessage({})).toBe('Something went wrong.');
  });
});
