"""Run the actual Windows EXE against an isolated, ephemeral loopback server."""
import argparse
from pathlib import Path
import subprocess
import sys
import tempfile
import threading

from chat_server import ChatState, ChatHTTPServer


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if sys.platform != "win32":
        parser.error("This test runs the native Windows executable on Windows.")
    with tempfile.TemporaryDirectory(prefix="myday-chat-windows-") as directory:
        code = "local-test-code-only"
        state = ChatState(Path(directory) / "chat.sqlite3", code)
        server = ChatHTTPServer(("127.0.0.1", 0), state)
        thread = threading.Thread(target=server.serve_forever, kwargs={"poll_interval": .05}, daemon=True)
        thread.start()
        address = f"http://127.0.0.1:{server.server_port}/"
        try:
            for options in (("--chat-test", address, code),
                            ("--chat-smoke", address, code, str(args.output.resolve()))):
                result = subprocess.run([str(args.exe.resolve()), *options], capture_output=True,
                                        timeout=60, creationflags=subprocess.CREATE_NO_WINDOW)
                sys.stdout.buffer.write(result.stdout)
                sys.stderr.buffer.write(result.stderr)
                if result.returncode:
                    raise SystemExit(result.returncode)
        finally:
            server.shutdown()
            server.server_close()
            thread.join(2)


if __name__ == "__main__":
    main()
