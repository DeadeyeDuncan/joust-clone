from joust import config
from joust.entities.kinematics import step_airborne


def _sign(value):
    return (value > 0) - (value < 0)


class Enemy:
    def __init__(self, tier, x, y, rng):
        if tier not in (1, 2, 3):
            raise ValueError("tier must be 1, 2, or 3")

        self.tier = tier
        self.points = config.SCORE_TIERS[tier - 1]
        self.x = float(x)
        self.y = float(y)
        self.vx = 0.0
        self.vy = 0.0
        self.facing = 1
        self.alive = True
        self.mounted = True
        self.wraps = True
        self.lance_offset_y = -20
        self.state = "shimmer"

        self._rng = rng
        self._dir = 0
        self._age = 0.0
        self._invulnerable = True
        self._decision_in = 0.0

    @property
    def lance_y(self):
        return self.y + self.lance_offset_y

    @property
    def invulnerable(self):
        return self._invulnerable

    def rect(self):
        return (
            self.x - config.MOUNT_W / 2,
            self.y - config.MOUNT_H,
            config.MOUNT_W,
            config.MOUNT_H,
        )

    def update(self, dt, world, players):
        if not self.alive:
            return

        self._age += dt
        if self._age >= config.SPAWN_INVULN_S:
            self._invulnerable = False

        self._decision_in -= dt
        if self._decision_in <= 0.0:
            self._decide(players)
            self._decision_in = config.AI_FLAP_COOLDOWN_S

        kwargs = {}
        if self.tier == 3:
            kwargs = {
                "thrust_ax": config.THRUST_AX * config.LORD_SPEED_MULT,
                "max_vx": config.MAX_VX * config.LORD_SPEED_MULT,
            }
        (self.x, self.y), (self.vx, self.vy) = step_airborne(
            (self.x, self.y), (self.vx, self.vy), self._dir, dt, **kwargs
        )
        self.x = world.wrap_x(self.x)
        if self.y < 0:
            self.y = 0.0
            self.vy = max(0.0, self.vy)

        if self._invulnerable:
            self.state = "shimmer"

    def _decide(self, players):
        if self.y > config.AI_LAVA_AVOID_Y:
            self._flap()
            return

        if self.tier == 1:
            self._bounder_decision()
            return

        target = self._nearest_living_player(players)
        if target is None:
            self._dir = 0
            return

        dx = (
            (target.x - self.x + config.LOGICAL_W / 2) % config.LOGICAL_W
        ) - config.LOGICAL_W / 2
        self._dir = _sign(dx)
        if self._dir * self.vx < 0:
            self.vx = -self.vx
        if self._dir:
            self.facing = self._dir

        if self.tier == 2:
            desired_y = target.y + self._rng.uniform(-16.0, 16.0)
            if self.y > desired_y:
                self._flap()
            return

        horizontal_distance = abs(dx)
        horizon = config.AI_FLAP_COOLDOWN_S
        projected_y = (
            self.y
            + self.vy * horizon
            + 0.5 * config.GRAVITY * horizon * horizon
        )
        if horizontal_distance >= config.LORD_DIVE_RANGE:
            cruise_y = target.y - config.LORD_CRUISE_ABOVE
            if projected_y > cruise_y:
                self._flap()
        else:
            if projected_y > target.y - 16.0:
                self._flap()
            else:
                self.state = "dive"

    def _bounder_decision(self):
        self._dir = self._rng.choice((-1, 0, 1))
        if self._dir:
            self.facing = self._dir
        if (
            self.y > config.BOUNDER_RECOVER_Y
            or self._rng.random() < config.BOUNDER_FLAP_CHANCE
        ):
            self._flap()

    def _flap(self):
        self.vy = config.FLAP_VY
        self._invulnerable = False
        self.state = "flap"

    def _nearest_living_player(self, players):
        living = [player for player in players if player.alive]
        if not living:
            return None
        return min(
            living,
            key=lambda player: (
                ((player.x - self.x + config.LOGICAL_W / 2) % config.LOGICAL_W)
                - config.LOGICAL_W / 2
            ) ** 2 + (player.y - self.y) ** 2,
        )
