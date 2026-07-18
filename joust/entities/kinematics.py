from joust import config


def step_airborne(
    pos,
    vel,
    thrust_dir,
    dt,
    thrust_ax=config.THRUST_AX,
    max_vx=config.MAX_VX,
):
    """Advance one airborne fixed step and return ``(pos, vel)`` tuples."""
    x, y = pos
    vx, vy = vel

    if thrust_dir:
        vx += thrust_dir * thrust_ax * dt
    else:
        vx *= config.DRAG_X
    vx = max(-max_vx, min(max_vx, vx))
    vy += config.GRAVITY * dt

    return (x + vx * dt, y + vy * dt), (vx, vy)
