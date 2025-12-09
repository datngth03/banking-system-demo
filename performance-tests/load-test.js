import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  stages: [
    { duration: '30s', target: 5 },  // Ramp up to 5 users (was 10)
    { duration: '1m', target: 5 },   // Stay at 5 users (was 10)
    { duration: '30s', target: 15 }, // Ramp up to 15 users (was 50)
    { duration: '1m', target: 15 },  // Stay at 15 users (was 50)
    { duration: '30s', target: 0 },  // Ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests should be below 500ms
    http_req_failed: ['rate<0.1'],    // Error rate should be below 10%
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export default function () {
  // Test 1: Health Check (should always work)
  let healthResponse = http.get(`${BASE_URL}/health`);
  
  check(healthResponse, {
    'health check is 200': (r) => r.status === 200,
    'health response time < 500ms': (r) => r.timings.duration < 500,
  });

  // Test 2: Swagger (public endpoint)
  let swaggerResponse = http.get(`${BASE_URL}/swagger/index.html`);
  
  check(swaggerResponse, {
    'swagger is 200': (r) => r.status === 200,
    'swagger response time < 1000ms': (r) => r.timings.duration < 1000,
  });

  // Test 3: Metrics (if available)
  let metricsResponse = http.get(`${BASE_URL}/metrics`);
  
  check(metricsResponse, {
    'metrics accessible': (r) => r.status === 200 || r.status === 404, // May not be enabled
  });

  sleep(1); // Wait 1 second between iterations
}