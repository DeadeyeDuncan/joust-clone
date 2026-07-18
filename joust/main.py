"""Pygame initialization and fixed-timestep application loop."""

import pygame

from joust import assets, audio, config
from joust.states import AttractState, StateMachine


class GameLoop:
    MAX_STEPS_PER_FRAME = 5

    def __init__(self, update, render, headless=False, toggle_fullscreen=None):
        self._update = update
        self._render = render
        self._acc = 0.0
        self._toggle_fullscreen = toggle_fullscreen
        self.headless = headless
        self.running = True

    def pump(self, frame_seconds):
        self._acc = min(self._acc + frame_seconds, config.DT * self.MAX_STEPS_PER_FRAME)
        while self._acc >= config.DT:
            self._update(config.DT)
            self._acc -= config.DT
        self._render(self._acc / config.DT)

    def toggle_fullscreen(self):
        if self._toggle_fullscreen is not None:
            self._toggle_fullscreen()

    def run(self):
        clock = pygame.time.Clock()
        while self.running:
            self.pump(clock.tick(config.FPS) / 1000.0)


def main():
    pygame.init()
    game_assets = assets.load()
    audio.init()
    logical = pygame.Surface((config.LOGICAL_W, config.LOGICAL_H))
    machine = StateMachine()
    fullscreen = False
    window = pygame.display.set_mode(
        (config.LOGICAL_W * config.SCALE_DEFAULT, config.LOGICAL_H * config.SCALE_DEFAULT)
    )

    def toggle_fullscreen():
        nonlocal fullscreen, window
        fullscreen = not fullscreen
        if fullscreen:
            window = pygame.display.set_mode((0, 0), pygame.FULLSCREEN)
        else:
            window = pygame.display.set_mode(
                (config.LOGICAL_W * config.SCALE_DEFAULT, config.LOGICAL_H * config.SCALE_DEFAULT)
            )

    def update(dt):
        for event in pygame.event.get():
            machine.current.handle_event(event)
        machine.current.update(dt)

    def render(alpha):
        machine.current.draw(logical)
        width, height = window.get_size()
        scale = max(1, min(width // config.LOGICAL_W, height // config.LOGICAL_H))
        scaled = pygame.transform.scale(
            logical, (config.LOGICAL_W * scale, config.LOGICAL_H * scale)
        )
        window.fill((0, 0, 0))
        window.blit(scaled, ((width - scaled.get_width()) // 2, (height - scaled.get_height()) // 2))
        pygame.display.flip()

    loop = GameLoop(update=update, render=render, toggle_fullscreen=toggle_fullscreen)
    machine.push(AttractState(loop, machine, game_assets))
    try:
        loop.run()
    finally:
        pygame.quit()
