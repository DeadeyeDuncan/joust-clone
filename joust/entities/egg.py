from joust import config


class Egg:
    def __init__(self, from_tier, x, y, vx, vy):
        self.from_tier = from_tier
        self.next_tier = min(from_tier + 1, 3)
        self.x = float(x)
        self.y = float(y)
        self.vx = float(vx)
        self.vy = float(vy)
        self.state = "falling"
        self.mounted = False
        self.doomed = False

        self._state_age = 0.0

    @property
    def collectible(self):
        return self.state in {"falling", "resting", "hatchling"}

    def rect(self):
        if self.state == "hatchling":
            width, height = config.HATCHLING_W, config.HATCHLING_H
        else:
            width, height = config.EGG_W, config.EGG_H
        return self.x - width / 2, self.y - height, width, height

    def collect(self):
        self.state = "dead"

    def update(self, dt, world):
        if self.state == "dead":
            return None
        if self.state == "falling":
            self._update_falling(dt, world)
        elif self.state == "resting":
            self._state_age += dt
            if self._state_age >= config.EGG_HATCH_S:
                self.state = "hatchling"
                self._state_age = 0.0
        elif self.state == "hatchling":
            self._state_age += dt
            if self._state_age >= config.HATCHLING_WAIT_S:
                self.state = "remounting"
        if self.state == "remounting":
            self.state = "dead"
            return "spawn_enemy", self.next_tier, self.x, self.y
        return None

    def _update_falling(self, dt, world):
        previous_y = self.y
        self.vy += config.GRAVITY * dt
        self.x = world.wrap_x(self.x + self.vx * dt)
        self.y += self.vy * dt

        platform = world.ground_under(self.x, previous_y, self.y)
        if platform is not None:
            self.y = float(platform.y)
            self.vy *= -config.EGG_BOUNCE_DAMP
            if abs(self.vy) < config.EGG_REST_VY:
                self.vy = 0.0
                self.state = "resting"
                self._state_age = 0.0
            return

        if self.y >= world.LAVA_Y:
            self.state = "dead"
            self.doomed = True


class EggChain:
    def __init__(self):
        self.reset()

    def value(self):
        value = config.EGG_CHAIN[self._index]
        self._index = min(self._index + 1, len(config.EGG_CHAIN) - 1)
        return value

    def reset(self):
        self._index = 0
