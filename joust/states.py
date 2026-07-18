"""Game states and the integration layer for all gameplay systems."""

from __future__ import annotations

import random
from dataclasses import dataclass

import pygame

from joust import audio, combat, config, hud, persistence
from joust.entities.egg import Egg, EggChain
from joust.entities.enemy import Enemy
from joust.entities.player import Player
from joust.entities.pterodactyl import PteroDirector
from joust.entities.troll import Troll
from joust.waves import WaveDirector
from joust.world import World


class State:
    def handle_event(self, event):
        pass

    def update(self, dt):
        pass

    def draw(self, surface):
        pass


class StateMachine:
    def __init__(self):
        self._stack = []

    @property
    def current(self):
        return self._stack[-1] if self._stack else None

    def push(self, state):
        self._stack.append(state)

    def pop(self):
        return self._stack.pop() if self._stack else None

    def switch(self, state):
        if self._stack:
            self._stack.pop()
        self._stack.append(state)


class AttractState(State):
    def __init__(self, loop, machine, assets):
        self.loop = loop
        self.machine = machine
        self.assets = assets
        self.scores = persistence.load_scores()
        self.joysticks = []
        if not pygame.joystick.get_init():
            pygame.joystick.init()
        for index in range(min(2, pygame.joystick.get_count())):
            joystick = pygame.joystick.Joystick(index)
            joystick.init()
            self.joysticks.append(joystick)

    def _start(self, two_player):
        self.machine.switch(
            PlayState(
                self.loop,
                self.machine,
                self.assets,
                two_player=two_player,
                joysticks=self.joysticks,
            )
        )

    def handle_event(self, event):
        if event.type == pygame.QUIT:
            self.loop.running = False
        elif event.type == pygame.KEYDOWN:
            if event.key == pygame.K_ESCAPE:
                self.loop.running = False
            elif event.key == pygame.K_1:
                self._start(False)
            elif event.key == pygame.K_2:
                self._start(True)
            elif event.key == pygame.K_F11:
                self.loop.toggle_fullscreen()
        elif (
            event.type == pygame.JOYBUTTONDOWN
            and event.button in (6, 7)
            and event.joy < len(self.joysticks)
        ):
            self._start(event.joy == 1)

    def draw(self, surface):
        surface.fill((10, 10, 24))
        _blit(self.assets, surface, "logo", config.LOGICAL_W / 2, 84)
        hud.draw_text(surface, self.assets, "1 ONE PLAYER", config.LOGICAL_W // 2, 132, centered=True)
        hud.draw_text(surface, self.assets, "2 TWO PLAYERS", config.LOGICAL_W // 2, 150, centered=True)
        hud.draw_text(surface, self.assets, "HIGH SCORES", config.LOGICAL_W // 2, 194, centered=True)
        for index, (initials, score) in enumerate(self.scores[:10]):
            hud.draw_text(
                surface,
                self.assets,
                f"{index + 1:02d} {initials:3s} {score:06d}",
                config.LOGICAL_W // 2,
                212 + index * 12,
                centered=True,
            )


@dataclass
class RiderlessBuzzard:
    x: float
    y: float
    vx: float
    vy: float
    facing: int
    alive: bool = True
    wraps: bool = False

    def update(self, dt):
        self.x += self.vx * dt
        self.y += self.vy * dt
        self.vy -= 35.0 * dt
        if self.x < -64 or self.x > config.LOGICAL_W + 64 or self.y < -64:
            self.alive = False


class PlayState(State):
    def __init__(self, loop=None, machine=None, assets=None, two_player=False, joysticks=None):
        self.loop = loop
        self.machine = machine
        self.assets = assets
        self.joysticks = list(joysticks or ())
        self.setup_logic(two_player)

    def setup_logic(self, two_player):
        self.two_player = bool(two_player)
        self.loop = getattr(self, "loop", None)
        self.machine = getattr(self, "machine", None)
        self.assets = getattr(self, "assets", None)
        self.joysticks = list(getattr(self, "joysticks", ()))
        self.rng = random.Random()
        self.world = World()
        self.director = WaveDirector(self.two_player)
        self.wave_n = 1
        self.players = []
        player_count = 2 if self.two_player else 1
        for index in range(player_count):
            x, y = self.world.spawn_pads[index]
            player = Player(index + 1, x, y)
            player.score = 0
            player.lives = config.START_LIVES
            self._configure_lance(player, f"p{player.pid}_stand")
            self.players.append(player)
        self.enemies = []
        self.eggs = []
        self.pteros = []
        self.riderless = []
        self.troll = Troll()
        self.ptero_director = None
        self.egg_chains = {player.pid: EggChain() for player in self.players}
        self.deaths_this_wave = {player.pid: 0 for player in self.players}
        self.jousted_teammate = False
        self.wave_type = "normal"
        self.wave_active_seconds = 0.0
        self.wave_banner_s = 0.0
        self.animation_s = 0.0
        self.paused = False
        self._held = set()
        self._flap_requests = set()
        self._start_wave()

    def _start_wave(self):
        plan = self.director.start(self.wave_n, self.world, self.rng)
        self.wave_type = plan["wave_type"]
        self.wave_active_seconds = 0.0
        self.wave_banner_s = 2.0
        self.deaths_this_wave = {player.pid: 0 for player in self.players}
        self.jousted_teammate = False
        for chain in self.egg_chains.values():
            chain.reset()
        self.enemies = []
        for index, tier in enumerate(plan["buzzards"]):
            x, y = self.world.spawn_pads[index % len(self.world.spawn_pads)]
            self.enemies.append(self._new_enemy(tier, x, y))
        self.eggs = [Egg(1, x, y, 0.0, 0.0) for x, y in plan["eggs"]]
        self.pteros = []
        self.ptero_director = PteroDirector(self.wave_type == "ptero")
        self.troll = Troll()
        audio.play("fanfare")

    def handle_event(self, event):
        if event.type == pygame.QUIT:
            if self.loop is not None:
                self.loop.running = False
            return
        if event.type in (pygame.KEYDOWN, pygame.KEYUP):
            pressed = event.type == pygame.KEYDOWN
            if pressed:
                self._held.add(event.key)
            else:
                self._held.discard(event.key)
            if not pressed:
                return
            if event.key == pygame.K_SPACE:
                self._flap_requests.add(1)
            elif event.key == pygame.K_LSHIFT and self.two_player:
                self._flap_requests.add(2)
            elif event.key == pygame.K_p:
                self.paused = not self.paused
            elif event.key == pygame.K_ESCAPE and self.machine is not None:
                self.machine.switch(AttractState(self.loop, self.machine, self.assets))
            elif event.key == pygame.K_F11 and self.loop is not None:
                self.loop.toggle_fullscreen()
        elif event.type == pygame.JOYBUTTONDOWN and event.button == 0:
            pid = event.joy + 1
            if pid <= len(self.players):
                self._flap_requests.add(pid)

    def _apply_input(self):
        directions = {
            1: int(pygame.K_RIGHT in self._held) - int(pygame.K_LEFT in self._held),
            2: int(pygame.K_d in self._held) - int(pygame.K_a in self._held),
        }
        for index, joystick in enumerate(self.joysticks[:len(self.players)]):
            x = joystick.get_axis(0) if joystick.get_numaxes() else 0.0
            if abs(x) > config.PAD_DEADZONE:
                directions[index + 1] = 1 if x > 0 else -1
            elif joystick.get_numhats():
                hat_x = joystick.get_hat(0)[0]
                if hat_x:
                    directions[index + 1] = hat_x
        for player in self.players:
            if player.alive:
                player.set_dir(directions[player.pid])
        for pid in self._flap_requests:
            player = self._player(pid)
            if player is None or not player.alive:
                continue
            if self.troll.victim is player:
                self.troll.fight()
            else:
                player.flap()
            audio.play("flap")
        self._flap_requests.clear()

    def update(self, dt):
        if self.paused:
            return

        self.world.update_erosion(dt)
        # Required assembly order: input, players, enemies, eggs, troll,
        # pteros, collisions, wave clear, HUD timers.
        self._apply_input()
        for player in self.players:
            if player.alive:
                player.update(dt, self.world)
        for enemy in self.enemies:
            enemy.update(dt, self.world, self.players)
        for egg in self.eggs:
            spawn = egg.update(dt, self.world)
            if spawn:
                _, tier, x, y = spawn
                self.enemies.append(self._new_enemy(tier, x, y))
                audio.play("hatch")

        if self.wave_n >= config.TROLL_ACTIVE_FROM_WAVE:
            mounts = [
                mount for mount in (*self.players, *self.enemies)
                if mount.alive and not mount.invulnerable
            ]
            self.troll.update(dt, self.world, mounts)
            if self.troll.consumed is not None:
                victim = self.troll.consumed
                if isinstance(victim, Player):
                    self._kill_player(victim)
                else:
                    victim.alive = False
                self.troll.consumed = None

        living_players = [player for player in self.players if player.alive]
        self.pteros.extend(
            self.ptero_director.update(
                dt,
                self.wave_active_seconds,
                sum(enemy.alive for enemy in self.enemies),
            )
        )
        for ptero in self.pteros:
            target = min(
                living_players,
                key=lambda player: (player.x - ptero.x) ** 2 + (player.y - ptero.y) ** 2,
                default=None,
            )
            ptero.update(dt, self.world, target)
        for buzzard in self.riderless:
            buzzard.update(dt)

        self._resolve_collisions()
        self._prune()
        if self.director.is_clear(self.enemies, self.eggs):
            self._finish_wave()

        self.wave_active_seconds += dt
        self.animation_s += dt
        self.wave_banner_s = max(0.0, self.wave_banner_s - dt)
        if self.is_game_over() and self.machine is not None:
            self.machine.switch(GameOverState(self.loop, self.machine, self.assets, self.players))

    def _resolve_collisions(self):
        for player in self.players:
            if not player.alive or player.invulnerable:
                continue
            for enemy in self.enemies:
                if not enemy.alive or enemy.invulnerable or not combat.collide(player, enemy):
                    continue
                outcome = (
                    combat.resolve(
                        player.y + config.JOUST_RIDER_Y,
                        enemy.y + config.JOUST_RIDER_Y,
                    )
                    if player.mounted and enemy.mounted
                    else combat.resolve_pair(player, enemy)
                )
                if outcome == "a":
                    self._defeat_enemy(player.pid, enemy)
                elif outcome == "b":
                    self._kill_player(player)
                    break
                elif outcome == "tie":
                    combat.bounce(player, enemy)
            if not player.alive or player.invulnerable:
                continue
            for egg in self.eggs:
                if egg.collectible and combat.collide(player, egg):
                    egg.collect()
                    value = self.egg_chains[player.pid].value()
                    self.award(player.pid, value)
                    audio.play(f"collect_{min(3, config.EGG_CHAIN.index(value))}")
            for ptero in self.pteros:
                if not ptero.alive:
                    continue
                lance_x = player.x + player.facing * player.lance_offset_x
                if ptero.check_lance(lance_x, player.lance_y) == "kill":
                    self.award(player.pid, config.SCORE_PTERO)
                    audio.play("screech")
                elif combat.collide(player, ptero):
                    self._kill_player(player)
                    break

        if len(self.players) == 2:
            first, second = self.players
            if (
                first.alive and second.alive
                and not first.invulnerable and not second.invulnerable
                and combat.collide(first, second)
            ):
                outcome = (
                    combat.resolve(
                        first.y + config.JOUST_RIDER_Y,
                        second.y + config.JOUST_RIDER_Y,
                    )
                    if first.mounted and second.mounted
                    else combat.resolve_pair(first, second)
                )
                if outcome == "a":
                    self.apply_pvp_joust(first, second)
                elif outcome == "b":
                    self.apply_pvp_joust(second, first)
                elif outcome == "tie":
                    combat.bounce(first, second)

        for player in self.players:
            if (
                player.alive
                and player is not self.troll.victim
                and player.y >= self.world.LAVA_Y
            ):
                self._kill_player(player)
        for enemy in self.enemies:
            if (
                enemy.alive
                and enemy is not self.troll.victim
                and enemy.y >= self.world.LAVA_Y
            ):
                enemy.alive = False

    def _defeat_enemy(self, pid, enemy):
        if not enemy.alive:
            return
        enemy.alive = False
        self.award(pid, enemy.points)
        self.eggs.append(Egg(enemy.tier, enemy.x, enemy.y, enemy.vx * 0.35, enemy.vy))
        direction = enemy.facing or 1
        self.riderless.append(
            RiderlessBuzzard(enemy.x, enemy.y, direction * 100.0, -45.0, direction)
        )
        audio.play("joust")
        audio.play("egg_plop")

    def _kill_player(self, player):
        if not player.alive:
            return
        if self.troll.victim is player:
            self.troll.release()
        player.lives -= 1
        self.deaths_this_wave[player.pid] += 1
        audio.play("death")
        if player.lives <= 0:
            player.alive = False
            return
        score, lives, pid = player.score, player.lives, player.pid
        x, y = self.world.spawn_pads[(pid - 1) % len(self.world.spawn_pads)]
        Player.__init__(player, pid, x, y)
        player.score = score
        player.lives = lives
        self._configure_lance(player, f"p{pid}_stand")
        audio.play("shimmer")

    def apply_pvp_joust(self, winner, loser):
        self.jousted_teammate = True
        self.award(winner.pid, config.SCORE_PVP)
        self._kill_player(loser)

    def award(self, pid, points):
        player = self._player(pid)
        if player is None:
            raise ValueError(f"unknown player {pid}")
        old_score = player.score
        player.score += points
        if not player.alive and player.lives <= 0:
            return
        old_crossings = old_score // config.EXTRA_LIFE_EVERY
        new_crossings = player.score // config.EXTRA_LIFE_EVERY
        player.lives += new_crossings - old_crossings

    def wave_end_bonuses(self):
        return self.director.end_bonuses(
            self.wave_n,
            self.deaths_this_wave,
            self.jousted_teammate,
        )

    def _finish_wave(self):
        for pid, points in self.wave_end_bonuses().items():
            player = self._player(pid)
            if points and player is not None and player.alive:
                self.award(pid, points)
                audio.play("bonus")
        for ptero in self.pteros:
            ptero.alive = False
        self.pteros.clear()
        self.wave_n += 1
        self._start_wave()

    def _prune(self):
        self.enemies = [enemy for enemy in self.enemies if enemy.alive]
        self.eggs = [egg for egg in self.eggs if egg.state != "dead"]
        self.pteros = [ptero for ptero in self.pteros if ptero.alive]
        self.riderless = [buzzard for buzzard in self.riderless if buzzard.alive]

    def is_game_over(self):
        return all(player.lives <= 0 for player in self.players)

    def _player(self, pid):
        return next((player for player in self.players if player.pid == pid), None)

    def _new_enemy(self, tier, x, y):
        enemy = Enemy(tier, x, y, self.rng)
        self._configure_lance(enemy, f"buzzard{tier}_flap_0")
        return enemy

    def _configure_lance(self, mount, frame_name):
        lance = self.assets.lance(frame_name) if self.assets is not None else None
        if lance is not None:
            mount.lance_offset_x, mount.lance_offset_y = lance
        elif not hasattr(mount, "lance_offset_x"):
            mount.lance_offset_x = config.MOUNT_W / 2 + 12

    def draw(self, surface):
        surface.fill((10, 10, 24))
        frame = int(self.animation_s * 8)
        for platform in self.world.platforms:
            if not platform.alive and not platform.burning:
                continue
            platform_name = (
                f"plat_burn_{int(platform.burn_timer * 8) % 3}"
                if platform.burning
                else "plat_tile"
            )
            x = platform.x + 16
            while x <= platform.x + platform.w:
                _blit(self.assets, surface, platform_name, x, platform.y + 8)
                x += 32
        lava_name = f"lava_{frame % 4}"
        for x in range(16, config.LOGICAL_W + 16, 32):
            _blit(self.assets, surface, lava_name, x, config.LOGICAL_H)

        for egg in self.eggs:
            if egg.state == "hatchling":
                name = f"hatchling_{frame % 2}"
            elif egg.state == "resting" and getattr(egg, "_state_age", 0) > config.EGG_HATCH_S * 0.75:
                name = "egg_crack"
            else:
                name = f"egg_{frame % 2}"
            _blit(self.assets, surface, name, egg.x, egg.y, wraps=egg.wraps)
        for buzzard in self.riderless:
            _blit(self.assets, surface, f"buzzard_free_{frame % 2}", buzzard.x, buzzard.y, buzzard.facing, buzzard.wraps)
        for enemy in self.enemies:
            name = f"buzzard{enemy.tier}_glide" if enemy.state == "dive" else f"buzzard{enemy.tier}_flap_{frame % 3}"
            _blit(self.assets, surface, name, enemy.x, enemy.y, enemy.facing, enemy.wraps)
            if enemy.invulnerable:
                _blit(self.assets, surface, f"shimmer_{frame % 3}", enemy.x, enemy.y, wraps=enemy.wraps)
        for player in self.players:
            if not player.alive:
                continue
            prefix = f"p{player.pid}"
            if player.state == "run":
                name = f"{prefix}_run_{frame % 4}"
            elif player.state == "flap":
                name = f"{prefix}_flap_{frame % 3}"
            elif player.state == "brake":
                name = f"{prefix}_brake"
            else:
                name = f"{prefix}_stand"
            _blit(self.assets, surface, name, player.x, player.y, player.facing, player.wraps)
            if player.invulnerable:
                _blit(self.assets, surface, f"shimmer_{frame % 3}", player.x, player.y, wraps=player.wraps)
        if self.wave_n >= config.TROLL_ACTIVE_FROM_WAVE and self.troll.state != "idle":
            name = f"troll_{self.troll.state}"
            victim = self.troll.victim
            _blit(self.assets, surface, name, victim.x if victim else config.LOGICAL_W / 2, self.world.LAVA_Y)
        for ptero in self.pteros:
            name = "ptero_mouth" if ptero.mouth_open else f"ptero_fly_{frame % 2}"
            _blit(self.assets, surface, name, ptero.x, ptero.y, ptero.facing, ptero.wraps)
        hud.draw(surface, self.assets, self)


class GameOverState(State):
    def __init__(self, loop, machine, assets, players):
        self.loop = loop
        self.machine = machine
        self.assets = assets
        self.score = max((player.score for player in players), default=0)

    def handle_event(self, event):
        if event.type == pygame.QUIT:
            self.loop.running = False
        elif event.type == pygame.KEYDOWN:
            if event.key == pygame.K_ESCAPE:
                self.machine.switch(AttractState(self.loop, self.machine, self.assets))
            elif event.key in (pygame.K_RETURN, pygame.K_SPACE):
                self._continue()
        elif event.type == pygame.JOYBUTTONDOWN and event.button in (6, 7):
            self._continue()

    def _continue(self):
        scores = persistence.load_scores()
        if len(scores) < 10 or self.score > scores[-1][1]:
            self.machine.switch(HighScoreEntryState(self.loop, self.machine, self.assets, self.score))
        else:
            self.machine.switch(AttractState(self.loop, self.machine, self.assets))

    def draw(self, surface):
        surface.fill((10, 10, 24))
        hud.draw_text(surface, self.assets, "GAME OVER", config.LOGICAL_W // 2, 140, centered=True)
        hud.draw_text(surface, self.assets, f"SCORE {self.score:06d}", config.LOGICAL_W // 2, 166, centered=True)
        hud.draw_text(surface, self.assets, "PRESS ENTER", config.LOGICAL_W // 2, 208, centered=True)


class HighScoreEntryState(State):
    LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"

    def __init__(self, loop, machine, assets, score):
        self.loop = loop
        self.machine = machine
        self.assets = assets
        self.score = score
        self.initials = [0, 0, 0]
        self.position = 0
        audio.play("hiscore")

    def handle_event(self, event):
        if event.type == pygame.QUIT:
            self.loop.running = False
            return
        if event.type == pygame.KEYDOWN:
            if event.key in (pygame.K_UP, pygame.K_w):
                self._move(0, 1)
            elif event.key in (pygame.K_DOWN, pygame.K_s):
                self._move(0, -1)
            elif event.key in (pygame.K_LEFT, pygame.K_a):
                self._move(-1, 0)
            elif event.key in (pygame.K_RIGHT, pygame.K_d):
                self._move(1, 0)
            elif event.key == pygame.K_ESCAPE:
                self.machine.switch(AttractState(self.loop, self.machine, self.assets))
            elif event.key in (pygame.K_RETURN, pygame.K_SPACE):
                self._submit()
        elif event.type == pygame.JOYHATMOTION and event.hat == 0:
            self._move(*event.value)
        elif event.type == pygame.JOYAXISMOTION and event.axis in (0, 1):
            if abs(event.value) > config.PAD_DEADZONE:
                if event.axis == 0:
                    self._move(1 if event.value > 0 else -1, 0)
                else:
                    self._move(0, -1 if event.value > 0 else 1)
        elif event.type == pygame.JOYBUTTONDOWN and event.button == 0:
            self._submit()

    def _move(self, dx, dy):
        if dy:
            self.initials[self.position] = (
                self.initials[self.position] + (1 if dy > 0 else -1)
            ) % len(self.LETTERS)
        if dx:
            self.position = max(0, min(2, self.position + (1 if dx > 0 else -1)))

    def _submit(self):
        name = "".join(self.LETTERS[index] for index in self.initials)
        rows = persistence.load_scores() + [(name, self.score)]
        rows.sort(key=lambda row: row[1], reverse=True)
        persistence.save_scores(rows[:10])
        self.machine.switch(AttractState(self.loop, self.machine, self.assets))

    def draw(self, surface):
        surface.fill((10, 10, 24))
        hud.draw_text(surface, self.assets, "NEW HIGH SCORE", config.LOGICAL_W // 2, 120, centered=True)
        name = "".join(self.LETTERS[index] for index in self.initials)
        hud.draw_text(surface, self.assets, name, config.LOGICAL_W // 2, 158, centered=True)
        hud.draw_text(surface, self.assets, "  " + "  " * self.position + ">", config.LOGICAL_W // 2, 174, centered=True)
        hud.draw_text(surface, self.assets, f"{self.score:06d}", config.LOGICAL_W // 2, 198, centered=True)


def _blit(assets, surface, name, x, y, facing=1, wraps=True):
    frame = assets.frame(name)
    anchor_x, anchor_y = assets.anchor(name)
    if facing < 0:
        frame = pygame.transform.flip(frame, True, False)
        anchor_x = frame.get_width() - anchor_x
    draw_x = round(x - anchor_x)
    draw_y = round(y - anchor_y)
    surface.blit(frame, (draw_x, draw_y))
    if wraps and draw_x < 0:
        surface.blit(frame, (draw_x + config.LOGICAL_W, draw_y))
    elif wraps and draw_x + frame.get_width() > config.LOGICAL_W:
        surface.blit(frame, (draw_x - config.LOGICAL_W, draw_y))
