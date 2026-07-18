class State:
    def handle_event(self, e):
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
