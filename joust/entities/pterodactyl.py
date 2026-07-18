import math

from joust import config


_WAVE_SPAWN_Y_OFFSETS = (-48.0, 0.0, 48.0)
_WAVE_SPAWN_PHASES = (0.0, 1.0 / 3.0, 2.0 / 3.0)


class Ptero:
    def __init__(self, side, y=None, phase=0.0):
        if side not in (-1, 1):
            raise ValueError("side must be -1 or 1")

        self.side = side
        self.x = (
            -config.PTERO_W / 2
            if side == -1
            else config.LOGICAL_W + config.PTERO_W / 2
        )
        self.y = config.LOGICAL_H / 2 if y is None else float(y)
        self.facing = -side
        self.alive = True
        self.mouth_open = False
        self._phase = float(phase) % 1.0

    def update(self, dt, world, target):
        if not self.alive:
            return

        if target is not None and target.alive:
            dx = target.x - self.x
            if dx:
                self.facing = 1 if dx > 0.0 else -1
            step = config.PTERO_SPEED * dt
            self.x += max(-step, min(step, dx))
            self._phase = (
                self._phase + dt / config.PTERO_BOB_PERIOD_S
            ) % 1.0
            self.y = target.y + config.PTERO_BOB_AMP * math.sin(
                2.0 * math.pi * self._phase
            )
            self.mouth_open = abs(target.x - self.x) < config.PTERO_MOUTH_RANGE
        else:
            self.x += self.facing * config.PTERO_SPEED * dt
            self.mouth_open = False

        if self.x < -config.PTERO_W or self.x > config.LOGICAL_W + config.PTERO_W:
            self.alive = False

    def rect(self):
        return (
            self.x - config.PTERO_W / 2,
            self.y - config.PTERO_H,
            config.PTERO_W,
            config.PTERO_H,
        )

    def mouth_rect(self):
        mouth_x = self.x + self.facing * config.PTERO_W / 2
        return (
            mouth_x - config.PTERO_MOUTH_W / 2,
            self.y - config.PTERO_H * 0.65 - config.PTERO_MOUTH_H / 2,
            config.PTERO_MOUTH_W,
            config.PTERO_MOUTH_H,
        )

    def check_lance(self, lance_x, lance_y):
        if not self.alive or not self.mouth_open:
            return None

        x, y, width, height = self.mouth_rect()
        if x <= lance_x <= x + width and y <= lance_y <= y + height:
            self.alive = False
            return "kill"
        return None


class PteroDirector:
    def __init__(self, wave_is_ptero):
        self.wave_is_ptero = wave_is_ptero
        self._started = False
        self._active = []
        self._respawn_elapsed = 0.0
        self._next_side = -1

    def update(self, dt, wave_active_seconds, enemies_left):
        if self.wave_is_ptero:
            if self._started:
                return []
            self._started = True
            spawned = [
                Ptero(
                    side,
                    config.LOGICAL_H / 2 + _WAVE_SPAWN_Y_OFFSETS[index],
                    _WAVE_SPAWN_PHASES[index],
                )
                for index, side in enumerate((-1, 1, -1))
            ]
            self._active.extend(spawned)
            return spawned

        living = [ptero for ptero in self._active if ptero.alive]
        self._active = living

        if not self._started:
            if (
                enemies_left > 0
                and wave_active_seconds >= config.PTERO_FIRST_S
            ):
                self._started = True
                return self._spawn_one()
            return []

        if living or enemies_left <= 0:
            self._respawn_elapsed = 0.0
            return []

        self._respawn_elapsed += dt
        if self._respawn_elapsed >= config.PTERO_RESPAWN_S:
            self._respawn_elapsed = 0.0
            return self._spawn_one()
        return []

    def _spawn_one(self):
        ptero = Ptero(self._next_side)
        self._next_side *= -1
        self._active.append(ptero)
        return [ptero]
