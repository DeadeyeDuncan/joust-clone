from dataclasses import dataclass

from joust import config


@dataclass
class Platform:
    x: float
    y: float
    w: float
    id: str
    alive: bool = True


class World:
    LAVA_Y = 344

    _LAYOUT = (
        (240, 64, 160),
        (24, 120, 96),
        (520, 120, 96),
        (0, 208, 72),
        (568, 208, 72),
        (200, 160, 64),
        (376, 160, 64),
        (96, 300, 120),
        (216, 300, 208),
        (424, 300, 120),
    )

    def __init__(self, wave=1):
        self.platforms = [
            Platform(x, y, width, f"p{index}")
            for index, (x, y, width) in enumerate(self._LAYOUT)
        ]
        self.spawn_pads = [
            (platform.x + platform.w / 2, platform.y)
            for platform in (self.platforms[index] for index in (0, 1, 2, 7))
        ]
        self._max_erosion_wave = 1
        self.apply_erosion(wave)

    def apply_erosion(self, wave):
        self._max_erosion_wave = max(self._max_erosion_wave, wave)
        doomed = set()
        if self._max_erosion_wave >= 4:
            doomed.add("p8")
        if self._max_erosion_wave >= 6:
            doomed.update(("p3", "p4"))
        if self._max_erosion_wave >= 9:
            doomed.add("p5")
        for platform in self.platforms:
            if platform.id in doomed:
                platform.alive = False

    def wrap_x(self, x):
        return x % config.LOGICAL_W

    def ground_under(self, x, prev_y, y):
        if y <= prev_y:
            return None
        for platform in self.platforms:
            if (
                platform.alive
                and prev_y <= platform.y <= y
                and platform.x <= x <= platform.x + platform.w
            ):
                return platform
        return None

    def in_grab_band(self, y):
        return self.LAVA_Y - config.TROLL_GRAB_BAND <= y <= self.LAVA_Y
