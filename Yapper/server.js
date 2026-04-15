// server.js  –  Yapper backend (Node.js, zero dependencies)
// Usage: node server.js

const http = require("http");
const fs   = require("fs");
const path = require("path");

// -- In-memory store ----------------------------------------------------------
let tasks  = [];
let nextId = 1;

const getLatest = () => {
  const pool = tasks.filter(t => t.timeRemainingMins != null && !t.done);
  return pool.sort((a, b) => b.timeRemainingMins - a.timeRemainingMins)[0] || null;
};

// -- Server -------------------------------------------------------------------
const server = http.createServer((req, res) => {
  const cors = () => {
    res.setHeader("Access-Control-Allow-Origin", "*");
    res.setHeader("Access-Control-Allow-Methods", "GET,POST,PATCH,DELETE,OPTIONS");
    res.setHeader("Access-Control-Allow-Headers", "Content-Type");
  };
  const json = (code, payload) => {
    cors();
    res.writeHead(code, { "Content-Type": "application/json" });
    res.end(JSON.stringify(payload, null, 2));
  };
  const body = () => new Promise(resolve => {
    let d = "";
    req.on("data", c => d += c);
    req.on("end", () => resolve(d ? JSON.parse(d) : {}));
  });

  // CORS preflight
  if (req.method === "OPTIONS") { cors(); res.writeHead(204); res.end(); return; }

  const url = new URL(req.url, "http://localhost");

  // Serve frontend
  if (req.method === "GET" && (url.pathname === "/" || url.pathname === "/index.html")) {
    const html = fs.readFileSync(path.join(__dirname, "yapper-tasks.html"));
    res.writeHead(200, { "Content-Type": "text/html; charset=utf-8" });
    res.end(html);
    return;
  }

  // GET /api/tasks
  if (req.method === "GET" && url.pathname === "/api/tasks") {
    return json(200, { count: tasks.length, latestDueTask: getLatest(), tasks, timestamp: new Date().toISOString() });
  }

  // GET /api/tasks/:id
  const idMatch = url.pathname.match(/^\/api\/tasks\/(\d+)$/);
  if (req.method === "GET" && idMatch) {
    const t = tasks.find(t => t.id === +idMatch[1]);
    return t ? json(200, t) : json(404, { error: "Not found" });
  }

  // POST /api/tasks
  if (req.method === "POST" && url.pathname === "/api/tasks") {
    return body().then(data => {
      if (!data.text?.trim()) return json(400, { error: "text is required" });
      const task = { id: nextId++, text: data.text.trim(), priority: data.priority || "medium",
        timeRemainingMins: data.timeRemainingMins ?? null, done: false, createdAt: new Date().toISOString() };
      tasks.unshift(task);
      json(201, task);
    });
  }

  // PATCH /api/tasks/:id
  if (req.method === "PATCH" && idMatch) {
    return body().then(data => {
      const task = tasks.find(t => t.id === +idMatch[1]);
      if (!task) return json(404, { error: "Not found" });
      ["text","priority","timeRemainingMins","done"].forEach(f => { if (f in data) task[f] = data[f]; });
      json(200, task);
    });
  }

  // DELETE /api/tasks/:id
  if (req.method === "DELETE" && idMatch) {
    const tid = +idMatch[1];
    const before = tasks.length;
    tasks = tasks.filter(t => t.id !== tid);
    return tasks.length < before ? json(200, { deleted: tid }) : json(404, { error: "Not found" });
  }

  json(404, { error: "Not found" });
});

const PORT = process.env.PORT || 3001;
server.listen(PORT, () => {
  console.log(`
Yapper is running!
  Frontend  ->  http://localhost:${PORT}/
  API       ->  http://localhost:${PORT}/api/tasks

Endpoints:
  GET    /api/tasks        list all tasks + latestDueTask
  POST   /api/tasks        create a task
  GET    /api/tasks/:id    get one task
  PATCH  /api/tasks/:id    update a task
  DELETE /api/tasks/:id    delete a task

Press Ctrl+C to stop.
`);
});
