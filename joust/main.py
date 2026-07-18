import pygame
from joust import config
from joust.entities.player import Player
from joust.states import State, StateMachine
from joust.world import World


class GameLoop:
    MAX_STEPS_PER_FRAME = 5

    def __init__(self, update, render, headless=False):
        self._update, self._render, self._acc = update, render, 0.0
        self.headless = headless

    def pump(self, frame_seconds):
        self._acc = min(self._acc + frame_seconds, config.DT * self.MAX_STEPS_PER_FRAME)
        while self._acc >= config.DT:
            self._update(config.DT)
            self._acc -= config.DT
        self._render(self._acc / config.DT)

    def run(self):
        clock = pygame.time.Clock()
        while getattr(self, "running", True):
            self.pump(clock.tick(config.FPS) / 1000.0)


class AttractState(State):
    def __init__(self, loop):
        self.loop = loop

    def handle_event(self, e):
        if e.type == pygame.QUIT or (e.type == pygame.KEYDOWN and e.key == pygame.K_ESCAPE):
            self.loop.running = False

    def draw(self, surface):
        surface.fill((10, 10, 24))


class DevSceneState(AttractState):
    """Minimal playable scene used while production sprite rendering is absent."""

    def __init__(self, loop):
        super().__init__(loop)
        self.world = World()
        spawn_x, spawn_y = self.world.spawn_pads[0]
        self.player = Player(1, spawn_x, spawn_y)
        self.player.grounded = True
        self._left = False
        self._right = False

    def handle_event(self, event):
        super().handle_event(event)
        if event.type not in (pygame.KEYDOWN, pygame.KEYUP):
            return

        pressed = event.type == pygame.KEYDOWN
        if event.key in (pygame.K_LEFT, pygame.K_a):
            self._left = pressed
        elif event.key in (pygame.K_RIGHT, pygame.K_d):
            self._right = pressed
        elif pressed and event.key in (pygame.K_SPACE, pygame.K_UP, pygame.K_w):
            self.player.flap()

        self.player.set_dir(int(self._right) - int(self._left))

    def update(self, dt):
        self.player.update(dt, self.world)

    def draw(self, surface):
        super().draw(surface)
        pygame.draw.rect(
            surface,
            (120, 34, 24),
            (0, self.world.LAVA_Y, config.LOGICAL_W, config.LOGICAL_H - self.world.LAVA_Y),
        )
        for platform in self.world.platforms:
            if platform.alive:
                pygame.draw.rect(surface, (150, 160, 175), (platform.x, platform.y, platform.w, 8))

        pygame.draw.rect(surface, (232, 190, 70), self.player.rect())
        lance_x = self.player.x + self.player.facing * (config.MOUNT_W / 2 + 12)
        pygame.draw.line(
            surface,
            (235, 235, 220),
            (self.player.x, self.player.lance_y),
            (lance_x, self.player.lance_y),
            2,
        )


def main():
    pygame.init()
    window = pygame.display.set_mode(
        (config.LOGICAL_W * config.SCALE_DEFAULT, config.LOGICAL_H * config.SCALE_DEFAULT)
    )
    logical = pygame.Surface((config.LOGICAL_W, config.LOGICAL_H))
    machine = StateMachine()

    def update(dt):
        for event in pygame.event.get():
            machine.current.handle_event(event)
        machine.current.update(dt)

    def render(alpha):
        machine.current.draw(logical)
        window.blit(pygame.transform.scale_by(logical, config.SCALE_DEFAULT), (0, 0))
        pygame.display.flip()

    loop = GameLoop(update=update, render=render)
    machine.push(DevSceneState(loop))
    try:
        loop.run()
    finally:
        pygame.quit()
