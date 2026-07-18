from joust import config


def resolve(a_lance_y, b_lance_y, tie_px=config.TIE_PX):
    if abs(a_lance_y - b_lance_y) <= tie_px:
        return "tie"
    return "a" if a_lance_y < b_lance_y else "b"


def resolve_pair(a, b):
    if a.mounted and b.mounted:
        return resolve(a.lance_y, b.lance_y)
    if a.mounted:
        return "collect_a"
    if b.mounted:
        return "collect_b"
    return None


def bounce(a, b):
    a_speed = max(config.BOUNCE_MIN_VX, abs(a.vx))
    b_speed = max(config.BOUNCE_MIN_VX, abs(b.vx))
    dx = ((a.x - b.x + config.LOGICAL_W / 2) % config.LOGICAL_W) - config.LOGICAL_W / 2
    if dx <= 0:
        a.vx, b.vx = -a_speed, b_speed
    else:
        a.vx, b.vx = a_speed, -b_speed
    a.vy = b.vy = config.BOUNCE_NUDGE_VY


def collide(a, b):
    ax, ay, aw, ah = a.rect()
    bx, by, bw, bh = b.rect()
    ax_center = ax + aw / 2
    bx_center = bx + bw / 2
    dx = (
        (ax_center - bx_center + config.LOGICAL_W / 2) % config.LOGICAL_W
    ) - config.LOGICAL_W / 2
    return (
        abs(dx) < (aw + bw) / 2
        and ay < by + bh
        and ay + ah > by
    )
