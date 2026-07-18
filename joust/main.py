import pygame
from joust import config
from joust.states import State, StateMachine


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
    machine.push(AttractState(loop))
    try:
        loop.run()
    finally:
        pygame.quit()
