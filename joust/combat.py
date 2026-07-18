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
    if a.x <= b.x:
        a.vx, b.vx = -a_speed, b_speed
    else:
        a.vx, b.vx = a_speed, -b_speed
    a.vy = b.vy = config.BOUNCE_NUDGE_VY


def collide(a, b):
    ax, ay, aw, ah = a.rect()
    bx, by, bw, bh = b.rect()
    return (
        ax < bx + bw
        and ax + aw > bx
        and ay < by + bh
        and ay + ah > by
    )
