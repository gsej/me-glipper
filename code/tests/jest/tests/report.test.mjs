"use strict";

describe('report', () => {
  const baseUrl = 'http://localhost:5200/api';
  const validApiKey = 'apikey';

  const sampleData = {
    users: [
      { userId: "ApiUser", heightInCm: 150 }
    ],
    weightRecords: [
      { weightId: "955c82e8-124a-427b-9160-358db7e51e41", date: "2025-04-10T00:00:00Z", weight: 71.0, userId: "ApiUser", deleted: false },
      { weightId: "5bf0a60a-58d9-4136-8b4c-85a82e34fb02", date: "2025-04-11T00:00:00Z", weight: 70.5, userId: "ApiUser", deleted: false },
      { weightId: "4f7c8a9d-e5b6-42f3-a1d9-7c6b8e2f5a0c", date: "2025-04-11T00:00:00Z", weight: 71.5, userId: "ApiUser", deleted: false }
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


  it('should return correct report with each date in the input data represented once with a valid API key', async () => {

    const reportResponse = await fetch(`${baseUrl}/report`, {
      headers: {
        'X-API-Key': validApiKey
      }
    });
    expect(reportResponse.status).toBe(200);

     const reportData = await reportResponse.json();
    
     expect(reportData.entries.length).toBe(2);

    const april10Record = reportData.entries.find(r => r.date.startsWith('2025-04-10'));
    expect(april10Record).toBeTruthy();
    expect(april10Record.recordedWeight).toBe(71.0);
    expect(april10Record.averageWeight).toBe(71.0);

    const april11Record = reportData.entries.find(r => r.date.startsWith('2025-04-11'));
    expect(april11Record).toBeTruthy();
    expect(april11Record.recordedWeight).toBe(71.0); // Mean of 70.5 and 71.5
    expect(april11Record.averageWeight).toBe(71.0); // Mean of 70.5 and 71.5
  });

  it('should return 401 when calling backup endpoint without API key', async () => {
    const response = await fetch(`${baseUrl}/report`);
    expect(response.status).toBe(401);
  });
});
