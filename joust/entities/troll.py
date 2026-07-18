from joust import config


class Troll:
    def __init__(self):
        self.victim = None
        self.consumed = None
        self.state = "idle"
        self.escape_v = 0.0
        self._cooldown = 0.0

    def fight(self):
        self.escape_v += config.TROLL_ESCAPE_VY

    def update(self, dt, world, mounts):
        if self.victim is not None:
            self._update_victim(dt, world)
            return

        if self._cooldown > 0.0:
            self._cooldown = max(0.0, self._cooldown - dt)
            self.state = "rise"
            return

        candidates = [
            mount
            for mount in mounts
            if mount.alive and world.in_grab_band(mount.y)
        ]
        if candidates:
            self.victim = max(candidates, key=lambda mount: mount.y)
            self.state = "grab"
        else:
            self.state = "idle"

    def _update_victim(self, dt, world):
        if not self.victim.alive:
            self._unlatch()
            return

        if self.victim.y >= world.LAVA_Y:
            self.consumed = self.victim
            self._unlatch()
            return

        decay = config.TROLL_ESCAPE_DECAY * dt
        if self.escape_v < 0.0:
            self.escape_v = min(0.0, self.escape_v + decay)
        elif self.escape_v > 0.0:
            self.escape_v = max(0.0, self.escape_v - decay)

        pull = config.TROLL_DRAG_VY + self.escape_v
        self.victim.vy = pull
        if pull <= config.TROLL_RELEASE_VY:
            self._unlatch()
            self._cooldown = config.TROLL_COOLDOWN_S
            self.state = "rise"
            return

        self.state = "drag"

    def _unlatch(self):
        self.victim = None
        self.escape_v = 0.0
        self.state = "idle"
