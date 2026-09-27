"use strict";

describe('backup', () => {
  const baseUrl = 'http://localhost:5200/api';
  const validApiKey = 'apikey';

  // Includes the authenticating user (ApiUser) so the snapshot round-trips exactly: the
  // auth filter seeds a row for the caller, and restore replaces it with this one.
  const sampleData = {
    users: [
      { userId: "ApiUser", heightInCm: 150 }
    ],
    weightRecords: [
      { weightId: "955c82e8-124a-427b-9160-358db7e51e41", userId: "ApiUser", date: "2025-04-10T00:00:00Z", weight: 71.0, deleted: false },
      { weightId: "5bf0a60a-58d9-4136-8b4c-85a82e34fb02", userId: "ApiUser", date: "2025-04-11T00:00:00Z", weight: 70.5, deleted: false }
    ]
  };

  it('should restore and back up both users and weights with a valid API key', async () => {

    // Restore data
    const restoreResponse = await fetch(`${baseUrl}/backup/restore`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': validApiKey
      },
      body: JSON.stringify(sampleData)
    });
    expect(restoreResponse.status).toBe(200);

    // Get backup
    const backupResponse = await fetch(`${baseUrl}/backup`, {
      headers: {
        'X-API-Key': validApiKey
      }
    });
    expect(backupResponse.status).toBe(200);

    const backupData = await backupResponse.json();
    expect(backupData).toEqual(sampleData);

  });

  it('should return 401 when calling backup endpoint without API key', async () => {
    const response = await fetch(`${baseUrl}/backup`);
    expect(response.status).toBe(401);
  });

  it('should return 401 when calling restore endpoint without API key', async () => {
    const response = await fetch(`${baseUrl}/backup/restore`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(sampleData)
    });
    expect(response.status).toBe(401);
  });
});
