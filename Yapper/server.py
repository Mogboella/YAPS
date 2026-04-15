#!/usr/bin/env python3
"""
Yapper - single-file server
  Serves the frontend HTML  ->  GET /
  REST API (in-memory)      ->  /api/tasks
"""

import http.server
import json
import os
import re
from datetime import datetime, timezone

# -- In-memory store ----------------------------------------------------------
tasks = []
next_id = [1]


def get_latest_task():
    with_time = [t for t in tasks if t.get("timeRemainingMins") is not None and not t["done"]]
    return max(with_time, key=lambda t: t["timeRemainingMins"], default=None)


# -- Request handler ----------------------------------------------------------
class Handler(http.server.BaseHTTPRequestHandler):

    def _cors(self):
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "GET, POST, PATCH, DELETE, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "Content-Type")

    def _json(self, code, payload):
        body = json.dumps(payload, indent=2).encode()
        self.send_response(code)
        self._cors()
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def _read_body(self):
        length = int(self.headers.get("Content-Length", 0))
        return json.loads(self.rfile.read(length)) if length else {}

    def log_message(self, fmt, *args):
        print(f"  {self.command} {self.path}  ->  {args[1]}")

    def do_OPTIONS(self):
        self.send_response(204)
        self._cors()
        self.end_headers()

    def do_GET(self):
        if self.path in ("/", "/index.html"):
            html_path = os.path.join(os.path.dirname(__file__), "yapper-tasks.html")
            with open(html_path, "rb") as f:
                body = f.read()
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return

        if self.path == "/api/tasks":
            self._json(200, {
                "count": len(tasks),
                "latestDueTask": get_latest_task(),
                "tasks": tasks,
                "timestamp": datetime.now(timezone.utc).isoformat(),
            })
            return

        m = re.match(r"^/api/tasks/(\d+)$", self.path)
        if m:
            tid = int(m.group(1))
            task = next((t for t in tasks if t["id"] == tid), None)
            self._json(200, task) if task else self._json(404, {"error": f"Task {tid} not found"})
            return

        self._json(404, {"error": "Not found"})

    def do_POST(self):
        if self.path != "/api/tasks":
            self._json(404, {"error": "Not found"}); return
        data = self._read_body()
        text = str(data.get("text", "")).strip()
        if not text:
            self._json(400, {"error": "text is required"}); return
        task = {
            "id": next_id[0],
            "text": text,
            "priority": data.get("priority", "medium"),
            "timeRemainingMins": data.get("timeRemainingMins"),
            "done": False,
            "createdAt": datetime.now(timezone.utc).isoformat(),
        }
        next_id[0] += 1
        tasks.append(task)
        self._json(201, task)

    def do_PATCH(self):
        m = re.match(r"^/api/tasks/(\d+)$", self.path)
        if not m:
            self._json(404, {"error": "Not found"}); return
        tid = int(m.group(1))
        task = next((t for t in tasks if t["id"] == tid), None)
        if not task:
            self._json(404, {"error": f"Task {tid} not found"}); return
        data = self._read_body()
        for field in ("text", "priority", "timeRemainingMins", "done"):
            if field in data:
                task[field] = data[field]
        self._json(200, task)

    def do_DELETE(self):
        m = re.match(r"^/api/tasks/(\d+)$", self.path)
        if not m:
            self._json(404, {"error": "Not found"}); return
        tid = int(m.group(1))
        before = len(tasks)
        tasks[:] = [t for t in tasks if t["id"] != tid]
        self._json(200, {"deleted": tid}) if len(tasks) < before else self._json(404, {"error": f"Task {tid} not found"})


if __name__ == "__main__":
    PORT = int(os.environ.get("PORT", 3001))
    server = http.server.HTTPServer(("", PORT), Handler)
    print(f"""
Yapper is running!
  Frontend  ->  http://localhost:{PORT}/
  API       ->  http://localhost:{PORT}/api/tasks

Endpoints:
  GET    /api/tasks        list all tasks + latestDueTask
  POST   /api/tasks        create a task
  GET    /api/tasks/:id    get one task
  PATCH  /api/tasks/:id    update (text / priority / timeRemainingMins / done)
  DELETE /api/tasks/:id    delete a task

Press Ctrl+C to stop.
""")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("Stopped.")
