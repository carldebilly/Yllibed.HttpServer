/*
  E2E (End-to-End) = tests that validate the whole system from the
  user/consumer point of view: we start a real server app, send a real
  HTTP request, and assert the response.

  Here, this Node script acts as the E2E runner:
  - it starts the .NET E2EApp (dotnet run) that hosts our HTTP server;
  - the app prints a machine-readable line PORT=<number> followed by READY;
  - the script waits for READY and captures the port;
  - it performs a GET /ping to http://127.0.0.1:<port>/ping and verifies status/content/body;
  - it connects to an SSE endpoint /sse/js and validates the first event (id, event, data);
  - it then terminates the spawned process cleanly.

  These tests differ from unit/integration tests because they exercise the
  entire chain: client → network → server → handler → response.
*/
// Simple Node E2E script to validate the server works end-to-end
const { spawn } = require('child_process');
const http = require('http');
const path = require('path');

function waitForReady(proc, timeoutMs = 10000) {
  return new Promise((resolve, reject) => {
    let ready = false;
    let port = null;
    const timer = setTimeout(() => {
      if (!ready) reject(new Error('Timeout waiting for READY'));
    }, timeoutMs);

    proc.stdout.on('data', (data) => {
      const txt = data.toString();
      process.stdout.write(txt);
      // In case multiple lines are delivered at once, split and scan each
      for (const line of txt.split(/\r?\n/)) {
        if (!line) continue;
        if (line.startsWith('PORT=')) {
          const v = parseInt(line.substring('PORT='.length), 10);
          if (!Number.isNaN(v) && v > 0) port = v;
        }
        if (line.includes('READY')) {
          ready = true;
          clearTimeout(timer);
          if (!port) return reject(new Error('READY received but no PORT=<n> captured'));
          return resolve(port);
        }
      }
    });
    proc.stderr.on('data', (data) => process.stderr.write(data.toString()));
    proc.on('exit', (code) => {
      if (!ready) reject(new Error(`E2E app exited before ready. Code: ${code}`));
    });
  });
}

function httpGet(url) {
  return new Promise((resolve, reject) => {
    const req = http.get(url, (res) => {
      let body = '';
      res.setEncoding('utf8');
      res.on('data', (chunk) => (body += chunk));
      res.on('end', () => resolve({ status: res.statusCode, body, headers: res.headers }));
    });
    req.on('error', reject);
  });
}

(async () => {
  // Compute project path relative to this script location
  const scriptDir = __dirname;
  const testProjectRoot = path.resolve(scriptDir, '..');
  const e2eAppProj = path.resolve(testProjectRoot, 'E2EApp');

  const app = spawn('dotnet', ['run', '--project', e2eAppProj, '-c', 'Release'], { cwd: testProjectRoot, env: process.env });
  try {
    const port = await waitForReady(app);

    const res = await httpGet(`http://127.0.0.1:${port}/ping`);
    if (res.status !== 200) throw new Error(`Unexpected status: ${res.status}`);
    if ((res.headers['content-type']||'').split(';')[0] !== 'text/plain') throw new Error(`Unexpected content-type: ${res.headers['content-type']}`);
    if (res.body !== 'pong') throw new Error(`Unexpected body: ${res.body}`);

    // SSE: connect and validate first event
    await new Promise((resolve, reject) => {
      const req = http.get(`http://127.0.0.1:${port}/sse/js`, (res2) => {
        const ct = (res2.headers['content-type']||'').split(';')[0];
        if (res2.statusCode !== 200) return reject(new Error(`SSE unexpected status: ${res2.statusCode}`));
        if (ct !== 'text/event-stream') return reject(new Error(`SSE unexpected content-type: ${res2.headers['content-type']}`));
        res2.setEncoding('utf8');
        let buffer = '';
        let got = false;
        const timer = setTimeout(() => {
          if (!got) reject(new Error('Timeout waiting for first SSE event'));
        }, 5000);
        res2.on('data', (chunk) => {
          buffer += chunk;
          // Parse by lines, server may send multiple events or heartbeats
          let idx;
          while ((idx = buffer.indexOf('\n')) >= 0) {
            const line = buffer.slice(0, idx).replace(/\r$/, '');
            buffer = buffer.slice(idx + 1);
            if (!line) {
              // blank line terminates an event; keep collecting in outer scope if needed
              continue;
            }
            // Collect fields
            // We expect at least: id:e2e-1, event:obj, data:{"A":1,"B":"x"}
            if (line.startsWith('id:')) {
              const id = line.substring(3).trim();
              if (id !== 'e2e-1') return reject(new Error(`SSE unexpected id: ${id}`));
            } else if (line.startsWith('event:')) {
              const ev = line.substring(6).trim();
              if (ev !== 'obj') return reject(new Error(`SSE unexpected event: ${ev}`));
            } else if (line.startsWith('data:')) {
              const data = line.substring(5).trim();
              if (data !== '{"A":1,"B":"x"}') return reject(new Error(`SSE unexpected data: ${data}`));
              got = true;
              clearTimeout(timer);
              resolve();
              req.destroy();
              return;
            }
          }
        });
        res2.on('error', reject);
      });
      req.on('error', reject);
    });

    console.log('E2E OK');
  } catch (e) {
    console.error('E2E FAILED:', e);
    process.exitCode = 1;
  } finally {
    if (process.platform === 'win32') {
      spawn('taskkill', ['/PID', String(app.pid), '/T', '/F']);
    } else {
      app.kill('SIGKILL');
    }
  }
})();
