# GamesManagement local dev — app + RabbitMQ in Rancher Desktop
# Requires: Rancher Desktop with dockerd enabled, Tilt installed
#
# Start:  tilt up
# Stop:   tilt down

docker_compose('docker-compose.yml')

# ── infrastructure ────────────────────────────────────────────────────────────

dc_resource('rabbitmq', labels=['infra'],
    links=[link('http://localhost:15672', 'RabbitMQ Management')])

# ── GamesManagement service ───────────────────────────────────────────────────

dc_resource(
    'games-management',
    labels=['games-management'],
    links=[link('http://localhost:5050/scalar/v1', 'GamesManagement API')],
    trigger_mode=TRIGGER_MODE_MANUAL,
)
