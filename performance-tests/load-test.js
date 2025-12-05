import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  stages: [
    { duration: '30s', target: 10 }, // Ramp up to 10 users over 30 seconds
    { duration: '1m', target: 10 },  // Stay at 10 users for 1 minute
    { duration: '30s', target: 50 }, // Ramp up to 50 users over 30 seconds
    { duration: '1m', target: 50 },  // Stay at 50 users for 1 minute
    { duration: '30s', target: 0 },  // Ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests should be below 500ms
    http_req_failed: ['rate<0.1'],    // Error rate should be below 10%
  },
};

const BASE_URL = 'http://localhost:5000'; // Adjust to your API URL

export default function () {
  // Test login endpoint
  let loginPayload = {
    email: 'test@example.com',
    password: 'TestPassword123!'
  };

  let loginResponse = http.post(`${BASE_URL}/api/auth/login`, JSON.stringify(loginPayload), {
    headers: {
      'Content-Type': 'application/json',
    },
  });

  check(loginResponse, {
    'login status is 200': (r) => r.status === 200,
    'login response time < 500ms': (r) => r.timings.duration < 500,
  });

  if (loginResponse.status === 200) {
    let token = JSON.parse(loginResponse.body).token;

    // Test get user cards endpoint
    let cardsResponse = http.get(`${BASE_URL}/api/cards/my-cards`, {
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    });

    check(cardsResponse, {
      'cards status is 200': (r) => r.status === 200,
      'cards response time < 500ms': (r) => r.timings.duration < 500,
    });

    // Test get accounts endpoint (assuming it exists)
    let accountsResponse = http.get(`${BASE_URL}/api/accounts/my-accounts`, {
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    });

    check(accountsResponse, {
      'accounts status is 200': (r) => r.status === 200,
      'accounts response time < 500ms': (r) => r.timings.duration < 500,
    });
  }

  sleep(1); // Wait 1 second between iterations
}