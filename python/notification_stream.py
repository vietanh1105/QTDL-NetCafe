"""Nhận PostgreSQL NOTIFY với cả psycopg 3 và psycopg2."""

import select


def iter_notifications(connection):
    notifications = connection.notifies
    if callable(notifications):
        # psycopg 3 cung cấp generator; không có Connection.poll().
        yield from notifications()
        return

    # psycopg2 giữ notification trong danh sách sau khi poll socket.
    while True:
        if not select.select([connection], [], [], 5)[0]:
            continue
        connection.poll()
        while connection.notifies:
            yield connection.notifies.pop(0)
