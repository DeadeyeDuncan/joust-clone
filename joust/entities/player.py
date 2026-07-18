from joust import config
from joust.entities.kinematics import step_airborne


def _sign(value):
    return (value > 0) - (value < 0)


def _toward_zero(value, amount):
    if value > 0:
        return max(0.0, value - amount)
    if value < 0:
        return min(0.0, value + amount)
    return 0.0


class Player:
    def __init__(self, pid, x, y):
        self.pid = pid
        self.x = float(x)
        self.y = float(y)
        self.vx = 0.0
        self.vy = 0.0
        self.grounded = False
        self.facing = 1
        self.alive = True
        self.mounted = True
        self.wraps = True
        self.lance_offset_y = -20
        self.state = "shimmer"

        self._dir = 0
        self._age = 0.0
        self._invulnerable = True
        self._platform = None

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

    def flap(self):
        self.vy = config.FLAP_VY
        self.grounded = False
        self._platform = None
        self._invulnerable = False
        self.state = "flap"

    def set_dir(self, direction):
        if direction not in (-1, 0, 1):
            raise ValueError("direction must be -1, 0, or 1")
        self._dir = direction
        if direction:
            self.facing = direction
            self._invulnerable = False

    def update(self, dt, world):
        self._age += dt
        if self._age >= config.SPAWN_INVULN_S:
            self._invulnerable = False

        if self.grounded:
            self._update_grounded(dt, world)
        else:
            self._update_airborne(dt, world)

        if self._invulnerable:
            self.state = "shimmer"

    def _update_airborne(self, dt, world):
        previous_y = self.y
        (self.x, self.y), (self.vx, self.vy) = step_airborne(
            (self.x, self.y), (self.vx, self.vy), self._dir, dt
        )
        self.x = world.wrap_x(self.x)

        if self.y < 0:
            self.y = 0.0
            self.vy = max(0.0, self.vy)

        platform = world.ground_under(self.x, previous_y, self.y)
        if platform is not None:
            self.y = float(platform.y)
            self.vy = 0.0
            self.grounded = True
            self._platform = platform

        if not self.grounded:
            self.state = "flap"

    def _update_grounded(self, dt, world):
        platform = self._platform or self._supporting_platform(world)
        if platform is None or not platform.alive:
            self.grounded = False
            self._platform = None
            self._update_airborne(dt, world)
            return

        self._platform = platform
        reversing = self._dir and self.vx and _sign(self._dir) != _sign(self.vx)
        if reversing:
            self.vx = _toward_zero(self.vx, config.GROUND_SKID_DECEL * dt)
            self.state = "brake"
        elif self._dir:
            self.vx += self._dir * config.THRUST_AX * dt
            self.vx = max(-config.MAX_VX, min(config.MAX_VX, self.vx))
            self.state = "run"
        else:
            self.vx = _toward_zero(self.vx, config.GROUND_SKID_DECEL * dt)
            self.state = "run" if self.vx else "stand"

        self.x = world.wrap_x(self.x + self.vx * dt)
        if not (platform.x <= self.x <= platform.x + platform.w):
            self.grounded = False
            self._platform = None

    def _supporting_platform(self, world):
        for platform in world.platforms:
            if (
                platform.alive
                and platform.x <= self.x <= platform.x + platform.w
                and abs(platform.y - self.y) < 1e-6
            ):
                return platform
        return None
