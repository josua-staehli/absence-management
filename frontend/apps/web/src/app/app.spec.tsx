import { render } from '@testing-library/react';

import App from './app';

describe('app', () => {
  it('renders successfully', () => {
    const { baseElement } = render(<App />);

    expect(baseElement).toBeDefined();
  });
});
