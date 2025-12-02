# Banking System Monitoring Stack

This directory contains the monitoring configuration for the Banking System.

## Components

- **Prometheus**: Metrics collection and alerting
- **Grafana**: Metrics visualization and dashboards
- **Alertmanager**: Alert routing and notification

## Directory Structure

```
monitoring/
??? prometheus.yml              # Prometheus main config
??? alertmanager.yml            # Alertmanager config
??? alerts/
?   ??? banking-system-rules.yml  # Alert rules
??? grafana/
    ??? provisioning/
    ?   ??? datasources/
    ?   ?   ??? prometheus.yaml   # Grafana datasource
    ?   ??? dashboards/
    ?       ??? default.yaml      # Dashboard provisioning
    ??? dashboards/
        ??? banking-system-overview.json  # Main dashboard
```

## Quick Start

### Start Monitoring Stack

```bash
# Start all services including monitoring
docker-compose up -d

# Check services
docker-compose ps
```

### Access Monitoring Tools

- **Grafana**: http://localhost:3000
  - Username: `admin`
  - Password: `admin`
  - Pre-configured dashboard: "Banking System Overview"

- **Prometheus**: http://localhost:9090
  - Query metrics directly
  - View targets: http://localhost:9090/targets
  - View alerts: http://localhost:9090/alerts

- **Alertmanager**: http://localhost:9093
  - View active alerts
  - Silence alerts

- **API Metrics**: http://localhost:5000/metrics
  - Raw Prometheus metrics from the API

## Metrics Exposed

### Business Metrics

- `banking_users_active` - Number of active users (logged in last 24h)
- `banking_accounts_total` - Total number of active accounts
- `banking_transactions_count` - Transaction counter by type and success
- `banking_login_attempts` - Login attempt counter
- `banking_login_failed` - Failed login counter
- `banking_cards_issued` - Card issuance counter

### System Metrics (OpenTelemetry)

- `http_server_requests_total` - HTTP request count
- `http_server_request_duration_seconds` - Request duration histogram
- `process_working_set_bytes` - Memory usage
- `process_cpu_seconds_total` - CPU usage
- `dotnet_*` - .NET runtime metrics

## Alert Rules

The following alerts are configured:

| Alert | Severity | Condition | Description |
|-------|----------|-----------|-------------|
| HighErrorRate | Critical | 5xx error rate > 5% for 5min | Server errors detected |
| SlowAPIResponses | Warning | p95 latency > 500ms for 5min | API is slow |
| HighFailedLoginRate | Warning | Failed logins > 0.1/sec for 5min | Possible brute force |
| DatabaseDown | Critical | Database unreachable for 1min | Database connection lost |
| HighMemoryUsage | Warning | Memory > 1GB for 5min | High memory consumption |
| APIDown | Critical | API unreachable for 1min | API service down |

## Grafana Dashboards

### Banking System Overview

Pre-configured panels:
- Request rate by endpoint
- Response latency (p50, p95)
- Active users (gauge)
- Total accounts (gauge)
- Login activity (success/failed)
- Transaction rate by type

### Customize Dashboards

1. Log in to Grafana: http://localhost:3000
2. Navigate to dashboard
3. Click "Edit" on any panel
4. Modify queries, visualization, or add new panels
5. Save dashboard

## Alert Configuration

### Email Alerts (Optional)

Edit `monitoring/alertmanager.yml`:

```yaml
receivers:
  - name: 'critical'
    email_configs:
      - to: 'ops-team@bankingsystem.com'
        from: 'alerts@bankingsystem.com'
        smarthost: 'smtp.gmail.com:587'
        auth_username: 'alerts@bankingsystem.com'
        auth_password: 'your-app-password'
```

### Slack Alerts (Optional)

```yaml
receivers:
  - name: 'critical'
    slack_configs:
      - api_url: 'https://hooks.slack.com/services/YOUR/WEBHOOK/URL'
        channel: '#alerts'
        title: 'Banking System Alert'
```

### Webhook Alerts

Alerts are sent to the API webhook endpoint:
```
POST http://banking-api:80/api/webhooks/alerts
```

Implement the webhook controller to handle alerts (send SMS, create tickets, etc.)

## Troubleshooting

### Prometheus Not Scraping Metrics

```bash
# Check Prometheus targets
curl http://localhost:9090/api/v1/targets

# Check API metrics endpoint
curl http://localhost:5000/metrics

# View Prometheus logs
docker-compose logs prometheus
```

### Grafana Dashboard Not Loading

```bash
# Check Grafana logs
docker-compose logs grafana

# Verify datasource
curl http://localhost:3000/api/datasources

# Restart Grafana
docker-compose restart grafana
```

### Alerts Not Firing

```bash
# Check alert rules
curl http://localhost:9090/api/v1/rules

# Check Alertmanager
curl http://localhost:9093/api/v2/alerts

# View Alertmanager logs
docker-compose logs alertmanager
```

## Production Considerations

### Security

1. **Change default passwords**:
   - Grafana admin password
   - Add authentication to Prometheus/Alertmanager

2. **Enable HTTPS**:
   - Use reverse proxy (Nginx/Traefik)
   - Add TLS certificates

3. **Network isolation**:
   - Use private networks
   - Expose only necessary ports

### Storage

1. **Prometheus retention**:
   ```yaml
   command:
     - '--storage.tsdb.retention.time=30d'
     - '--storage.tsdb.retention.size=10GB'
   ```

2. **Use persistent volumes** for production data

### High Availability

1. **Prometheus**:
   - Run multiple Prometheus instances
   - Use Thanos for long-term storage

2. **Grafana**:
   - Use external database (PostgreSQL)
   - Run multiple Grafana instances

3. **Alertmanager**:
   - Cluster Alertmanager instances
   - Use gossip protocol for HA

## Next Steps

- [ ] Add more custom dashboards
- [ ] Configure email/SMS alerts
- [ ] Set up long-term metrics storage (Thanos/Cortex)
- [ ] Add application-specific SLIs/SLOs
- [ ] Integrate with incident management (PagerDuty/Opsgenie)
- [ ] Add cost monitoring dashboards
- [ ] Configure backup for Grafana dashboards
