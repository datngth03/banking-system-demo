import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
    stages: [
        { duration: '30s', target: 5 },
        { duration: '1m', target: 10 },
        { duration: '30s', target: 0 },
    ],
    thresholds: {
        http_req_duration: ['p(95)<1000'], // More lenient for auth operations
        http_req_failed: ['rate<0.2'],     // 20% error tolerance (rate limiting expected)
    },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

// Test user credentials (must be seeded via test-workflow.ps1)
const TEST_USERS = [
    { email: 'admin@bankingsystem.com', password: 'Secure!2025$Bank#Mgr' },
    { email: 'user1@test.com', password: 'Verify!2025$Safe#U1' },
    { email: 'user2@test.com', password: 'Check!2025$Valid#U2' },
];

export default function () {
    // Select random user
    const user = TEST_USERS[Math.floor(Math.random() * TEST_USERS.length)];

    // Login
    let loginPayload = JSON.stringify({
        email: user.email,
        password: user.password
    });

    let loginResponse = http.post(`${BASE_URL}/api/auth/login`, loginPayload, {
        headers: {
            'Content-Type': 'application/json',
        },
    });

    let loginSuccess = check(loginResponse, {
        'login status is 200 or 401': (r) => r.status === 200 || r.status === 401,
        'login response time < 1000ms': (r) => r.timings.duration < 1000,
    });

    if (loginResponse.status === 200) {
        try {
            let body = JSON.parse(loginResponse.body);
            let token = body.token || body.accessToken;

            if (token) {
                // Test authenticated endpoints

                // User profile
                let profileResponse = http.get(`${BASE_URL}/api/users/profile`, {
                    headers: {
                        'Authorization': `Bearer ${token}`,
                    },
                });

                check(profileResponse, {
                    'profile status is 200': (r) => r.status === 200,
                    'profile response time < 500ms': (r) => r.timings.duration < 500,
                });

                // Accounts
                let accountsResponse = http.get(`${BASE_URL}/api/accounts`, {
                    headers: {
                        'Authorization': `Bearer ${token}`,
                    },
                });

                check(accountsResponse, {
                    'accounts status is 200 or 404': (r) => r.status === 200 || r.status === 404,
                    'accounts response time < 500ms': (r) => r.timings.duration < 500,
                });

                // Cards
                let cardsResponse = http.get(`${BASE_URL}/api/cards`, {
                    headers: {
                        'Authorization': `Bearer ${token}`,
                    },
                });

                check(cardsResponse, {
                    'cards status is 200 or 404': (r) => r.status === 200 || r.status === 404,
                    'cards response time < 500ms': (r) => r.timings.duration < 500,
                });
            }
        } catch (e) {
            console.error('Failed to parse login response:', e);
        }
    }

    sleep(1);
}
