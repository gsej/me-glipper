"use strict";

describe('healthz', () => {
  it('returns 200', async () => {
    const response = await fetch('http://localhost:5200/api/healthz');
    expect(response.status).toBe(200);
  })
});