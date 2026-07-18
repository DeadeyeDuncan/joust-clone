"""Wave scheduling, composition, and rewards."""

from joust import config


_SCHEDULE = (
    ("survival", config.SURVIVAL_WAVE_FIRST, config.SURVIVAL_WAVE_STEP),
    ("egg", config.EGG_WAVE_FIRST, config.EGG_WAVE_STEP),
    ("ptero", config.PTERO_WAVE_FIRST, config.PTERO_WAVE_STEP),
    ("team", config.TEAM_WAVE_FIRST, config.TEAM_WAVE_STEP),
)


def _slots(first, step):
    while True:
        yield first
        first += step


def wave_type(n, two_player):
    """Return the resolved type for wave *n*, including deferred collisions."""
    schedules = _SCHEDULE if two_player else _SCHEDULE[:-1]
    generators = {name: _slots(first, step) for name, first, step in schedules}
    next_slots = {name: next(generator) for name, generator in generators.items()}

    resolved = "normal"
    for wave in range(1, n + 1):
        candidates = [name for name, _, _ in schedules if next_slots[name] == wave]
        winner = candidates[0] if candidates else "normal"
        for name in candidates:
            next_slots[name] = next(generators[name])
        if wave == n:
            resolved = winner
    return resolved


def composition(n, rng):
    """Build the buzzard tier list for a non-egg wave."""
    if n == 1:
        return [1, 1, 1]
    if n == 2:
        return [1, 1, 1, 1]
    if n == 3:
        return [1, 1, 2, 2]

    count = rng.randint(config.WAVE_GROUP_MIN, config.WAVE_GROUP_MAX)
    bounder_count = rng.randint(0, count // 2)
    hunter_count = count - bounder_count
    lord_count = (
        rng.randint(config.LORD_COUNT_MIN, config.LORD_COUNT_MAX)
        if n >= config.LORD_WAVE_THRESHOLD
        else 0
    )
    return [1] * bounder_count + [2] * hunter_count + [3] * lord_count


def egg_wave_layout(world):
    """Place six eggs just above the first six living platforms."""
    living = [platform for platform in world.platforms if platform.alive]
    # Relies on erosion killing at most 4 of 10 platforms, leaving EGG_WAVE_COUNT alive.
    return [
        (platform.x + platform.w / 2, platform.y - config.EGG_WAVE_Y_OFFSET)
        for platform in living[:config.EGG_WAVE_COUNT]
    ]


class WaveDirector:
    def __init__(self, two_player):
        self.two_player = two_player

    def start(self, n, world, rng):
        world.apply_erosion(n)
        kind = wave_type(n, self.two_player)
        return {
            "wave_type": kind,
            "buzzards": [] if kind == "egg" else composition(n, rng),
            "eggs": egg_wave_layout(world) if kind == "egg" else [],
            "pteros": 1 if kind == "ptero" else 0,
        }

    def is_clear(self, enemies, eggs):
        return not enemies and not eggs

    def end_bonuses(self, n, deaths_by_player, jousted_teammate):
        bonuses = {player_id: 0 for player_id in deaths_by_player}
        kind = wave_type(n, self.two_player)
        if kind == "survival":
            return {
                player_id: config.BONUS_SURVIVAL if deaths == 0 else 0
                for player_id, deaths in deaths_by_player.items()
            }
        if kind == "team" and not jousted_teammate:
            return {player_id: config.BONUS_TEAM for player_id in deaths_by_player}
        return bonuses
