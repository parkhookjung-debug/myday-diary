"""Package numbered PNG frames from the native renderer into a preview GIF."""
import argparse
from pathlib import Path
from PIL import Image


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("frames", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--duration", type=int, default=50, help="Milliseconds per frame")
    args = parser.parse_args()
    if args.duration <= 0:
        parser.error("duration must be positive")
    paths = sorted((p for p in args.frames.glob("*.png") if p.stem.isdecimal()),
                   key=lambda p: int(p.stem))
    if len(paths) < 2 or [int(p.stem) for p in paths] != list(range(len(paths))):
        parser.error("expected at least two consecutive numbered PNG frames starting at zero")
    frames = []
    try:
        for path in paths:
            with Image.open(path) as source:
                frames.append(source.convert("RGB"))
        if any(frame.size != frames[0].size for frame in frames):
            parser.error("frame dimensions must match")
        args.output.parent.mkdir(parents=True, exist_ok=True)
        frames[0].save(args.output, save_all=True, append_images=frames[1:],
                       duration=args.duration, loop=0, optimize=False)
        with Image.open(args.output) as result:
            if result.n_frames != len(frames):
                raise RuntimeError("output lost animation frames")
            print(f"Saved {result.n_frames} frames at {result.size}: {args.output}")
    finally:
        for frame in frames:
            frame.close()


if __name__ == "__main__":
    main()
