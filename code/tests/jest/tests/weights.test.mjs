"use strict";

describe('weights', () => {
  const baseUrl = 'http://localhost:5200/api';
  const validApiKey = 'apikey';

  const sampleData = {
    users: [
      { userId: "ApiUser", heightInCm: 150 }
    ],
    weightRecords: [
      { weightId: "955c82e8-124a-427b-9160-358db7e51e41", date: "2025-04-10T00:00:00Z", weight: 71.0, userId: "ApiUser", deleted: false  },
      { weightId: "5bf0a60a-58d9-4136-8b4c-85a82e34fb02", date: "2025-04-11T00:00:00Z", weight: 70.5, userId: "ApiUser", deleted: false  },
      { weightId: "086efd13-99a7-42b1-9394-cc03177d04fc", date: "2025-04-12T00:00:00Z", weight: 99.0, userId: "ApiUser", deleted: true }
    ]
  };

  beforeAll(async () => {

    // put the system in a known state before testing
    const restoreResponse = await fetch(`${baseUrl}/backup/restore`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': validApiKey
      },
      body: JSON.stringify(sampleData)
    });
    expect(restoreResponse.status).toBe(200);
  });

  it('should return all weights correctly with valid API key', async () => {

    const backupResponse = await fetch(`${baseUrl}/weights`, {
      headers: {
        'X-API-Key': validApiKey
      }
    });
    expect(backupResponse.status).toBe(200);

    const backupData = await backupResponse.json();

    const expectedRecords = sampleData.weightRecords.filter(record => !record.deleted);

    expect(backupData.weightRecords).toEqual(expectedRecords);

  }); 

  it('should return 401 when calling backup endpoint without API key', async () => {
     const response = await fetch(`${baseUrl}/weights`);
     expect(response.status).toBe(401);
   });
});
