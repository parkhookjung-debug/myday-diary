"""Run the same local MyDay web app on macOS and Windows (Python 3)."""
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import threading
import webbrowser


if __name__ == "__main__":
    handler = partial(SimpleHTTPRequestHandler, directory=str(Path(__file__).resolve().parent))
    try:
        server = ThreadingHTTPServer(("127.0.0.1", 8765), handler)
    except OSError as error:
        raise SystemExit(f"Cannot start MyDay at port 8765. Close the previous server and retry.\n{error}")
    url = "http://127.0.0.1:8765"
    print(f"MyDay: {url}\nKeep this terminal open. Press Ctrl+C to stop.")
    threading.Timer(0.5, lambda: webbrowser.open(url)).start()
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
